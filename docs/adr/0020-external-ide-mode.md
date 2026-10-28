# 0020 External IDE mode

## Status
Accepted

## Context
Some users prefer their own IDE. The app should still manage puzzles, inputs, running, and submitting.

## Decision
- A per-workspace setting "External IDE mode" hides the embedded editor, LSP and debug panels (0012, 0013); the day page keeps puzzle view, console, run, submit and performance.
- The app watches the day's solution folder (0016) for file changes using file system events; no polling of AoC. Changes are shown, not auto-run.
- "Open in IDE" launches a user-configured command (e.g. `code {dir}`) with the day folder; the command template is user-supplied and run without a shell.
- Run uses the same sandboxed Runner (0011) and the same plugin commands (0009), reading the files as they are on disk.
- Scaffolding, shared helper wiring and auto-commit behave as in normal mode.

- The MCP server (0021) works unchanged in this mode, with its per-tool settings.

## Consequences
- Small cost: mostly UI toggles plus a file watcher.
- IDE and app may both write files; the app never writes to a file the user has modified since the last scaffold.
- Language server and toolchain setup is the user's own responsibility in the IDE, though 0010 still provisions the run toolchain.

## Open questions
- None.
