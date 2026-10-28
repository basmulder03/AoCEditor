---
name: validate-practices
description: Run before reporting a change done. Mechanical check plus a short best-practice review for .NET, Avalonia, tests and AoC compliance.
---
1. Run `scripts/check-practices.sh`, `dotnet build` (zero warnings), `dotnet test --solution AoCEditor.slnx`, `dotnet format --verify-no-changes`. Fix findings; do not suppress rules to pass.
2. Review the diff against this list; fix or open an issue for each miss:
- .NET: async all the way with CancellationToken, no blocking, IDisposable/await using for processes/streams/handles, nullable honoured, records/immutability for domain values, no static mutable state, DI via constructor, System.Text.Json source generation (no reflection), LibraryImport for P/Invoke, no process started through a shell.
- Avalonia: compiled bindings with x:DataType, no code logic in views beyond view concerns, tokens from UiKit only (no hardcoded colors/sizes), AutomationProperties on interactive controls, keyboard reachable.
- Tests: Shouldly only, behaviour over implementation, Reqnroll per tests/README.md, new logic has a test written first, synthetic fixtures (never puzzle text or inputs).
- AoC and privacy: AoC traffic only via the gateway, no tokens/inputs/answers in logs or git, paths redacted in reports.
- Slices: reference only Shared; docs/ page updated; comments sparse, behaviour-only, no ADR or issue numbers.
3. State anything unverified (e.g. Windows) in the report.
