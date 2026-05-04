namespace WhereIsMyMoney.Api.Models.AccountModels;

public sealed record AuthenticateRequest(
    string Email,
    string Username,
    string Password);
