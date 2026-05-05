using System.ComponentModel.DataAnnotations;

namespace WhereIsMyMoney.Api.Models.TransactionModels
{
    public class UpdateTransactionRequest
    {
        [Required]
        [StringLength(256, MinimumLength = 3)]
        public required string Description { get; set; }
        [Required]
        public decimal Amount { get; set; }
        [Required]
        public DateTime Date { get; set; }
        public List<int> CategoryIds { get; set; } = [];
    }
}
