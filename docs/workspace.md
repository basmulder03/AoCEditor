# Solutions workspace

`WorkspaceService` (`src/Infrastructure/Workspace`; `WorkspaceLayout` is in `src/Core/Workspace`) owns the folder where the user's solutions live. Layout (ADR 0016):

```
<root>/<year>/<dd>/<language>/   a day's solution files
<root>/shared/<language>/        helpers shared across years
```

- `SetRootAsync(path)`: the user picks the root once. It must be an absolute path and must not overlap the app-data folder (`Shared.DataRootValidator`), so inputs and puzzle text can never end up in the solutions repo. The folder is created when missing and remembered in the settings store (`workspace.root`).
- `ScaffoldDayAsync(year, day, language, files)`: runs only on user action. It creates the day folder and writes the template files that do not exist yet; existing files are never overwritten. Template names must be plain file names (`Shared.FileNames.IsPlain`: no separators, `..`, colons such as `C:x` or `name:stream`, Windows device names like `CON`, `NUL`, `COM1`-`COM9`/`LPT1`-`LPT9` with or without extension, or trailing dot/space), the resolved path must stay under the day folder, and a language id is lowercase letters, digits, `-` and `_`, so a plugin cannot write outside the day folder.
- `LanguagesAsync(year, day)`: languages that already have a folder for the day, for the language picker.

Templates and the `shared/` wiring come from the language plugins; the plugin manifest slice is next. Offering a `.gitignore` at creation waits for the plugins too, since build outputs differ per language.
