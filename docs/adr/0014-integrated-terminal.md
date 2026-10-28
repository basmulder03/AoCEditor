# 0014 Integrated terminal

## Status
Proposed

## Context
The brief requires an integrated terminal next to the editor and console, as a custom Avalonia control (0002). It is for user commands (git, package managers), separate from sandboxed solution runs.

## Decision
- A custom terminal control renders a VT100/xterm-style cell grid, with scrollback, selection, copy/paste and keyboard input.
- Backend: pseudo-terminal. Linux via `forkpty`/`openpty` P/Invoke; Windows via ConPTY (`CreatePseudoConsole`). Both through one `IPty` port.
- Default shell is the user's shell (`$SHELL`, PowerShell/cmd). Working directory is the solutions workspace (0016).
- The terminal is not sandboxed: it is the user's own shell. The UI labels it as such.
- Escape-sequence parser is our own state machine with unit tests; scope is the common xterm subset first (colors, cursor, alt screen).
- Terminal output never leaves the app; the MCP server (0021) exposes it only if the user enables that tool.

## Consequences
- Significant custom code (parser, renderer, input mapping); keep scope to a documented subset.
- Accessibility of a canvas-style control needs explicit work (0022).
- Environment must not leak the AoC session token into the shell.

## Open questions
- Whether an AoT-compatible terminal emulator library exists to reuse (unverified).
- Performance of cell-grid rendering in Avalonia for heavy output.
