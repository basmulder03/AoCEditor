# Running solutions

`IRunner` / `ProcessRunner` (`src/Features/Runner`) starts the expanded run command of a language plugin (`docs/plugins.md`) and reports a `RunResult`.

- **No shell.** The command is a program plus an argument list, so quotes, `;` or `$(...)` in a path or value reach the program as plain text.
- **Output** is reported line by line as `OutputLine(Stdout | Stderr, text)` through `IProgress`, which the console panel consumes. Output is read in chunks: a single line is cut at 8 KB and ends in an ellipsis, and after 4 MB per run nothing more is reported except one stderr note (the pipes are still drained so the program is not blocked; the timeout still applies). The day page also stops posting lines to the UI thread once the console holds its maximum.
- **Input** is piped to stdin from the cached input file when `InputPath` is set; the program may exit without reading it.
- **Timeout and cancel** kill the whole process tree (`Process.Kill(entireProcessTree: true)`). The pipes are drained for at most two seconds afterwards, so a surviving grandchild cannot hang the run. The status says which of `Completed`, `TimedOut` or `Cancelled` happened.
- **Timing** is the wall-clock `Elapsed` measured by the runner, not by the user's code.
- **Environment** is cleared and only a short allow-list (PATH, HOME, TEMP, LANG, DOTNET_ROOT and a few more) is passed on, so the solution does not see other variables of the app's environment.
- **Start failures** (program not found) come back as `FailedToStart` with the reason instead of an exception.

## Sandbox

On Linux the runner confines the user's program. `ProcessRunner` does not start the program directly: it starts the app itself with a hidden first argument (`--aoc-sandbox-exec`, handled first thing in `Program.Main` through `SandboxHelper.RunIfRequested`). That helper applies the protections to itself with direct syscalls (AoT-safe `LibraryImport`) and then `execvp`s the command, so nothing but the program runs afterwards. If any protection cannot be applied, the program does not start (exit code 125 with a message). Test hosts call the same entry from a module initializer.

`SandboxLevel` is a flag set, detected at startup, and `Level` reports what this computer can enforce:

- **Limits** (always on Linux): `setrlimit` for writable memory (`RLIMIT_DATA`, 4 GiB by default; address space is not used because .NET reserves huge ranges), CPU seconds, file size, core dumps off, and the number of processes and threads (`RLIMIT_NPROC`, set to the user's current task count plus 512, because the kernel counts all tasks of the user).
- **Filesystem** (Landlock, any ABI >= 1): read-only `/usr /lib /bin /sbin /etc /opt /sys` (those that exist), the PATH folders (except the home folder or its parents), the folder of the toolchain executable, the working (solution) folder, the paths in `RunRequest.ReadablePaths` (the coordinator passes the input file), and a few `/proc` and `/dev` entries. Writable: only a private per-run folder in the temp directory, plus `RunRequest.WritablePaths`. The rest of the home folder, the app-data folder, `~/.ssh`, `~/.config` and keyring sockets are not reachable. `HOME`, `TMPDIR`, `TMP`, `TEMP`, `DOTNET_CLI_HOME` and `XDG_CACHE_HOME` point at the private folder, which is deleted after the run.
- **Network** (seccomp): a filter makes `socket()` and `io_uring_setup()` fail with EACCES (and any syscall of a foreign ABI). This is used instead of Landlock's TCP rules because those leave UDP and abstract unix sockets open. Supported on x64 and arm64.

Only `Full` runs without asking. Below it `RunAsync` returns `SandboxUnavailable` with the missing protections named (`SandboxLevel.Missing()`) unless the caller sets `AllowUnsandboxed`; the UI then shows that text in the consent dialog, and the run gets whatever protections exist. Windows has no sandbox yet (`Level` is `None`).

The whole process group is killed on timeout, cancel and after the run, so background children do not survive.

### Build steps

Package restore needs the network and a build writes into the day folder, so the coordinator's compile step runs with `AllowNetwork` and the day folder writable. Filesystem confinement and limits still apply; the run step gets neither exception. Because `HOME` is the private folder, `dotnet build` restores packages into it on every build (slow, needs the network, and nothing is cached between runs). This exception is a documented property of the sandbox and not reflected in `Level`.

### Limitations

- No cgroups: memory is capped per process (`RLIMIT_DATA`), not for the whole tree, and the task cap is only as exact as the count taken at start.
- Toolchains installed in the home folder are readable only through the folder of the executable (or PATH entries); layouts with shims elsewhere need `ReadablePaths`.
- Signals to and from other processes of the user, and pathname unix sockets reachable by inherited descriptors, are not restricted; `/proc/self` only is exposed, but other readable system folders stay world-readable.
- Kernels without Landlock (or with it disabled) report `Limits`; seccomp-less or non-x64/arm64 systems report no `Network`.

The tests start real processes through `sh`/`python3` and run on Linux only; sandbox tests skip with a reason when Landlock or seccomp are missing.

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

The day page splits into the puzzle (left) and the solution panel (right, `SolutionPanel` in `src/App`): a language picker, Run and Stop, the solutions folder with a chooser, and the console. Run goes through `DayRunCoordinator`. When the sandbox is incomplete, the first run asks for confirmation ("Run with an incomplete sandbox?") naming the missing protections, and only then runs. A solutions folder that overlaps the app-data folder is refused with a message.

The panel also holds the code editor (`docs/editor.md`) between the folder controls and the console. When the page opens, the language changes or a folder is chosen, `DayRunCoordinator.ScaffoldAsync` creates the day's template files (no input needed; `NoWorkspace` is a result, not an exception) and the source file opens in an `EditorViewModel` rooted at the day folder. Run first saves a dirty editor; if the file changed on disk the run does not start (`SourceChanged`) and the editor's banner offers Reload or Save anyway. Switching language or choosing a folder saves a dirty file first; if it changed on disk the switch is cancelled, the language box returns to the previous language and the editor's banner stays so the user can Reload or Save anyway.
