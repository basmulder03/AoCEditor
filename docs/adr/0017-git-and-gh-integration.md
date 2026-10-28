# 0017 Git and gh integration

## Status
Accepted

## Context
The solutions workspace (0016) is a git repo. The owner wants history, automatic commits of correct solutions, and no inputs or puzzle text ever committed (AoC asks that these are not redistributed).

## Decision
- Use the `git` and `gh` CLIs as external processes; no libgit2 (AoT and size). Detection and install guidance follow 0010.
- History: the day page shows `git log` for the day's folder.
- Auto-commit: on first correct answer (0015), the app commits only that day's solution files under `<year>/<dd>/` and `shared/` changes, with a message like `2024 day 05 part 1`. Auto-commit is on by default and can be turned off; push is never automatic unless the user enables it.
- Guard: before every commit the app checks staged paths against a deny list (input files, puzzle text, cache, DB). The workspace is also kept free of those by design (0007). A violation aborts the commit and shows an error.
- `gh` is used for auth status and optional repo creation/push only; the app stores no GitHub token itself.
- Commit authorship is the user's git config.

## Consequences
- Needs git installed; the app degrades to no-history if missing.
- Process-spawn parsing of git output must use porcelain formats.
- Users with dirty trees are not surprised: only the day's own paths are staged.
