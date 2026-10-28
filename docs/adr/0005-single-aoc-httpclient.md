# 0005 Single AoC HttpClient

## Status
Accepted

## Context
Advent of Code asks automated tools to be considerate: identify yourself, avoid hammering the servers, do not poll, respect submit cooldowns, and cache inputs. The app must comply fully.

## Decision
- All AoC traffic goes through exactly one `HttpClient` wrapper (the AoC gateway). Nothing else may construct an HTTP client for adventofcode.com; an architecture test enforces this.
- The gateway enforces:
  - an identifying User-Agent with the maintainer contact and repo URL: `AoCEditor/<version> (+https://github.com/basmulder03/AoCEditor; maintainer github.com/basmulder03)`;
  - throttling with one request in flight and two request classes: Interactive (puzzle, input, submit; at least 5 s apart) and Statistics (events list and calendar pages; at least 3 minutes apart, a floor that configuration cannot lower), following AoC's community guidance to throttle statistics scraping to once every few minutes;
  - no automated polling or background refresh; every request is caused by a user action;
  - cooldown-aware submits: a submit is refused locally while a known cooldown is active (see 0015);
  - cache first: puzzle text and input are fetched once per day (see 0007, 0008) and never refetched unless the user asks; a manual re-download obeys the same throttle;
  - the session cookie is attached only here (see 0006).
- Failed or 4xx/5xx responses are not retried automatically.
- The MCP server never reaches AoC except via this gateway through app actions (see 0021).

## Consequences
- ToS compliance is one reviewable place.
- Features that "feel" like they need polling (for example leaderboards) are out of scope.
- Tests use a fake handler; no test touches the live site.

## Open questions
- The community guidance is quoted in the `aoc-tos` skill. The maintainer contact is the GitHub handle, not an email (owner decision); AoC may ask for more.
