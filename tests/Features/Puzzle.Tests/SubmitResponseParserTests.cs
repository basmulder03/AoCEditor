using Puzzle.Domain;
using Shouldly;
using Xunit;

namespace Puzzle.Tests;

public class SubmitResponseParserTests
{
    private static string Page(string message) => $"<main><article><p>{message} <a href=\"/2020/day/1\">[Return to Day 1]</a></p></article></main>";

    [Theory]
    [InlineData("That's the right answer!  You are <span class=\"day-success\">one gold star</span> closer to saving your vacation.", SubmitOutcome.Correct)]
    [InlineData("That's not the right answer.  If you're stuck, make sure you're using the full input data.", SubmitOutcome.Wrong)]
    [InlineData("That's not the right answer; your answer is <span>too high</span>.", SubmitOutcome.TooHigh)]
    [InlineData("That's not the right answer; your answer is <span>too low</span>.", SubmitOutcome.TooLow)]
    [InlineData("You don't seem to be solving the right level.  Did you already complete it?", SubmitOutcome.AlreadySolved)]
    public void KnownMessagesMapToAnOutcome(string message, SubmitOutcome expected)
    {
        SubmitResponseParser.Parse(Page(message)).Outcome.ShouldBe(expected);
    }

    [Theory]
    [InlineData("You gave an answer too recently; you have to wait after submitting an answer before trying again.  You have 4m 12s left to wait.", 252)]
    [InlineData("You gave an answer too recently.  You have 45s left to wait.", 45)]
    [InlineData("You gave an answer too recently.  You have 1m left to wait.", 60)]
    public void RateLimitCarriesTheWait(string message, int seconds)
    {
        var result = SubmitResponseParser.Parse(Page(message));

        result.Outcome.ShouldBe(SubmitOutcome.RateLimited);
        result.Wait.ShouldBe(TimeSpan.FromSeconds(seconds));
    }

    [Fact]
    public void RateLimitWithoutAReadableWaitStillRateLimits()
    {
        var result = SubmitResponseParser.Parse(Page("You gave an answer too recently."));

        result.Outcome.ShouldBe(SubmitOutcome.RateLimited);
        result.Wait.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("<html>[Log In]</html>")]
    [InlineData("<article><p>Something new</p></article>")]
    public void UnrecognisedResponsesAreNotFinal(string html)
    {
        var result = SubmitResponseParser.Parse(html);

        result.Outcome.ShouldBe(SubmitOutcome.Unknown);
        result.Wait.ShouldBeNull();
    }
}
