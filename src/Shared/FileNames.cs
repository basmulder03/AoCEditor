namespace Shared;

public static class FileNames
{
    private static readonly HashSet<string> Reserved = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    /// <summary>A single name that is safe on every platform: no separators, drive or stream colons, device names or trailing dot/space.</summary>
    public static bool IsPlain(string name) =>
        name.Length > 0
        && name is not ("." or "..")
        && name.IndexOfAny(['/', '\\', '\0', ':']) < 0
        && name[^1] is not ('.' or ' ')
        && !Reserved.Contains(name.Split('.')[0]);
}
