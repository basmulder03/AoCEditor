# 0009 Language plugin model

## Status
Proposed

## Context
Users solve puzzles in many languages. Native AoT forbids loading assemblies at runtime, and plugins must not be able to crash or compromise the app. Python and .NET come first.

## Decision
- A language plugin is a folder with a JSON manifest (`plugin.json`) and, optionally, helper scripts. No code is loaded into the app process.
- Manifest declares: id, display name, file extensions, toolchain detection and provisioning info (see 0010), compile command (optional), run command, LSP server command (0012), debug adapter command (0013), project/template files, and how input and output are passed.
- Commands are templates with typed placeholders (`{source}`, `{input}`, `{workdir}`); the app never passes them through a shell.
- Manifests are parsed with source-generated System.Text.Json and validated against a versioned schema; invalid manifests are rejected with a visible error.
- All spawned processes (compile, run, LSP, DAP) run out-of-process; running user code goes through the sandbox (0011).
- Built-in plugins: Python and .NET, shipped in the same format as third-party ones.

- v1 distribution: built-in plugins plus folder drop into the plugins directory, with explicit confirmation on first use. No gallery yet.

## Consequences
- Adding a language needs no app release; plugins are data.
- Anything needing logic beyond command templates needs a helper executable, kept out-of-process.
- Third-party plugins can execute arbitrary commands, so install requires explicit user confirmation.

## Open questions
- Manifest schema details and versioning are not designed.

