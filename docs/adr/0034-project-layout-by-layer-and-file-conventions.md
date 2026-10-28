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
- Namespaces mirror the folders: `AoCEditor.<Layer>.<Slice>[.<Folder>]`, for example `AoCEditor.Core.Sync.Domain`, `AoCEditor.Infrastructure.Storage`, `AoCEditor.UI.Sync`. Tests mirror the same way (`AoCEditor.Core.Tests.Sync`). `Shared`, `UiKit` and the App namespace `AoCEditor` keep their names. The architecture test checks that each namespace matches its folder.

## Consequences
- Fewer csproj files in the IDE and fewer test projects.
- The compiler no longer enforces slice isolation; the architecture test does.
- One-time large move done with `git mv` to keep history.
- Supersedes the one-project-per-slice rule of 0003.

## Resolved questions
- The architecture test uses no library. Slice isolation scans the sources of `src/Core`, `src/Infrastructure` and `src/UI` for `AoCEditor.<Layer>.<OtherSlice>` (usings, fully qualified names, XAML `using:`). The layer rules read the assembly references of the built `AoCEditor.Core`, `AoCEditor.Infrastructure` and `AoCEditor.UI` with `System.Reflection.Metadata`. Both live in `tests/Shared.Tests/Architecture`.
- Placement of types that mix layers: Core may use plain `System.IO` but no Avalonia, SQLite, HTTP, process or secret-store code. `WorkspaceService`, `ToolchainLocator` and `DiagnosticsServices` (which creates the file and console sinks) are in Infrastructure. `EditorViewModel` does file I/O but is a view model used by the UI, so it lives in `src/UI/Editor`.
