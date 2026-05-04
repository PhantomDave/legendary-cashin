namespace WhereIsMyMoney.Api;

public sealed record CashinResponse(
    Guid Id,
    decimal Amount,
    string Currency,
    string Reference,
    string Status,
    DateTimeOffset CreatedAtUtc);

