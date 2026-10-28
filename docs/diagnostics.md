# Diagnostics

The `Diagnostics` slice (`src/Features/Diagnostics`) owns logging. Other slices depend only on `Microsoft.Extensions.Logging.Abstractions` and log through `[LoggerMessage]` source-generated methods (no reflection, AoT safe).

## Sinks

Every log call goes through `DiagnosticsLoggerProvider`, which formats the message, **redacts it** and only then hands it to the sinks:

- Rolling file `<app data>/logs/aoceditor.log` (`FileLogSink`). It rolls at 2 MB, keeps at most 5 files and deletes files older than 7 days (constants in `DiagnosticsServices`).
- In-memory ring buffer of the last 1000 entries (`LogBuffer`), which feeds the Debug page.
- Console (stderr), only while debug mode is on.

A failing sink (full disk) never breaks the app.

## Redaction

`Redactor` removes before anything is stored or shown: values of `session=`, `token=` and `cookie=` pairs, whole `Cookie`, `Set-Cookie` and `Authorization` header lines, JSON `"session"`/`"token"`/`"cookie"` values, hex strings of 32 or more characters, and the home folder (replaced by `~`, either slash style). Gateway requests are logged as method, path, status and timing only. Sign-in navigation is logged as host and path only (no query or fragment).

Never pass a token, cookie or session value to a logging call. `scripts/check-practices.sh` flags logging calls and `[LoggerMessage]` parameters whose names mention token, cookie, secret, password or session.

## Debug mode

Turned on by the `--debug` flag, `AOCEDITOR_DEBUG=1` (or `true`), or the **Debug mode** check box in Settings (stored in the `kv` table through `IKeyValueStore`). It lowers the minimum level from Information to Debug, prints to the console and adds the **Debug** entry to the navigation list. Toggling takes effect immediately. When the flag or variable forces it, the check box is disabled.

## Debug page

- Log list, newest first, with a minimum-level filter, a source filter (the slice name) and **Copy log** (redacted).
- Recent gateway requests: method, path, status, duration and throttle wait (`IRequestLog`, fed by the gateway; never headers, bodies or tokens).
- Environment: app, OS and runtime versions, session state (signed in or out only), database path, schema version and migrations applied.
- **Copy diagnostics** shows a redacted preview first; the clipboard is only written after **Copy to clipboard**.

## Failure reasons

Failure results carry a reason (`GatewayResult.NetworkError.Reason`, `SignInResult.NetworkError.Reason`, `BrowserUnsupported.Reason`, `StoreUnavailable.Reason`, `YearError.Reason`). The reason is logged and `SignInResultText` shows it in Settings.

## Log files

Find them under the app-data folder in `logs`. They contain only redacted text, but read them before attaching them to an issue. Nothing is uploaded.

## Not yet

Tracing (`ActivitySource`) and metrics (`Meter`) are not implemented.
