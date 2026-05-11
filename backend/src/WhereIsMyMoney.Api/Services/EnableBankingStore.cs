

using Microsoft.EntityFrameworkCore;
using WhereIsMyMoney.Api.Data;
using WhereIsMyMoney.Api.Models;
using WhereIsMyMoney.Api.Models.EnableBankingModels;

namespace WhereIsMyMoney.Api.Services;

public sealed class EnableBankingStore(AppDbContext db) : IStore<EnableBanking, CreateEnableBankingRequest, EnableBanking>
{
    public async Task<EnableBanking?> GetAsync(long id)
    {
        return await db.EnableBanking.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<IReadOnlyList<EnableBanking>> GetAllAsync()
    {
        return await db.EnableBanking.ToListAsync();
    }

    public async Task<IReadOnlyList<EnableBanking>> GetAllByAccountId(long accountId)
    {
        return await db.EnableBanking
            .Where(e => e.AccountId == accountId)
            .ToListAsync();
    }

    public async Task<PaginatedResponse<EnableBanking>> GetAllByAccountIdPaginatedAsync(long accountId, PaginationRequest request)
    {
        return await db.EnableBanking
            .Where(e => e.AccountId == accountId)
            .OrderByDescending(e => e.CreatedAtUtc)
            .ToPaginatedResponseAsync(request);
    }

    public async Task<EnableBanking> CreateAsync(CreateEnableBankingRequest value)
    {
        // Note: AccountId should be set by the controller before calling this
        EnableBanking enableBanking = new EnableBanking
        {
            Asps = value.Asps,
            CreatedAtUtc = DateTime.UtcNow
        };

        db.EnableBanking.Add(enableBanking);
        await db.SaveChangesAsync();
        return enableBanking;
    }

    // Store an EnableBankingIntegration configuration
    public async Task<EnableBankingIntegration> CreateIntegrationAsync(EnableBankingIntegration integration)
    {
        db.EnableBanking.Add(integration);
        await db.SaveChangesAsync();
        return integration;
    }

    public async Task<bool> UpdateAsync(long id, EnableBanking value)
    {
        EnableBanking? existing = await db.EnableBanking.FindAsync(id);
        if (existing is null) return false;

        existing.Asps = value.Asps;

        db.EnableBanking.Update(existing);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        EnableBanking? enableBanking = await db.EnableBanking.FindAsync(id);
        if (enableBanking is null) return false;

        db.EnableBanking.Remove(enableBanking);
        await db.SaveChangesAsync();
        return true;
    }
}
