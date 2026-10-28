using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using AoCEditor.Core.Gateway;
using AoCEditor.Infrastructure.Gateway;
using Microsoft.Extensions.Time.Testing;
using Shared.Contracts;

namespace Specs.Drivers.Gateway;

public sealed class FetchingDriver : IDisposable
{
    public const string Page = "/2024/day/1";
    private readonly string _dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    private readonly FakeTimeProvider _time = new();
    private readonly FileResponseCache _cache;
    private readonly AocGateway _gateway;
    private HttpStatusCode _status = HttpStatusCode.OK;
    private Task<GatewayResult>? _pending;

    public FetchingDriver()
    {
        _cache = new FileResponseCache(_dir);
        _gateway = new AocGateway(new Handler(this), new Token(), _cache, NullLogger<AocGateway>.Instance, new GatewayOptions { InteractiveInterval = TimeSpan.FromSeconds(5) }, _time);
    }

    public List<string> Requests { get; } = [];

    public GatewayResult? Result { get; private set; }

    public void Dispose()
    {
        _gateway.Dispose();
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    public Task SavePageAsync(string content) => _cache.SetAsync(Page, content, CancellationToken.None).AsTask();

    public void ReportPageMissing() => _status = HttpStatusCode.NotFound;

    public async Task RequestFirstAsync() => await _gateway.GetAsync("/first", RequestKind.Interactive, FetchPolicy.Refresh, CancellationToken.None);

    public async Task RequestFirstStatisticsAsync() => await _gateway.GetAsync("/first", RequestKind.Statistics, FetchPolicy.Refresh, CancellationToken.None);

    public async Task StartSecondStatisticsRequestAsync()
    {
        _pending = _gateway.GetAsync("/second", RequestKind.Statistics, FetchPolicy.Refresh, CancellationToken.None);
        await Task.Yield();
    }

    public async Task WaitStatisticsSpacingAsync()
    {
        _time.Advance(TimeSpan.FromMinutes(3));
        await _pending!;
    }

    public async Task OpenPageAsync(FetchPolicy policy) => Result = await _gateway.GetAsync(Page, RequestKind.Interactive, policy, CancellationToken.None);

    public async Task StartSecondRequestAsync()
    {
        _pending = _gateway.GetAsync("/second", RequestKind.Interactive, FetchPolicy.Refresh, CancellationToken.None);
        await Task.Yield();
    }

    public async Task WaitMinimumSpacingAsync()
    {
        _time.Advance(TimeSpan.FromSeconds(5));
        await _pending!;
    }

    public async Task<string?> SavedPageAsync() => await _cache.TryGetAsync(Page, CancellationToken.None);

    private sealed class Token : ISessionTokenProvider
    {
        public ValueTask<string?> GetAsync(CancellationToken cancellationToken) => ValueTask.FromResult<string?>("synthetic");
    }

    private sealed class Handler(FetchingDriver owner) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            owner.Requests.Add(request.RequestUri!.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(owner._status) { Content = new StringContent("fresh") });
        }
    }
}
