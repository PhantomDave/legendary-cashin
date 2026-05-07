using WhereIsMyMoney.Api.Models.TransactionModels;

namespace WhereIsMyMoney.Api.Services;

public class RecurrenceEngine
{
    /// <summary>
    /// Calculates the next date a recurring transaction should occur.
    /// </summary>
    public DateTime GetNextOccurrenceDate(RecurringTransaction schedule, DateTime? fromDate = null)
    {
        fromDate ??= schedule.LastGeneratedDate ?? schedule.StartDate;

        DateTime nextDate = schedule.Frequency switch
        {
            RecurrenceFrequency.Daily => GetNextDaily(fromDate.Value, schedule.Interval),
            RecurrenceFrequency.Weekly => GetNextWeekly(fromDate.Value, schedule.Interval, schedule.DaysOfWeek),
            RecurrenceFrequency.BiWeekly => GetNextWeekly(fromDate.Value, 2, schedule.DaysOfWeek),
            RecurrenceFrequency.Monthly => GetNextMonthly(fromDate.Value, schedule.Interval, schedule.DayOfMonth),
            RecurrenceFrequency.Quarterly => GetNextMonthly(fromDate.Value, schedule.Interval * 3, schedule.DayOfMonth),
            RecurrenceFrequency.Yearly => GetNextYearly(fromDate.Value, schedule.Interval, schedule.DayOfMonth),
            _ => throw new ArgumentException($"Unknown frequency: {schedule.Frequency}")
        };

        return nextDate;
    }

    /// <summary>
    /// Determines if a transaction should be generated today.
    /// </summary>
    public bool ShouldGenerateTransactionToday(RecurringTransaction schedule)
    {
        if (!schedule.IsActive)
            return false;

        DateTime today = DateTime.UtcNow.Date;

        // Not yet started
        if (today < schedule.StartDate.Date)
            return false;

        // Already ended
        if (schedule.EndDate.HasValue && today > schedule.EndDate.Value.Date)
            return false;

        // Max occurrences reached
        if (schedule.MaxOccurrences.HasValue && schedule.GeneratedCount >= schedule.MaxOccurrences)
            return false;

        // Already generated today
        if (schedule.LastGeneratedDate.HasValue && schedule.LastGeneratedDate.Value.Date == today)
            return false;

        // Calculate next occurrence and check if it's today or in the past
        DateTime nextDate = GetNextOccurrenceDate(schedule);
        return nextDate.Date <= today;
    }

    /// <summary>
    /// Gets all future occurrence dates for preview.
    /// </summary>
    public List<DateTime> GetAllFutureOccurrences(RecurringTransaction schedule, int maxCount = 12)
    {
        List<DateTime> occurrences = new List<DateTime>();
        DateTime currentDate = schedule.LastGeneratedDate ?? schedule.StartDate;

        for (int i = 0; i < maxCount; i++)
        {
            currentDate = GetNextOccurrenceDate(schedule, currentDate);

            if (schedule.EndDate.HasValue && currentDate.Date > schedule.EndDate.Value.Date)
                break;

            if (schedule.MaxOccurrences.HasValue && (schedule.GeneratedCount + occurrences.Count) >= schedule.MaxOccurrences)
                break;

            occurrences.Add(currentDate);
        }

        return occurrences;
    }

    /// <summary>
    /// Counts how many occurrences are due on or before the provided date.
    /// </summary>
    public int CountElapsedOccurrences(RecurringTransaction schedule, DateTime asOfDateUtc)
    {
        if (asOfDateUtc.Date < schedule.StartDate.Date)
            return 0;

        DateTime upperBound = schedule.EndDate.HasValue && schedule.EndDate.Value.Date < asOfDateUtc.Date
            ? schedule.EndDate.Value.Date
            : asOfDateUtc.Date;

        int count = 0;
        DateTime currentDate = schedule.StartDate;

        while (true)
        {
            DateTime nextOccurrence = GetNextOccurrenceDate(schedule, currentDate);

            if (nextOccurrence.Date > upperBound)
                break;

            count++;
            currentDate = nextOccurrence;

            if (schedule.MaxOccurrences.HasValue && count >= schedule.MaxOccurrences.Value)
                break;
        }

        return count;
    }

    /// <summary>
    /// Returns the last occurrence on or before the provided date.
    /// </summary>
    public DateTime? GetLastOccurrenceOnOrBefore(RecurringTransaction schedule, DateTime asOfDateUtc)
    {
        if (asOfDateUtc.Date < schedule.StartDate.Date)
            return null;

        DateTime upperBound = schedule.EndDate.HasValue && schedule.EndDate.Value.Date < asOfDateUtc.Date
            ? schedule.EndDate.Value.Date
            : asOfDateUtc.Date;

        DateTime? lastOccurrence = null;
        int count = 0;
        DateTime currentDate = schedule.StartDate;

        while (true)
        {
            DateTime nextOccurrence = GetNextOccurrenceDate(schedule, currentDate);

            if (nextOccurrence.Date > upperBound)
                break;

            lastOccurrence = nextOccurrence;
            currentDate = nextOccurrence;
            count++;

            if (schedule.MaxOccurrences.HasValue && count >= schedule.MaxOccurrences.Value)
                break;
        }

        return lastOccurrence;
    }

    // Private helpers

    private static DateTime GetNextDaily(DateTime from, int interval)
    {
        return from.AddDays(interval);
    }

    private static DateTime GetNextWeekly(DateTime from, int interval, IReadOnlyList<DayOfWeek>? daysOfWeek)
    {
        daysOfWeek ??= new[] { from.DayOfWeek }; // Default to same day of week

        List<DayOfWeek> daysList = daysOfWeek is List<DayOfWeek> l ? l : new List<DayOfWeek>(daysOfWeek);
        if (daysList.Count == 0)
            daysList = new List<DayOfWeek> { from.DayOfWeek };

        // Sort for consistent ordering
        daysList.Sort();

        DateTime current = from.AddDays(1);
        int weeksToAdd = interval - 1;

        while (true)
        {
            if (daysList.Contains(current.DayOfWeek))
            {
                if (weeksToAdd <= 0)
                    return current;
                weeksToAdd--;
            }

            current = current.AddDays(1);

            // Safety: don't go more than 5 years out
            if ((current - from).TotalDays > 1825)
                throw new InvalidOperationException("Could not calculate next weekly occurrence within reasonable timeframe.");
        }
    }

    private static DateTime GetNextMonthly(DateTime from, int interval, int? dayOfMonth)
    {
        dayOfMonth ??= from.Day;

        // Try to find the date in the next month(s)
        DateTime next = new DateTime(from.Year, from.Month, 1).AddMonths(interval);

        // Cap day at the last day of the target month
        int lastDayOfMonth = DateTime.DaysInMonth(next.Year, next.Month);
        int targetDay = Math.Min(dayOfMonth.Value, lastDayOfMonth);

        return new DateTime(next.Year, next.Month, targetDay);
    }

    private static DateTime GetNextYearly(DateTime from, int interval, int? dayOfMonth)
    {
        dayOfMonth ??= from.Day;

        DateTime next = new DateTime(from.Year + interval, from.Month, 1);

        // Handle Feb 29 in non-leap years
        int lastDayOfMonth = DateTime.DaysInMonth(next.Year, next.Month);
        int targetDay = Math.Min(dayOfMonth.Value, lastDayOfMonth);

        return new DateTime(next.Year, next.Month, targetDay);
    }
}
