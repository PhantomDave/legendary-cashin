using System.ComponentModel.DataAnnotations;

namespace WhereIsMyMoney.Api.Models.TransactionModels;

public sealed class PatchTransactionRequest
{
    [StringLength(256, MinimumLength = 3)]
    public string? Description { get; init; }

    public decimal? Amount { get; init; }

    public DateTime? Date { get; init; }

    public IReadOnlyList<int>? CategoryIds { get; init; }

    public long? BudgetId { get; init; }
}
