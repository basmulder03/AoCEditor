---
name: aoc-tos
description: Rules every AoC-facing change must respect. Load before touching networking, sync, submit or caching code.
---
- One HttpClient (the AoC gateway) for all AoC traffic; nothing else may call adventofcode.com.
- Identifying User-Agent with the repo URL and owner contact. Throttle requests; honour submit cooldowns shown by AoC.
- Never poll or auto-refresh. Sync is user-triggered. Puzzle text and input are fetched once, on first open, then cached.
- Never publish inputs, puzzle text or private-leaderboard data: they live in app-data, never in the solutions repo.
- Never automate leaderboard races; MCP/automation may not call AoC directly.
Verify specifics against https://adventofcode.com/about and /r/adventofcode automation guidance before relying on them.
