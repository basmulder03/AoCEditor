# 0028 Code documentation standard

## Status
Accepted

## Context
Comments rot. Decisions are recorded in ADRs and docs, so code should not duplicate or point at them. Existing XML docs add background that is not needed and omit the parameter and return details a caller does need.

## Decision
- Comments are sparse. Prefer clear names and small methods over comments.
- Every public and protected type and member of Shared, Core and plugin-facing contracts gets XML docs that describe the functionality:
  - `<summary>`: one sentence saying what it does.
  - `<param>` for every parameter: meaning, units, accepted range, null/empty behaviour.
  - `<returns>`: what comes back, including the "nothing" cases (null, empty, false).
  - `<exception>` for the exceptions it deliberately throws.
  - `<remarks>` only for behaviour a caller must know: ordering, threading, cancellation, side effects.
- XML docs never carry background, history, rationale, design discussion, ADR or issue references, or a restatement of how it is implemented. Rationale lives in ADRs and docs.
- Only a non-obvious constraint of the code itself (a protocol quirk, a limit being enforced) stays as a short inline `//` comment.
- Internal and private members get a comment only when the name cannot carry the meaning.
- No commented-out code, no change-log comments, no TODO without an issue (ADR 0027). Comments never reference ADRs, issue numbers or decision history.
- Enforcement: `GenerateDocumentationFile` with CS1591 (missing XML comment on publicly visible member), CS1573 (missing param tag) and CS1572/CS1571 (param tag mismatch or duplicate) as errors for the Core, Shared and Infrastructure public surface. Until the existing code is cleaned up they are warnings tracked by an issue; then they are raised to errors.
- Reviews and agents (ADR 0029) reject context-heavy docs, comments that restate the code, and comments that cite ADRs.

## Consequences
- XML docs read as a reference of behaviour and double as the generated API docs.
- Less noise to read and maintain; rationale is found through ADRs, docs and git blame, not inline.
- Existing docs must be rewritten before the warnings can become errors.
