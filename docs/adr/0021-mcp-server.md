# 0021 MCP server

## Status
Accepted

## Context
Users may want AI agents to read and edit solution files. The brief requires that the app stays in the loop and never lets an agent reach AoC directly. AI use may be against AoC rules (their automation and AI guidance).

## Decision
- The app hosts an MCP server for the application itself. It exposes tools such as read, create and edit solution files, run, and read console output.
- The server never calls AoC. Tools that need AoC data go through the app's single HttpClient (ADR 0005), cache first, with the same throttling. Submitting is not exposed unless the user enables it.
- File tools are limited to the solutions workspace (ADR 0016). Inputs and puzzle text are not served for editing.
- Every tool is individually configurable (on/off) in settings. Default: all off until the user enables the server.
- Enabling the server shows a notice that AI use may be against AoC rules, and the user must acknowledge it.
- Each tool call goes through the app, which performs the action (so UI state, git and run lifecycle stay consistent) and logs it.
- Transport: local only (stdio or loopback), no remote listener.

## Consequences
- Agents cannot bypass ToS enforcement or sandboxing.
- Tool surface needs its own schema, tests and docs, and must stay AoT-safe (source-generated JSON).
- Users carry the decision on AI use; the notice documents that.

## Open questions
- Which MCP SDK for .NET is AoT-compatible (unverified); otherwise implement the small JSON-RPC subset ourselves.
- Stdio vs loopback HTTP as default transport.
