using AoCEditor.Core.Diagnostics.Application;
using AoCEditor.Core.Diagnostics.Domain;
using AoCEditor.Infrastructure.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Contracts;

namespace AoCEditor;

public static class DiagnosticsRegistration
{
    public const long MaxFileBytes = 2 * 1024 * 1024;
    public const int MaxFiles = 5;
    public static readonly TimeSpan MaxAge = TimeSpan.FromDays(7);
    public const int BufferCapacity = 1000;
    public const int RequestCapacity = 100;

    /// <summary>
    /// Registers logging with file, buffer and console providers. Every provider is wrapped so it only sees
    /// redacted text, and the minimum level follows debug mode at runtime. Needs TimeProvider and IAppDataPaths.
    /// </summary>
    public static IServiceCollection AddDiagnostics(this IServiceCollection services, DebugMode? debug = null, string? homeDirectory = null)
    {
        var mode = debug ?? DebugMode.FromLaunch(Environment.GetCommandLineArgs(), Environment.GetEnvironmentVariable);
        services
            .AddSingleton(mode)
            .AddSingleton(new Redactor(homeDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)))
            .AddSingleton(new GatewayRequestHistory(RequestCapacity))
            .AddSingleton(sp => new BufferLoggerProvider(BufferCapacity, sp.GetRequiredService<TimeProvider>()))
            .AddSingleton(sp => new DiagnosticsView(
                sp.GetRequiredService<BufferLoggerProvider>(), sp.GetRequiredService<GatewayRequestHistory>(), sp.GetRequiredService<Redactor>(), mode))
            .AddSingleton<ILoggerProvider>(sp => Redacting(sp, sp.GetRequiredService<BufferLoggerProvider>()))
            .AddSingleton<ILoggerProvider>(sp => Redacting(sp, new FileLoggerProvider(
                Path.Combine(sp.GetRequiredService<IAppDataPaths>().Root, "logs"), sp.GetRequiredService<TimeProvider>(), MaxFileBytes, MaxFiles, MaxAge)))
            .AddSingleton<ILoggerProvider>(sp => Redacting(sp, ConsoleLogging.CreateProvider(mode)));
        services.AddLogging(b => b
            .SetMinimumLevel(LogLevel.Trace)
            .AddFilter((_, level) => level >= (mode.IsEnabled ? LogLevel.Debug : LogLevel.Information)));
        return services;
    }

    private static RedactingLoggerProvider Redacting(IServiceProvider sp, ILoggerProvider inner) =>
        new(sp.GetRequiredService<Redactor>(), inner);
}
