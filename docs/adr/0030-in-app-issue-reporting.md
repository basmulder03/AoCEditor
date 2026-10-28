# 0030 In-app issue reporting

## Status
Accepted

## Context
When something breaks during use, the owner wants a one-click way to file a GitHub issue with useful details, without leaking personal data.

## Decision
- A "Report a problem" button opens a preview dialog, then opens the pre-filled new-issue page of the repo in the browser (title, body, BUG + SHOULD default labels by template). The app never posts on its own; the user reviews and submits.
- Prefilled data: app version, OS and architecture, language plugin and toolchain versions, last error message and stack trace.
- Data minimisation: no session token, no usernames or paths (home folder replaced with `~`), no puzzle text, no input, no answers, no solution source. Optional extras (log tail) are opt-in checkboxes, off by default, and shown in the preview.
- No telemetry and no automatic upload. Nothing leaves the machine before the user submits.

## Consequences
- GDPR-friendly by design: no collection, explicit consent per report.
- Reports are less rich than automatic crash upload; the preview keeps users in control.
- A redaction unit test suite is required (paths, tokens, input content).
