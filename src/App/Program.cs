using AoCEditor.Infrastructure.Runner;
using Avalonia;

namespace AoCEditor;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        if (SandboxHelper.RunIfRequested(args) is { } helperExit)
        {
            return helperExit;
        }

        if (args.Contains("--version"))
        {
            Console.WriteLine($"AoCEditor {Shared.AppInfo.Version}");
            return 0;
        }
        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect();
}
