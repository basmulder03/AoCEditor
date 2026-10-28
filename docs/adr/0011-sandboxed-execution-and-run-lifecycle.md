# 0011 Sandboxed execution and run lifecycle

## Status
Accepted

## Context
Users run their own (and pasted, possibly untrusted) solution code locally at native speed. Code must not read secrets (session token), write outside its folder, or hang the machine. No admin rights are available (see 0024).

## Decision
- A Runner component starts the language plugin's run command (0009) as a child process inside an OS-native sandbox:
  - Linux: Landlock (filesystem), seccomp (syscalls), cgroups v2 / rlimits (memory, CPU, pids).
  - Windows: Job Object (memory, CPU, process count, kill-on-close) plus restricted token or AppContainer.
- Filesystem: read-only toolchain and solution folder, read-only puzzle input, writable temp dir. Network denied by default.
- Lifecycle: wall-clock timeout (default configurable), memory cap, stdout/stderr streamed to the console pane, kill the whole process tree on timeout, cancel or app exit.
- Run time is measured by the Runner (feeds 0018), not by the user code.
- No admin elevation; if a sandbox feature is unavailable the Runner reports the degraded level and asks the user before running.

## Consequences
- Native speed, no container runtime needed.
- Two platform implementations behind one `ISandbox` port; each needs its own tests.
- Toolchains that need network or extra paths (package restore) need an explicit, visible allowance.
- Debugger sessions (0013) must run under the same sandbox.

## Open questions
- Whether AppContainer or a restricted token is practical for all toolchains (unverified).
- Landlock ABI availability on older kernels; fallback behavior.
