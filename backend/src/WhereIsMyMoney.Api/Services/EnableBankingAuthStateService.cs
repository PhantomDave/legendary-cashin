using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using WhereIsMyMoney.Api.Models.EnableBankingModels;

namespace WhereIsMyMoney.Api.Services;

public sealed class EnableBankingAuthStateService
{
    private sealed record PendingAuth(
        long IntegrationId,
        long AccountId,
        bool IsForceSync = false,
        DateTime? ForceSyncStartDate = null,
        DateTime? ForceSyncEndDate = null);
    private readonly IDistributedCache _cache;
    private readonly TimeSpan _ttl;

    public EnableBankingAuthStateService(IDistributedCache cache, IOptions<EnableBankingOptions> options)
    {
        _cache = cache;
        int callbackTimeoutSeconds = options.Value.CallbackTimeout;
        _ttl = TimeSpan.FromSeconds(Math.Max(30, callbackTimeoutSeconds));
    }

    public string CreateState(long integrationId, long accountId)
    {
        string state = Guid.NewGuid().ToString();
        PendingAuth pending = new(integrationId, accountId);
        string json = JsonSerializer.Serialize(pending);
        _cache.SetString($"auth_state:{state}", json, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = _ttl });
        return state;
    }

    public string CreateForceSyncState(long integrationId, long accountId, DateTime startDate, DateTime endDate)
    {
        string state = Guid.NewGuid().ToString();
        PendingAuth pending = new(
            integrationId,
            accountId,
            IsForceSync: true,
            ForceSyncStartDate: startDate,
            ForceSyncEndDate: endDate);
        string json = JsonSerializer.Serialize(pending);
        _cache.SetString($"auth_state:{state}", json, new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = _ttl });
        return state;
    }

    public (long IntegrationId, long AccountId)? ConsumeState(string state)
    {
        string? json = _cache.GetString($"auth_state:{state}");
        if (json is null)
            return null;

        _cache.Remove($"auth_state:{state}");
        PendingAuth? pending = JsonSerializer.Deserialize<PendingAuth>(json);
        return pending is null ? null : (pending.IntegrationId, pending.AccountId);
    }

    public (long IntegrationId, long AccountId, DateTime StartDate, DateTime EndDate)? ConsumeForceSyncState(string state)
    {
        string? json = _cache.GetString($"auth_state:{state}");
        if (json is null)
            return null;

        _cache.Remove($"auth_state:{state}");
        PendingAuth? pending = JsonSerializer.Deserialize<PendingAuth>(json);
        if (pending is null ||
            !pending.IsForceSync ||
            !pending.ForceSyncStartDate.HasValue ||
            !pending.ForceSyncEndDate.HasValue)
        {
            return null;
        }

        return (pending.IntegrationId, pending.AccountId, pending.ForceSyncStartDate.Value, pending.ForceSyncEndDate.Value);
    }
}
