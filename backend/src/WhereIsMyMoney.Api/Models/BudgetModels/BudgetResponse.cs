namespace WhereIsMyMoney.Api.Models.BudgetModels;

public sealed record BudgetResponse(
    long Id,
    long AccountId,
    string Name,
    string DefaultCurrency,
    decimal Amount,
    DateTimeOffset CreatedAtUtc);
