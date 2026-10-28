using System.Globalization;
using Sync.Domain;

namespace Sync.UI;

public sealed class DaySelectedEventArgs(int year, int day) : EventArgs
{
    public int Year { get; } = year;

    public int Day { get; } = day;
}
