# 0032 Diagnostics and observability

## Status
Proposed

## Context
A sign-in window that closes instantly could not be explained because the app records nothing and hides failure reasons. The owner wants monitoring and tracing built in, plus a debug mode and view.

## Decision
- All logging goes through `Microsoft.Extensions.Logging` with `[LoggerMessage]` source generation (AoT-safe, no reflection). Other slices depend only on the abstraction.
- The hand-written sink layer (ILogSink, DiagnosticsLoggerProvider fan-out, ConsoleLogSink, DiagnosticsServices) is replaced by plain `ILoggerProvider`s on a normal `LoggerFactory`: the built-in console provider (only while debug mode is on), `FileLoggerProvider` (rolling files under `app-data/logs`, size and age capped) and `BufferLoggerProvider` (in-memory ring buffer for the debug view).
- Every class that logs takes a required `ILogger<T>` constructor parameter (T = the class itself), resolved by dependency injection. There is no optional logger parameter and no `NullLogger` fallback in production code; tests pass `NullLogger<T>.Instance` or a capturing logger. The log category is therefore the full type name, and the slice shown in the debug view is derived from its namespace. `scripts/check-practices.sh` enforces this.
- Every provider is wrapped by one `RedactingLoggerProvider`, so redaction is structural. A test fails if an unwrapped provider is registered.
- The minimum level follows debug mode through a runtime filter callback. `Redactor` and the gateway request history (`GatewayRequestHistory`) stay app-specific.
- Tracing and metrics use `System.Diagnostics` (`ActivitySource`, `Meter`) with one source per slice. Spans cover user actions and each gateway request; counters cover requests, cache hits, throttle waits and sign-in outcomes. OpenTelemetry export is a later option, off by default.
- Redaction is applied before any sink: session tokens and cookies, the home folder path (`~`), puzzle text, inputs and answers are never logged. Logging a request records method, path, status and timing only, never headers or bodies.
- Debug mode: setting, `--debug` flag or `AOCEDITOR_DEBUG=1`. It lowers the minimum level to Debug and enables the Debug page: live logs with level and source filters, recent gateway requests and throttle waits, session state (signed in or out, never the token), storage and migration status, app, OS and runtime versions, and "copy diagnostics" (redacted, preview first).
- Failure results always carry a reason that reaches both the log and the UI message.
- No telemetry leaves the machine (0030).

## Consequences
- Problems like silent window closes become diagnosable from the log.
- Every slice gains a small logging dependency; instrumentation must be reviewed for secrets (the practice check can flag logging of token-like names).
- Log files hold personal paths only in redacted form; retention is capped.

## Open questions
- Retention defaults (size, days) and the default minimum level.
- Whether the debug view should also show trace spans as a timeline in v1.
