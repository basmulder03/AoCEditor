# Signing in

`SignInCoordinator` (`src/Features/Session`) obtains a session token, proves it and only then stores it.

1. If secure storage is unavailable the result is `StoreUnavailable(reason)` and nothing else happens (no fallback storage).
2. The token comes from an `ISignInBrowser` or from the user's paste. A pasted token is trimmed and stripped of quotes and a `session=` prefix; anything that is not a plain cookie value is `InvalidToken` without a request.
3. One interactive request through the gateway (`CheckTokenAsync`, page `/settings`) checks it. A rejected token is `InvalidToken` and is never saved; other failures are `NetworkError`.
4. A good token is saved through `SessionTokenProvider`. Result: `SignedIn`, `Cancelled`, `InvalidToken`, `StoreUnavailable`, `NetworkError` or `BrowserUnsupported`.

Tokens never appear in results, `ToString` output or logs. Every WebView navigation (host and path), candidate verdict and outcome with its reason is logged; see [Diagnostics](diagnostics.md).

## Embedded browser

`WebViewSignInBrowser` (`Session/UI`) hosts Avalonia's `NativeWebView` in our own `SignInWindow` instead of `NativeWebDialog`: the dialog has no owner, status line or Cancel button, and could not be centred on the app window. The window is owned by the shell, opens centred on it, stays on the owner's screen and fully inside that screen's working area (`SignInWindowPlacement`, 520x720 by default, never larger than the working area, minimum 360x480), and can be moved and resized. The browser stretches to fill it. A status line and an always-visible Cancel sit below the page.

Before the site is loaded, the window opens on a blank page and checks that the browser gives access to its cookie store (`TryGetCookieManager`). Where it does not, the outcome is `Unsupported` with the reason `The embedded browser on this system does not give access to its cookies...`, nothing is requested from the site, and Settings shows that reason together with the paste form. On Linux with libwebkit2gtk-4.1 the store is not exposed (the adapter looks for the WebKitGTK 6 network session), so there the guided paste flow is the way to sign in; the same limit applied to the earlier `NativeWebDialog` variant, which only appeared to work because it never read cookies on the login hops.

The window waits until the user is really signed in. After every finished navigation the HttpOnly `session` cookie is read again; `SignInPage.Candidate` turns (URL, finished, cookie) into a candidate only when the page is on the site, outside `/auth/` and a cookie exists. A candidate goes through the coordinator to the gateway (`CheckTokenAsync`). Only an accepted token closes the window. A rejected one (for example a stale cookie from the profile) is deleted, the status line says so and the login page is shown again; an unreachable site keeps the window open with a note. Closing the window or pressing Cancel yields `Cancelled`; after 10 minutes (`SignInCoordinator.DefaultBrowserTimeout`) the result is `TimedOut`. Both, and a missing WebView runtime (`Unsupported`), make Settings show the paste flow.

After each finished page a script adds `<meta name="viewport" content="width=device-width, initial-scale=1">` when the page has none (`NativeWebView.InvokeScript`); DPI scaling is left to Avalonia and the placement helper converts between pixels and DIPs with the screen's scaling.

The browser profile (cookies, remembered provider logins) lives in `<app data>/webview` (WebKitGTK base data directory, WebView2 user data folder) and is deleted on sign out.

Linux needs libwebkit2gtk-4.1, libsoup3 and GTK3 (X11 or XWayland); Windows needs the WebView2 runtime.

## Guided paste sign-in (primary)

Settings, Account shows the guided paste flow while nobody is signed in: four numbered steps (sign in at the site, open the browser's developer tools, copy the value of the cookie named `session`, paste it), an "Open Advent of Code in your browser" button (the login URL, built from the gateway base address, opened through Avalonia's launcher, no shell), a masked box with a Show toggle, "Validate and save", a status message and a note on storage. Failures have their own message: invalid token, network error (with reason) and secure storage unavailable (with reason). The token is never logged or echoed; the box is cleared after each attempt.

"Try browser sign-in" (labelled experimental on Linux) is a secondary button and is never started automatically. When it returns `Unsupported` the reason is shown next to the still-visible paste flow. After a successful save the flow is replaced by the signed-in state and a Sign out button.

## Unverified

Windows, OAuth providers inside the embedded browser (some, such as Google, block embedded browsers; use the paste fallback), and the sign-in window on a real login (placement on a second monitor, page fit) were not exercised by tests; only the pure decision, placement and coordinator logic are.
