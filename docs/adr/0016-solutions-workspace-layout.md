# 0016 Solutions workspace layout

## Status
Accepted

## Context
Solutions live in a user-defined folder that is usually a git repo (0017). It must support all AoC years, several languages, and helpers shared across years. Inputs and puzzle text must never be inside it (0007).

## Decision
- The user picks the workspace root once (changeable in settings). Default suggestion only; no fixed location.
- Layout: `<root>/<year>/<dd>/<language>/` for a day's solution files (year four digits, day two digits), plus `<root>/shared/<language>/` for helpers usable from any year.
- The language plugin (0009) provides the day template and decides how `shared/` is referenced (e.g. Python path entry, .NET project reference). The app wires this up when scaffolding.
- Multiple languages for the same day coexist side by side; the day page lets the user pick the active one.
- The app only creates files in the workspace on user action (open day, new solution). It never writes inputs, puzzle text or the cache there.
- A workspace `.gitignore` is offered at creation to keep build outputs out of git.

## Consequences
- One repo can hold all years and shared code, keeping git history (0017) coherent.
- Per-language scaffolding must handle the shared reference correctly; each plugin owns that.
- Existing user repos with another layout need an import or mapping step, not covered here.

## Open questions
- Whether to allow a configurable path template for users with existing layouts.
