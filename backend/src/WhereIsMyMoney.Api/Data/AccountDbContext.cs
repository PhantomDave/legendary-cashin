using Microsoft.EntityFrameworkCore;

namespace WhereIsMyMoney.Api.Data
{
    public sealed class AccountDbContext(DbContextOptions<AccountDbContext> options) : DbContext(options)
    {
        public DbSet<Account> Accounts => Set<Account>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Account>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(256);
                entity.Property(e => e.PasswordHash).HasMaxLength(256);
                entity.Property(e => e.CreatedAtUtc).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });
        }
    }
}
