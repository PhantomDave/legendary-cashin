namespace WhereIsMyMoney.Api.Models.CategoryModels;

public class UpdateCategoryRequest
{
    public required string Name { get; set; }
    public decimal Budget { get; set; }
}

