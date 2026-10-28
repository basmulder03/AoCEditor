using Avalonia.Controls;
using Avalonia.Platform;

namespace Session.UI;

/// <summary>Reads and deletes the cookies of the embedded browser, whatever the platform offers.</summary>
internal interface ICookieSource
{
    Task<CookieRead> ReadAsync(Uri site, CancellationToken cancellationToken);

    Task DeleteAsync(Uri site, string name, CancellationToken cancellationToken);
}
