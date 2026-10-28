# Diagnostics

The `Diagnostics` slice (`src/Core/Diagnostics`, sinks and wiring in `src/Infrastructure/Diagnostics`) owns logging. Other slices depend only on `Microsoft.Extensions.Logging.Abstractions` and log through `[LoggerMessage]` source-generated methods (no reflection, AoT safe).

## Providers

Logging uses standard `ILoggerProvider`s, registered by `AddDiagnostics` (`src/App/DiagnosticsRegistration.cs`). Every provider is wrapped in `RedactingLoggerProvider`, which formats the message, appends the exception text, **redacts the whole string** and only then hands it on, so no provider ever sees unredacted text. A test builds the real service provider and fails if a registered provider is not wrapped. A failing provider (full disk) never breaks the app or the other providers.

- Rolling file `<app data>/logs/aoceditor.log` (`FileLoggerProvider`). It rolls at 2 MB, keeps at most 5 files and deletes files older than 7 days (constants in `DiagnosticsRegistration`).
- In-memory ring buffer of the last 1000 entries (`BufferLoggerProvider`), which feeds the Debug page.
- Console (stderr) through the built-in `Microsoft.Extensions.Logging.Console` simple formatter (single line, `HH:mm:ss.fff`), only while debug mode is on (`ConsoleLogging`). It is AoT clean.

The minimum level is a filter callback that reads debug mode on every call: Information normally, Debug while debug mode is on.

## Getting a logger

Every class that logs takes a required `ILogger<T>` (T = the class) constructor parameter, resolved from the container by `AppServiceRegistration`. There is no optional logger and no `NullLogger` fallback in `src/`; `scripts/check-practices.sh` flags `ILogger? ` and `NullLogger.Instance`. Tests pass `NullLogger<T>.Instance` or a capturing `ILogger<T>`. The category is the full type name, and the log source shown on the Debug page is its slice segment (`AoCEditor.Core.Sync.Application.SyncService` gives `Sync`). `Migrator` is static, so it takes a plain `ILogger` from its caller and logs under the caller's category.

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
