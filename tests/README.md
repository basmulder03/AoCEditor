# Tests

- Unit: `dotnet test --project tests/Shared.Tests`
- Specs (Reqnroll): `dotnet test --project tests/Specs`
- E2E (starts the built App): `dotnet test --project tests/E2E`

Filter by category in a mixed run: `dotnet test --solution AoCEditor.slnx --filter-not-trait "Category=E2E" --ignore-exit-code 8` (exit code 8 is "zero tests ran" in the all-E2E project) (or `--filter-trait "Category=E2E"`).

## Assertions: Shouldly only

`Xunit.Assert` is banned in `tests/` (build error RS0030, see `tests/BannedSymbols.txt`). Use Shouldly:

- Actual first: `result.ShouldBe(expected)`. Pick the most specific assertion (`ShouldBeEmpty`, `ShouldContain`, `ShouldBeOfType<T>`, `ShouldBeInRange`, `ShouldStartWith`) over `ShouldBeTrue()` on a boolean expression.
- Assert on named locals or plain expressions. Shouldly puts the source expression in the failure message, so `ratio.ShouldBeGreaterThanOrEqualTo(4.5)` explains itself and `Check(x).ShouldBeTrue()` does not.
- Exceptions: `Should.Throw<T>(() => ...)` and assert on the result's message; async: `await Should.ThrowAsync<T>(...)`. Use `Should.NotThrow` only when "does not throw" is the behaviour under test.
- Collections: `ShouldBe(expected)` is order-sensitive; pass `ignoreOrder: true` when order is not part of the behaviour. Floating point: `ShouldBe(expected, tolerance)`.
- Add `customMessage:` only when the expression and values do not already tell the story.
- Several asserts for one outcome are fine; unrelated outcomes belong in separate tests.

## Reqnroll practices

- Layout follows the slices: `Features/<Slice>/*.feature`, `Steps/<Slice>/*Steps.cs` (namespace `Specs.Steps.<Slice>`) and `Drivers/<Slice>/*Driver.cs` (namespace `Specs.Drivers.<Slice>`). Genuinely shared code goes in `Steps/Common` and `Drivers/Common`.
- A feature opens with the business reason (`In order to / As a / I want`). A scenario describes one behaviour in the language of the user, never in terms of controls, classes or endpoints (declarative, not imperative).
- Given sets up state, When performs one action, Then observes the outcome. Assertions (Shouldly) belong only in Then steps. Keep scenarios short; use `Background` for Givens shared by every scenario of a feature and `Scenario Outline` with `Examples` for data variations.
- Driver pattern: step classes are thin glue that translate Gherkin into driver calls and run Shouldly assertions in Then steps on values the driver exposes; they hold no setup and never call application code. Drivers own how the system is driven (slice objects, fake time/cache/token, temp dirs), expose results, contain no assertions, are constructor-injected (per-scenario lifetime, reusable by steps and hooks) and dispose temp resources via `IDisposable`. Never share state through static fields or `ScenarioContext` string keys.
- Prefer Cucumber expressions (`{int}`, `{string}`) over regular expressions. Step text has no tech terms and is reused across scenarios rather than duplicated.
- Scenarios must be independent and parallel-safe: no shared files, ports or ordering. Use per-scenario temporary folders.
- Tags classify scenarios (`@e2e`, `@ui`, `@slow`); hooks (`[BeforeScenario]`) are scoped with tags, not global.
- Never put puzzle text, inputs or real answers in features; use synthetic data.
- Generated `*.feature.cs` files are not committed.

## Layout

`tests/Features/<Slice>.Tests` holds each slice's unit tests (mirrors `src/Features`). `tests/Specs` holds all Reqnroll scenarios, `tests/E2E` the process-level tests, `tests/Ui.Tests` and `tests/Shared.Tests` the UI kit and shared code.

## One top-level type per file

`src` and `tests` follow one top-level type per file, named after the type (generic parameters are not part of the file name; `Foo.axaml.cs` holds `Foo`). Partial parts, nested types and generated files (`*.g.cs`, `*.feature.cs`) are excepted. Test helpers (fakes, builders) therefore get their own file too, or are nested private classes inside the test class that uses them. `scripts/check-practices.sh` fails otherwise.
