using Avalonia;
using Avalonia.Styling;

namespace UiKit;

public static class ThemeChoice
{
    public static readonly ThemeVariant HighContrast = new("HighContrast", ThemeVariant.Dark);

    public static readonly IReadOnlyList<string> Names = ["System", "Light", "Dark", "High contrast"];

    public static ThemeVariant Resolve(string name) => name switch
    {
        "Light" => ThemeVariant.Light,
        "Dark" => ThemeVariant.Dark,
        "High contrast" => HighContrast,
        _ => ThemeVariant.Default,
    };

    public static void Apply(Application app, string name) => app.RequestedThemeVariant = Resolve(name);
}
