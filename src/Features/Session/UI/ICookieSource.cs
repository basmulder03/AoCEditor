using Avalonia.Controls;
using Avalonia.Platform;

namespace Session.UI;

internal sealed record BrowserCookie(string Name, string Domain, string Value);

internal abstract record CookieRead
{
    public sealed record Cookies(IReadOnlyList<BrowserCookie> Items) : CookieRead;

    public sealed record Unsupported(string Reason) : CookieRead;
}

/// <summary>Reads and deletes the cookies of the embedded browser, whatever the platform offers.</summary>
internal interface ICookieSource
{
    Task<CookieRead> ReadAsync(Uri site, CancellationToken cancellationToken);

    Task DeleteAsync(Uri site, string name, CancellationToken cancellationToken);
}

internal static class CookieSources
{
    internal static readonly TimeSpan NativeTimeout = TimeSpan.FromSeconds(10);

    /// <summary>The first source that exists: the browser's own cookie manager, else the Linux WebKitGTK reader; null when neither applies.</summary>
    internal static ICookieSource? Choose(params Func<ICookieSource?>[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (candidate() is { } source)
            {
                return source;
            }
        }

        return null;
    }

    internal static ICookieSource? For(NativeWebView web) => Choose(
        () => web.TryGetCookieManager() is { } manager ? new AvaloniaCookieSource(manager) : null,
        () => OperatingSystem.IsLinux() && web.TryGetPlatformHandle() is IGtkWebViewPlatformHandle gtk ? new LinuxCookieSource(gtk.WebKitWebView) : null);

    /// <summary>Turns a hang or failure of a native read into an Unsupported result instead of an exception.</summary>
    internal static async Task<CookieRead> Guard(Func<CancellationToken, Task<CookieRead>> read, TimeSpan timeout, CancellationToken cancellationToken)
    {
        try
        {
            return await read(cancellationToken).WaitAsync(timeout, cancellationToken);
        }
        catch (TimeoutException)
        {
            return new CookieRead.Unsupported("The embedded browser did not answer when asked for its cookies.");
        }
    }

    /// <summary>The cookie with the given name that applies to the host (its domain is the host or a parent of it).</summary>
    internal static BrowserCookie? Find(IEnumerable<BrowserCookie> cookies, string name, string host) =>
        cookies.FirstOrDefault(c => c.Name == name && host.EndsWith(c.Domain.TrimStart('.'), StringComparison.OrdinalIgnoreCase));
}

internal sealed class AvaloniaCookieSource(NativeWebViewCookieManager manager) : ICookieSource
{
    public async Task<CookieRead> ReadAsync(Uri site, CancellationToken cancellationToken) =>
        new CookieRead.Cookies([.. (await manager.GetCookiesAsync()).Select(c => new BrowserCookie(c.Name, c.Domain, c.Value))]);

    public async Task DeleteAsync(Uri site, string name, CancellationToken cancellationToken)
    {
        foreach (var cookie in (await manager.GetCookiesAsync()).Where(c => c.Name == name))
        {
            manager.DeleteCookie(cookie);
        }
    }
}
