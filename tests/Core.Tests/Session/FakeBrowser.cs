using AoCEditor.Core.Session.Application;
using Microsoft.Extensions.Time.Testing;
using Shared.Contracts;
using Shouldly;
using Xunit;

namespace AoCEditor.Core.Tests.Session;

internal sealed class FakeBrowser(BrowserOutcome outcome) : ISignInBrowser
{
    public bool Opened { get; private set; }

    public string[] Candidates { get; init; } = [];

    public bool WaitsForCancel { get; init; }

    public List<CandidateVerdict> Verdicts { get; } = [];

    public async Task<BrowserOutcome> AcquireTokenAsync(TokenValidator validate, CancellationToken cancellationToken)
    {
        Opened = true;
        foreach (var candidate in Candidates)
        {
            var verdict = await validate(candidate, cancellationToken);
            Verdicts.Add(verdict);
            if (verdict == CandidateVerdict.Accepted)
            {
                return new BrowserOutcome.Token(candidate);
            }
        }

        if (WaitsForCancel)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken).ContinueWith(_ => { }, TaskScheduler.Default);
        }

        return outcome;
    }
}
