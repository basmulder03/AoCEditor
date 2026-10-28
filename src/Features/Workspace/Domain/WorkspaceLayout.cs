using System.Globalization;

namespace Workspace.Domain;

/// <summary>Where solutions live: <c>&lt;root&gt;/&lt;year&gt;/&lt;dd&gt;/&lt;language&gt;</c> and <c>&lt;root&gt;/shared/&lt;language&gt;</c>.</summary>
public static class WorkspaceLayout
{
    public const int FirstYear = 2015;

    public static string DayFolder(string root, int year, int day, string language)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(year, FirstYear);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(year, 9999);
        ArgumentOutOfRangeException.ThrowIfLessThan(day, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(day, 25);
        return Path.Combine(root, year.ToString(CultureInfo.InvariantCulture), day.ToString("00", CultureInfo.InvariantCulture), CheckLanguage(language));
    }

    public static string SharedFolder(string root, string language) => Path.Combine(root, "shared", CheckLanguage(language));

    /// <summary>Template files are written straight into the day folder, so a name must not reach out of it.</summary>
    public static bool IsPlainFileName(string name) =>
        name.Length > 0 && name is not ("." or "..") && name.IndexOfAny(['/', '\\', '\0']) < 0;

    private static string CheckLanguage(string language)
    {
        var ok = language.Length > 0
            && language.All(c => c is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '_' or '-')
            && language[0] is not ('-' or '_');
        return ok ? language : throw new ArgumentException("A language id is lowercase letters, digits, '-' and '_'.", nameof(language));
    }
}
