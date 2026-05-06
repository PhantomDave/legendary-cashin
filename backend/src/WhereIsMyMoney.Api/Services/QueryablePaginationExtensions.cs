using Microsoft.EntityFrameworkCore;
using WhereIsMyMoney.Api.Models;

namespace WhereIsMyMoney.Api.Services;

public static class QueryablePaginationExtensions
{
    public static async Task<PaginatedResponse<T>> ToPaginatedResponseAsync<T>(
        this IQueryable<T> query,
        PaginationRequest request,
        CancellationToken cancellationToken = default)
    {
        int totalCount = await query.CountAsync(cancellationToken);

        List<T> items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        int totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return new PaginatedResponse<T>(
            items,
            request.PageNumber,
            request.PageSize,
            totalCount,
            totalPages);
    }
}
