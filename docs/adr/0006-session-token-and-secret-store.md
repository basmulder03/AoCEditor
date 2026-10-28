# 0006 Session token and secret store

## Status
Accepted

## Context
AoC has no API keys. Requests authenticate with the `session` cookie, obtained by signing in through one of the third-party providers on the AoC site. The app must not handle provider passwords.

## Decision
- When the stored token is missing or rejected (AoC redirects or returns an auth failure), the app opens the AoC sign-in page in a `NativeWebView` (see 0002).
- After the user signs in, the app reads the `session` cookie from the WebView cookie store, validates it through the AoC gateway (0005), then closes the WebView.
- The token is stored in the OS secret store: libsecret on Linux, Credential Manager on Windows. Never in the app-data DB, config, logs, or the solutions repo.
- The token is held in memory only as long as needed and is never logged or sent to plugins, the MCP server, or the sandbox environment.
- User can sign out, which deletes the stored secret and clears the WebView profile.

## Consequences
- The app never sees provider credentials, only the AoC cookie.
- Linux requires a running Secret Service (GNOME Keyring or KWallet); headless setups need a fallback.
- Cookie read depends on the WebView API exposing cookies on each platform.

## Open questions
- Which library wraps libsecret / Credential Manager under AoT is unverified.
- Whether Avalonia `NativeWebView` exposes cookie access on both platforms is unverified.
- Behaviour when no Secret Service is available (refuse vs encrypted file) is not decided.
