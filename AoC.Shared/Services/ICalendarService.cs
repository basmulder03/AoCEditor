using AoC.Shared.Models;

namespace AoC.Shared.Services;

public interface ICalendarService
{
    public int GetLatestPuzzleYear();
    public List<int> GetAvailablePuzzleYears();
    public List<CalendarDay> GetAvailablePuzzleDays(int year);
}