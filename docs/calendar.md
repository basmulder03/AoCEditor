# Year calendar

The Calendar page (`src/Features/Sync/UI`, hosted by `src/App/CalendarPage`) shows one year at a time. Opening it reads only the local database; AoC is contacted solely when the user presses a sync button.

## Years

The year list is newest first and the newest year is selected on start. It spans 2015 up to the newest event year (derived from the clock, see `CalendarRules.LatestEventYear`) plus any synced year, so a year that was never synced is still reachable. Pick a year in the Year drop-down (keyboard operable, announced as "Year").

## Day tiles

One tile per day found in the synced data (12 for 2025, never a fixed 25). Each tile shows the day number, an icon and a text label, so state is never conveyed by colour alone:

| State | Label | Icon |
|---|---|---|
| Locked | Locked | padlock |
| Open | Open | ring |
| Part 1 done | One star | one star |
| Complete | Two stars | two stars |

Locked is computed locally from `CalendarRules` and the clock; past years have no locked days. The accessible name reads for example "Day 5, two stars" or "Day 12, locked, unlocks Dec 12 at 05:00 UTC", and doubles as the tooltip. Arrow keys move between tiles (five per row); Enter, Space or a click on an unlocked tile raises `DaySelected(year, day)` (the app shows a "coming soon" page for now). Locked tiles are focusable and announced but cannot be selected.

A year never synced shows "Not synced yet" and no tiles. Per-year totals ("3 of 50 stars") and the last sync time are shown above the grid.

## Sync controls

- Sync this year: refetches the selected year.
- Sync all years: fetches the event list, then every year not yet synced plus the newest year.
- Cancel: stops the run; years already saved stay.

While syncing, the page shows the year in progress, how many years are queued and a countdown to the next request, updated once a second from the clock (`TimeProvider`) starting at `SyncProgress.SecondsUntilNextRequest`. Afterwards it shows the summary and one line per failed year. Not signed in shows a message and an "Open account settings" button that navigates to Settings (the shell also shows its sign-in prompt).

## Design

Icons live in `src/UiKit/Icons/Icons.axaml`, tile sizes and `IconSize` in `Tokens/Metrics.axaml`; tile colours reuse existing tokens only.
