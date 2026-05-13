using WhereIsMyMoney.Api.Models.TransactionModels;

namespace WhereIsMyMoney.Api.Models.TransactionModels
{
    public class RecurringTransactionResponse
    {
        public long Id { get; set; }
        public long AccountId { get; set; }
        public long BudgetId { get; set; }

        public string? Description { get; set; }
        public decimal Amount { get; set; }
        public required IReadOnlyList<int> CategoryIds { get; set; }

        public RecurrenceFrequency Frequency { get; set; }
        public int Interval { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? MaxOccurrences { get; set; }

        public IReadOnlyList<DayOfWeek>? DaysOfWeek { get; set; }
        public int? DayOfMonth { get; set; }

        public DateTime? LastGeneratedDate { get; set; }
        public int GeneratedCount { get; set; }
        public bool IsActive { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
    }
}
