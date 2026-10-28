# Signing in

`SignInCoordinator` (`src/Features/Session`) obtains a session token, proves it and only then stores it.

1. If secure storage is unavailable the result is `StoreUnavailable(reason)` and nothing else happens (no fallback storage).
2. The token comes from an `ISignInBrowser` or from the user's paste. A pasted token is trimmed and stripped of quotes and a `session=` prefix; anything that is not a plain cookie value is `InvalidToken` without a request.
3. One interactive request through the gateway (`CheckTokenAsync`, page `/settings`) checks it. A rejected token is `InvalidToken` and is never saved; other failures are `NetworkError`.
4. A good token is saved through `SessionTokenProvider`. Result: `SignedIn`, `Cancelled`, `InvalidToken`, `StoreUnavailable`, `NetworkError` or `BrowserUnsupported`.

Tokens never appear in results, `ToString` output or logs.

## Embedded browser

`WebViewSignInBrowser` (`Session/UI`) opens the site's login page in Avalonia's `NativeWebDialog`. After a navigation back to the site outside `/auth/` it reads the HttpOnly `session` cookie from the dialog's cookie manager and closes the dialog. Closing the dialog yields `Cancelled`; a missing WebView runtime yields `Unsupported`, and the Settings page then shows the paste form.

The browser profile (cookies, remembered provider logins) lives in `<app data>/webview` (WebKitGTK base data directory, WebView2 user data folder) and is deleted on sign out.

Linux needs libwebkit2gtk-4.1, libsoup3 and GTK3 (X11 or XWayland); Windows needs the WebView2 runtime.

## Paste fallback

Sign in at the site in a normal browser, open the developer tools (F12), Application or Storage, Cookies, and copy the value of the cookie named `session`. Paste it in Settings, Account.

## Sign-in prompt

`ISessionEvents.SignInRequired` makes the shell show a non-blocking bar with a Sign in button, which opens Settings and starts the browser sign-in. It hides after a successful sign-in or on Dismiss.

## Unverified

Windows, OAuth providers inside the embedded browser (some, such as Google, block embedded browsers; use the paste fallback), and the browser dialog itself on a real login were not exercised.
