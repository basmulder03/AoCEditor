# Tests

- Unit: `dotnet test --project tests/Shared.Tests`
- Specs (Reqnroll): `dotnet test --project tests/Specs`
- E2E (starts the built App): `dotnet test --project tests/E2E`

Filter by category in a mixed run: `dotnet test --solution AoCEditor.slnx --filter-not-trait "Category=E2E" --ignore-exit-code 8` (exit code 8 is "zero tests ran" in the all-E2E project) (or `--filter-trait "Category=E2E"`).
