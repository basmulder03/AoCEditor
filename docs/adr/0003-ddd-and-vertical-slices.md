# 0003 DDD and vertical slices

## Status
Accepted

## Context
Work is tracked as GitHub issues per slice and delivered by agents with small context windows. The code layout should let one slice be understood and changed without reading the rest.

## Decision
- Domain-driven design with vertical slicing: one folder/project per feature slice (for example Calendar, Puzzle, Submission, Runner, Workspace), each holding its own domain, application and UI code.
- Shared kernel is minimal: value objects such as Year, Day, Part, and the AoC HTTP gateway abstraction.
- Dependency rules: domain depends on nothing; application depends on domain; infrastructure and UI depend on application. Slices talk to each other only through interfaces or events in the shared kernel, never through another slice's internals.
- Composition root wires slices; no service locator, no reflection-based DI scanning (AoT).
- Architecture tests (xUnit, see 0023) enforce the dependency rules.

## Consequences
- A slice maps to one issue and one agent task with a small read set.
- Some duplication between slices is accepted over shared "common" growth.
- Architecture tests fail the build on boundary violations.

## Open questions
- Final slice list and project granularity (project per slice vs folder per slice) are not decided.
- Choice of DI/mediator library, if any, is unverified for AoT.
