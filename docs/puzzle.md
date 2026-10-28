# Puzzle parsing

`PuzzleParser.Parse(html)` (`src/Features/Puzzle`) turns a puzzle page into a `PuzzleDocument`: the title, one `PuzzlePart` per `article.day-desc` and the "Your puzzle answer was" values found outside the articles. It returns null when the page has no puzzle article (login page, error page), so the caller can treat that as a fetch problem.

The HTML reader is hand-written and forgiving (no parser library, so it stays AoT-safe): unmatched end tags are ignored, void tags are recognised and entities are decoded.

## Model

- Blocks: paragraph, list and code block. Part headings are dropped, the part tabs name them.
- Inlines: plain, emphasis, code and link (with `Href`). Emphasised code (`<em><code>`) counts as code.
- Unknown elements degrade to their text, so a new AoC element never breaks rendering.

Rendering with Avalonia controls and fetching through the gateway come in later slices.

## Submit responses

`SubmitResponseParser.Parse(html)` reads the answer response into a `SubmitResult`: `Correct`, `Wrong`, `TooHigh`, `TooLow`, `RateLimited` (with `Wait` when the "You have 4m 12s left to wait" text is readable) or `AlreadySolved`. Anything else, including a login page, is `Unknown`: show it raw and treat it as non-final. The phrases come from the wording other AoC tools match and are not yet verified against live responses (ADR 0015 open question), so the fixtures are synthetic.

## PuzzleService

`PuzzleService` (`Puzzle.Application`) wraps the gateway for one day. Every call is `RequestKind.Interactive`, and every request follows a user action:

- `LoadPuzzleAsync(year, day, refresh)`: `GET /{year}/day/{day}`, parsed with `PuzzleParser`.
- `LoadInputAsync(year, day, refresh)`: `GET /{year}/day/{day}/input`, returned as raw text. The input stays in the gateway cache (app data) and never reaches the solutions folder.
- `SubmitAsync(year, day, part, answer)`: one `POST /{year}/day/{day}/answer` with `level` and the trimmed answer, parsed with `SubmitResponseParser`. It is never retried. A part outside 1 to 2 or a blank answer throws before any request.

Results are `Fetched<T>` with a `FetchStatus` (`Ok`, `NotSignedIn`, `RateLimited`, `NotFound`, `NetworkError`, `Unreadable`). The default is cache-first, so after a correct part 1 the caller must refresh the puzzle to pick up part 2. Locked days are not checked here, the caller must not ask for them.
