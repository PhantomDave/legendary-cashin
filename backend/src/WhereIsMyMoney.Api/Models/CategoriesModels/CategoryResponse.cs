namespace WhereIsMyMoney.Api.Models.CategoryModels;

public record CategoryResponse(
    int Id,
    long AccountId,
    string Name,
    decimal Budget
);
