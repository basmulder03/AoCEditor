# Roadmap

Issues are the source of truth; this page is a snapshot. Hierarchy: Epic > Feature > Story/Task/Spike (GitHub sub-issues). Order is enforced with `blocked by` links.

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

## Foundation: solution, tests, CI, design system (#1, MUST)

- [MUST] #10 Solution scaffold with vertical slices and AoT build
  - SPIKE #49 Spike: Avalonia AoT plus AvaloniaEdit compatibility
  - TASK #64 Create the .NET 10 solution and project structure
  - TASK #65 Add LICENSE headers policy, editorconfig and analyzers
- [MUST] #11 Test infrastructure: xUnit v3, Reqnroll, E2E harness
- [MUST] #12 CI: build and test matrix on Linux and Windows
- [SHOULD] #13 DocFX documentation skeleton
  - SPIKE #55 Spike: DocFX publish to gh-pages
- [MUST] #14 Design system and app shell
  - STORY #63 High-contrast theme and keyboard-only navigation
- [SHOULD] #66 Issue and PR templates enforcing type and MoSCoW labels
- [MUST] #67 Add GitHub Project Kanban board and automation

## AoC access: gateway, sign-in, storage, sync (#2, MUST)

- [MUST] #15 AoC gateway: single ToS-enforcing HttpClient
  - STORY #56 Identify the app to AoC with a contact User-Agent
  - STORY #57 Throttle and serialize AoC requests
  - STORY #58 Cache-first fetch
- [MUST] #16 Sign-in via WebView and token capture
  - SPIKE #50 Spike: reading the session cookie from NativeWebView
  - STORY #59 Detect expired session and trigger sign-in
- [MUST] #17 OS secret store for session token
- [MUST] #18 Local storage and SQLite database
  - SPIKE #51 Spike: AoT-safe SQLite provider
- [MUST] #19 Manual calendar sync

## Calendar views (#3, MUST)

- [MUST] #20 Year calendar view
  - STORY #60 Show which days are completed, partial or locked
- [MUST] #21 Year switcher

## Day workspace: puzzle, input, console, submit (#4, MUST)

- [MUST] #22 Day page split layout
- [MUST] #23 Native puzzle rendering with part tabs
  - SPIKE #52 Spike: HTML to native puzzle rendering
- [MUST] #24 Puzzle input tab
- [MUST] #25 Console output panel
- [MUST] #26 Answer submission with cooldown tracking
  - STORY #61 Show submission cooldown countdown
  - STORY #62 Store correct answers locally

## Languages, toolchains and sandboxed execution (#5, MUST)

- [MUST] #27 Language plugin model
- [MUST] #28 Toolchain detection and provisioning
- [MUST] #29 Linux sandboxed runner
  - SPIKE #53 Spike: Landlock/seccomp from .NET AoT
- [MUST] #30 Windows sandboxed runner
  - SPIKE #54 Spike: Windows AppContainer vs restricted token
- [MUST] #31 Python language plugin
- [MUST] #32 .NET language plugin
- [MUST] #33 Run lifecycle management

## Editor intelligence: LSP, debugging, terminal (#6, SHOULD)

- [SHOULD] #34 Code editor control
- [SHOULD] #35 LSP client
- [SHOULD] #36 Debugging via DAP
- [SHOULD] #37 Integrated terminal

## Solutions workspace, Git and statistics (#7, SHOULD)

- [SHOULD] #38 Solutions workspace with multi-year layout and shared helpers
- [SHOULD] #39 Git and gh integration with auto-commit
- [SHOULD] #40 Performance tracking
- [COULD] #41 Statistics markdown templating

## Integrations: MCP, external IDE, issue reporting (#8, COULD)

- [COULD] #42 MCP server
- [COULD] #43 External IDE mode
- [SHOULD] #44 In-app problem reporting

## Distribution: installer, updates, releases, docs (#9, MUST)

- [MUST] #45 Velopack installer (per-user, unsigned)
- [MUST] #46 In-place self-update
- [MUST] #47 Release automation
- [SHOULD] #48 Publish docs to gh-pages on release
