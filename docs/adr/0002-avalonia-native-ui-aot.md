# 0002 Avalonia native UI, AoT

## Status
Accepted

## Context
The product is a small, low-memory, cross-platform (Linux and Windows first) desktop editor that replaces the browser for solving Advent of Code puzzles. An embedded browser engine would cost memory and startup time, and AoT compilation rules out reflection-heavy UI stacks.

## Decision
- UI is Avalonia, rendered natively, compiled AoT.
- Editor is a native control based on AvaloniaEdit; terminal is a custom native control (see 0012, 0014).
- Puzzle text is rendered by native controls, not a web view (see 0008).
- `NativeWebView` is used only for the AoC sign-in flow (see 0006) and nowhere else.
- Bindings are compiled (`x:DataType`), no runtime XAML loading, no reflection-based binding.
- The app uses its own design system with tokens and themes (see 0022).

## Consequences
- Low memory and fast startup; no Chromium dependency in the app process.
- A WebView runtime is still needed on the machine for sign-in (WebView2 on Windows, WebKitGTK on Linux).
- We own rendering for puzzle text, editor and terminal, which is more work than embedding a browser.
- Every UI dependency must be AoT/trim-safe; incompatible libraries are rejected.

## Open questions
- Exact AvaloniaEdit package and its trim/AoT warnings are unverified.
- WebKitGTK availability on target Linux distros is unverified.
