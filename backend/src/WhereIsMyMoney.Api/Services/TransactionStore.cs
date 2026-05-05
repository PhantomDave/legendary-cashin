using Microsoft.EntityFrameworkCore;
using WhereIsMyMoney.Api.Data;
using WhereIsMyMoney.Api.Models.TransactionModels;

namespace WhereIsMyMoney.Api.Services;

public sealed class TransactionStore(AppDbContext db) : IStore<TransactionResponse, CreateTransactionRequest, UpdateTransactionRequest>
{
    public Task<bool> BudgetBelongsToAccountAsync(long budgetId, long accountId)
    {
        return db.Budgets.AnyAsync(b => b.Id == budgetId && b.AccountId == accountId);
    }

    public async Task<TransactionResponse?> GetAsync(long id)
    {
        Transaction? transaction = await db.Transactions.FindAsync(id);
        return transaction is null ? null : ToResponse(transaction);
    }

    public async Task<IReadOnlyList<TransactionResponse>> GetAllAsync()
    {
        return await db.Transactions
            .Select(t => ToResponse(t))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<TransactionResponse>> GetByBudgetAsync(int budgetId, long accountId)
    {
        return await db.Transactions
            .Where(t => t.BudgetId == budgetId && t.AccountId == accountId)
            .OrderByDescending(t => t.Date)
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
        var transaction = new Transaction
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

        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();

        return ToResponse(transaction);
    }

    public async Task<bool> UpdateAsync(long id, UpdateTransactionRequest value)
    {
        Transaction? transaction = await db.Transactions.FindAsync(id);
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

    public async Task<bool> DeleteAsync(long id)
    {
        Transaction? transaction = await db.Transactions.FindAsync(id);
        if (transaction is null) return false;

        db.Transactions.Remove(transaction);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<TransactionMetricsResponse> GetMetricsAsync(long accountId, long budgetId)
    {
        var now = DateTime.UtcNow;
        var startOfYear = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var thirtyDaysAgo = now.AddDays(-30);
        var sixtyDaysAgo = now.AddDays(-60);

        int daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);
        int daysElapsed = Math.Max(now.Day, 1);

        // YTD
        var ytd = await SumTransactionsAsync(accountId, budgetId, startOfYear, now);

        // YTD same period last year
        var lastYearStart = startOfYear.AddYears(-1);
        var lastYearSameDay = now.AddYears(-1);
        var ytdLastYear = await SumTransactionsAsync(accountId, budgetId, lastYearStart, lastYearSameDay);

        // MTD
        var mtd = await SumTransactionsAsync(accountId, budgetId, startOfMonth, now);

        // MTD same period last month
        var startOfLastMonth = startOfMonth.AddMonths(-1);
        int daysInLastMonth = DateTime.DaysInMonth(startOfLastMonth.Year, startOfLastMonth.Month);
        var sameDayLastMonth = startOfLastMonth.AddDays(Math.Min(daysElapsed, daysInLastMonth) - 1);
        var mtdLastMonth = await SumTransactionsAsync(accountId, budgetId, startOfLastMonth, sameDayLastMonth);

        // Predicted EOM
        var predictedEndOfMonth = (mtd / daysElapsed) * daysInMonth;

        // Last month's actual total (for predicted EOM trend)
        var endOfLastMonth = startOfMonth.AddDays(-1);
        var lastMonthTotal = await SumTransactionsAsync(accountId, budgetId, startOfLastMonth, endOfLastMonth);

        // Balance 30 days ago (cumulative sum up to that point)
        var balance30DaysAgo = await db.Transactions
            .Where(t => t.AccountId == accountId && t.BudgetId == budgetId && t.Date <= thirtyDaysAgo)
            .SumAsync(t => (decimal?)t.Amount) ?? 0m;

        // Balance 60 days ago
        var balance60DaysAgo = await db.Transactions
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

    private async Task<decimal> SumTransactionsAsync(long accountId, long budgetId, DateTime from, DateTime to) =>
        await db.Transactions
            .Where(t => t.AccountId == accountId && t.BudgetId == budgetId && t.Date >= from && t.Date <= to)
            .SumAsync(t => (decimal?)t.Amount) ?? 0m;

    private static decimal? Trend(decimal current, decimal previous) =>
        previous == 0 ? null : Math.Round((current - previous) / Math.Abs(previous) * 100, 1);

    internal static TransactionResponse ToResponse(Transaction transaction) =>
        new(transaction.Id, transaction.AccountId, transaction.Description, transaction.Amount, transaction.Date, transaction.BudgetId, transaction.Categories.Select(c => c.Id).ToList());
}
