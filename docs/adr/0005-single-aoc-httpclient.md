# 0005 Single AoC HttpClient

## Status
Accepted

## Context
Advent of Code asks automated tools to be considerate: identify yourself, avoid hammering the servers, do not poll, respect submit cooldowns, and cache inputs. The app must comply fully.

## Decision
- All AoC traffic goes through exactly one `HttpClient` wrapper (the AoC gateway). Nothing else may construct an HTTP client for adventofcode.com; an architecture test enforces this.
- The gateway enforces:
  - an identifying User-Agent containing the repo URL (github.com/basmulder03/AoCEditor) and a contact;
  - throttling with a minimum interval between requests and a low concurrency cap;
  - no automated polling or background refresh; every request is caused by a user action;
  - cooldown-aware submits: a submit is refused locally while a known cooldown is active (see 0015);
  - cache first: puzzle text and input are fetched once per day (see 0007, 0008) and never refetched unless the user asks;
  - the session cookie is attached only here (see 0006).
- Failed or 4xx/5xx responses are not retried automatically.
- The MCP server never reaches AoC except via this gateway through app actions (see 0021).

## Consequences
- ToS compliance is one reviewable place.
- Features that "feel" like they need polling (for example leaderboards) are out of scope.
- Tests use a fake handler; no test touches the live site.

## Open questions
- Exact interval and cap values are not decided.
