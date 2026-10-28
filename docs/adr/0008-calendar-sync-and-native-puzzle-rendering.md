# 0008 Calendar sync and native puzzle rendering

## Status
Proposed

## Context
The home view shows a calendar per year the event has run, 25 days each, with stars and locked days. Day pages show the puzzle next to the editor. Background refresh would violate 0005.

## Decision
- Initial sync is manual: a user-pressed "Sync" fetches star counts per year through the gateway (0005). No automatic refresh.
- Locked days (not yet unlocked by date, 00:00 UTC-5) are computed locally and are never requested.
- Puzzle text and input are downloaded the first time a day is opened, then served from the cache (0007). Part 2 text is re-fetched only after part 1 is answered correctly, or on explicit user refresh.
- Puzzle HTML is parsed into a small document model (paragraph, heading, list, code block, inline code/emphasis/link) and rendered with native Avalonia controls, one tab per part plus an input tab.
- Unknown HTML elements degrade to plain text rather than failing.
- Links open in the system browser after confirmation.

## Consequences
- Offline use works for every day already opened.
- Rendering fidelity is limited to the supported element set; AoC's occasional ASCII art relies on monospace blocks.
- Parser needs fixtures built from saved sample HTML kept out of the repo if it is puzzle text (see 0007); synthetic fixtures are used instead.

## Open questions
- HTML parser library (AoT-safe) is unverified; a hand-written parser may be needed.
- How to detect "locked" vs "unlocked but not yet synced" for the current year is not designed.
