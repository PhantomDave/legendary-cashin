using WhereIsMyMoney.Api.Models.CategoryModels;

namespace WhereIsMyMoney.Api.Models.TransactionModels
{
    public class Transaction
    {
        public int Id { get; set; }
        public required string Description { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public ICollection<Category> Categories { get; set; } = [];
        public long BudgetId { get; set; }
        public long AccountId { get; set; }
    }
}
