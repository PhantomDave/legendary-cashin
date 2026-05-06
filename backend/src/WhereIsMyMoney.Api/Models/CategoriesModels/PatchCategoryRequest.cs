using System.ComponentModel.DataAnnotations;

namespace WhereIsMyMoney.Api.Models.CategoryModels;

public sealed class PatchCategoryRequest
{
    [StringLength(256, MinimumLength = 1)]
    public string? Name { get; init; }

    public decimal? Budget { get; init; }
}
