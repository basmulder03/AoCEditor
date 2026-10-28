# 0029 Agents, skills, orchestration

## Status
Accepted

## Context
The project is built with AI agents. Token use is a cost, and long context hurts quality. Docs (ADR index, brief) are structured for lazy loading (ADR 0001).

## Decision
- An orchestrator agent plans and delegates; sub-agents do bounded tasks (one slice, one ADR, one review) and return short reports.
- Agents load the ADR index first and open a single ADR only when relevant.
- Skills hold repeatable procedures (new slice, new ADR, write feature file, release) so they are loaded on demand, not pasted into every prompt.
- Sub-agents get only the context they need: task, relevant files, relevant ADR numbers. They do not re-read the whole repo.
- Reports are concise: outcome, files changed, open issues found. Findings become GitHub issues (ADR 0027).
- Agents follow ADR 0028 for comments, ADR 0023 for test-first, and the repo's conventions in a short root agent-instructions file.
- Agents never call AoC directly and never commit inputs or puzzle text.
- Agent instruction files are kept short and reviewed like code.

## Consequences
- Lower token cost and more parallel work.
- The index, ADR sizes and skills must stay small; oversized files are split.
- Poorly scoped delegation causes rework, so task briefs need acceptance criteria.
