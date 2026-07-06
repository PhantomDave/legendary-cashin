using System.ComponentModel.DataAnnotations;

namespace WhereIsMyMoney.Api.Models.TransactionModels;

public sealed class TransactionQueryRequest
{
    [Range(1, int.MaxValue)]
    public int PageNumber { get; init; } = 1;

    [Range(1, Models.PaginationRequest.MaxPageSize)]
    public int PageSize { get; init; } = Models.PaginationRequest.DefaultPageSize;

    public DateTime? Date { get; init; }
    public long? BudgetId { get; init; }
    public int? CategoryId { get; init; }
    public bool? Uncategorized { get; init; }

    [StringLength(256)]
    public string? Description { get; init; }

    public decimal? Amount { get; init; }

    [RegularExpression("^(equals|gt|gte|lt|lte)$")]
    public string? AmountMatchMode { get; init; }

    public bool HasFilters =>
        Date.HasValue
        || BudgetId.HasValue
        || CategoryId.HasValue
        || (Uncategorized.HasValue && Uncategorized.Value)
        || !string.IsNullOrEmpty(Description)
        || Amount.HasValue;
}
