using System.Reflection.Metadata;
using AoC.Shared.Extensions;
using AoC.Shared.Models;

namespace AoC.Shared.Services;

public class CalendarService(TimeProvider timeProvider) : ICalendarService
{
    /// <summary>
    /// This method returns the latest year when Advent of Code has puzzles available for.
    /// </summary>
    /// <returns>The latest year there are puzzles available for.</returns>
    public int GetLatestPuzzleYear()
    {
        // To determine the latest year, we check if midnight EST of december 1st has passed for the current year.
        // If it has, we return the current year. If not, we return the previous year.
        var currentDate = timeProvider.GetUtcNow().ToOffset(TimeSpan.FromHours(-5)); // Convert to EST
        var firstPuzzleDateThisYear = Constants.FirstPuzzleDate.SetYear(currentDate.Year);
        if (currentDate < firstPuzzleDateThisYear)
        {
            // If the current date is before the first puzzle date, return the year before the first puzzle date.
            return currentDate.Year - 1;
        }
        return currentDate.Year;
    }

   /// <summary>
   /// Returns a list of years for which Advent of Code puzzles are available.
   /// The list starts from the first puzzle year and ends with the latest available puzzle year.
   /// </summary>
   /// <returns>A list of integers representing available puzzle years.</returns>
   public List<int> GetAvailablePuzzleYears()
   {
       // The first puzzle was released in 2015, so we start from that year.
       var latestYear = GetLatestPuzzleYear();
       var years = new List<int>();
       
       for (var year = Constants.FirstPuzzleYear; year <= latestYear; year++)
       {
           years.Add(year);
       }
       
       return years;
   }

   public List<CalendarDay> GetAvailablePuzzleDays(int year)
   {
       // Validate the year is within the range of available puzzle years.
         if (year < Constants.FirstPuzzleYear || year > GetLatestPuzzleYear())
         {
              throw new ArgumentOutOfRangeException(nameof(year), "The specified year is not available for puzzles.");
         }
         var days = new List<CalendarDay>();
            // The first puzzle is available on December 1st of the specified year.
            for (var day = 1; day <= 25; day++) 
            {
                // Check if the day is available for the specified year.
                // If the year is before the current year, all days are available.
                // If the year is the current year, we need to check if the day is before or on the current date.
                var locked = true;
                var currentDate = timeProvider.GetUtcNow().ToOffset(TimeSpan.FromHours(-5)); // Convert to EST
                if (year < currentDate.Year || (year == currentDate.Year && day <= currentDate.Day))
                {
                    locked = false; // The day is available if it's before or on the current date.
                }
                
                // Create a new CalendarDay object for each day of December.
                var calendarDay = new CalendarDay
                {
                    Year = (short)year,
                    Day = (short)day,
                    IsLocked = locked // Initially, all days are unlocked.
                };
                
                // Add the CalendarDay to the list.
                days.Add(calendarDay);
            }
        return days;
   }
}