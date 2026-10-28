# 0012 Editor and LSP client

## Status
Proposed

## Context
The day page needs a native editor with syntax highlighting, completion, diagnostics (linting) for any plugin language, under AoT and low memory (0002, 0004).

## Decision
- Editor control: AvaloniaEdit (TextMate grammars for highlighting).
- Language features via a Language Server Protocol client in the app. Each language plugin manifest (0009) declares its server command and args; the app starts it out-of-process, talking JSON-RPC over stdio.
- Supported: diagnostics, completion, hover, go-to-definition, formatting. One server per language per open workspace, stopped when idle.
- Protocol types use System.Text.Json source generation (AoT-safe, no reflection).
- Language servers run under the sandbox rules of 0011 where feasible, with read access to the solutions workspace (0016).
- Languages without a server still get highlighting and run support.

## Consequences
- Language servers need provisioning through 0010.
- A hand-written minimal LSP client is likely needed; a third-party client may not be AoT-clean.
- Memory cost of servers (e.g. Roslyn, Pylance-like) must be measured against the low-memory goal.

## Open questions
- Whether an existing LSP client library is AoT-compatible (unverified).
- AvaloniaEdit 12.0.0 with Avalonia 12.1.x is AoT-clean on linux-x64; Windows and highlight rendering for non-C# grammars are unverified.
