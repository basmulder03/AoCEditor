# 0033 Security practices

## Status
Accepted

## Context
The app handles a login token, runs user-supplied code, loads language plugins, exposes an MCP server and updates itself. Generic reviews miss these boundaries.

## Decision
- A written threat model (`docs/threat-model.md`) lists assets, threats and controls and is updated when a slice adds a trust boundary.
- A project `security-reviewer` agent (read-only) and the `security-checklist` skill review changes to the gateway, session, runner, plugins, MCP, updater, logging and file handling, and run before every release. Findings become issues labelled SECURITY with a MoSCoW label.
- Automation on the public repo: CodeQL for C#, dependency review on pull requests, Dependabot updates and alerts, secret scanning with push protection, private vulnerability reporting, and `SECURITY.md`.
- `scripts/check-practices.sh` enforces mechanical rules (no host name outside the gateway, no secret-named values in logging, no blocking, no reflection).
- Actions are pinned to full released versions.

## Consequences
- Security review is a repeatable step instead of an ad hoc one.
- CodeQL and dependency review add CI time and may need triage.
- The agent reviews; it does not replace a human decision on severity.
