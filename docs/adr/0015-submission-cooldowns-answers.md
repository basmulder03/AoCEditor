# 0015 Submission, cooldowns, answers

## Status
Proposed

## Context
AoC rate-limits wrong answers (a wait before the next attempt) and its automation rules forbid hammering. Submits must go through the single HttpClient (0005). After a correct answer the app should know the answer for local verification (0018).

## Decision
- Submit is always a user click; never automatic, never retried by the app.
- A Submission slice stores per day/part: each attempt (answer, time, outcome: correct / wrong / too high / too low / rate-limited), and the cooldown end time parsed from the AoC response.
- The submit button is disabled with a live countdown while a cooldown is active, and also while a request is in flight. The cooldown is persisted across restarts.
- Obviously repeated answers (already tried wrong) are blocked locally with a warning, without a request.
- On first correct result, the answer is stored as the known answer for that day/part in the local DB (0007). It is not committed to the solutions repo (0017).
- Response parsing is isolated behind one parser with fixture-based tests; unrecognised responses are shown raw and treated as non-final.

## Consequences
- Fewer wasted or rate-limited requests; wrong-answer history is available to the user.
- Parser depends on AoC page wording, which can change; fixtures need upkeep.
- Known answers are plain text in app data; they are personal, not secret.

## Open questions
- Exact AoC response phrases and cooldown formats must be verified against real responses.
