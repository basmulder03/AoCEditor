using Avalonia.Controls;
using Avalonia.Platform;

namespace AoCEditor.UI.Session;

internal sealed record BrowserCookie(string Name, string Domain, string Value);
