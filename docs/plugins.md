# Language plugins

A language plugin is a `plugin.json` (schema version 1) read by `ManifestParser` (`src/Features/Plugins`). No code from a plugin runs inside the app. A manifest with any error is rejected whole, and the errors are listed in plain words so the user can see why.

```json
{
  "schemaVersion": 1,
  "id": "python",
  "name": "Python",
  "extensions": [".py"],
  "templateFiles": { "main.py": "..." },
  "toolchain": { "executables": ["python3", "python"] },
  "compile": { "command": ["{toolchain}", "build"] },
  "run": { "command": ["{toolchain}", "{source}"], "input": "stdin" }
}
```

- `id`: lowercase letters, digits, `-` and `_`; it is also the folder name in the workspace (`docs/workspace.md`).
- `templateFiles`: written into a new day's folder; names must be plain file names.
- `toolchain.executables`: names searched on `PATH`, in order; never paths.
- `compile` is optional. `run.input` is `stdin` (the input is piped in) or `file` (the input path is passed as `{input}`).
- Commands are argument lists, never shell strings. `CommandTemplate.Expand` fills `{toolchain}`, `{source}`, `{input}` and `{workdir}` in one pass per argument, so a value with spaces stays one argument and a value that looks like a placeholder or shell syntax is not interpreted. Any other placeholder is a manifest error.

Python and .NET ship built in (`BuiltIn/*.json`, loaded by `BuiltInPlugins.Load`) in this same format. `ToolchainLocator.Find(executables, PATH)` detects the toolchain: manifest names in order, `PATH` directories in order, only files that are executable (Windows: `.exe`, `.cmd`, `.bat`); relative `PATH` entries are ignored so a planted program in the current folder is never picked. Managed toolchain downloads (ADR 0010) are not built. Also not built yet: the confirmation for plugins dropped into the plugins folder, wiring `shared/` per language, and the LSP and debug adapter commands.
