# 0010 Toolchain provisioning

## Status
Accepted

## Context
A language plugin (0009) is useless without its compiler or interpreter. Users may already have one; others need help. The app runs per-user without admin rights.

## Decision
For each plugin toolchain, in order:
1. Detect: look for an existing install (PATH, known locations, plugin-declared probe command and version range).
2. Offer managed install: with explicit user consent, download into a per-user folder under the app-data directory (0007), no admin, no system changes. Source and checksum come from the plugin manifest.
3. Guide: if neither works, show instructions and a link, and re-detect on demand.
- Downloads go through a separate download client, not the AoC gateway (0005), and are verified by hash before use.
- Managed toolchains are versioned side by side and removable from settings.
- The user may always point a plugin at their own toolchain path.

## Consequences
- First run works for users with nothing installed, without admin prompts.
- The app takes on responsibility for toolchain download URLs and hashes staying current.
- Managed installs consume disk space in the app-data folder and are shown with their size.

## Open questions
- Per-language sources (for example python-build-standalone, dotnet-install scripts) are unverified.
- Checksum/signature source for third-party plugins is not decided.
