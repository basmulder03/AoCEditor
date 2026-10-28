# Roadmap

Snapshot of the GitHub issues, which are the source of truth. Hierarchy: Epic > Feature > Story/Task/Spike (sub-issues); ordering uses `blocked by` links. Closed items are checked.

| Milestone | Epics |
|---|---|
| M1 Foundation | #1 |
| M2 AoC access and calendar | #2, #3 |
| M3 Day workspace | #4 |
| M4 Languages and sandbox | #5 |
| M5 Editor intelligence | #6 |
| M6 Workspace and Git | #7 |
| M7 Integrations | #8 |
| M8 Distribution and release | #9 |

## Foundation: solution, tests, CI, design system

- [ ] FEATURE #10 [MUST] Solution scaffold with vertical slices and AoT build
  - [x] SPIKE #49 [MUST] Spike: Avalonia AoT plus AvaloniaEdit compatibility
  - [x] TASK #64 [MUST] Create the .NET 10 solution and project structure
  - [x] TASK #65 [MUST] Add LICENSE headers policy, editorconfig and analyzers
  - [ ] TASK #131 [MUST] Unit tests follow Arrange-Act-Assert and use fixtures at the lowest needed scope and visibility
  - [ ] TASK #136 [SHOULD] Constructor-inject services into pages instead of the AppServices bag
  - [ ] TASK #137 [SHOULD] Rename ambiguous test classes to the type under test
  - [ ] TASK #138 [SHOULD] Rename the Shared namespace to AoCEditor.Shared (and UiKit if consistent)
  - [x] TASK #123 [MUST] Adopt Microsoft.Extensions.DependencyInjection with explicit registrations
  - [x] TASK #124 [SHOULD] One top-level type per file
  - [x] TASK #125 [SHOULD] Consolidate slice projects into Core, Infrastructure and UI projects
  - [x] TASK #128 [SHOULD] Replace the hand-written logging sink layer with ILoggerProviders
  - [x] TASK #130 [SHOULD] Inject a required ILogger<T>; remove optional loggers and NullLogger fallbacks
  - [x] TASK #129 [SHOULD] Naming conventions and renames (ADR 0035)
  - [ ] TASK #126 [SHOULD] XML docs describe functionality: summary, params, returns, exceptions (blocked by #131, #132, #133, #134, #135)
- [x] FEATURE #11 [MUST] Test infrastructure: xUnit v3, Reqnroll, E2E harness
- [x] FEATURE #12 [MUST] CI: build and test matrix on Linux and Windows
- [x] FEATURE #13 [SHOULD] DocFX documentation skeleton
  - [ ] SPIKE #55 [SHOULD] Spike: DocFX publish to gh-pages
- [x] FEATURE #14 [MUST] Design system and app shell
  - [ ] STORY #63 [SHOULD] High-contrast theme and keyboard-only navigation
- [x] TASK #66 [SHOULD] Issue and PR templates enforcing type and MoSCoW labels
- [ ] TASK #67 [MUST] Add GitHub Project Kanban board and automation

## AoC access: gateway, sign-in, storage, sync

- [x] FEATURE #15 [MUST] AoC gateway: single ToS-enforcing HttpClient
  - [x] STORY #56 [MUST] Identify the app to AoC with a contact User-Agent
  - [x] STORY #57 [MUST] Throttle and serialize AoC requests
  - [x] STORY #58 [MUST] Cache-first fetch
- [ ] FEATURE #16 [MUST] Sign-in via WebView and token capture
  - [x] SPIKE #50 [MUST] Spike: reading the session cookie from NativeWebView
  - [ ] STORY #59 [MUST] Detect expired session and trigger sign-in
- [x] FEATURE #17 [MUST] OS secret store for session token
- [x] FEATURE #18 [MUST] Local storage and SQLite database
  - [x] SPIKE #51 [MUST] Spike: AoT-safe SQLite provider
- [x] FEATURE #19 [MUST] Manual calendar sync

## Calendar views

- [ ] FEATURE #20 [MUST] Year calendar view
  - [ ] STORY #60 [MUST] Show which days are completed, partial or locked
- [ ] FEATURE #21 [MUST] Year switcher

## Day workspace: puzzle, input, console, submit

- [ ] FEATURE #22 [MUST] Day page split layout
  - [ ] BUG #132 [MUST] Day page tab selection jumps while the puzzle loads
  - [ ] STORY #133 [MUST] Show a loading indicator while the puzzle and input download
- [x] FEATURE #23 [MUST] Native puzzle rendering with part tabs
  - [x] SPIKE #52 [MUST] Spike: HTML to native puzzle rendering
  - [ ] BUG #134 [SHOULD] Links in puzzle text float above the line
  - [ ] STORY #135 [SHOULD] Style puzzle text elements (inline code, emphasis, headings, code blocks)
- [x] FEATURE #24 [MUST] Puzzle input tab
- [ ] FEATURE #25 [MUST] Console output panel
- [x] FEATURE #26 [MUST] Answer submission with cooldown tracking
  - [x] STORY #61 [MUST] Show submission cooldown countdown
  - [x] STORY #62 [MUST] Store correct answers locally
- [ ] FEATURE #74 [SHOULD] Puzzle examples: select from the puzzle or create your own, with expected answers
  - [ ] STORY #75 [SHOULD] Select text in the puzzle and save it as an example input
  - [ ] STORY #76 [SHOULD] Create your own example with expected answers per part
  - [ ] STORY #77 [SHOULD] Run against examples and show pass/fail per example and part
  - [ ] STORY #78 [SHOULD] Manage examples: rename, edit, enable/disable, delete
  - [ ] STORY #79 [COULD] Suggest expected answers from the puzzle text

## Languages, toolchains and sandboxed execution

- [ ] FEATURE #27 [MUST] Language plugin model
  - [x] SECURITY #117 [MUST] Reject ':' and reserved device names in template and executable names
- [ ] FEATURE #28 [MUST] Toolchain detection and provisioning
- [ ] FEATURE #29 [MUST] Linux sandboxed runner
  - [ ] SPIKE #53 [MUST] Spike: Landlock/seccomp from .NET AoT
  - [x] SECURITY #116 [MUST] Cap program output size in the runner and console
  - [x] SECURITY #119 [MUST] Compile step can reach the session bus, keyring and SSH agent over AF_UNIX
  - [ ] BUG #127 [MUST] C# plugin cannot build or run in the Linux sandbox (.NET child processes die)
  - [x] SECURITY #120 [MUST] Sandboxed code can signal and ptrace other processes
  - [x] SECURITY #121 [SHOULD] Landlock ABI 1-2 leaves truncate unrestricted and chmod/utimes/setxattr are never governed
  - [ ] SECURITY #122 [SHOULD] Harden sandbox lifecycle (lower findings)
- [ ] FEATURE #30 [MUST] Windows sandboxed runner
  - [ ] SPIKE #54 [SHOULD] Spike: Windows AppContainer vs restricted token
- [ ] FEATURE #31 [MUST] Python language plugin
- [ ] FEATURE #32 [MUST] .NET language plugin
- [ ] FEATURE #33 [MUST] Run lifecycle management
  - [ ] SECURITY #118 [SHOULD] Harden scaffolding and run lifecycle (low findings)

## Editor intelligence: LSP, debugging, terminal

- [x] FEATURE #34 [SHOULD] Code editor control
- [ ] FEATURE #35 [SHOULD] LSP client
- [ ] FEATURE #36 [SHOULD] Debugging via DAP
- [ ] FEATURE #37 [SHOULD] Integrated terminal

## Solutions workspace, Git and statistics

- [ ] FEATURE #38 [SHOULD] Solutions workspace with multi-year layout and shared helpers
- [ ] FEATURE #39 [SHOULD] Git and gh integration with auto-commit
- [ ] FEATURE #40 [SHOULD] Performance tracking
- [ ] FEATURE #41 [COULD] Statistics markdown templating
  - [ ] SECURITY #68 [SHOULD] Verify AoC policy on publishing puzzle titles in stats
  - [ ] STORY #80 [SHOULD] Per-year table placeholder: all days with stars, language and time
  - [ ] STORY #81 [SHOULD] All-years overview matrix across every year
  - [ ] STORY #82 [SHOULD] Selectable icon sets for day states
  - [ ] STORY #83 [SHOULD] Built-in README template gallery
  - [ ] STORY #84 [COULD] Extra stats: per-language breakdown, streaks and totals
  - [ ] STORY #85 [COULD] Generated badges and charts as images

## Integrations: MCP, external IDE, issue reporting

- [ ] FEATURE #42 [COULD] MCP server
- [ ] FEATURE #43 [COULD] External IDE mode
- [ ] FEATURE #44 [SHOULD] In-app problem reporting

## Distribution: installer, updates, releases, docs

- [ ] FEATURE #45 [MUST] Velopack installer (per-user, unsigned)
- [ ] FEATURE #46 [MUST] In-place self-update
- [ ] FEATURE #47 [MUST] Release automation
- [ ] FEATURE #48 [SHOULD] Publish docs to gh-pages on release
