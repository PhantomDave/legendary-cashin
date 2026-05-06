using WhereIsMyMoney.Api.Models;

namespace WhereIsMyMoney.Api.Services;

public interface IStore<TResponse, TCreate, TUpdate>
    where TResponse : class
    where TCreate : class
    where TUpdate : class
{
    Task<TResponse?> GetAsync(long id);
    Task<IReadOnlyList<TResponse>> GetAllAsync();
    Task<IReadOnlyList<TResponse>> GetAllByAccountId(long accountId);
    Task<PaginatedResponse<TResponse>> GetAllByAccountIdPaginatedAsync(long accountId, PaginationRequest request);
    Task<TResponse> CreateAsync(TCreate value);
    Task<bool> UpdateAsync(long id, TUpdate value);
    Task<bool> DeleteAsync(long id);
}
