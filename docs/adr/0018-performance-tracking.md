# 0018 Performance tracking

## Status
Accepted

## Context
After the first correct answer the app knows the expected answer (0015) and can verify locally. The owner wants solution performance tracked over time.

## Decision
- Each normal run (not debug) records: year, day, part, language, solution revision (git hash if available, else file hash), wall time, peak memory, exit status, and whether the output matched the known answer.
- Timing is taken by the Runner outside the user process (0011), so it includes process start; the CPU time and the language's startup are reported separately where available.
- Results are stored in the local DB (0007). Data stays local.
- Local verification: when a known answer exists, a run shows pass/fail without any AoC request.
- Regression view per day: timeline of runs, flagging a slowdown over a configurable threshold against the best or previous run. Machine noise is handled by keeping the median of the last N runs, N configurable.
- Data feeds the stats template (0019).

## Consequences
- Timings are comparable only on the same machine; the view says so.
- Small DB growth; old rows can be pruned by setting.
- Sandbox overhead (0011) is part of every measurement and must be stable.

## Open questions
- Whether to provide an explicit benchmark mode (repeated runs, warm-up) later.
