using System.Globalization;
using System.Xml.Linq;
using Shouldly;
using Xunit;

namespace Ui.Tests;

public class ContrastTests
{
    static readonly XNamespace X = "http://schemas.microsoft.com/winfx/2006/xaml";

    public static TheoryData<string, string, string, double> Cases()
    {
        var data = new TheoryData<string, string, string, double>();
        foreach (var theme in new[] { "Light", "Dark", "HighContrast" })
        {
            foreach (var (fg, bg, min) in Pairs)
            {
                data.Add(theme, fg, bg, min);
            }
        }
        return data;
    }

    static readonly (string Fg, string Bg, double Min)[] Pairs =
    [
        ("TextColor", "BackgroundColor", 4.5),
        ("TextColor", "SurfaceColor", 4.5),
        ("TextMutedColor", "BackgroundColor", 4.5),
        ("TextMutedColor", "SurfaceColor", 4.5),
        ("OnAccentColor", "AccentColor", 4.5),
        ("BorderColor", "BackgroundColor", 3.0),
        ("BorderColor", "SurfaceColor", 3.0),
        ("FocusColor", "BackgroundColor", 3.0),
        ("FocusColor", "SurfaceColor", 3.0),
        ("AccentColor", "BackgroundColor", 3.0),
    ];

    [Theory]
    [MemberData(nameof(Cases))]
    public void TokenPairMeetsWcagAa(string theme, string fg, string bg, double min)
    {
        var colors = Load(theme);
        var ratio = Ratio(colors[fg], colors[bg]);
        ratio.ShouldBeGreaterThanOrEqualTo(min, $"{theme}: {fg} on {bg} is {ratio:F2}");
    }

    static Dictionary<string, string> Load(string theme)
    {
        var doc = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Tokens", "Colors.axaml"));
        var dict = doc.Descendants().Single(e => (string?)e.Attribute(X + "Key") == theme && e.Name.LocalName == "ResourceDictionary");
        return dict.Elements().Where(e => e.Name.LocalName == "Color")
            .ToDictionary(e => (string)e.Attribute(X + "Key")!, e => e.Value.Trim());
    }

    static double Ratio(string a, string b)
    {
        var (l1, l2) = (Luminance(a), Luminance(b));
        return (Math.Max(l1, l2) + 0.05) / (Math.Min(l1, l2) + 0.05);
    }

    static double Luminance(string hex)
    {
        double Channel(int i)
        {
            var c = int.Parse(hex.AsSpan(1 + i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture) / 255.0;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }
        return 0.2126 * Channel(0) + 0.7152 * Channel(1) + 0.0722 * Channel(2);
    }
}
