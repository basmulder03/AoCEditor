using System.Globalization;
using Shared;

namespace AoCEditor.Core.Workspace.Domain;

/// <summary>Where solutions live: <c>&lt;root&gt;/&lt;year&gt;/&lt;dd&gt;/&lt;language&gt;</c> and <c>&lt;root&gt;/shared/&lt;language&gt;</c>.</summary>
public static class WorkspaceLayout
{
    public static string DayFolder(string root, int year, int day, string language)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(year, AocEvent.FirstYear);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(year, 9999);
        ArgumentOutOfRangeException.ThrowIfLessThan(day, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(day, 25);
        return Path.Combine(root, year.ToString(CultureInfo.InvariantCulture), day.ToString("00", CultureInfo.InvariantCulture), CheckLanguage(language));
    }

    public static string SharedFolder(string root, string language) => Path.Combine(root, "shared", CheckLanguage(language));

    private static string CheckLanguage(string language)
    {
        var ok = language.Length > 0
            && language.All(c => c is (>= 'a' and <= 'z') or (>= '0' and <= '9') or '_' or '-')
            && language[0] is not ('-' or '_');
        return ok ? language : throw new ArgumentException("A language id is lowercase letters, digits, '-' and '_'.", nameof(language));
    }
}
