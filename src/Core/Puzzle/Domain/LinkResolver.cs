namespace AoCEditor.Core.Puzzle.Domain;

public static class LinkResolver
{
    /// <summary>Resolves a puzzle link against the site address; only http and https links are openable. Without a site address only absolute links resolve.</summary>
    public static Uri? Resolve(Uri? site, string? href)
    {
        if (string.IsNullOrWhiteSpace(href) || href.StartsWith("//", StringComparison.Ordinal))
        {
            return null;
        }

        var ok = site is null ? Uri.TryCreate(href, UriKind.Absolute, out var uri) : Uri.TryCreate(site, href, out uri);
        return ok && uri!.Scheme is "http" or "https" ? uri : null;
    }
}
