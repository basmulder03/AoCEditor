# 0022 Design system and accessibility

## Status
Proposed

## Context
The UI is Avalonia with an own design system (ADR 0002). Users solve puzzles for long sessions, so contrast, keyboard use and screen-reader support matter.

## Decision
- A dedicated UI-kit project holds design tokens (color, spacing, type scale, radius, motion) as Avalonia resources, and styles/controls built only from tokens. Features do not hardcode colors or sizes.
- Themes: light, dark, and a high-contrast theme, switchable at runtime; default follows the OS.
- Target WCAG 2.2 AA: 4.5:1 text contrast, 3:1 for UI components and focus indicators, visible focus on every control, no information by color alone.
- Everything is reachable by keyboard with a logical tab order; shortcuts are listed in docs and rebindable later.
- Controls set Avalonia automation properties (names, roles) so screen readers can read them. Editor and terminal are custom controls and need explicit work here.
- Respect OS text scaling and reduced-motion settings.
- Token contrast is checked by a unit test over theme definitions.

## Consequences
- Slower first UI slices, consistent look afterwards.
- Custom editor/terminal accessibility will likely be partial at first; track gaps as issues.

## Open questions
- Avalonia's screen-reader support level on Linux (AT-SPI) is unverified.
- Whether AvaloniaEdit exposes usable automation peers.
