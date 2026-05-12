using System.Collections.Concurrent;

namespace WhereIsMyMoney.Api.Services;

public sealed class EnableBankingAuthStateService
{
    private sealed record PendingAuth(long IntegrationId, long AccountId, DateTime ExpiresAt);
    private readonly ConcurrentDictionary<string, PendingAuth> _pending = new();
    private readonly TimeSpan _ttl = TimeSpan.FromMinutes(15);

    public string CreateState(long integrationId, long accountId)
    {
        string state = Guid.NewGuid().ToString();
        _pending[state] = new PendingAuth(integrationId, accountId, DateTime.UtcNow.Add(_ttl));
        CleanExpired();
        return state;
    }

    public (long IntegrationId, long AccountId)? ConsumeState(string state)
    {
        if (_pending.TryRemove(state, out PendingAuth? pending) && pending.ExpiresAt > DateTime.UtcNow)
            return (pending.IntegrationId, pending.AccountId);
        return null;
    }

    private void CleanExpired()
    {
        DateTime now = DateTime.UtcNow;
        foreach (var kvp in _pending.Where(kvp => kvp.Value.ExpiresAt <= now).ToList())
            _pending.TryRemove(kvp.Key, out _);
    }
}
