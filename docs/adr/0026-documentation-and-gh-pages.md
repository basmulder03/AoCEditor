# 0026 Documentation and gh-pages

## Status
Accepted

## Context
Users and contributors need docs from the start. Code comments are kept sparse (ADR 0028), so docs carry the explanation.

## Decision
- A /docs folder exists from day one, in markdown, in the repo. It contains user guide, contributor guide, architecture overview, plugin authoring, MCP tool reference, roadmap pointer, and /docs/adr (ADR 0001).
- Docs change in the same PR as the feature or behavior they describe; a slice is not done without its docs.
- On release, the pipeline (ADR 0025) builds a static site from /docs and publishes it to the gh-pages branch.
- Docs state the AoC ToS constraints, the SmartScreen caveat (ADR 0024) and the AI notice (ADR 0021).
- Docs must not contain puzzle text, inputs or other users' data.
- Internal links are relative so docs work on GitHub and on gh-pages.

## Consequences
- Docs drift is controlled by the PR rule, not by tooling.
- The published site only changes on release; main-branch docs are read on GitHub.

## Open questions
- Generator: DocFX. Chosen because it is .NET-native, can build API reference from the C# code, and is actively released (v2.81.0 in Sept 2026). MkDocs core was rejected: its last release was Aug 2024.
