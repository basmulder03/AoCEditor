using AoCEditor;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Shouldly;
using Xunit;

[assembly: AvaloniaTestApplication(typeof(AoCEditor.UI.Tests.TestAppBuilder))]

namespace AoCEditor.UI.Tests;

public class ShellTests
{
    [AvaloniaFact]
    public void NavigatesToSettingsAndSwitchesTheme()
    {
        var shell = new ShellWindow();
        shell.Show();

        shell.CurrentPage.ShouldBeOfType<CalendarPage>();
        shell.NavigateTo("Settings");
        var settings = shell.CurrentPage.ShouldBeOfType<SettingsPage>();

        settings.SelectTheme("Dark");
        Application.Current!.ActualThemeVariant.ShouldBe(ThemeVariant.Dark);
        Application.Current.TryFindResource("TextBrush", ThemeVariant.Dark, out var dark).ShouldBeTrue();
        ((Avalonia.Media.ISolidColorBrush)dark!).Color.ShouldBe(Avalonia.Media.Color.Parse("#F9FAFB"));
        settings.SelectTheme("Light");
        Application.Current!.ActualThemeVariant.ShouldBe(ThemeVariant.Light);
    }
}
