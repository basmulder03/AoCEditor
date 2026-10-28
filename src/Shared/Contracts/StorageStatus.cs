namespace Shared.Contracts;

public sealed record StorageStatus(string DatabasePath, int SchemaVersion, int MigrationsApplied, string? Error = null);
