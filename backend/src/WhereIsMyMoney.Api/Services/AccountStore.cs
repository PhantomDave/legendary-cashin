using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using WhereIsMyMoney.Api.Data;
using WhereIsMyMoney.Api.Models.AccountModels;
using WhereIsMyMoney.Api.Services;

namespace WhereIsMyMoney.Api;

public sealed class AccountStore(AppDbContext db) : IStore<AccountResponse>
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
        var account = new Account
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

    async Task<AccountResponse> IStore<AccountResponse>.CreateAsync(AccountResponse value)
    {
        throw new NotSupportedException("Use CreateAsync(CreateAccountRequest) instead");
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

    internal static AccountResponse ToResponse(Account account) =>
        new(account.Id, account.Name, account.Email);

    private static string HashPassword(string password)
    {
        const int saltSize = 16;       // 128-bit salt
        const int hashSize = 32;       // 256-bit derived key
        const int iterations = 350_000; // OWASP recommended minimum for PBKDF2-HMAC-SHA256

        var salt = RandomNumberGenerator.GetBytes(saltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, hashSize);

        // Format: {iterations}${algorithm}${base64(salt)}${base64(hash)}
        return $"{iterations}$SHA256${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool VerifyPassword(string password, string passwordHash)
    {
        var parts = passwordHash.Split('$');
        if (parts.Length != 4) return false;

        if (!int.TryParse(parts[0], out var iterations)) return false;
        var algorithm = new HashAlgorithmName(parts[1]);
        var salt = Convert.FromBase64String(parts[2]);
        var expectedHash = Convert.FromBase64String(parts[3]);

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, algorithm, expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
