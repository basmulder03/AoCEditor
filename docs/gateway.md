# AoC gateway

`AocGateway` (`src/Features/Gateway`) is the only code that talks to adventofcode.com. `scripts/check-practices.sh` fails the build if an HTTP client or the AoC host name appears anywhere else in `src`.

## What the site asks

From https://adventofcode.com/about (checked 2026-10-28): "Please be careful with automated requests; I'm not a massive company, and I can only take so much traffic.  Please be considerate so that everyone gets to play." The page sets no numeric limit and does not require a User-Agent format or an email address; the rules below are our reading of "considerate" plus the owner's reading of the r/adventofcode community automation guidelines (not re-verified against the source; check by hand before release):

- "Please throttle your requests to at least once every few minutes" for tools scraping statistics. We space statistics requests at least 3 minutes apart.
- Inputs are cached after the first download; a manual re-download is allowed if it obeys the throttle.
- The User-Agent must carry the maintainer's contact info plus a URL.

## Rules enforced

| Rule | Where |
| --- | --- |
| One `HttpClient`, no auto-redirect (a login redirect becomes `NotSignedIn`), no cookie container, 30 s timeout | `AocGateway` constructor |
| User-Agent exactly `AoCEditor/<version> (+https://github.com/basmulder03/AoCEditor; maintainer github.com/basmulder03)`; no email is sent | `AocGateway.SendAsync` |
| Session cookie set per request from `ISessionTokenProvider`, never logged; no token means no request | `AocGateway.SendAsync` |
| One request in flight overall. Two request classes (below), each with its own spacing clock; waiting is cancellable and happens outside the in-flight gate, so a long statistics wait does not block an interactive request. Clock is an injectable `TimeProvider` | `AocGateway.AcquireAsync` |
| Cache first: `FetchPolicy.CacheFirst` (default) never touches the network on a hit; `Refresh` is explicit | `AocGateway.GetAsync` |
| Only successful GET responses are cached (files under `IAppDataPaths.Cache`, keyed by SHA-256 of the path, atomic temp-and-move writes) | `FileResponseCache` |
| No automatic retries; no polling | `AocGateway` |

## Request classes

`RequestKind` is an explicit argument of `GetAsync` (no default).

| Kind | Used for | Default spacing | Floor |
| --- | --- | --- | --- |
| `Interactive` | puzzle text, input, submit (`PostFormAsync` is always interactive) | 5 s (`InteractiveInterval`) | 1 s |
| `Statistics` | events list, calendar pages | 3 min (`StatisticsInterval`) | 3 min |

A configured value below its floor is raised to the floor. Cache hits send nothing and do not start a spacing period. `NextAllowedAt(kind)` returns the earliest time a request of that kind may go out (now if it may go at once) so the UI can show a countdown.

## Results

`GetAsync(path, kind, policy, ct)` and `PostFormAsync(path, form, ct)` return `GatewayResult`: `Ok(Content)`, `NotSignedIn` (3xx, 400, 401, 403 or no token), `RateLimited` (429), `NotFound` (404), `NetworkError` (anything else, including 5xx and timeouts). Parsing is left to feature slices. `IAocGateway`, `FetchPolicy` and `GatewayResult` live in `Shared.Contracts` so other slices can use the gateway without referencing it.

## Sign-in support

- `CheckTokenAsync(token)` sends one uncached interactive request for `/settings` with a token that is not stored yet. A rejected token is `NotSignedIn`.
- When a normal request ends in `NotSignedIn` (including "no token"), the gateway calls `ISessionEvents.NotifySignInRequired()`. Prompts are at least `SignInPromptInterval` (30 s) apart, so a burst of failures raises one prompt. Token checks never raise it.

## Wiring

`AddAocEditor` (`src/App/AppServiceRegistration.cs`) registers `AocGateway` as a singleton, built by an explicit factory with `new FileResponseCache(paths.Cache)`, the shared `SessionEvents` and the request log. `IAocGateway` resolves to the same instance, so the spacing clocks and the in-flight gate are shared. The container disposes it on exit.
