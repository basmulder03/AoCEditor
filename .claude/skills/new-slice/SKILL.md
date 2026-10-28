---
name: new-slice
description: Checklist to add a vertical slice to the solution.
---
1. Issue exists with type + MoSCoW labels.
2. Folder `src/Features/<Slice>/{Domain,Application,Infrastructure,UI}`; no references between slices except via `src/Shared` contracts.
3. Feature file in `tests/Specs`, unit tests in `tests/<Slice>.Tests`, both red first.
4. Implement, keep AoT/trim warnings at zero.
5. Add/update `docs/` page, then commit referencing the issue.
