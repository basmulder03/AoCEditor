# Architecture Decision Records

Load this index first. Open a single ADR only when its row is relevant to the task.

Format: `NNNN-kebab-title.md`, sections Status / Context / Decision / Consequences (max ~40 lines).
Status: `Accepted` = decided with the owner, `Proposed` = default chosen by the team, awaiting owner review.

| # | Title | Status | Covers |
|---|-------|--------|--------|
| 0001 | Record architecture decisions | Accepted | ADR format, index, lazy loading |
| 0002 | Avalonia native UI, AoT | Accepted | UI stack, native editor, WebView for login only |
| 0003 | DDD and vertical slices | Accepted | Solution layout, slice boundaries, dependency rules |
| 0004 | .NET version policy | Accepted | net10 now, one-line bump to 11/12, AoT constraints |
| 0005 | Single AoC HttpClient | Accepted | ToS enforcement, throttling, User-Agent, cache-first |
| 0006 | Session token and secret store | Accepted | WebView SSO, token capture, OS secret stores |
| 0007 | Local storage layout | Accepted | App-data cache/DB, SQLite bundled, never inside the solutions repo |
| 0008 | Calendar sync and native puzzle rendering | Proposed | Manual sync, per-day fetch, HTML to native view |
| 0009 | Language plugin model | Proposed | Out-of-process manifest plugins, AoT-safe |
| 0010 | Toolchain provisioning | Accepted | Detect, offer managed download, or guide |
| 0011 | Sandboxed execution and run lifecycle | Accepted | OS-native sandbox, limits, kill tree, timeouts |
| 0012 | Editor and LSP client | Proposed | AvaloniaEdit, LSP, linting, completion |
| 0013 | Debugging via DAP | Proposed | Step-through for pluggable languages |
| 0014 | Integrated terminal | Proposed | PTY, terminal control |
| 0015 | Submission, cooldowns, answers | Proposed | Submit flow, cooldown tracking, answer store |
| 0016 | Solutions workspace layout | Accepted | Multi-year, shared helpers across years, user-defined path |
| 0017 | Git and gh integration | Accepted | History, auto-commit of correct solutions, no inputs |
| 0018 | Performance tracking | Proposed | Timings after first correct answer, regression view |
| 0019 | Stats markdown templating | Accepted | Template to generated README section, own results only |
| 0020 | External IDE mode | Accepted | App as controller, IDE edits files |
| 0021 | MCP server | Accepted | App-in-the-loop, configurable tools, AoC-rule notice |
| 0022 | Design system and accessibility | Proposed | Tokens, themes, WCAG, keyboard |
| 0023 | Testing strategy | Accepted | xUnit v3, Reqnroll BDD, E2E, TDD flow |
| 0024 | Installer and self-update | Accepted | Velopack, per-user, unsigned at first |
| 0025 | CI/CD and release automation | Accepted | Build/test matrix, release, gh-pages |
| 0026 | Documentation and gh-pages | Accepted | DocFX, docs folder, publish on release |
| 0027 | Issue tracking and roadmap | Accepted | Type and MoSCoW labels, roadmap, auto-issues |
| 0028 | Code documentation standard | Accepted | Comment density, no ADR references |
| 0029 | Agents, skills, orchestration | Accepted | Token-lean agents/skills, orchestrator rules |
| 0030 | In-app issue reporting | Accepted | Report button, prefilled issue, GDPR-safe redaction |
