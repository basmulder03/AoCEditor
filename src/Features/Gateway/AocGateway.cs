using System.Net;
using System.Net.Http.Headers;
using Shared;
using Shared.Contracts;

namespace Gateway;

/// <summary>The only component allowed to talk to adventofcode.com.</summary>
public sealed class AocGateway : IDisposable
{
    private readonly HttpClient _http;
    private readonly ISessionTokenProvider _tokens;
    private readonly IResponseCache _cache;
    private readonly GatewayOptions _options;
    private readonly TimeProvider _time;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private long? _lastRequest;

    public AocGateway(ISessionTokenProvider tokens, IResponseCache cache, GatewayOptions? options = null, TimeProvider? time = null)
        : this(new SocketsHttpHandler { AllowAutoRedirect = false, UseCookies = false }, tokens, cache, options, time)
    {
    }

    public AocGateway(HttpMessageHandler handler, ISessionTokenProvider tokens, IResponseCache cache, GatewayOptions? options = null, TimeProvider? time = null)
    {
        _options = options ?? new GatewayOptions();
        _tokens = tokens;
        _cache = cache;
        _time = time ?? TimeProvider.System;
        _http = new HttpClient(handler) { BaseAddress = _options.BaseAddress, Timeout = _options.Timeout };
    }

    public async Task<GatewayResult> GetAsync(string path, FetchPolicy policy, CancellationToken cancellationToken)
    {
        if (policy == FetchPolicy.CacheFirst && await _cache.TryGetAsync(path, cancellationToken) is { } cached)
        {
            return new GatewayResult.Ok(cached);
        }

        var result = await SendAsync(HttpMethod.Get, path, null, cancellationToken);
        if (result is GatewayResult.Ok ok)
        {
            await _cache.SetAsync(path, ok.Content, cancellationToken);
        }

        return result;
    }

    public Task<GatewayResult> PostFormAsync(string path, IReadOnlyDictionary<string, string> form, CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Post, path, form, cancellationToken);

    public void Dispose()
    {
        _http.Dispose();
        _gate.Dispose();
    }

    private async Task<GatewayResult> SendAsync(HttpMethod method, string path, IReadOnlyDictionary<string, string>? form, CancellationToken cancellationToken)
    {
        var token = await _tokens.GetAsync(cancellationToken);
        if (string.IsNullOrEmpty(token))
        {
            return new GatewayResult.NotSignedIn();
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            await WaitForSpacingAsync(cancellationToken);
            using var request = new HttpRequestMessage(method, path.TrimStart('/'));
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("AoCEditor", AppInfo.Version));
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue($"(+{_options.RepositoryUrl})"));
            request.Headers.TryAddWithoutValidation("Cookie", $"session={token}");
            if (form is not null)
            {
                request.Content = new FormUrlEncodedContent(form);
            }

            try
            {
                using var response = await _http.SendAsync(request, cancellationToken);
                return await MapAsync(response, cancellationToken);
            }
            catch (HttpRequestException)
            {
                return new GatewayResult.NetworkError();
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return new GatewayResult.NetworkError();
            }
            finally
            {
                _lastRequest = _time.GetTimestamp();
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task WaitForSpacingAsync(CancellationToken cancellationToken)
    {
        if (_lastRequest is not { } last)
        {
            return;
        }

        var remaining = _options.MinInterval - _time.GetElapsedTime(last);
        if (remaining > TimeSpan.Zero)
        {
            await Task.Delay(remaining, _time, cancellationToken);
        }
    }

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
            _ => new GatewayResult.NetworkError(),
        };
    }
}
