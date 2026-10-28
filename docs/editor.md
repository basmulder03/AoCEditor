# Code editor

`EditorViewModel` and `EditorView` (`src/Features/Editor`) edit one solution file with AvaloniaEdit.

- **Folder rule.** The view model is created with a root folder (the solutions workspace). `Load(path)` throws `ArgumentException` for any path that resolves outside it, including `..` segments. Symbolic links inside the folder are not resolved.
- **Encoding and line endings.** Files are read and written as UTF-8 without BOM. A BOM present on load is kept, and a CRLF file stays CRLF on save (otherwise LF). A missing file starts an empty buffer and is created on save.
- **External changes.** The SHA-256 of the file is remembered on load and save. `CheckDisk()` (the view calls it when the editor gets focus) sets `ChangedOnDisk` when the content differs or the file is gone; a rewrite with identical content is not a change. `Save()` re-checks and returns `SaveResult.ChangedOnDisk` without writing. The view then shows "This file changed on disk." with **Reload** (`Reload()`, discards edits) and **Save anyway** (`Save(overwrite: true)`).
- **Highlighting.** `Highlighting.ScopeFor` maps the extension to a TextMate scope (`.py` to `source.python`, `.cs` to `source.cs`); unknown extensions get no highlighting. AvaloniaEdit's built-in xshd definitions have no Python, so TextMate (AvaloniaEdit.TextMate 12.0.0) is used. The theme is fixed to Light+ for now and does not follow the app theme yet.
- **Accessibility.** The editor has the automation name "Code editor", a monospace font, and a 2 px focus-brush frame while it has focus. Ctrl+S saves. How well screen readers read AvaloniaEdit's text is unverified.

The app must include the AvaloniaEdit theme (`avares://AvaloniaEdit/Themes/Fluent/AvaloniaEdit.xaml`) in its styles for the editor to render correctly.
