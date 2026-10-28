using System.Security.Cryptography;
using System.Text;
using Shared;

namespace Editor.Application;

/// <summary>Edits one file inside a root folder. Saving never overwrites a file that changed on disk since it was loaded unless asked to.</summary>
public sealed class EditorViewModel(string rootFolder) : ObservableBase
{
    private static readonly UTF8Encoding Utf8 = new(false);

    private readonly string _root = Path.GetFullPath(rootFolder);
    private string? _path;
    private string _text = string.Empty;
    private string _savedText = string.Empty;
    private string _lineEnding = "\n";
    private bool _bom;
    private byte[]? _diskHash;
    private bool _changedOnDisk;

    public string? FilePath { get => _path; private set => Set(ref _path, value); }

    public string Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value;
                Raise();
                Raise(nameof(IsDirty));
            }
        }
    }

    public bool IsDirty => _text != _savedText;

    public bool ChangedOnDisk { get => _changedOnDisk; private set => Set(ref _changedOnDisk, value); }

    /// <summary>Loads a file, or starts an empty buffer when it does not exist yet. Throws <see cref="ArgumentException"/> for paths outside the root folder.</summary>
    public void Load(string path)
    {
        FilePath = Resolve(path);
        Reload();
    }

    public void Reload()
    {
        if (_path is null)
        {
            return;
        }

        var bytes = File.Exists(_path) ? File.ReadAllBytes(_path) : null;
        _bom = bytes is [0xEF, 0xBB, 0xBF, ..];
        _diskHash = Hash(bytes);
        var content = bytes is null ? string.Empty : Utf8.GetString(bytes, _bom ? 3 : 0, bytes.Length - (_bom ? 3 : 0));
        _lineEnding = content.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        _savedText = content;
        _text = content;
        Raise(nameof(Text));
        Raise(nameof(IsDirty));
        ChangedOnDisk = false;
    }

    /// <summary>Compares the file on disk with what was loaded; call when the editor regains focus.</summary>
    public void CheckDisk()
    {
        if (_path is not null)
        {
            ChangedOnDisk = !HashesEqual(Hash(ReadOrNull()), _diskHash);
        }
    }

    public SaveResult Save(bool overwrite = false)
    {
        if (_path is null)
        {
            return SaveResult.NoFile;
        }

        CheckDisk();
        if (ChangedOnDisk && !overwrite)
        {
            return SaveResult.ChangedOnDisk;
        }

        var normalized = _text.Replace("\r\n", "\n", StringComparison.Ordinal);
        var content = _lineEnding == "\r\n" ? normalized.Replace("\n", "\r\n", StringComparison.Ordinal) : normalized;
        byte[] bytes = [.. (_bom ? Encoding.UTF8.GetPreamble() : []), .. Utf8.GetBytes(content)];
        File.WriteAllBytes(_path, bytes);
        _diskHash = Hash(bytes);
        _savedText = _text;
        Raise(nameof(IsDirty));
        ChangedOnDisk = false;
        return SaveResult.Saved;
    }

    private byte[]? ReadOrNull() => File.Exists(_path) ? File.ReadAllBytes(_path) : null;

    private static byte[]? Hash(byte[]? bytes) => bytes is null ? null : SHA256.HashData(bytes);

    private static bool HashesEqual(byte[]? a, byte[]? b) => a is null ? b is null : b is not null && a.AsSpan().SequenceEqual(b);

    private string Resolve(string path)
    {
        var full = Path.GetFullPath(path, _root);
        var relative = Path.GetRelativePath(_root, full);
        if (relative == ".." || relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) || Path.IsPathRooted(relative))
        {
            throw new ArgumentException("The file is outside the solutions folder.", nameof(path));
        }

        return full;
    }
}
