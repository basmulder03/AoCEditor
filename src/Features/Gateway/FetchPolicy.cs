namespace Gateway;

public enum FetchPolicy
{
    /// <summary>Use the cached copy when present; only go to the network on a miss.</summary>
    CacheFirst,

    /// <summary>Always ask the server; used only when the user explicitly requests a refresh.</summary>
    Refresh,
}
