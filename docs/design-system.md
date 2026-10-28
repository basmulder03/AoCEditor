# Design system

The `src/UiKit` project holds all design tokens and base control styles. Features use tokens only; no hardcoded colors or sizes.

## Tokens

- `Tokens/Colors.axaml`: per-theme colors (`BackgroundColor`, `SurfaceColor`, `TextColor`, `TextMutedColor`, `BorderColor`, `AccentColor`, `OnAccentColor`, `FocusColor`) in `ThemeDictionaries` keyed `Light`, `Dark`, `HighContrast`.
- `Tokens/Brushes.axaml`: a `*Brush` for every color, resolved dynamically so it follows the active theme.
- `Tokens/Metrics.axaml`: spacing (`Space1..6`), radius (`RadiusSm/Md/Lg`), type scale (`FontSize*`), border and focus widths (`BorderWidth`, `FocusWidth` are `Thickness`, since they feed `BorderThickness`), insets (`InsetXs`, `InsetSm`, `InsetMd`, `InsetTopMd`) for `Margin`, motion durations (`Motion*Ms`).
- `Icons/Icons.axaml`: vector icons as `StreamGeometry` (`IconLocked`, `IconOpen`, `IconOneStar`, `IconTwoStars`), used with `PathIcon` and `IconSize`.
- `Styles/Controls.axaml`: base styles for Window, TextBlock, Button, TextBox, TabControl, ListBox and its items, with a visible focus adorner.

## Themes

`ThemeChoice` maps System, Light, Dark and High contrast to Avalonia theme variants; System follows the OS. `ThemeChoice.Apply(app, name)` switches at runtime (Settings page uses it). The Fluent theme supplies control templates underneath; UiKit styles override colors and sizes.

## Add a token

Add the color to all three theme dictionaries in `Colors.axaml`, add its brush in `Brushes.axaml`, and if it is text or UI, add its pair to `Pairs` in `tests/UI.Tests/ContrastTests.cs`. Metrics go in `Metrics.axaml`.

## Add a theme

Add a `ResourceDictionary` with the new key and all colors to `Colors.axaml`, define a `ThemeVariant` in `ThemeChoice`, and add it to the contrast test list. Rebranding means editing token values only.

## Accessibility rules

- WCAG 2.2 AA: 4.5:1 for text, 3:1 for UI components and focus indicators. `ContrastTests` parses `Colors.axaml` and checks every pair in every theme.
- Every focusable control shows the focus adorner (`FocusColor`, `FocusWidth`).
- Interactive controls set `AutomationProperties.Name`.
- Everything is keyboard reachable; the navigation list uses arrow keys, Tab moves into the page.
- No information by color alone; no animations are defined, so reduced motion is respected by default.
