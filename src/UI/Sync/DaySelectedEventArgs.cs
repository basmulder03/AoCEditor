using System.Globalization;
using AoCEditor.Core.Sync.Domain;

namespace AoCEditor.UI.Sync;

public sealed class DaySelectedEventArgs(int year, int day) : EventArgs
{
    public int Year { get; } = year;

    public int Day { get; } = day;
}
