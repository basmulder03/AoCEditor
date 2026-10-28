using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;

namespace AoCEditor;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var provider = new ServiceCollection().AddAocEditor().BuildServiceProvider();
            desktop.MainWindow = provider.GetRequiredService<ShellWindow>();
            desktop.Exit += (_, _) => provider.Dispose();
        }
        base.OnFrameworkInitializationCompleted();
    }
}
