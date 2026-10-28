# 0007 Local storage layout

## Status
Accepted

## Context
Puzzle text and inputs are copyrighted and AoC asks that they not be redistributed. Solutions are meant to be committed to a public repo. The two must never mix.

## Decision
- All app data lives in a per-user app-data folder (Linux `$XDG_DATA_HOME/AoCEditor`, Windows `%LOCALAPPDATA%\AoCEditor`), location configurable in settings.
- Contents: local database (progress, answers, timings), cached puzzle text, inputs, settings, logs.
- Puzzle text and inputs are never written into the solutions folder, and never staged or committed by the git integration (see 0017). The runner passes the input to a solution by path or stdin from the app-data folder.
- The solutions folder is a separate, user-defined path (see 0016).
- Cache layout is keyed by year/day/part; removing the folder only costs re-downloads on next open.
- Moving the data folder is a supported setting; the app copies, verifies, then switches.

## Consequences
- The solutions repo can be public without leaking inputs or puzzle text.
- Users who relocate the data folder into the solutions repo are warned; the app refuses paths inside the solutions folder.
- Backup of progress means backing up the app-data folder.

## Open questions
- Local database engine (for example SQLite) and its AoT-safe provider are unverified.
