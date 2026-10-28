using Avalonia.Controls;
using Avalonia.Platform;
using Session.Application;

namespace Session.UI;

/// <summary>
/// Signs in through the site's own login page in an embedded browser and reads the HttpOnly session cookie from its cookie store.
/// The profile folder keeps provider logins between sign-ins.
/// </summary>
public sealed class WebViewSignInBrowser(Uri loginUrl, string profileFolder) : ISignInBrowser
{
    private const string CookieName = "session";

    public async Task<BrowserOutcome> AcquireTokenAsync(CancellationToken cancellationToken)
    {
        var done = new TaskCompletionSource<BrowserOutcome>(TaskCreationOptions.RunContinuationsAsynchronously);
        NativeWebDialog dialog;
        try
        {
            dialog = new NativeWebDialog { Title = "Sign in to Advent of Code" };
            dialog.EnvironmentRequested += (_, e) => UseProfile(e);
            dialog.Closing += (_, _) => done.TrySetResult(new BrowserOutcome.Cancelled());
            dialog.NavigationCompleted += async (_, e) =>
            {
                try
                {
                    await OnNavigatedAsync(dialog, e.Request, done);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    done.TrySetResult(new BrowserOutcome.Unsupported(ex.Message));
                }
            };
            dialog.Navigate(loginUrl);
            dialog.Show();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Missing runtime libraries surface as assorted platform, DLL-load or COM exceptions.
            return new BrowserOutcome.Unsupported(ex.Message);
        }

        using (cancellationToken.Register(() => done.TrySetResult(new BrowserOutcome.Cancelled())))
        {
            var outcome = await done.Task;
            dialog.Dispose();
            return outcome;
        }
    }

    /// <summary>Forgets cookies and provider logins; false when files are still in use.</summary>
    public bool TryClearProfile()
    {
        try
        {
            if (Directory.Exists(profileFolder))
            {
                Directory.Delete(profileFolder, recursive: true);
            }

            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private async Task OnNavigatedAsync(NativeWebDialog dialog, Uri? address, TaskCompletionSource<BrowserOutcome> done)
    {
        // Provider pages and the login page itself are not the end of the flow.
        if (done.Task.IsCompleted || address is null || address.Host != loginUrl.Host
            || address.AbsolutePath.StartsWith("/auth/", StringComparison.Ordinal))
        {
            return;
        }

        var cookies = dialog.TryGetCookieManager();
        if (cookies is null)
        {
            done.TrySetResult(new BrowserOutcome.Unsupported("The embedded browser cannot give access to cookies."));
            return;
        }

        var all = await cookies.GetCookiesAsync();
        var session = all.FirstOrDefault(c => c.Name == CookieName && loginUrl.Host.EndsWith(c.Domain.TrimStart('.'), StringComparison.OrdinalIgnoreCase));
        if (session is { Value.Length: > 0 })
        {
            done.TrySetResult(new BrowserOutcome.Token(session.Value));
            dialog.Close();
        }
    }

    private void UseProfile(WebViewEnvironmentRequestedEventArgs e)
    {
        Directory.CreateDirectory(profileFolder);
        switch (e)
        {
            case GtkWebViewEnvironmentRequestedEventArgs gtk:
                gtk.BaseDataDirectory = profileFolder;
                gtk.BaseCacheDirectory = Path.Combine(profileFolder, "cache");
                break;
            case WindowsWebView2EnvironmentRequestedEventArgs win:
                win.UserDataFolder = profileFolder;
                break;
        }
    }
}
