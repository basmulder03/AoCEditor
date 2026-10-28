using TextMateSharp.Grammars;

namespace AoCEditor.UI.Editor;

public static class Highlighting
{
    /// <summary>TextMate scope for the file's extension, or null when no grammar is known.</summary>
    public static string? ScopeFor(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Length == 0 ? null : new RegistryOptions(ThemeName.LightPlus).GetScopeByExtension(extension);
    }
}
