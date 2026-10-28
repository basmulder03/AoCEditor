using System.Globalization;
using AoCEditor.Core.Puzzle.Application;
using AoCEditor.Core.Puzzle.Domain;
using Shared;

namespace AoCEditor.UI.Puzzle;

/// <summary>State of one day's page: the unlocked puzzle parts, the input and the answer box with its cooldown.</summary>
public sealed class DayViewModel(PuzzleService service, int year, int day, TimeProvider time, Uri? siteAddress = null) : ObservableBase
{
    private IReadOnlyList<PuzzlePart> _parts = [];
    private int _solved;
    private string _heading = string.Create(CultureInfo.InvariantCulture, $"Day {day}");
    private string _status = string.Empty;
    private string _result = string.Empty;
    private string _answer = string.Empty;
    private string? _inputText;
    private bool _busy;
    private bool _awaitingReload;
    private DateTimeOffset? _cooldownUntil;
    private int _cooldownSeconds;

    /// <summary>What relative puzzle links resolve against.</summary>
    public Uri? SiteAddress { get; } = siteAddress;

    public string Heading { get => _heading; private set => Set(ref _heading, value); }

    public IReadOnlyList<PuzzlePart> Parts { get => _parts; private set => Set(ref _parts, value); }

    /// <summary>The part a new answer counts for: the last unlocked one.</summary>
    public int OpenPart => Math.Max(1, Parts.Count);

    public bool IsComplete => Parts.Count == 2 && _solved >= 2;

    /// <summary>Why the puzzle could not be shown; empty when it loaded.</summary>
    public string Status { get => _status; private set => Set(ref _status, value); }

    /// <summary>What the last submit said.</summary>
    public string Result { get => _result; private set => Set(ref _result, value); }

    public string? InputText { get => _inputText; private set => Set(ref _inputText, value); }

    public string Answer
    {
        get => _answer;
        set
        {
            Set(ref _answer, value);
            Raise(nameof(CanSubmit));
        }
    }

    public int CooldownSeconds { get => _cooldownSeconds; private set => Set(ref _cooldownSeconds, value); }

    public bool CanSubmit => !_busy && Parts.Count > 0 && !IsComplete && !_awaitingReload && CooldownSeconds == 0 && !string.IsNullOrWhiteSpace(Answer);

    public async Task LoadAsync(bool refresh, CancellationToken cancellationToken)
    {
        var loaded = await service.LoadPuzzleAsync(year, day, refresh, cancellationToken);
        if (loaded.Value is { } doc)
        {
            Show(doc);
            await RestoreCooldownAsync(cancellationToken);
        }

        Status = loaded.Status == FetchStatus.Ok ? string.Empty : Describe(loaded.Status, loaded.Reason);
    }

    public async Task LoadInputAsync(bool refresh, CancellationToken cancellationToken)
    {
        var loaded = await service.LoadInputAsync(year, day, refresh, cancellationToken);
        InputText = loaded.Value;
        if (loaded.Status != FetchStatus.Ok)
        {
            Status = Describe(loaded.Status, loaded.Reason);
        }
    }

    public async Task SubmitAsync(CancellationToken cancellationToken)
    {
        if (!CanSubmit)
        {
            return;
        }

        SetBusy(true);
        try
        {
            var response = await service.SubmitAsync(year, day, OpenPart, Answer, cancellationToken);
            if (response.Value is not { } submission)
            {
                Result = response.Status switch
                {
                    FetchStatus.CoolingDown => "Wait for the cooldown to end before answering again.",
                    FetchStatus.AlreadyTried => "You already tried that answer.",
                    _ => Describe(response.Status, response.Reason),
                };
                return;
            }

            Result = Describe(submission.Verdict);
            if (submission.Verdict.Outcome == SubmitOutcome.Correct)
            {
                Answer = string.Empty;
                if (submission.Unlocked is { } unlocked)
                {
                    Show(unlocked);
                }
                else
                {
                    _solved = Math.Max(_solved, OpenPart);
                    _awaitingReload = true;
                    Status = "Part 1 is solved. Reload the puzzle to see part 2.";
                }
            }

            await RestoreCooldownAsync(cancellationToken);
        }
        finally
        {
            SetBusy(false);
        }
    }

    /// <summary>Called about once a second while the page is visible to count the cooldown down.</summary>
    public void Tick()
    {
        var left = _cooldownUntil is { } until ? (int)Math.Ceiling(Math.Max(0, (until - time.GetUtcNow()).TotalSeconds)) : 0;
        CooldownSeconds = left;
        Raise(nameof(CanSubmit));
    }

    private void Show(PuzzleDocument doc)
    {
        _awaitingReload = false;
        Heading = string.Create(CultureInfo.InvariantCulture, $"Day {day}: {doc.Title}");
        _solved = doc.Answers.Count;
        Parts = doc.Parts;
        Raise(nameof(OpenPart));
        Raise(nameof(IsComplete));
        Raise(nameof(CanSubmit));
    }

    private async Task RestoreCooldownAsync(CancellationToken cancellationToken)
    {
        _cooldownUntil = await service.CooldownUntilAsync(year, day, OpenPart, cancellationToken);
        Tick();
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        Raise(nameof(CanSubmit));
    }

    private static string Describe(SubmitResult result) => result.Outcome switch
    {
        SubmitOutcome.Correct => "That's the right answer!",
        SubmitOutcome.Wrong => "That's not the right answer.",
        SubmitOutcome.TooHigh => "That's not the right answer: it is too high.",
        SubmitOutcome.TooLow => "That's not the right answer: it is too low.",
        SubmitOutcome.RateLimited => "You answered too recently; wait for the countdown.",
        SubmitOutcome.AlreadySolved => "AoC says this part is already solved.",
        _ => "AoC's reply was not recognised. Check the website to see whether the answer counted.",
    };

    private static string Describe(FetchStatus status, string? reason) => status switch
    {
        FetchStatus.NotSignedIn => "Sign in to load this puzzle.",
        FetchStatus.RateLimited => "AoC asked us to slow down. Try again in a moment.",
        FetchStatus.NotFound => "AoC has no puzzle for this day yet.",
        FetchStatus.Unreadable => "AoC returned a page that could not be read.",
        _ => reason is null ? "Could not reach AoC." : $"Could not reach AoC ({reason}).",
    };
}
