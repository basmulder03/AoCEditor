# Running solutions

`IRunner` / `ProcessRunner` (`src/Features/Runner`) starts the expanded run command of a language plugin (`docs/plugins.md`) and reports a `RunResult`.

- **No shell.** The command is a program plus an argument list, so quotes, `;` or `$(...)` in a path or value reach the program as plain text.
- **Output** is reported line by line as `OutputLine(Stdout | Stderr, text)` through `IProgress`, which the console panel consumes. Output is read in chunks: a single line is cut at 8 KB and ends in an ellipsis, and after 4 MB per run nothing more is reported except one stderr note (the pipes are still drained so the program is not blocked; the timeout still applies). The day page also stops posting lines to the UI thread once the console holds its maximum.
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

## Console

`ConsoleView` and `ConsoleViewModel` (`Runner.UI`) show the output of the latest run: `Begin()` clears the old output, `Append` adds a line (stderr lines are italic, so the difference is not colour alone), `End(result)` writes a one-line status such as "Finished with exit code 0 in 1.50 s", and `Fail(message)` covers problems before a run starts. At most 10,000 lines are kept; after that output is dropped and a note says so, so a program that prints forever cannot exhaust memory.

## Day page

The day page splits into the puzzle (left) and the solution panel (right, `SolutionPanel` in `src/App`): a language picker, Run and Stop, the solutions folder with a chooser, and the console. Run goes through `DayRunCoordinator`. While no sandbox exists, the first run asks for confirmation ("Run without a sandbox?") and only then runs with the user's normal rights. A solutions folder that overlaps the app-data folder is refused with a message.
