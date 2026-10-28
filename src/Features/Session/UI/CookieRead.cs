using Avalonia.Controls;
using Avalonia.Platform;

namespace Session.UI;

internal abstract record CookieRead
{
    public sealed record Cookies(IReadOnlyList<BrowserCookie> Items) : CookieRead;

    public sealed record Unsupported(string Reason) : CookieRead;
}
