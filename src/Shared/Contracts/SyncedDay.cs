namespace Shared.Contracts;

/// <summary>Stars earned on one day as last seen on the calendar page.</summary>
public sealed record SyncedDay(int Day, int Stars);
