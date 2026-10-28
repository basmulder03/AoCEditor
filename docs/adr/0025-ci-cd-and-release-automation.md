# 0025 CI/CD and release automation

## Status
Accepted

## Context
The repo is public on GitHub. Builds target Linux and Windows with AoT, and need tests, installers and docs publishing.

## Decision
- GitHub Actions. PR workflow: restore, build, unit and BDD tests on ubuntu and windows runners, plus a format/analyzer check.
- AoT publish smoke job on both OSes for PRs touching the app head or plugins.
- Release workflow, triggered by a version tag: full test run including E2E (ADR 0023), AoT publish per OS, Velopack packaging (ADR 0024), GitHub Release with artifacts and notes generated from labeled issues/PRs (ADR 0027), then docs deploy to gh-pages (ADR 0026).
- Version comes from the tag; one source of truth for the .NET version in a shared props file (ADR 0004).
- Dependabot or equivalent for NuGet and Actions updates.
- Secrets are not required for unsigned builds; signing secrets are added only when signing starts.
- Pin third-party actions by version.

## Consequences
- Every release is reproducible from a tag.
- Windows and Linux runner minutes are the main cost; free for public repos.
- E2E runs nightly on Linux and Windows and on release tags; a failing nightly opens a BUG issue.

## Open questions
- Whether macOS gets a build job later.
- Release notes tooling (GitHub native vs custom).
