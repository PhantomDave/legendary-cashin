using System.ComponentModel.DataAnnotations;

namespace WhereIsMyMoney.Api.Models.BudgetModels;

public sealed class PatchBudgetRequest
{
    [StringLength(256, MinimumLength = 1)]
    public string? Name { get; init; }

    [StringLength(3, MinimumLength = 3)]
    public string? DefaultCurrency { get; init; }

    [Range(0, double.MaxValue)]
    public decimal? Amount { get; init; }
}
