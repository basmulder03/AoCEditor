using Avalonia;
using Avalonia.Controls;
using UiKit;

namespace AoCEditor;

public partial class SettingsPage : UserControl
{
    public SettingsPage()
    {
        InitializeComponent();
        ThemeBox.ItemsSource = ThemeChoice.Names;
        ThemeBox.SelectedItem = ThemeChoice.Names.FirstOrDefault(n => ThemeChoice.Resolve(n) == Application.Current!.RequestedThemeVariant) ?? "System";
    }

    public void SelectTheme(string name) => ThemeBox.SelectedItem = name;

    void OnThemeChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ThemeBox.SelectedItem is string name)
        {
            ThemeChoice.Apply(Application.Current!, name);
        }
    }
}
