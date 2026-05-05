namespace WhereIsMyMoney.Api.Services;

public interface IStore<T> where T : class
{
    Task<T?> GetAsync(long id);
    Task<IReadOnlyList<T>> GetAllAsync();
    Task<T> CreateAsync(T value);
    Task<bool> UpdateAsync(long id, T value);
    Task<bool> DeleteAsync(long id);
}
