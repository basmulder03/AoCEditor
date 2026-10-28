using Avalonia.Controls;
using Avalonia.Platform;

namespace AoCEditor.UI.Session;

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
    internal static async Task<CookieRead> GuardAsync(Func<CancellationToken, Task<CookieRead>> read, TimeSpan timeout, CancellationToken cancellationToken)
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
