using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Session.UI;
using Shouldly;
using Xunit;

namespace Session.Tests;

public sealed class LinuxCookieSourceIntegrationTests
{
    public static bool CanRun =>
        OperatingSystem.IsLinux()
        && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY"))
        && NativeLibrary.TryLoad("libwebkit2gtk-4.1.so.0", out _);

    private sealed class TestApp : Avalonia.Application
    {
        public static TaskCompletionSource<CookieRead> Outcome { get; } = new();

        public override void OnFrameworkInitializationCompleted()
        {
            var web = new NativeWebView();
            var window = new Window { Width = 400, Height = 300, Content = web };
            var asked = false;
            web.NavigationCompleted += async (_, _) =>
            {
                if (asked)
                {
                    return;
                }

                asked = true;
                var source = CookieSources.For(web);
                Outcome.TrySetResult(source is null
                    ? new CookieRead.Unsupported("no source")
                    : await source.ReadAsync(new Uri("https://adventofcode.com/"), CancellationToken.None));
                ((IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!).Shutdown();
            };
            ((IClassicDesktopStyleApplicationLifetime)ApplicationLifetime!).MainWindow = window;
            window.Opened += (_, _) => web.Source = new Uri("data:text/html,<title>x</title>");
            base.OnFrameworkInitializationCompleted();
        }
    }

    [Fact(SkipUnless = nameof(CanRun), SkipType = typeof(LinuxCookieSourceIntegrationTests), Skip = "Needs Linux, a display and libwebkit2gtk-4.1.")]
    public async Task ARealWebViewAnswersWithNoCookiesForABlankPage()
    {
        var ui = new Thread(() => AppBuilder.Configure<TestApp>().UsePlatformDetect().StartWithClassicDesktopLifetime([])) { IsBackground = true };
        ui.Start();

        var result = await TestApp.Outcome.Task.WaitAsync(TimeSpan.FromSeconds(60), TestContext.Current.CancellationToken);

        result.ShouldBeOfType<CookieRead.Cookies>().Items.ShouldBeEmpty();
    }
}
