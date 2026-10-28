# 0003 DDD and vertical slices

## Status
Accepted

## Context
Work is tracked as GitHub issues per slice and delivered by agents with small context windows. The code layout should let one slice be understood and changed without reading the rest.

## Decision
- Domain-driven design with vertical slicing: one folder/project per feature slice (for example Calendar, Puzzle, Submission, Runner, Workspace), each holding its own domain, application and UI code.
- Shared kernel is minimal: value objects such as Year, Day, Part, and the AoC HTTP gateway abstraction.
- Dependency rules: domain depends on nothing; application depends on domain; infrastructure and UI depend on application. Slices talk to each other only through interfaces or events in the shared kernel, never through another slice's internals.
- Composition root wires slices with Microsoft.Extensions.DependencyInjection using explicit registrations only (factories or closed types in `src/App/AppServiceRegistration.cs`); no service locator, no assembly scanning, no open generics, no activation of unknown types (AoT-safe). Slice projects never reference the container and keep plain constructors.
- Architecture tests (xUnit, see 0023) enforce the dependency rules.

- Granularity: one project per slice (`src/Features/<Slice>/<Slice>.csproj`, now historical) with Domain, Application, Infrastructure and UI folders inside. Slices reference only `Shared`, never each other, so the compiler enforces the boundary. Superseded by 0034: projects are now split by layer and an architecture test enforces slice isolation.

## Consequences
- A slice maps to one issue and one agent task with a small read set.
- Some duplication between slices is accepted over shared "common" growth.
- Architecture tests fail the build on boundary violations.

## Open questions
- Final slice list: slices are added as their features are built.
