using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using WhereIsMyMoney.Api.Data;
using WhereIsMyMoney.Api.Models;
using WhereIsMyMoney.Api.Models.CategoryModels;
using WhereIsMyMoney.Api.Models.EnableBankingModels;
using WhereIsMyMoney.Api.Models.RuleModels;
using WhereIsMyMoney.Api.Models.TransactionModels;

namespace WhereIsMyMoney.Api.Services;

public sealed class TransactionStore(AppDbContext db, RuleStore ruleStore) : IStore<TransactionResponse, CreateTransactionRequest, UpdateTransactionRequest>
{
    public Task<bool> BudgetBelongsToAccountAsync(long budgetId, long accountId)
    {
        return db.Budgets.AnyAsync(b => b.Id == budgetId && b.AccountId == accountId);
    }

    public async Task<TransactionResponse?> GetAsync(long id)
    {
        Transaction? transaction = await db.Transactions
            .Include(t => t.Categories)
            .FirstOrDefaultAsync(t => t.Id == id);
        return transaction is null ? null : ToResponse(transaction);
    }

    public async Task<TransactionResponse?> GetByIdAndAccountAsync(long id, long accountId)
    {
        Transaction? transaction = await db.Transactions
            .Include(t => t.Categories)
            .FirstOrDefaultAsync(t => t.Id == id && t.AccountId == accountId);

        return transaction is null ? null : ToResponse(transaction);
    }

    public async Task<IReadOnlyList<TransactionResponse>> GetAllAsync()
    {
        return await db.Transactions
            .Select(t => ToResponse(t))
            .ToListAsync();
    }

    public async Task<bool> CategoryIdsBelongToAccountAsync(IReadOnlyCollection<int> categoryIds, long accountId)
    {
        if (categoryIds.Count == 0)
            return true;

        int distinctIds = categoryIds.Distinct().Count();
        int existingIds = await db.Categories
            .Where(c => c.AccountId == accountId && categoryIds.Contains(c.Id))
            .Select(c => c.Id)
            .Distinct()
            .CountAsync();

        return distinctIds == existingIds;
    }

    public async Task<IReadOnlyList<TransactionResponse>> GetAllByAccountId(long accountId)
    {
        return await db.Transactions
            .Where(t => t.AccountId == accountId)
            .Select(t => ToResponse(t))
            .ToListAsync();
    }

    public async Task<PaginatedResponse<TransactionResponse>> GetAllByAccountIdPaginatedAsync(long accountId, PaginationRequest request)
    {
        return await db.Transactions
            .Where(t => t.AccountId == accountId)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Id)
            .Select(t => ToResponse(t))
            .ToPaginatedResponseAsync(request);
    }

    public async Task<IReadOnlyList<TransactionResponse>> GetByBudgetAsync(int budgetId, long accountId)
    {
        return await db.Transactions
            .Where(t => t.BudgetId == budgetId && t.AccountId == accountId)
            .OrderByDescending(t => t.Date)
            .Select(t => ToResponse(t))
            .ToListAsync();
    }

    public async Task<PaginatedResponse<TransactionResponse>> GetByBudgetPaginatedAsync(int budgetId, long accountId, PaginationRequest request)
    {
        return await db.Transactions
            .Where(t => t.BudgetId == budgetId && t.AccountId == accountId)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Id)
            .Select(t => ToResponse(t))
            .ToPaginatedResponseAsync(request);
    }

    public async Task<long?> GetLatestBudgetIdForAccountAsync(long accountId)
    {
        return await db.Budgets
            .Where(b => b.AccountId == accountId)
            .OrderByDescending(b => b.CreatedAtUtc)
            .ThenByDescending(b => b.Id)
            .Select(b => (long?)b.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyList<TransactionResponse>> GetByBudgetAndDateRangeAsync(
        long accountId,
        long budgetId,
        DateTime from,
        DateTime to)
    {
        return await db.Transactions
            .Where(t => t.AccountId == accountId && t.BudgetId == budgetId && t.Date >= from && t.Date <= to)
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Id)
            .Select(t => ToResponse(t))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<TransactionResponse>> GetByAccountAsync(long accountId)
    {
        return await db.Transactions
            .Where(t => t.AccountId == accountId)
            .Select(t => ToResponse(t))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<TransactionResponse>> GetByDateRangeAsync(long accountId, DateTime startDate, DateTime endDate)
    {
        return await db.Transactions
            .Where(t => t.AccountId == accountId && t.Date >= startDate && t.Date <= endDate)
            .Select(t => ToResponse(t))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<TransactionResponse>> GetByCategoryAsync(long accountId, int categoryId)
    {
        return await db.Transactions
            .Where(t => t.AccountId == accountId && t.Categories.Any(c => c.Id == categoryId))
            .Select(t => ToResponse(t))
            .ToListAsync();
    }

    public async Task<TransactionResponse> CreateAsync(CreateTransactionRequest value)
    {
        Transaction transaction = new Transaction
        {
            AccountId = value.AccountId,
            BudgetId = value.BudgetId,
            Description = value.Description,
            Amount = value.Amount,
            Date = value.Date,
            Categories = value.CategoryIds.Count == 0
                ? []
                : await db.Categories
                    .Where(c => value.CategoryIds.Contains(c.Id) && c.AccountId == value.AccountId)
                    .ToListAsync()
        };

        int[] ruleCategories = await ruleStore.GetMatchedCategoryIdsAsync(transaction, value.AccountId);
        if (ruleCategories.Length > 0)
        {
            HashSet<int> existing = transaction.Categories.Select(c => c.Id).ToHashSet();
            int[] toAdd = ruleCategories.Where(id => !existing.Contains(id)).ToArray();
            if (toAdd.Length > 0)
            {
                List<Category> extra = await db.Categories
                    .Where(c => c.AccountId == value.AccountId && toAdd.Contains(c.Id))
                    .ToListAsync();
                foreach (Category c in extra) transaction.Categories.Add(c);
            }
        }

        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();

        return ToResponse(transaction);
    }

    public async Task<bool> UpdateAsync(long id, UpdateTransactionRequest value)
    {
        Transaction? transaction = await db.Transactions
            .Include(t => t.Categories)
            .FirstOrDefaultAsync(t => t.Id == id);
        if (transaction is null) return false;

        transaction.Description = value.Description;
        transaction.Amount = value.Amount;
        transaction.Date = value.Date;

        if (!value.CategoryIds.Any())
        {
            transaction.Categories.Clear();
        }
        else
        {
            transaction.Categories = await db.Categories
                .Where(c => value.CategoryIds.Contains(c.Id) && c.AccountId == transaction.AccountId)
                .ToListAsync();
        }

        db.Transactions.Update(transaction);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<TransactionResponse?> PatchAsync(long id, long accountId, PatchTransactionRequest value)
    {
        Transaction? transaction = await db.Transactions
            .Include(t => t.Categories)
            .FirstOrDefaultAsync(t => t.Id == id && t.AccountId == accountId);

        if (transaction is null)
            return null;

        if (value.Description is not null)
            transaction.Description = value.Description;

        if (value.Amount.HasValue)
            transaction.Amount = value.Amount.Value;

        if (value.Date.HasValue)
            transaction.Date = value.Date.Value;

        if (value.BudgetId.HasValue)
            transaction.BudgetId = value.BudgetId.Value;

        if (value.CategoryIds is not null)
        {
            transaction.Categories = value.CategoryIds.Count == 0
                ? []
                : await db.Categories
                    .Where(c => c.AccountId == accountId && value.CategoryIds.Contains(c.Id))
                    .ToListAsync();
        }

        await db.SaveChangesAsync();
        return ToResponse(transaction);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        Transaction? transaction = await db.Transactions.FindAsync((int)id);
        if (transaction is null) return false;

        db.Transactions.Remove(transaction);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<TransactionMetricsResponse> GetMetricsAsync(long accountId, long budgetId)
    {
        DateTime now = DateTime.UtcNow;
        DateTime startOfYear = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime thirtyDaysAgo = now.AddDays(-30);
        DateTime sixtyDaysAgo = now.AddDays(-60);

        int daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);
        int daysElapsed = Math.Max(now.Day, 1);

        // YTD
        decimal ytd = await SumTransactionsAsync(accountId, budgetId, startOfYear, now);

        // YTD same period last year
        DateTime lastYearStart = startOfYear.AddYears(-1);
        DateTime lastYearSameDay = now.AddYears(-1);
        decimal ytdLastYear = await SumTransactionsAsync(accountId, budgetId, lastYearStart, lastYearSameDay);

        // MTD
        decimal mtd = await SumTransactionsAsync(accountId, budgetId, startOfMonth, now);

        // MTD same period last month
        DateTime startOfLastMonth = startOfMonth.AddMonths(-1);
        int daysInLastMonth = DateTime.DaysInMonth(startOfLastMonth.Year, startOfLastMonth.Month);
        DateTime sameDayLastMonth = startOfLastMonth.AddDays(Math.Min(daysElapsed, daysInLastMonth) - 1);
        decimal mtdLastMonth = await SumTransactionsAsync(accountId, budgetId, startOfLastMonth, sameDayLastMonth);

        // Predicted EOM
        decimal predictedEndOfMonth = (mtd / daysElapsed) * daysInMonth;

        // Last month's actual total (for predicted EOM trend)
        DateTime endOfLastMonth = startOfMonth.AddDays(-1);
        decimal lastMonthTotal = await SumTransactionsAsync(accountId, budgetId, startOfLastMonth, endOfLastMonth);

        // Balance 30 days ago (cumulative sum up to that point)
        decimal balance30DaysAgo = await db.Transactions
            .Where(t => t.AccountId == accountId && t.BudgetId == budgetId && t.Date <= thirtyDaysAgo)
            .SumAsync(t => (decimal?)t.Amount) ?? 0m;

        // Balance 60 days ago
        decimal balance60DaysAgo = await db.Transactions
            .Where(t => t.AccountId == accountId && t.BudgetId == budgetId && t.Date <= sixtyDaysAgo)
            .SumAsync(t => (decimal?)t.Amount) ?? 0m;

        return new TransactionMetricsResponse(
            ytd,
            Trend(ytd, ytdLastYear),
            mtd,
            Trend(mtd, mtdLastMonth),
            predictedEndOfMonth,
            Trend(predictedEndOfMonth, lastMonthTotal),
            balance30DaysAgo,
            Trend(balance30DaysAgo, balance60DaysAgo)
        );
    }

    public async Task<IReadOnlyList<MonthlySummaryResponse>> GetMonthlySummaryAsync(long accountId, long budgetId)
    {
        const int monthsToInclude = 6; // Dashboard charts display a rolling 6-month trend window.
        DateTime now = DateTime.UtcNow;
        DateTime currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime firstMonthStart = currentMonthStart.AddMonths(-(monthsToInclude - 1));
        DateTime endExclusive = currentMonthStart.AddMonths(1);

        List<MonthlySummaryResponse> aggregates = await db.Transactions
            .Where(t =>
                t.AccountId == accountId &&
                t.BudgetId == budgetId &&
                t.Date >= firstMonthStart &&
                t.Date < endExclusive)
            .GroupBy(t => new { t.Date.Year, t.Date.Month })
            .Select(g => new MonthlySummaryResponse(
                g.Key.Year,
                g.Key.Month,
                g.Where(t => t.Amount > 0).Sum(t => (decimal?)t.Amount) ?? 0m,
                Math.Abs(g.Where(t => t.Amount < 0).Sum(t => (decimal?)t.Amount) ?? 0m)))
            .ToListAsync();

        Dictionary<(int Year, int Month), MonthlySummaryResponse> lookup = aggregates.ToDictionary(
            m => (m.Year, m.Month));

        return Enumerable.Range(0, monthsToInclude)
            .Select(offset =>
            {
                DateTime monthStart = firstMonthStart.AddMonths(offset);
                return lookup.TryGetValue((monthStart.Year, monthStart.Month), out MonthlySummaryResponse? summary)
                    ? summary
                    : new MonthlySummaryResponse(monthStart.Year, monthStart.Month, 0m, 0m);
            })
            .ToList();
    }

    /// <summary>
    /// Inserts imported Enable Banking transactions, skipping any that already exist
    /// (matched via <see cref="BuildExternalRef"/>).
    /// Returns the number of rows inserted and skipped.
    /// </summary>
    public async Task<(int Inserted, int Skipped)> ImportFromEnableBankingAsync(
        IReadOnlyList<ImportedTransaction> transactions,
        long budgetId)
    {
        if (transactions.Count == 0)
            return (0, 0);

        // Build the dedup key for every incoming transaction up front.
        List<(ImportedTransaction Source, string Ref)> keyed = transactions
            .Select(t => (t, BuildExternalRef(t)))
            .ToList();

        HashSet<string> incomingRefs = keyed.Select(k => k.Ref).ToHashSet();
        long accountId = transactions[0].OwnerAccountId;

        // Single round-trip: fetch every matching ref that already lives in the DB.
        HashSet<string> existingRefs = (await db.Transactions
            .Where(t => t.AccountId == accountId
                     && t.ExternalRef != null
                     && incomingRefs.Contains(t.ExternalRef!))
            .Select(t => t.ExternalRef!)
            .ToListAsync())
            .ToHashSet();

        // Load active rules once for the entire batch.
        IReadOnlyList<Rule> activeRules = await ruleStore.GetActiveRulesByAccountIdAsync(accountId);

        int inserted = 0;
        int skipped = 0;

        foreach ((ImportedTransaction source, string externalRef) in keyed)
        {
            if (existingRefs.Contains(externalRef))
            {
                skipped++;
                continue;
            }

            decimal amount = decimal.Parse(source.Amount, CultureInfo.InvariantCulture);
            // Debits are outgoing money — store as negative.
            if (source.CreditDebitIndicator == "DBIT")
                amount = -Math.Abs(amount);
            else
                amount = Math.Abs(amount);

            DateTime date = ParseImportedTransactionDate(source);

            string description = (source.Description ?? string.Empty).Trim();
            if (description.Length == 0) description = "Enable Banking import";
            if (description.Length > 256) description = description[..256];

            Transaction tx = new Transaction
            {
                AccountId = source.OwnerAccountId,
                BudgetId = budgetId,
                Description = description,
                Amount = amount,
                Date = date,
                ExternalRef = externalRef,
            };

            // Apply rules to auto-categorize the imported transaction.
            if (activeRules.Count > 0)
            {
                int[] matchedCategoryIds = ruleStore.GetMatchedCategoryIds(activeRules, tx);
                if (matchedCategoryIds.Length > 0)
                {
                    tx.Categories = await db.Categories
                        .Where(c => c.AccountId == accountId && matchedCategoryIds.Contains(c.Id))
                        .ToListAsync();
                }
            }

            db.Transactions.Add(tx);

            // Track locally so within-batch dupes are also caught without a second DB trip.
            existingRefs.Add(externalRef);
            inserted++;
        }

        if (inserted > 0)
            await db.SaveChangesAsync();

        return (inserted, skipped);
    }

    /// <summary>
    /// Builds a stable, unique string key for an Enable Banking transaction.
    /// Prefers the bank-assigned ID; falls back to a SHA-256 content hash so
    /// banks that omit IDs are still deduplicated reliably.
    /// </summary>
    internal static string BuildExternalRef(ImportedTransaction t)
    {
        if (!string.IsNullOrWhiteSpace(t.TransactionId))
            return $"eb:{t.TransactionId}";

        // Include enough fields to make accidental collisions extremely unlikely.
        string raw = string.Join("|",
            t.AccountUid,
            t.BookingDate ?? t.ValueDate ?? string.Empty,
            t.Amount,
            t.Currency,
            t.CreditDebitIndicator,
            t.Description ?? string.Empty);

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return $"eb-hash:{Convert.ToHexString(hash)[..16]}";
    }

    private static DateTime ParseImportedTransactionDate(ImportedTransaction source)
    {
        string[] candidates =
        [
            source.BookingDateTime ?? string.Empty,
            source.ValueDateTime ?? string.Empty,
            source.BookingDate ?? string.Empty,
            source.ValueDate ?? string.Empty,
        ];

        foreach (string candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
                continue;

            if (DateTimeOffset.TryParse(candidate, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out DateTimeOffset dto))
            {
                return dto.UtcDateTime;
            }

            if (DateOnly.TryParse(candidate, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly d))
            {
                return d.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            }
        }

        return DateTime.UtcNow;
    }

    private async Task<decimal> SumTransactionsAsync(long accountId, long budgetId, DateTime from, DateTime to)
    {
        return await db.Transactions
            .Where(t => t.AccountId == accountId && t.BudgetId == budgetId && t.Date >= from && t.Date <= to)
            .SumAsync(t => (decimal?)t.Amount) ?? 0m;
    }

    private static decimal? Trend(decimal current, decimal previous)
    {
        return previous == 0 ? null : Math.Round((current - previous) / Math.Abs(previous) * 100, 1);
    }

    internal static TransactionResponse ToResponse(Transaction transaction)
    {
        return new(transaction.Id, transaction.AccountId, transaction.Description, transaction.Amount, transaction.Date, transaction.BudgetId, transaction.Categories.Select(c => c.Id).ToList());
    }
}
