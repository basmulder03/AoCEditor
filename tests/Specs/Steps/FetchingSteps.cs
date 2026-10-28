using System.Net;
using Gateway;
using Microsoft.Extensions.Time.Testing;
using Reqnroll;
using Shared.Contracts;
using Shouldly;

namespace Specs.Steps;

[Binding]
public sealed class FetchingSteps : IDisposable
{
    private const string Page = "/2024/day/1";
    private readonly string _dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    private readonly FakeTimeProvider _time = new();
    private readonly List<string> _requests = [];
    private readonly FileResponseCache _cache;
    private readonly AocGateway _gateway;
    private HttpStatusCode _status = HttpStatusCode.OK;
    private GatewayResult? _result;
    private Task<GatewayResult>? _pending;

    public FetchingSteps()
    {
        _cache = new FileResponseCache(_dir);
        _gateway = new AocGateway(new Handler(this), new Token(), _cache, new GatewayOptions { MinInterval = TimeSpan.FromSeconds(5) }, _time);
    }

    public void Dispose()
    {
        _gateway.Dispose();
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    [Given("I am signed in")]
    public static void GivenIAmSignedIn()
    {
    }

    [Given("I have already downloaded a page")]
    public Task GivenIHaveAlreadyDownloadedAPage() => _cache.SetAsync(Page, "saved copy", CancellationToken.None).AsTask();

    [Given("Advent of Code reports that the page does not exist")]
    public void GivenNotFound() => _status = HttpStatusCode.NotFound;

    [Given("I have just made a request")]
    public async Task GivenIHaveJustMadeARequest() => await _gateway.GetAsync("/first", FetchPolicy.Refresh, CancellationToken.None);

    [When("I open that page")]
    public async Task WhenIOpenThatPage() => _result = await _gateway.GetAsync(Page, FetchPolicy.CacheFirst, CancellationToken.None);

    [When("I explicitly refresh that page")]
    public async Task WhenIRefresh() => _result = await _gateway.GetAsync(Page, FetchPolicy.Refresh, CancellationToken.None);

    [When("I make another request straight away")]
    public async Task WhenIMakeAnotherRequest()
    {
        _pending = _gateway.GetAsync("/second", FetchPolicy.Refresh, CancellationToken.None);
        await Task.Yield();
    }

    [Then("I see the saved copy")]
    public void ThenSavedCopy() => _result.ShouldBe(new GatewayResult.Ok("saved copy"));

    [Then("Advent of Code was not contacted")]
    public void ThenNotContacted() => _requests.ShouldBeEmpty();

    [Then("Advent of Code was contacted once")]
    public void ThenContactedOnce() => _requests.Count.ShouldBe(1);

    [Then("it is held back")]
    public void ThenHeldBack() => _requests.ShouldBe(["/first"]);

    [Then("it goes ahead after the minimum spacing has passed")]
    public async Task ThenGoesAhead()
    {
        _time.Advance(TimeSpan.FromSeconds(5));
        await _pending!;
        _requests.ShouldBe(["/first", "/second"]);
    }

    [Then("I am told it was not found")]
    public void ThenNotFound() => _result.ShouldBeOfType<GatewayResult.NotFound>();

    [Then("nothing is saved for it")]
    public async Task ThenNothingSaved() => (await _cache.TryGetAsync(Page, CancellationToken.None)).ShouldBeNull();

    private sealed class Token : ISessionTokenProvider
    {
        public ValueTask<string?> GetAsync(CancellationToken cancellationToken) => ValueTask.FromResult<string?>("synthetic");
    }

    private sealed class Handler(FetchingSteps owner) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            owner._requests.Add(request.RequestUri!.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(owner._status) { Content = new StringContent("fresh") });
        }
    }
}
