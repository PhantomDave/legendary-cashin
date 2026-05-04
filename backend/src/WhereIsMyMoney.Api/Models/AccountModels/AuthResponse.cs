namespace WhereIsMyMoney.Api.Models.AccountModels;

public sealed record AuthResponse(long Id, string Name, string Email, string Token);
