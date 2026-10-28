---
name: aoc-tos
description: Rules every AoC-facing change must respect. Load before touching networking, sync, submit or caching code.
---
- One HttpClient (the AoC gateway) for all AoC traffic; nothing else may call adventofcode.com.
- Identifying User-Agent with the repo URL and owner contact. Throttle requests; honour submit cooldowns shown by AoC.
- Never poll or auto-refresh. Sync is user-triggered. Puzzle text and input are fetched once, on first open, then cached.
- Never publish inputs, puzzle text or private-leaderboard data: they live in app-data, never in the solutions repo.
- Never automate leaderboard races; MCP/automation may not call AoC directly.
Verified wording (page source and /about, Oct 2026): "Please be careful with automated requests; I'm not a massive company, and I can only take so much traffic. Please be considerate so that everyone gets to play." No numeric rate limit and no User-Agent requirement are published. /about also asks not to include puzzle text or inputs in repositories, not to make a site look like or be named similar to Advent of Code, and says AI should not be used to solve puzzles. The global leaderboard no longer exists and events run 12 days.
Community automation guidelines (r/adventofcode wiki, pasted by the owner Oct 2026): you are responsible for your tool and you get banned if it misbehaves. Scrapers of statistics must throttle to "at least once every few minutes"; the private leaderboard JSON at most every 15 minutes; inputs never change, so cache them after the first download (a manual re-download is fine within the throttle); the User-Agent must contain the maintainer's contact info, ideally with a URL. Our README states compliance as the wiki suggests.
Implemented as: Interactive requests at least 5 s apart, Statistics requests at least 3 min apart (floor), one in flight, UA `AoCEditor/<ver> (+repo; maintainer github.com/basmulder03)`.
