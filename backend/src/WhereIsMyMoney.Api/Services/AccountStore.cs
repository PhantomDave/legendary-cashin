using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using WhereIsMyMoney.Api.Data;
using WhereIsMyMoney.Api.Models;
using WhereIsMyMoney.Api.Models.AccountModels;
using WhereIsMyMoney.Api.Services;

namespace WhereIsMyMoney.Api;

public sealed class AccountStore(AppDbContext db) : IStore<AccountResponse, CreateAccountRequest, AccountResponse>
{
    public async Task<AccountResponse?> GetAsync(long id)
    {
        Account? account = await db.Accounts.FindAsync(id);
        return account is null ? null : ToResponse(account);
    }

    public async Task<IReadOnlyList<AccountResponse>> GetAllAsync()
    {
        return await db.Accounts
            .Select(a => ToResponse(a))
            .ToListAsync();
    }

    public async Task<Account?> GetByEmailAsync(string email)
    {
        Account? account = await db.Accounts.FirstOrDefaultAsync(a => a.Email == email);
        return account is null ? null : account;
    }

    public async Task<Account?> GetByUsernameAsync(string username)
    {
        Account? account = await db.Accounts.FirstOrDefaultAsync(a => a.Name == username);
        return account is null ? null : account;
    }

    public async Task<AccountResponse> CreateAsync(CreateAccountRequest request)
    {
        Account account = new Account
        {
            Name = request.Username,
            Email = request.Email,
            PasswordHash = HashPassword(request.Password),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        db.Accounts.Add(account);
        await db.SaveChangesAsync();

        return ToResponse(account);
    }

    public async Task<bool> UpdateAsync(long id, AccountResponse value)
    {
        Account? account = await db.Accounts.FindAsync(id);
        if (account is null) return false;

        account.Name = value.Name;
        account.Email = value.Email;

        db.Accounts.Update(account);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        Account? account = await db.Accounts.FindAsync(id);
        if (account is null) return false;

        db.Accounts.Remove(account);
        await db.SaveChangesAsync();
        return true;
    }

    internal static AccountResponse ToResponse(Account account)
    {
        return new(account.Id, account.Name, account.Email);
    }

    private static string HashPassword(string password)
    {
        const int saltSize = 16;       // 128-bit salt
        const int hashSize = 32;       // 256-bit derived key
        const int iterations = 350_000; // OWASP recommended minimum for PBKDF2-HMAC-SHA256

        byte[] salt = RandomNumberGenerator.GetBytes(saltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, hashSize);

        // Format: {iterations}${algorithm}${base64(salt)}${base64(hash)}
        return $"{iterations}$SHA256${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool VerifyPassword(string password, string passwordHash)
    {
        string[] parts = passwordHash.Split('$');
        if (parts.Length != 4) return false;

        if (!int.TryParse(parts[0], out int iterations)) return false;
        HashAlgorithmName algorithm = new HashAlgorithmName(parts[1]);
        byte[] salt = Convert.FromBase64String(parts[2]);
        byte[] expectedHash = Convert.FromBase64String(parts[3]);

        byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, algorithm, expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    public async Task<IReadOnlyList<AccountResponse>> GetAllByAccountId(long accountId)
    {
        return await db.Accounts
            .Where(a => a.Id == accountId)
            .Select(a => ToResponse(a))
            .ToListAsync();
    }

    public async Task<PaginatedResponse<AccountResponse>> GetAllByAccountIdPaginatedAsync(long accountId, PaginationRequest request)
    {
        return await db.Accounts
            .Where(a => a.Id == accountId)
            .OrderBy(a => a.Id)
            .Select(a => ToResponse(a))
            .ToPaginatedResponseAsync(request);
    }
}
