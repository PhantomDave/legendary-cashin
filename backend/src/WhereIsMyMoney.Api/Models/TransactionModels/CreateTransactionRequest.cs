using System.ComponentModel.DataAnnotations;

namespace WhereIsMyMoney.Api.Models.TransactionModels
{
    public class CreateTransactionRequest
    {
        [Required]
        [StringLength(256, MinimumLength = 3)]
        public required string Description { get; set; }
        [Required]
        public decimal Amount { get; set; }
        [Required]
        public DateTime Date { get; set; }
        public IReadOnlyList<int> CategoryIds { get; set; } = [];
        [Required]
        public long BudgetId { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public long AccountId { get; set; }
    }
}
