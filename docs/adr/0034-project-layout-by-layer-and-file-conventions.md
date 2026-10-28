# 0034 Project layout by layer and file conventions

## Status
Accepted

## Context
One csproj per slice (0003) produced many projects in the IDE and many test projects. The vertical slice idea is about folders and boundaries, not about project count.

## Decision
- Source projects by layer:
  - `src/Core`: Domain and Application of every slice. No UI or I/O framework references, AoT-safe.
  - `src/Infrastructure`: SQLite, gateway HttpClient, process runner and sandbox, secret stores, file system. References Core.
  - `src/UI`: Avalonia views and view models of every slice. References Core.
  - Unchanged: `src/Shared` (contracts), `src/UiKit` (design system), `src/App` (composition root, references everything).
- Slices stay top-level folders inside Core, Infrastructure and UI (for example `Core/Sync/Domain`), so a slice is still found by name.
- Slice isolation is enforced by an architecture test that fails the build when a type in one slice's namespace uses another slice's. Only Shared, and App as composition root, may cross.
- A layer test requires that Core does not reference Avalonia, Infrastructure or UI.
- Tests: unit tests of Core and Infrastructure consolidate into `tests/Core.Tests` and `tests/Infrastructure.Tests`. UI tests stay in `Ui.Tests`; Specs and E2E are unchanged.
- File conventions: one top-level type per file, the file named after the type. Partial classes and nested types are excepted. `scripts/check-practices.sh` enforces this.
- Namespaces stay slice-based (for example `Sync.Domain`) to keep the diff reviewable.

## Consequences
- Fewer csproj files in the IDE and fewer test projects.
- The compiler no longer enforces slice isolation; the architecture test does.
- One-time large move done with `git mv` to keep history.
- Supersedes the one-project-per-slice rule of 0003.

## Open questions
- Whether the architecture test uses a library or a small reflection-free check over project sources.
