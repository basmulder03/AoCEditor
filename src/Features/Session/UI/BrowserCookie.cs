using Avalonia.Controls;
using Avalonia.Platform;

namespace Session.UI;

internal sealed record BrowserCookie(string Name, string Domain, string Value);
