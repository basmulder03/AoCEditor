using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Session.Application;

namespace Session.UI;

/// <summary>
/// Signs in through the site's own login page in an embedded browser and reads the HttpOnly session cookie from its cookie store.
/// The profile folder keeps provider logins between sign-ins.
/// </summary>
public sealed class WebViewSignInBrowser(Uri loginUrl, string profileFolder, ILogger? logger = null) : ISignInBrowser
{
    private readonly ILogger _log = logger ?? NullLogger.Instance;
    private readonly string _loginPage = SignInPage.HostAndPath(loginUrl);
    private const string CookieName = "session";
    private const string NoCookieAccess = "The embedded browser on this system does not give access to its cookies, so the session cannot be read from it.";
    private const string BlankPage = "data:text/html,<title>Sign in</title>";
    private const string WaitingText = "Sign in to Advent of Code. This window closes by itself when you are signed in.";

    public Task<BrowserOutcome> AcquireTokenAsync(TokenValidator validate, CancellationToken cancellationToken) =>
        Dispatcher.UIThread.InvokeAsync(() => RunAsync(validate, cancellationToken));

    private async Task<BrowserOutcome> RunAsync(TokenValidator validate, CancellationToken cancellationToken)
    {
        var done = new TaskCompletionSource<BrowserOutcome>(TaskCreationOptions.RunContinuationsAsynchronously);
        SignInWindow window;
        try
        {
            var owner = (Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            window = new SignInWindow { Status = WaitingText };
            window.Browser.EnvironmentRequested += (_, e) => UseProfile(e);
            window.Closed += (_, _) =>
            {
                if (!done.Task.IsCompleted)
                {
                    _log.WindowClosed();
                }

                done.TrySetResult(new BrowserOutcome.Cancelled());
            };
            var preflightDone = false;
            ICookieSource? cookies = null;
            var busy = false;
            window.Browser.NavigationCompleted += async (_, e) =>
            {
                if (busy || done.Task.IsCompleted)
                {
                    return;
                }

                busy = true;
                try
                {
                    if (!preflightDone)
                    {
                        preflightDone = true;
                        cookies = await PreflightAsync(window, done, cancellationToken);
                        return;
                    }

                    await OnNavigatedAsync(window, cookies!, e, validate, done, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _log.BrowserFailure(ex, "navigation");
                    done.TrySetResult(new BrowserOutcome.Unsupported(ex.Message));
                }
                finally
                {
                    busy = false;
                }
            };
            _log.WindowOpening(_loginPage);
            window.Browser.Source = new Uri(BlankPage);
            window.PlaceOver(owner);
            if (owner is null)
            {
                window.Show();
            }
            else
            {
                window.Show(owner);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Missing runtime libraries surface as assorted platform, DLL-load or COM exceptions.
            _log.BrowserFailure(ex, "open");
            return new BrowserOutcome.Unsupported(ex.Message);
        }

        using (cancellationToken.Register(() => done.TrySetResult(new BrowserOutcome.Cancelled())))
        {
            var outcome = await done.Task;
            window.Close();
            return outcome;
        }
    }

    // Without cookie access the session can never be read, so the site is not even loaded.
    private async Task<ICookieSource?> PreflightAsync(SignInWindow window, TaskCompletionSource<BrowserOutcome> done, CancellationToken cancellationToken)
    {
        var source = CookieSources.For(window.Browser);
        var reason = source is null
            ? NoCookieAccess
            : await source.ReadAsync(loginUrl, cancellationToken) is CookieRead.Unsupported unsupported ? unsupported.Reason : null;
        if (reason is not null)
        {
            _log.CookiesRead(false, 0, false);
            done.TrySetResult(new BrowserOutcome.Unsupported(reason));
            return null;
        }

        window.Browser.Source = loginUrl;
        return source;
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

    private async Task OnNavigatedAsync(SignInWindow window, ICookieSource cookies, WebViewNavigationCompletedEventArgs e, TokenValidator validate, TaskCompletionSource<BrowserOutcome> done, CancellationToken cancellationToken)
    {
        var current = e.Request ?? window.Browser.Source;
        var page = SignInPage.HostAndPath(current);
        _log.Navigated(page, e.IsSuccess);

        if (e.IsSuccess)
        {
            await FitToWidthAsync(window.Browser);
        }

        if (await cookies.ReadAsync(loginUrl, cancellationToken) is not CookieRead.Cookies read)
        {
            _log.CookiesRead(false, 0, false);
            done.TrySetResult(new BrowserOutcome.Unsupported(NoCookieAccess));
            return;
        }

        var all = read.Items;
        var session = CookieSources.Find(all, CookieName, loginUrl.Host);
        var found = session is { Value.Length: > 0 };
        _log.CookiesRead(true, all.Count, found);
        if (SignInPage.Candidate(current, e.IsSuccess, session?.Value, loginUrl.Host) is not { } candidate)
        {
            _log.NoCandidate(page);
            window.Status = WaitingText;
            return;
        }

        window.Status = "Checking your sign-in...";
        switch (await validate(candidate, cancellationToken))
        {
            case CandidateVerdict.Accepted:
                done.TrySetResult(new BrowserOutcome.Token(candidate));
                break;
            case CandidateVerdict.Rejected:
                await cookies.DeleteAsync(loginUrl, CookieName, cancellationToken);
                window.Status = "Advent of Code did not accept that sign-in. Sign in again to continue.";
                window.Browser.Source = loginUrl;
                break;
            default:
                window.Status = "Could not reach Advent of Code. Reload the page to try again.";
                break;
        }
    }

    // Desktop WebKit and WebView2 ignore the viewport meta tag unless the page has one; adding it makes narrow windows lay out at their own width.
    private static async Task FitToWidthAsync(NativeWebView web)
    {
        try
        {
            await web.InvokeScript(
                "(function(){var m=document.querySelector('meta[name=viewport]');if(!m){m=document.createElement('meta');m.name='viewport';document.head.appendChild(m);}m.content='width=device-width, initial-scale=1';})()");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Cosmetic only; the sign-in still works without it.
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
