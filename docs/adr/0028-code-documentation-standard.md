# 0028 Code documentation standard

## Status
Accepted

## Context
Comments rot. Decisions are recorded in ADRs and docs, so code should not duplicate or point at them.

## Decision
- Comments are sparse. Prefer clear names and small methods over comments.
- A comment describes what the code does or a non-obvious constraint of the functionality (for example a protocol quirk or a ToS limit being enforced).
- Comments never reference ADRs, issue numbers, or decision history; that belongs in the ADR, the issue and the commit.
- No commented-out code, no change-log comments, no TODO without an issue (ADR 0027).
- Public API of shared libraries and plugin contracts gets short XML doc summaries; internal code does not need them.
- Reviews and agents (ADR 0029) reject comments that restate the code or cite ADRs.

## Consequences
- Less noise to read and maintain.
- Rationale must be found through git blame, ADRs and docs, not inline.
