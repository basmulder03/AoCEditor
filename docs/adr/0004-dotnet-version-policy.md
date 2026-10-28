# 0004 .NET version policy

## Status
Accepted

## Context
The app targets Native AoT. New .NET releases appear yearly and the project should follow them without a migration project each time.

## Decision
- Target `net10.0` now.
- The target framework is defined once in `Directory.Build.props` (single property); bumping to net11/net12 is a one-line change plus fixing build errors.
- SDK pinned via `global.json`; `rollForward` set so patch updates are accepted.
- AoT constraints: `PublishAot` for the app, trim and AoT analyzers enabled as errors for the app and libraries it uses; no runtime assembly loading, no `System.Reflection.Emit`, JSON via source generators only.
- Latest language version is allowed; preview features are not.
- Package versions managed centrally (`Directory.Packages.props`).

## Consequences
- Upgrades are cheap, and CI can build against the next version in a non-blocking job.
- Libraries that are not AoT-safe cannot be used, even if convenient.
- Contributors need the pinned SDK installed.

## Open questions
- Whether an LTS-only policy (net10, net12) should replace "every version" is not decided.
