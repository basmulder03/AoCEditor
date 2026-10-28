# AoC gateway

`AocGateway` (`src/Features/Gateway`) is the only code that talks to adventofcode.com. `scripts/check-practices.sh` fails the build if an HTTP client or the AoC host name appears anywhere else in `src`.

## What the site asks

From https://adventofcode.com/about (checked 2026-10-28): "Please be careful with automated requests; I'm not a massive company, and I can only take so much traffic.  Please be considerate so that everyone gets to play." The page sets no numeric limit and does not require a User-Agent format or an email address; the rules below are our reading of "considerate".

## Rules enforced

| Rule | Where |
| --- | --- |
| One `HttpClient`, no auto-redirect (a login redirect becomes `NotSignedIn`), no cookie container, 30 s timeout | `AocGateway` constructor |
| User-Agent `AoCEditor/<version> (+https://github.com/basmulder03/AoCEditor)`; no email is sent | `AocGateway.SendAsync` |
| Session cookie set per request from `ISessionTokenProvider`, never logged; no token means no request | `AocGateway.SendAsync` |
| One request in flight, at least `GatewayOptions.MinInterval` (5 s) between requests, cancellable, clock is an injectable `TimeProvider` | `AocGateway.SendAsync` / `WaitForSpacingAsync` |
| Cache first: `FetchPolicy.CacheFirst` (default) never touches the network on a hit; `Refresh` is explicit | `AocGateway.GetAsync` |
| Only successful GET responses are cached (files under `IAppDataPaths.Cache`, keyed by SHA-256 of the path, atomic temp-and-move writes) | `FileResponseCache` |
| No automatic retries; no polling | `AocGateway` |

## Results

`GetAsync(path, policy, ct)` and `PostFormAsync(path, form, ct)` return `GatewayResult`: `Ok(Content)`, `NotSignedIn` (3xx, 400, 401, 403 or no token), `RateLimited` (429), `NotFound` (404), `NetworkError` (anything else, including 5xx and timeouts). Parsing is left to feature slices. `IAocGateway`, `FetchPolicy` and `GatewayResult` live in `Shared.Contracts` so other slices can use the gateway without referencing it.

## Wiring

The app does not use dependency injection yet, so there is no registration extension: construct `new AocGateway(tokens, new FileResponseCache(paths.Cache))` once and share it.
