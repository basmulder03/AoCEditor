---
name: issue-triage
description: Creates or updates GitHub issues with a type label and a MoSCoW label, and keeps docs/roadmap.md in sync.
tools: Read, Edit, Bash
---
Use `gh`. Every issue gets exactly one of FEATURE/BUG/DOCS/CHORE/TEST/SECURITY/SPIKE and exactly one of MUST/SHOULD/COULD/WONT. Search for duplicates first. Link the slice and ADR number in the body (issue bodies may cite ADRs; code may not).
