# 0001 Record architecture decisions

## Status
Accepted

## Context
Decisions must survive across sessions and be cheap for humans and agents to load. Agents have limited context, so reading every decision for every task wastes tokens.

## Decision
- One Markdown file per decision in `docs/adr/`, named `NNNN-kebab-title.md`, numbered sequentially, never renumbered.
- Sections: Status, Context, Decision, Consequences, optional Open questions. Max about 40 lines.
- `docs/adr/README.md` is the index (number, title, status, what it covers). Load it first; open a single ADR only when its row is relevant.
- Status values: `Accepted` (decided with the owner) or `Proposed` (team default awaiting owner review).
- A changed decision gets a new ADR; the old one is marked `Superseded by NNNN`.
- Unverified facts (library choices, APIs) are listed under Open questions, not stated as fact.
- Code comments never reference ADRs (see 0028).

## Consequences
- Agents pay for one small index per task instead of the whole record.
- The index must be updated in the same change as any ADR.
- Proposed ADRs can be built against, but the owner may reverse them.
