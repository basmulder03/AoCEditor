# 0031 Puzzle examples

## Status
Proposed

## Context
Puzzles come with example inputs and expected answers inside the puzzle text. Solving is faster and safer when a solution can be run against these examples, and against the user's own edge cases, before the real input. Example text is AoC content, so it cannot go in the solutions repo (0007).

## Decision
- An example is: name, input text, expected answer per part (either may be empty), source (`puzzle` or `custom`), enabled flag. Stored per year/day in the local DB in app-data (0007).
- Select from the puzzle: the rendered puzzle view lets the user select a code block (or any text) and choose "Use as example input". The text is copied into a new example; the user then sets the expected answer, for example by selecting an emphasized value in the text.
- Create own: a form for name, input and expected answers for part 1 and part 2.
- Run: the run action offers "Examples", "Real input" or both; each example reports pass/fail per part with expected and actual output. A failing example never blocks running the real input.
- Examples are never written to the solutions folder or committed (0017). Optional export of `custom` examples only, as plain files the user chooses, is a later decision.
- Expected answers of examples are separate from the known real answer (0015) and never submitted.
- Local verification and performance tracking (0018) use the real input and its known answer only; example runs are not recorded as timings.

## Consequences
- Fast test loop that matches how people solve AoC.
- Selecting from rendered puzzle text needs a selectable, structured native renderer (0008).
- Exporting custom examples to a repo is a possible later feature; deciding it now would mix user-made and AoC content.

## Open questions
- Whether `custom` examples may live in the solutions repo (owner decision).
- Whether to suggest expected answers heuristically from the puzzle text.
