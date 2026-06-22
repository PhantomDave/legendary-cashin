using WhereIsMyMoney.Api.Models.TransactionModels;

namespace WhereIsMyMoney.Api.Models.TransactionModels
{
    public class UpdateRecurringTransactionRequest
    {
        public string? Description { get; set; }
        public decimal? Amount { get; set; }
        public IReadOnlyList<int>? CategoryIds { get; set; }

        public RecurrenceFrequency? Frequency { get; set; }
        public int? Interval { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? MaxOccurrences { get; set; }

        public IReadOnlyList<DayOfWeek>? DaysOfWeek { get; set; }
        public int? DayOfMonth { get; set; }

        public bool? IsActive { get; set; }
    }
}
