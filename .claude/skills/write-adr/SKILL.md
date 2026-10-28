---
name: write-adr
description: Create or amend an ADR in docs/adr with the correct index entry.
---
1. Next number = highest in `docs/adr/README.md` + 1. File `NNNN-kebab-title.md`.
2. Sections: Status (Accepted|Proposed|Superseded by NNNN) / Context / Decision / Consequences / optional Open questions. Max ~40 lines.
3. Add one index row: number, title, status, short "Covers". Do not read other ADRs.
