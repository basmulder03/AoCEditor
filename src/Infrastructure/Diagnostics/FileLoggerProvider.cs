using System.Globalization;
using AoCEditor.Core.Diagnostics.Application;
using Microsoft.Extensions.Logging;

namespace AoCEditor.Infrastructure.Diagnostics;

/// <summary>Logger provider that appends to <c>aoceditor.log</c>; rolls when full and keeps few, recent files.</summary>
public sealed class FileLoggerProvider : ILoggerProvider
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

    public FileLoggerProvider(string folder, TimeProvider time, long maxBytes, int maxFiles, TimeSpan maxAge)
    {
        _folder = folder;
        _time = time;
        _maxBytes = maxBytes;
        _maxFiles = maxFiles;
        _maxAge = maxAge;
        try
        {
            Directory.CreateDirectory(folder);
            var current = Path.Combine(folder, CurrentFileName);
            _size = File.Exists(current) ? new FileInfo(current).Length : 0;
            Prune();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Logging to disk is optional; the other providers still work.
        }
    }

    public ILogger CreateLogger(string categoryName) => new FileLogger(this, LogEntry.SourceOf(categoryName));

    public void Dispose()
    {
    }

    private void Write(LogEntry entry)
    {
        var lines = entry.Message.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var text = string.Concat(
            $"{entry.Time.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture)} {entry.Level} {entry.Source} {lines[0]}{Environment.NewLine}",
            string.Concat(lines.Skip(1).Select(l => $"    {l}{Environment.NewLine}")));
        lock (_lock)
        {
            try
            {
                var current = Path.Combine(_folder, CurrentFileName);
                if (_size > 0 && _size + text.Length > _maxBytes)
                {
                    Roll(current);
                }

                File.AppendAllText(current, text);
                _size += text.Length;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // A full or unwritable disk must never break the app.
            }
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

    private sealed class FileLogger(FileLoggerProvider owner, string source) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                owner.Write(new LogEntry(owner._time.GetUtcNow(), logLevel, source, formatter(state, exception)));
            }
        }
    }
}
