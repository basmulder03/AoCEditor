namespace AoCEditor.Infrastructure.Storage;

public static class UnixMs
{
    public static long From(DateTimeOffset value) => value.ToUnixTimeMilliseconds();

    public static DateTimeOffset To(long value) => DateTimeOffset.FromUnixTimeMilliseconds(value);
}
