using System.Net;
using System.Net.Http.Headers;
using AoCEditor.Core.Gateway;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shared;
using Shared.Contracts;

namespace AoCEditor.Infrastructure.Gateway;

/// <summary>The only component allowed to talk to adventofcode.com.</summary>
public sealed class AocGateway : IAocGateway, IDisposable
{
    private readonly HttpClient _http;
    private readonly ISessionTokenProvider _tokens;
    private readonly IResponseCache _cache;
    private readonly GatewayOptions _options;
    private readonly TimeProvider _time;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<RequestKind, long> _lastRequest = [];
    private readonly ISessionEvents? _events;
    private readonly ILogger _log;
    private readonly IRequestLog? _requests;
    private long? _lastPrompt;

    public AocGateway(ISessionTokenProvider tokens, IResponseCache cache, GatewayOptions? options = null, TimeProvider? time = null, ISessionEvents? events = null, ILogger? logger = null, IRequestLog? requests = null)
        : this(new SocketsHttpHandler { AllowAutoRedirect = false, UseCookies = false }, tokens, cache, options, time, events, logger, requests)
    {
    }

    public AocGateway(HttpMessageHandler handler, ISessionTokenProvider tokens, IResponseCache cache, GatewayOptions? options = null, TimeProvider? time = null, ISessionEvents? events = null, ILogger? logger = null, IRequestLog? requests = null)
    {
        _log = logger ?? NullLogger.Instance;
        _requests = requests;
        _events = events;
        _options = options ?? new GatewayOptions();
        _tokens = tokens;
        _cache = cache;
        _time = time ?? TimeProvider.System;
        _http = new HttpClient(handler) { BaseAddress = _options.BaseAddress, Timeout = _options.Timeout };
    }

    public async Task<GatewayResult> GetAsync(string path, RequestKind kind, FetchPolicy policy, CancellationToken cancellationToken)
    {
        if (policy == FetchPolicy.CacheFirst && await _cache.TryGetAsync(path, cancellationToken) is { } cached)
        {
            _log.CacheHit(path);
            return new GatewayResult.Ok(cached);
        }

        var result = await SendAsync(HttpMethod.Get, kind, path, null, cancellationToken);
        if (result is GatewayResult.Ok ok)
        {
            await _cache.SetAsync(path, ok.Content, cancellationToken);
        }

        return result;
    }

    public Task<GatewayResult> PostFormAsync(string path, IReadOnlyDictionary<string, string> form, CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Post, RequestKind.Interactive, path, form, cancellationToken);

    public Task<GatewayResult> CheckTokenAsync(string token, CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Get, RequestKind.Interactive, "/settings", null, cancellationToken, token);

    public void Dispose()
    {
        _http.Dispose();
        _gate.Dispose();
    }

    private async Task<GatewayResult> SendAsync(HttpMethod method, RequestKind kind, string path, IReadOnlyDictionary<string, string>? form, CancellationToken cancellationToken, string? candidateToken = null)
    {
        var token = candidateToken ?? await _tokens.GetAsync(cancellationToken);
        if (string.IsNullOrEmpty(token))
        {
            _log.NoToken(method.Method, path);
            return SignInRequired();
        }

        var waitStart = _time.GetTimestamp();
        await AcquireAsync(kind, cancellationToken);
        var waited = _time.GetElapsedTime(waitStart);
        var sendStart = _time.GetTimestamp();
        int? status = null;
        string? failure = null;
        try
        {
            using var request = new HttpRequestMessage(method, path.TrimStart('/'));
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("AoCEditor", AppInfo.Version));
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue($"(+{_options.RepositoryUrl}; maintainer {_options.Maintainer})"));
            request.Headers.TryAddWithoutValidation("Cookie", $"session={token}");
            if (form is not null)
            {
                request.Content = new FormUrlEncodedContent(form);
            }

            try
            {
                using var response = await _http.SendAsync(request, cancellationToken);
                status = (int)response.StatusCode;
                var result = await MapAsync(response, cancellationToken);
                return result is GatewayResult.NotSignedIn && candidateToken is null ? SignInRequired() : result;
            }
            catch (HttpRequestException ex)
            {
                failure = ex.HttpRequestError.ToString();
                return new GatewayResult.NetworkError(failure);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                failure = "Timed out";
                return new GatewayResult.NetworkError(failure);
            }
            finally
            {
                var took = _time.GetElapsedTime(sendStart);
                _requests?.Record(new GatewayRequestEntry(_time.GetUtcNow(), method.Method, path, status, took, waited));
                if (failure is not null)
                {
                    _log.RequestFailed(method.Method, path, failure, (long)took.TotalMilliseconds);
                }
                else
                {
                    _log.RequestDone(method.Method, path, status, (long)took.TotalMilliseconds, (long)waited.TotalMilliseconds);
                }

                lock (_lastRequest)
                {
                    _lastRequest[kind] = _time.GetTimestamp();
                }
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    // One prompt per window so a burst of rejected requests shows one sign-in invitation.
    private GatewayResult.NotSignedIn SignInRequired()
    {
        var raise = false;
        lock (_lastRequest)
        {
            if (_lastPrompt is not { } last || _time.GetElapsedTime(last) >= _options.SignInPromptInterval)
            {
                _lastPrompt = _time.GetTimestamp();
                raise = true;
            }
        }

        if (raise)
        {
            _log.SignInPrompted();
            _events?.NotifySignInRequired();
        }

        return new GatewayResult.NotSignedIn();
    }

    public DateTimeOffset NextAllowedAt(RequestKind kind) => _time.GetUtcNow() + Remaining(kind);

    // Waits outside the gate so a long statistics wait never blocks an interactive request.
    private async Task AcquireAsync(RequestKind kind, CancellationToken cancellationToken)
    {
        while (true)
        {
            await _gate.WaitAsync(cancellationToken);
            var remaining = Remaining(kind);
            if (remaining <= TimeSpan.Zero)
            {
                return;
            }

            _gate.Release();
            await Task.Delay(remaining, _time, cancellationToken);
        }
    }

    private TimeSpan Remaining(RequestKind kind)
    {
        long last;
        lock (_lastRequest)
        {
            if (!_lastRequest.TryGetValue(kind, out last))
            {
                return TimeSpan.Zero;
            }
        }

        var interval = kind == RequestKind.Statistics
            ? Max(_options.StatisticsInterval, GatewayOptions.StatisticsFloor)
            : Max(_options.InteractiveInterval, GatewayOptions.InteractiveFloor);
        return interval - _time.GetElapsedTime(last);
    }

    private static TimeSpan Max(TimeSpan a, TimeSpan b) => a > b ? a : b;

    private static async Task<GatewayResult> MapAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var status = response.StatusCode;
        if (status == HttpStatusCode.OK)
        {
            return new GatewayResult.Ok(await response.Content.ReadAsStringAsync(cancellationToken));
        }

        return (int)status switch
        {
            >= 300 and < 400 or 400 or 401 or 403 => new GatewayResult.NotSignedIn(),
            429 => new GatewayResult.RateLimited(),
            404 => new GatewayResult.NotFound(),
            _ => new GatewayResult.NetworkError($"HTTP {(int)status}"),
        };
    }
}
