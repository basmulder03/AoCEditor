# Puzzle parsing

`PuzzleParser.Parse(html)` (`src/Core/Puzzle`) turns a puzzle page into a `PuzzleDocument`: the title, one `PuzzlePart` per `article.day-desc` and the "Your puzzle answer was" values found outside the articles. It returns null when the page has no puzzle article (login page, error page), so the caller can treat that as a fetch problem.

The HTML reader is hand-written and forgiving (no parser library, so it stays AoT-safe): unmatched end tags are ignored, void tags are recognised and entities are decoded.

## Model

- Blocks: paragraph, list and code block. Part headings are dropped, the part tabs name them.
- Fragments (inline runs of a paragraph or list item): plain, emphasis, code and link (with `Href`). Emphasised code (`<em><code>`) counts as code.
- Unknown elements degrade to their text, so a new AoC element never breaks rendering.

Rendering with Avalonia controls and fetching through the gateway come in later slices.

## Submit responses

`SubmitResponseParser.Parse(html)` reads the answer response into a `SubmitResult`: `Correct`, `Wrong`, `TooHigh`, `TooLow`, `RateLimited` (with `Wait` when the "You have 4m 12s left to wait" text is readable) or `AlreadySolved`. Anything else, including a login page, is `Unknown`: show it raw and treat it as non-final. The phrases come from the wording other AoC tools match and are not yet verified against live responses (ADR 0015 open question), so the fixtures are synthetic.

## PuzzleService

`PuzzleService` (`AoCEditor.Core.Puzzle.Application`) wraps the gateway for one day. Every call is `RequestKind.Interactive`, and every request follows a user action:

- `LoadPuzzleAsync(year, day, refresh)`: `GET /{year}/day/{day}`, parsed with `PuzzleParser`.
- `LoadInputAsync(year, day, refresh)`: `GET /{year}/day/{day}/input`, returned as raw text. The input stays in the gateway cache (app data) and never reaches the solutions folder.
- `SubmitAsync(year, day, part, answer)`: one `POST /{year}/day/{day}/answer` with `level` and the trimmed answer, parsed with `SubmitResponseParser`. It is never retried. A correct part 1 is followed by one refresh of the puzzle page (`Submission.Unlocked`) to pick up part 2; this is the only automatic request, directly caused by the user's submit, and the owner approved it. A part outside 1 to 2 or a blank answer throws before any request.

Results are `Fetched<T>` with a `FetchStatus` (`Ok`, `NotSignedIn`, `RateLimited`, `NotFound`, `NetworkError`, `Unreadable`). The default is cache-first, so any other refresh is the caller's explicit request. Locked days are not checked here, the caller must not ask for them.

## PuzzleView

`PuzzleView` (`AoCEditor.UI.Puzzle`) renders one `PuzzlePart` (`Part` property) with native controls: selectable wrapped text for paragraphs and list items (`• ` prefix), a monospace boxed block for code, italic for emphasis, monospace for inline code and and a clickable link for web links. A click raises `LinkRequested` with the resolved address; `LinkResolver` resolves relative links against adventofcode.com and accepts only http and https, so other schemes are shown as underlined text and never opened. `DayView` asks for confirmation before opening a link in the system browser (ADR 0008).

## Attempts, cooldowns and known answers

`PuzzleService` uses `IAnswerStore` (SQLite, see `docs/storage.md`) around `SubmitAsync`:

- An active cooldown returns `FetchStatus.CoolingDown` with no request; `CooldownUntilAsync` gives the end time for a countdown. A rate-limited response saves its end time, so the cooldown survives restarts. When the wait cannot be read, and after any reply that is not recognised, a one-minute cooldown applies so a changed AoC wording can never turn Enter into a resend loop.
- An answer already judged wrong, too high or too low for that part returns `AlreadyTried` with no request.
- Every recognised response is stored as an attempt; unrecognised ones are not. A correct answer is also saved as the known answer for that part. Known answers live in app data only, never in the solutions folder.

## Day page

Selecting an unlocked calendar tile opens `DayPage` (`src/App`), which hosts `DayView` (`AoCEditor.UI.Puzzle`) bound to `DayViewModel`:

- Tabs: one per unlocked part plus Input. The input is downloaded the first time the Input tab is opened.
- The answer box submits for the last unlocked part. Submit is disabled while a request is in flight, the box is empty, the day is complete or a cooldown runs; Enter in the box submits too.
- A cooldown shows "You can answer again in Ns" and counts down once a second (`Tick`). A cooldown saved before a restart is restored on load.
- If a correct part 1 could not refresh the puzzle, submitting stays disabled until the puzzle is reloaded.
- Load failures (not signed in, rate limited, not found, network) show as a plain sentence under the title.

The editor and console panels from the day workspace design are not built yet; they will go beside this view.
