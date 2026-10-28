# Threat model

Short and practical; update when a slice adds a trust boundary.

| Asset | Threat | Control |
|---|---|---|
| AoC session token | Leaks through logs, reports, MCP, child processes or the solutions repo | OS secret store only; redaction before every log sink; allow-listed child environment; diagnostics and issue reports exclude it |
| User's machine | A solution, plugin or language server runs hostile or buggy code | Solutions run in the OS-native sandbox (Linux: Landlock ABI >= 3, a seccomp filter for sockets, process inspection and file metadata, rlimits; build steps get internet sockets only; see runner.md for gaps: same-user signals on kernels below 6.12, loopback/TCP from builds, readable `/etc`, no working .NET child processes) with limits and timeouts; plugins are data, never loaded in-process; first-use confirmation; no shell strings |
| Files outside the workspace | Path traversal or symlink escape via plugin, MCP or network input | Canonicalize and check containment; data root never inside the solutions folder |
| Puzzle text and inputs | Published by accident | Stored in app-data only; the git integration never stages them |
| AoC servers | Abuse from the tool | One gateway; two throttle classes; cache first; no polling |
| MCP surface | An agent performs actions the user did not intend | Per-tool permissions, default off for writes and runs; the app performs AoC actions; notice about AoC's AI stance |
| Update channel | Tampered update | HTTPS from the project's releases only; integrity check before applying; no downgrade |
| Supply chain | Malicious or vulnerable dependency or action | Dependabot, dependency review on PRs, CodeQL, pinned action versions, secret scanning with push protection |

Out of scope for now: a compromised OS account, a malicious user attacking themselves, and signed binaries (no signing certificate yet, see the installer ADR).
