namespace AoC.Shared.Extensions;

public static class DateTimeOffsetExtensions
{
    public static DateTimeOffset SetYear(this DateTimeOffset dateTime, int year)
    {
        return new DateTimeOffset(year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, dateTime.Second, dateTime.Offset);
    }
}