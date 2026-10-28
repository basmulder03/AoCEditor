# AoCEditor

AoCEditor is a local desktop editor for Advent of Code, built with .NET 10 and Avalonia and published as a native AoT binary. It is meant to cover browsing puzzles, writing and running solutions, and submitting answers in one app.

## Status

Pre-release. The solution skeleton exists and most features are still planned. Expect missing pages and changing behavior.

## Where to go next

- [Getting started](getting-started.md)
- [Roadmap](roadmap.md)
- [Architecture decision records](adr/README.md)

Advent of Code access follows the site's automation rules. This documentation contains no puzzle text or personal inputs.

## Code conventions

Every `.cs` file under `src` and `tests` declares one top-level type named after the file; `scripts/check-practices.sh` enforces it.
