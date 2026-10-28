# Running solutions

`IRunner` / `ProcessRunner` (`src/Features/Runner`) starts the expanded run command of a language plugin (`docs/plugins.md`) and reports a `RunResult`.

- **No shell.** The command is a program plus an argument list, so quotes, `;` or `$(...)` in a path or value reach the program as plain text.
- **Output** is reported line by line as `OutputLine(Stdout | Stderr, text)` through `IProgress`, which the console panel will consume.
- **Input** is piped to stdin from the cached input file when `InputPath` is set; the program may exit without reading it.
- **Timeout and cancel** kill the whole process tree (`Process.Kill(entireProcessTree: true)`). The pipes are drained for at most two seconds afterwards, so a surviving grandchild cannot hang the run. The status says which of `Completed`, `TimedOut` or `Cancelled` happened.
- **Timing** is the wall-clock `Elapsed` measured by the runner, not by the user's code.
- **Environment** is cleared and only a short allow-list (PATH, HOME, TEMP, LANG, DOTNET_ROOT and a few more) is passed on, so the solution does not see other variables of the app's environment.
- **Start failures** (program not found) come back as `FailedToStart` with the reason instead of an exception.

## Sandbox status

The OS sandboxes of ADR 0011 (Landlock, seccomp and cgroups on Linux; Job Object and restricted token on Windows) are not built yet (issues for the Linux and Windows sandboxed runners). Until then `Level` is `SandboxLevel.None`, and `RunAsync` returns `SandboxUnavailable` without starting anything unless the caller sets `AllowUnsandboxed`, which the UI may only do after asking the user. Memory and CPU limits are not enforced yet either.

The tests start real processes through `sh` and run on Linux only; the Windows behaviour (tree kill, environment) is untested until the Windows runner work.

## Run coordination

`DayRunCoordinator` (`src/App`) is the only place that ties the slices together, since slices never reference each other. For one run it:

1. needs a solutions folder (`NoWorkspace` otherwise) and a toolchain on PATH (`NoToolchain`, with the names to install);
2. loads the puzzle input through the puzzle service (`InputUnavailable` when that fails);
3. scaffolds the day in the solutions folder (never overwriting) and saves the input as `<app data>/inputs/<year>/<dd>.txt`, outside the solutions folder, then pipes it to stdin or passes it as `{input}` depending on the plugin;
4. runs the compile command when the plugin has one (`CompileFailed` when it does not exit 0), then the run command.

`SandboxRefused` means the runner would not start without the user's consent; the UI asks and calls again with `allowUnsandboxed`. Timeouts are fixed at 2 minutes for a build and 1 minute for a run until they become settings.
