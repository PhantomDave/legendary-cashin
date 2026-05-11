

using Microsoft.EntityFrameworkCore;
using WhereIsMyMoney.Api.Data;
using WhereIsMyMoney.Api.Models;
using WhereIsMyMoney.Api.Models.EnableBankingModels;

namespace WhereIsMyMoney.Api.Services;

public sealed class EnableBankingStore(AppDbContext db, EncryptionService encryptionService) : IStore<EnableBanking, CreateEnableBankingRequest, EnableBanking>
{
    public async Task<EnableBanking?> GetAsync(long id)
    {
        EnableBanking? entity = await db.EnableBanking.FirstOrDefaultAsync(e => e.Id == id);
        if (entity is not null)
        {
            DecryptEntity(entity);
        }
        return entity;
    }

    public async Task<IReadOnlyList<EnableBanking>> GetAllAsync()
    {
        IReadOnlyList<EnableBanking> entities = await db.EnableBanking.ToListAsync();
        foreach (EnableBanking entity in entities)
        {
            DecryptEntity(entity);
        }
        return entities;
    }

    public async Task<IReadOnlyList<EnableBanking>> GetAllByAccountId(long accountId)
    {
        IReadOnlyList<EnableBanking> entities = await db.EnableBanking
            .Where(e => e.AccountId == accountId)
            .ToListAsync();
        foreach (EnableBanking entity in entities)
        {
            DecryptEntity(entity);
        }
        return entities;
    }

    public async Task<PaginatedResponse<EnableBanking>> GetAllByAccountIdPaginatedAsync(long accountId, PaginationRequest request)
    {
        PaginatedResponse<EnableBanking> response = await db.EnableBanking
            .Where(e => e.AccountId == accountId)
            .OrderByDescending(e => e.CreatedAtUtc)
            .ToPaginatedResponseAsync(request);

        foreach (EnableBanking entity in response.Items)
        {
            DecryptEntity(entity);
        }
        return response;
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
        // Encrypt the certificate before storing
        if (!string.IsNullOrEmpty(integration.Certificate))
        {
            integration.Certificate = encryptionService.Encrypt(integration.Certificate);
        }

        db.EnableBanking.Add(integration);
        await db.SaveChangesAsync();

        // Decrypt for returning to caller
        if (!string.IsNullOrEmpty(integration.Certificate))
        {
            integration.Certificate = encryptionService.Decrypt(integration.Certificate);
        }
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

    private void DecryptEntity(EnableBanking entity)
    {
        // Only decrypt if it's an EnableBankingIntegration with an encrypted certificate
        if (entity is EnableBankingIntegration integration && !string.IsNullOrEmpty(integration.Certificate))
        {
            try
            {
                integration.Certificate = encryptionService.Decrypt(integration.Certificate);
            }
            catch
            {
                // If decryption fails, leave as is (might be legacy unencrypted data)
            }
        }
    }
}
