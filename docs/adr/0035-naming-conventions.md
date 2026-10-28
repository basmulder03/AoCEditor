# 0035 Naming conventions

## Status
Accepted

## Context
Names drifted across slices (Coordinator vs Service, Panel vs View, `*Log` for unrelated things, `Ui` vs `UI`). The owner approved mechanical and convention renames to make names predictable.

## Decision
- Pure domain logic: Noun + Parser/Validator/Resolver/Rules/Locator (PuzzleParser, CalendarRules, LinkResolver, ToolchainLocator).
- Use-case orchestration over ports: Noun + Service (PuzzleService, SyncService, WorkspaceService). Coordinator is retired: SignInCoordinator becomes SignInService, DayRunCoordinator becomes DayRunService.
- Interfaces: I + capability noun (IAnswerStore, IRunner). Adapters: Technology/Platform + port name (SqliteAnswerStore, LibSecretStore, ProcessRunner, LinuxSandbox).
- Persistence is Store, disposable data is Cache. Never Repository, Manager or Helper.
- Logging: `<Slice>Log` is reserved for `[LoggerMessage]` partial classes. ILogger/ILoggerProvider implementations end in LoggerProvider (FileLoggerProvider, BufferLoggerProvider, RedactingLoggerProvider). Other records of events get their own noun (GatewayRequestHistory, not RequestLog).
- Results: record `XResult` when it carries data. Enums use a consistent suffix: Status (outcome of an attempt), State (lifecycle), Kind (category), Outcome (end result).
- UI: XView (slice control), XViewModel, XWindow, XDialog; XPage only for App-level screen hosts. XPanel is retired (SolutionPanel becomes SolutionView).
- Static helpers: singular descriptive noun, never Helper (SandboxHelper becomes SandboxEntryPoint, UnixMs becomes UnixMilliseconds). Extension classes are `<Type>Extensions`.
- Methods returning Task end in Async (tests excepted); the parameter is always named `cancellationToken`. UI event handlers are OnXxx. Constants PascalCase, fields `_camelCase`.
- Tests: `<TypeUnderTest>Tests` in a file of the same name, behaviour-sentence method names, fakes named `Fake<Name>`, one shared test-support location instead of copies.
- The UI layer is spelled `UI` in every folder and namespace (tests/Ui.Tests becomes tests/UI.Tests).
- Shared's public names are not renamed for now (IStorageStatus/StorageStatus, ISessionTokenProvider, AocGateway casing stay).

## Consequences
- One-time rename across code, tests and docs.
- Enforced by review and, where possible, analyzers: CA1711 (identifier suffixes) and the async suffix rule via `.editorconfig` (VSTHRD200 or the IDE rule).
- Shared's inconsistencies remain until a later decision.
