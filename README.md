# AoCEditor

A small, fast, AoT-compiled desktop app for solving [Advent of Code](https://adventofcode.com) puzzles locally: browse every year's calendar, read the puzzle, write and run a solution in any language, and submit answers. Linux and Windows first, built on .NET 10 and Avalonia.

**Unofficial.** This project is not affiliated with or endorsed by Advent of Code or its author. It does not replace the AoC website: leaderboards, the story and the ASCII art stay there.

> Status: early development. See the [roadmap](docs/roadmap.md) and the [architecture decisions](docs/adr/README.md).

## Automation guidelines

This tool follows the automation guidelines on the [/r/adventofcode community wiki](https://www.reddit.com/r/adventofcode/wiki/faqs/automation). Specifically:

- All requests to adventofcode.com go through one gateway. Calendar and event pages are requested at most once every 3 minutes; puzzle text, inputs and submissions at least 5 seconds apart, one request at a time. Nothing polls or refreshes in the background.
- Puzzle text and inputs are downloaded once and cached locally; a manual re-download obeys the same throttle.
- The User-Agent identifies the tool and its maintainer: `AoCEditor/<version> (+https://github.com/basmulder03/AoCEditor; maintainer github.com/basmulder03)`.
- Puzzle text and inputs are stored in the per-user app-data folder and are never written to your solutions repository.

Using AI to solve puzzles goes against Advent of Code's stated wishes.

## Develop

```
dotnet build
dotnet test --solution AoCEditor.slnx
```

Tests use xUnit v3, Shouldly and Reqnroll; see [tests/README.md](tests/README.md). The docs site is built with DocFX (`dotnet tool restore && dotnet docfx docfx.json`).

## License

[MIT](LICENSE)
