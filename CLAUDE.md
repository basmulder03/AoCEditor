# AoCEditor

Local AoT desktop editor for Advent of Code (.NET 10, Avalonia). Orchestrate; delegate work to the agents in `.claude/agents`.

- Decisions: read `docs/adr/README.md` (index) and open only the ADRs a task needs. Never load all ADRs.
- Flow per slice: GH issue (type + MoSCoW label) -> failing Reqnroll/xUnit test -> implement -> docs in `docs/` -> commit.
- Found or broke something: open a GH issue (type label + MoSCoW label) instead of fixing silently.
- Comments: sparse, describe behaviour, never cite ADRs or issue numbers.
- AoC access only through the single ToS-enforcing HttpClient. Inputs/puzzle text never enter the solutions repo.
- Unclear requirement or better idea: stop and ask the owner. No assumptions.
- Always apply best practices by default. Before reporting done, run the `validate-practices` skill (it runs `scripts/check-practices.sh`; CI runs it too).
- Tests: Shouldly only, Reqnroll per `tests/README.md`.
- Wrap test and build commands in `timeout` (e.g. `timeout 600 dotnet test ...`) so a hung test host cannot linger in the background.
