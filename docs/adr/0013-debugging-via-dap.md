# 0013 Debugging via DAP

## Status
Proposed

## Context
The day page offers a debug/step button for any plugin language. Debuggers differ per language; a shared protocol avoids per-language UI code.

## Decision
- Use the Debug Adapter Protocol. The plugin manifest (0009) optionally declares a debug adapter command; no adapter means the debug button is hidden for that language.
- The app is the DAP client: launch, breakpoints in the editor gutter, step in/over/out, continue, variables, call stack, watch, shown in a panel beside the console.
- The adapter and debuggee run out-of-process, stdio JSON-RPC, source-generated JSON types.
- Debuggee runs under the sandbox of 0011 with the wall-clock timeout suspended while paused and a separate idle limit.
- Debug runs are not recorded in performance tracking (0018).

## Consequences
- Debug UI is written once; per-language work is a manifest entry plus provisioning (0010).
- Some adapters (e.g. netcoredbg, debugpy) have license or ptrace/sandbox needs that may conflict with seccomp rules; sandbox profile may need a debug variant.
- Debugging relaxes isolation slightly; the UI must show that.

## Open questions
- Adapter choice and licensing for .NET (the Microsoft debugger is not freely redistributable; netcoredbg is a candidate, unverified).
- Whether debuggers work under the Linux seccomp profile.
