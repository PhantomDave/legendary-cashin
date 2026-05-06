using System.ComponentModel.DataAnnotations;

namespace WhereIsMyMoney.Api.Models;

public sealed class PaginationRequest
{
    public const int DefaultPageNumber = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    [Range(1, int.MaxValue)]
    public int PageNumber { get; init; } = DefaultPageNumber;

    [Range(1, MaxPageSize)]
    public int PageSize { get; init; } = DefaultPageSize;
}
