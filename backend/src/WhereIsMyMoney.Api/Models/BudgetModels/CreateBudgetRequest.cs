using System.ComponentModel.DataAnnotations;

namespace WhereIsMyMoney.Api.Models.BudgetModels;

public sealed class CreateBudgetRequest
{
    [Required]
    [StringLength(256, MinimumLength = 1)]
    public string Name { get; init; } = null!;

    [Required]
    [StringLength(3, MinimumLength = 3)]
    public string DefaultCurrency { get; init; } = null!;

    [Range(0, double.MaxValue)]
    public decimal Amount { get; init; }
}
