# 0023 Testing strategy

## Status
Accepted

## Context
The project is developed test-first, with human-readable behavior specs, and ships AoT binaries on several OSes.

## Decision
- Unit and integration tests: xUnit v3 (latest).
- Behavior tests: Reqnroll Gherkin features per vertical slice (ADR 0003), living next to the slice they describe. Features are written before implementation (BDD); unit tests drive the internals (TDD).
- E2E: drive the real app (published build) for key journeys: sign-in stub, open day, run, submit against a fake AoC server. Tests never contact the real AoC.
- AoC access is tested through the single HttpClient (ADR 0005) with a fake handler; throttling and cooldown logic are unit-tested with a fake clock.
- Sandbox and plugin tests run per OS in CI (Linux and Windows).
- Tests must pass on the AoT-published binary at least for smoke tests, since AoT trimming can break reflection.
- E2E and full matrix automation is added near the first release; before that, unit and BDD run on every PR.
- A bug fix starts with a failing test.

- Assertions use Shouldly only. `Xunit.Assert` is banned in `tests/` through BannedApiAnalyzers (`tests/BannedSymbols.txt`), so the build fails on violations. Guidelines are in `tests/README.md`.
- Reqnroll features follow the practices in `tests/README.md` (declarative scenarios, thin bindings, context injection, parallel-safe).
- Reqnroll code-behind (`*.feature.cs`) is generated at build and not committed.

## Consequences
- More up-front work per slice; regressions caught early.
- E2E is slower and runs on a schedule or release branch, not every commit.

## Open questions
- xUnit v3 and Reqnroll compatibility and the Reqnroll xUnit v3 adapter status are unverified.
- E2E UI driver choice (Avalonia headless vs OS-level automation).
