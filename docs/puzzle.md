# Puzzle parsing

`PuzzleParser.Parse(html)` (`src/Features/Puzzle`) turns a puzzle page into a `PuzzleDocument`: the title, one `PuzzlePart` per `article.day-desc` and the "Your puzzle answer was" values found outside the articles. It returns null when the page has no puzzle article (login page, error page), so the caller can treat that as a fetch problem.

The HTML reader is hand-written and forgiving (no parser library, so it stays AoT-safe): unmatched end tags are ignored, void tags are recognised and entities are decoded.

## Model

- Blocks: paragraph, list and code block. Part headings are dropped, the part tabs name them.
- Inlines: plain, emphasis, code and link (with `Href`). Emphasised code (`<em><code>`) counts as code.
- Unknown elements degrade to their text, so a new AoC element never breaks rendering.

Rendering with Avalonia controls and fetching through the gateway come in later slices.
