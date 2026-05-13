using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using WhereIsMyMoney.Api.Models.EnableBankingModels;

namespace WhereIsMyMoney.Api.Services;

public sealed class EnableBankingAuthStateService
{
    private sealed record PendingAuth(long IntegrationId, long AccountId, DateTime ExpiresAt);
    private readonly ConcurrentDictionary<string, PendingAuth> _pending = new();
    private readonly TimeSpan _ttl;

    public EnableBankingAuthStateService(IOptions<EnableBankingOptions> options)
    {
        int callbackTimeoutSeconds = options.Value.CallbackTimeout;
        _ttl = TimeSpan.FromSeconds(Math.Max(30, callbackTimeoutSeconds));
    }

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
        foreach (KeyValuePair<string, PendingAuth> kvp in _pending.Where(kvp => kvp.Value.ExpiresAt <= now).ToList())
            _pending.TryRemove(kvp.Key, out _);
    }
}
