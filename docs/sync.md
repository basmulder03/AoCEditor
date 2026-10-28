# Calendar sync

`SyncService` (`src/Features/Sync`) runs only on user action; nothing triggers it automatically and nothing polls. All its requests are `RequestKind.Statistics` with `FetchPolicy.Refresh`. The owner's reading of the AoC community guidelines: "Please throttle your requests to at least once every few minutes" for tools scraping statistics. The gateway therefore spaces these requests at least 3 minutes apart (see `docs/gateway.md`), so waiting is expected.

- `SyncYearAsync(year, refresh, progress, ct)`: used when the user opens a year. Fetches only `/{year}`. A year already synced is skipped unless `refresh` is true.
- `SyncAllAsync(refreshYears, progress, ct)`: fetches `/events`, then queues the listed years not yet synced one after another. Years in `refreshYears` are fetched again; no other synced year is refreshed, so past years only refresh on explicit request.

Each year's stars are saved on their own (`ISyncStateStore`, table `sync_state`), so a later failure keeps earlier years. Cancelling works while waiting for the throttle (an `OperationCanceledException` propagates; saved years stay).

## Progress

`SyncProgress(Year, YearsRemaining, SecondsUntilNextRequest)` is reported just before each year's request: the year in progress, the years queued after it and the wait from `IAocGateway.NextAllowedAt(RequestKind.Statistics)`. A UI can keep counting down from that value or query the gateway again.

## Result

`SyncSummary`: `Outcome` (`Completed`, `NotSignedIn`, `RateLimited`, `EventsUnavailable`), `YearsSynced`, `DaysUpdated` (days whose stars differ from the stored value) and `YearsSkipped` (already synced, not fetched) and `Errors` per year (`ParseError`, `NotFound`, `NetworkError`). A year with an error keeps its previously stored stars. `NotSignedIn` and `RateLimited` stop the sync at once.

## Day states

`CalendarRules.StateOf`: `Complete` (2 stars), `Part1Done` (1), otherwise `Locked` before 00:00 UTC-5 on Dec N of that year, else `Open`. Locking is computed from `TimeProvider` and never requested, so every day of a past year is unlocked. The number of days is not hardcoded: it is the number of day links on the synced calendar page (25 in 2024; later years may have fewer). For the running year, days that are still locked may have no link, so the count is only known once they appear.

## Markup facts

Verified on 2026-10-28 against the owner's signed-in account (`/events` and `/2025`, one request each):
- `/events` lists years as `<div class="eventlist-event"><a href="/2024">[2024]</a> <span class="star-count">50*</span> ...</div>`; signed in, each entry carries a star count (not parsed). The newest event links to `/`, so its year is derived from the clock (December means the current year, otherwise the previous one). The parser read 2025 down to 2015 correctly.
- Calendar pages have one `<a aria-label="Day N, two stars" href="/2025/day/N" class="calendar-dayN calendar-verycomplete">` per day, also containing `calendar-mark-complete` and `calendar-mark-verycomplete` spans. 2025 has 12 day links; all were parsed as 2 stars, matching the 24* on `/events`.

Inferred, not seen (no such day in the checked year): one star is class `calendar-complete` with an aria-label ending in "one star"; zero stars has neither class. Based on CSS rules and open-source clients (scarvalhojr/aoc-cli, apexatoll/aoc-cli). Unverified: whether locked days of the running year have a link. The parser ignores the aria-label.

Anything else (no day links, a day outside 1-25, a duplicate day, no event years) is a parse failure for that year, not an exception. Test fixtures are synthetic and contain only the day links.
