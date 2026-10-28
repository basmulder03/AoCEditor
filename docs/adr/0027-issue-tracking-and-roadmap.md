# 0027 Issue tracking and roadmap

## Status
Accepted

## Context
Work is split into vertical slices (ADR 0003). The owner wants every slice, bug and finding tracked on GitHub.

## Decision
- One GitHub issue per slice or task.
- Type labels: FEATURE, BUG, DOCS, CHORE, TEST, SECURITY, SPIKE. Every issue has exactly one.
- Priority labels (MoSCoW): MUST, SHOULD, COULD, WONT. Every issue has exactly one.
- When anything is found broken or missing during work, a new issue is opened instead of fixing it silently or leaving a TODO.
- PRs reference the issue they close.
- A roadmap file in the repo (under /docs) lists milestones and links issues; it is updated when scope changes.
- A SPIKE issue is time-boxed and ends with a written result, and an ADR if it changes a decision.
- Release notes (ADR 0025) group issues by type label.

## Consequences
- Label discipline is needed; a CI check or issue template can enforce it later.
- The roadmap file can lag the board; the issues are the source of truth.

## Open questions
- Whether to use a GitHub Project board in addition to labels and milestones.
