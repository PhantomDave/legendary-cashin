using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using WhereIsMyMoney.Api.Data;
using WhereIsMyMoney.Api.Models.AccountModels;

namespace WhereIsMyMoney.Api
{
    public sealed class AccountStore(AccountDbContext db)
    {
        public async Task<AccountResponse?> GetAsync(long id)
        {
            Account? account = await db.Accounts.FindAsync(id);
            return account is null ? null : ToResponse(account);
        }

        public async Task<Account?> GetByEmailAsync(string email)
        {
            Account? account = await db.Accounts.FirstOrDefaultAsync(a => a.Email == email);
            return account is null ? null : account;
        }

        public async Task<AccountResponse> CreateAsync(CreateAccountRequest request)
        {
            var account = new Account
            {
                Name = request.Name,
                Email = request.Email,
                PasswordHash = HashPassword(request.Password),
                CreatedAtUtc = DateTimeOffset.UtcNow
            };

            db.Accounts.Add(account);
            await db.SaveChangesAsync();

            return ToResponse(account);
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
}
