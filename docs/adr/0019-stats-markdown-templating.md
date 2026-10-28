# 0019 Stats markdown templating

## Status
Accepted

## Context
The owner wants stats for a repo README, generated from a markdown template. Only the user's own results may appear; no puzzle text, inputs, or other users' data.

## Decision
- The template is a markdown file in the workspace (default `stats.template.md`) with placeholders such as `{{stars.total}}`, `{{year.2024.stars}}`, `{{table.days}}`, `{{perf.best.2024.05}}`, `{{puzzle.title.2024.05}}` and `{{puzzle.url.2024.05}}` (a link to `https://adventofcode.com/2024/day/5`).
- The generator replaces the placeholders from the local DB (0007, 0018) and writes only between marker comments (`<!-- aoc-stats:start -->` / `<!-- aoc-stats:end -->`) in the target README, leaving the rest untouched.
- Table placeholders render whole blocks from fixed generators: `{{table.year.2024}}` (days of one year) and `{{table.years}}` (all years as a matrix with totals). Day states use a selectable icon set (emoji or unicode symbols, each with a text alternative). A few built-in templates ship for the user to copy.
- Substitution is a plain string scanner with a fixed placeholder set; unknown placeholders are errors, not passed through. No scripting, no reflection (AoT).
- Allowed data: stars, completion dates, language, timings, and puzzle titles (owner decision). Never: puzzle text bodies, inputs, answers.
- Generation is a user action, and optionally part of the auto-commit (0017) as a separate commit.

## Consequences
- Predictable, testable output with golden-file tests.
- Adding a placeholder is a code change; no user-defined logic.
- Puzzle titles are AoC content; the owner accepts including them, pending a policy check (see Open questions).

## Open questions
- Verify AoC's rules on republishing puzzle titles; if disallowed, `puzzle.title` renders "Day N" and links via `puzzle.url` carry the navigation. The link is a plain URL, so it contains no AoC content.
- Output of badges/graphs (images) is out of scope for now; it needs an amendment of this ADR first.
