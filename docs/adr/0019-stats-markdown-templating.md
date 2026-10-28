# 0019 Stats markdown templating

## Status
Accepted

## Context
The owner wants stats for a repo README, generated from a markdown template. Only the user's own results may appear; no puzzle text, inputs, or other users' data.

## Decision
- The template is a markdown file in the workspace (default `stats.template.md`) with placeholders such as `{{stars.total}}`, `{{year.2024.stars}}`, `{{table.days}}`, `{{perf.best.2024.05}}`.
- The generator replaces the placeholders from the local DB (0007, 0018) and writes only between marker comments (`<!-- aoc-stats:start -->` / `<!-- aoc-stats:end -->`) in the target README, leaving the rest untouched.
- Substitution is a plain string scanner with a fixed placeholder set; unknown placeholders are errors, not passed through. No scripting, no reflection (AoT).
- Allowed data: stars, completion dates, language, timings, and puzzle titles (owner decision). Never: puzzle text bodies, inputs, answers.
- Generation is a user action, and optionally part of the auto-commit (0017) as a separate commit.

## Consequences
- Predictable, testable output with golden-file tests.
- Adding a placeholder is a code change; no user-defined logic.
- Puzzle titles are AoC content; the owner accepts including them, pending a policy check (see Open questions).

## Open questions
- Verify AoC's rules on republishing puzzle titles; if disallowed, switch to day numbers only.
- Output of badges/graphs (images) is out of scope for now.
