namespace WhereIsMyMoney.Api.Models.TransactionModels
{
    public class RecurringTransaction
    {
        public long Id { get; set; }

        // References
        public long AccountId { get; set; }
        public long BudgetId { get; set; }

        // Transaction template
        public required string Description { get; set; }
        public decimal Amount { get; set; }
        public List<int> CategoryIds { get; set; } = [];

        // Recurrence pattern
        public RecurrenceFrequency Frequency { get; set; }
        public int Interval { get; set; } = 1; // Every N periods
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; } // null = runs indefinitely
        public int? MaxOccurrences { get; set; } // null = no limit

        // Weekly-specific: which days? (bitmask for Mon-Sun)
        public IReadOnlyList<DayOfWeek>? DaysOfWeek { get; set; }

        // Monthly-specific: which day of month? (1-31, or null for "same day")
        public int? DayOfMonth { get; set; }

        // Execution tracking
        public DateTime? LastGeneratedDate { get; set; }
        public int GeneratedCount { get; set; } = 0;
        public bool IsActive { get; set; } = true;

        // Audit
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }
}
