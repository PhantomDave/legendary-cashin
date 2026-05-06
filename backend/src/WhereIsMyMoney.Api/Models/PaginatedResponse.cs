namespace WhereIsMyMoney.Api.Models;

public sealed record PaginatedResponse<T>(
    IReadOnlyList<T> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages
);
