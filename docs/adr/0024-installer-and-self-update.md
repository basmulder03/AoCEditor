# 0024 Installer and self-update

## Status
Accepted

## Context
Users on Linux and Windows need an install that needs no admin rights and updates itself. No code-signing certificate is available yet.

## Decision
- Use Velopack for packaging and updates, per-user install, no admin/root.
- Updates download in the background, apply in place, and restart the app. The user is told and can defer.
- Releases are unsigned at first. Docs describe the Windows SmartScreen warning and how to proceed. Signing (e.g. SignPath) is revisited later.
- Install scripts (shell and PowerShell) resolve the version themselves, from the latest GitHub release, rather than hardcoding it.
- Update feed is the public GitHub Releases of basmulder03/AoCEditor.
- User data (ADR 0007) lives outside the install dir, so updates and uninstall never touch it unless the user asks.
- Update checks are plain GitHub requests, separate from the AoC client (ADR 0005), and happen at startup at most once per day.

## Consequences
- Windows users see SmartScreen until signing exists; this is documented.
- Linux package/format support depends on Velopack's offerings.
- Release pipeline (ADR 0025) must produce Velopack artifacts per OS.

## Open questions
- Velopack's AoT compatibility and exact Linux output format are unverified.
- Delta updates: enable or not.
