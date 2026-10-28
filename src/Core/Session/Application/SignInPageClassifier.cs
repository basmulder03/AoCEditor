namespace AoCEditor.Core.Session.Application;

/// <summary>Decides from what the embedded browser shows whether a session token may be ready.</summary>
public static class SignInPageClassifier
{
    /// <summary>The session cookie value once a finished page outside the login hops is shown; otherwise null (keep waiting).</summary>
    public static string? Candidate(Uri? current, bool navigationFinished, string? sessionCookie, string siteHost) =>
        navigationFinished
        && current is not null
        && string.Equals(current.Host, siteHost, StringComparison.OrdinalIgnoreCase)
        && !current.AbsolutePath.StartsWith("/auth/", StringComparison.Ordinal)
        && sessionCookie is { Length: > 0 }
            ? sessionCookie
            : null;

    /// <summary>What may be logged about a page address: host and path only, never query, fragment or credentials.</summary>
    public static string HostAndPath(Uri? uri) => uri is null ? "(none)" : uri.Host + uri.AbsolutePath;
}
