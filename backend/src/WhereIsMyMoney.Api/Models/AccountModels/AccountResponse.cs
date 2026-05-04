namespace WhereIsMyMoney.Api.Models.AccountModels;

public sealed record AccountResponse(
    long Id,
    string Name,
    string Email);
