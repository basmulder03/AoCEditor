---
name: new-slice
description: Checklist to add a vertical slice to the solution.
---
1. Issue exists with type + MoSCoW labels.
2. Folders `src/Core/<Slice>` (Domain, Application), `src/Infrastructure/<Slice>` and `src/UI/<Slice>` in the existing projects, namespace `AoCEditor.<Layer>.<Slice>[.<Folder>]`; never reference another slice (the architecture test fails).
3. Feature file in `tests/Specs`, unit tests in `tests/Core.Tests/<Slice>`, `tests/Infrastructure.Tests/<Slice>` or `tests/Ui.Tests/<Slice>`, both red first.
4. Implement, keep AoT/trim warnings at zero.
5. Add/update `docs/` page, then commit referencing the issue.
