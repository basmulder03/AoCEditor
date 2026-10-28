using System.Globalization;
using AoCEditor.Core.Diagnostics.Application;

namespace AoCEditor.Infrastructure.Diagnostics;

/// <summary>Appends to <c>aoceditor.log</c>; rolls when full and keeps few, recent files.</summary>
public sealed class FileLogSink : ILogSink
{
    public const string CurrentFileName = "aoceditor.log";
    private const string Prefix = "aoceditor";

    private readonly string _folder;
    private readonly TimeProvider _time;
    private readonly long _maxBytes;
    private readonly int _maxFiles;
    private readonly TimeSpan _maxAge;
    private readonly Lock _lock = new();
    private long _size;

    public FileLogSink(string folder, TimeProvider time, long maxBytes, int maxFiles, TimeSpan maxAge)
    {
        _folder = folder;
        _time = time;
        _maxBytes = maxBytes;
        _maxFiles = maxFiles;
        _maxAge = maxAge;
        Directory.CreateDirectory(folder);
        var current = Path.Combine(folder, CurrentFileName);
        _size = File.Exists(current) ? new FileInfo(current).Length : 0;
        Prune();
    }

    public void Write(LogEntry entry)
    {
        var lines = entry.Message.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var text = string.Concat(
            $"{entry.Time.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)} {entry.Level} {entry.Source} {lines[0]}{Environment.NewLine}",
            string.Concat(lines.Skip(1).Select(l => $"    {l}{Environment.NewLine}")));
        lock (_lock)
        {
            var current = Path.Combine(_folder, CurrentFileName);
            if (_size > 0 && _size + text.Length > _maxBytes)
            {
                Roll(current);
            }

            File.AppendAllText(current, text);
            _size += text.Length;
        }
    }

    private void Roll(string current)
    {
        var stamp = _time.GetUtcNow().ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);
        File.Move(current, Path.Combine(_folder, $"{Prefix}-{stamp}.log"), overwrite: true);
        _size = 0;
        Prune();
    }

    private void Prune()
    {
        var files = new DirectoryInfo(_folder).GetFiles($"{Prefix}*.log").Where(f => f.Name != CurrentFileName).OrderByDescending(f => f.LastWriteTimeUtc).ToList();
        var cutoff = _time.GetUtcNow().UtcDateTime - _maxAge;
        foreach (var file in files.Where((f, i) => i >= _maxFiles - 1 || f.LastWriteTimeUtc < cutoff))
        {
            file.Delete();
        }
    }
}
