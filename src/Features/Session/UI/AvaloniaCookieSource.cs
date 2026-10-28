using Avalonia.Controls;
using Avalonia.Platform;

namespace Session.UI;

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
