---
name: security-checklist
description: Security checklist for AoCEditor changes. Load when touching the gateway, session/token, sandbox, plugins, MCP, updater, logging, file paths or process launching.
---
Assets: session token; the user's machine (solutions run as their code); solution sources; puzzle inputs (AoC content); update channel.
Check each applicable item:
- Token: only in the OS secret store; never in logs, exceptions, UI text, diagnostics, issue reports, env of child processes, MCP output or the solutions repo.
- Processes: ArgumentList, never a shell string; no user-controlled executable path without confirmation; child env is an allow-list; run code only through the sandbox.
- Paths: canonicalize and verify containment for every file operation derived from user, plugin, MCP or network input (traversal, symlinks, case-insensitive Windows paths); data root never inside the solutions folder.
- Plugins: manifests validated against the schema; commands are templates with typed placeholders; explicit user confirmation on first use; no code loaded in-process.
- MCP: per-tool permissions default off for anything that writes or runs; the app, not the client, performs AoC actions; inputs validated; no AoC host reachable from MCP directly.
- Network: only the gateway talks to AoC; TLS validation never disabled; redirects not followed blindly; response sizes bounded.
- Updater: downloads over HTTPS from the project's GitHub releases only; verify integrity (hash/signature) before applying; no downgrade; clear user prompt.
- Logging/diagnostics: redaction applied before sinks; no inputs, puzzle text or answers.
- Dependencies: new packages justified, pinned, maintained, license-compatible; no pre-release in main.
- Native interop: LibraryImport with explicit marshalling, handle lifetimes, no unbounded buffers.
Output: findings ranked by severity with a fix, and what was not verified.
