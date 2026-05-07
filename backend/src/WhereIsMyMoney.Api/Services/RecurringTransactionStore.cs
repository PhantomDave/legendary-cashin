using Microsoft.EntityFrameworkCore;
using WhereIsMyMoney.Api.Data;
using WhereIsMyMoney.Api.Models;
using WhereIsMyMoney.Api.Models.TransactionModels;

namespace WhereIsMyMoney.Api.Services;

public sealed class RecurringTransactionStore(AppDbContext db)
{
    public async Task<RecurringTransactionResponse?> GetByIdAndAccountAsync(long id, long accountId)
    {
        RecurringTransaction? transaction = await db.RecurringTransactions
            .FirstOrDefaultAsync(t => t.Id == id && t.AccountId == accountId);

        return transaction is null ? null : ToResponse(transaction);
    }

    public async Task<IReadOnlyList<RecurringTransactionResponse>> GetAllByAccountAsync(long accountId)
    {
        return await db.RecurringTransactions
            .Where(t => t.AccountId == accountId)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => ToResponse(t))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<RecurringTransactionResponse>> GetActiveByAccountAsync(long accountId)
    {
        return await db.RecurringTransactions
            .Where(t => t.AccountId == accountId && t.IsActive)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => ToResponse(t))
            .ToListAsync();
    }

    public async Task<RecurringTransactionResponse> CreateAsync(CreateRecurringTransactionRequest request)
    {
        DateTime now = DateTime.UtcNow;
        RecurringTransaction entity = new RecurringTransaction
        {
            AccountId = request.AccountId,
            BudgetId = request.BudgetId,
            Description = request.Description,
            Amount = request.Amount,
            CategoryIds = request.CategoryIds,
            Frequency = request.Frequency,
            Interval = request.Interval,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            MaxOccurrences = request.MaxOccurrences,
            DaysOfWeek = request.DaysOfWeek,
            DayOfMonth = request.DayOfMonth,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        db.RecurringTransactions.Add(entity);
        await db.SaveChangesAsync();

        return ToResponse(entity);
    }

    public async Task<RecurringTransactionResponse> UpdateAsync(long id, long accountId, UpdateRecurringTransactionRequest request)
    {
        RecurringTransaction? entity = await db.RecurringTransactions
            .FirstOrDefaultAsync(t => t.Id == id && t.AccountId == accountId)
            ?? throw new KeyNotFoundException($"Recurring transaction {id} not found.");

        if (!string.IsNullOrEmpty(request.Description))
            entity.Description = request.Description;

        if (request.Amount.HasValue)
            entity.Amount = request.Amount.Value;

        if (request.CategoryIds is not null)
            entity.CategoryIds = request.CategoryIds;

        if (request.Frequency.HasValue)
            entity.Frequency = request.Frequency.Value;

        if (request.Interval.HasValue)
            entity.Interval = request.Interval.Value;

        if (request.StartDate.HasValue)
            entity.StartDate = request.StartDate.Value;

        if (request.EndDate.HasValue || (request.EndDate is null && request.EndDate != entity.EndDate))
            entity.EndDate = request.EndDate;

        if (request.MaxOccurrences.HasValue || (request.MaxOccurrences is null && request.MaxOccurrences != entity.MaxOccurrences))
            entity.MaxOccurrences = request.MaxOccurrences;

        if (request.DaysOfWeek is not null)
            entity.DaysOfWeek = request.DaysOfWeek;

        if (request.DayOfMonth.HasValue)
            entity.DayOfMonth = request.DayOfMonth.Value;

        if (request.IsActive.HasValue)
            entity.IsActive = request.IsActive.Value;

        entity.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return ToResponse(entity);
    }

    public async Task<bool> DeleteAsync(long id, long accountId)
    {
        RecurringTransaction? entity = await db.RecurringTransactions
            .FirstOrDefaultAsync(t => t.Id == id && t.AccountId == accountId);

        if (entity is null)
            return false;

        db.RecurringTransactions.Remove(entity);
        await db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> BudgetBelongsToAccountAsync(long budgetId, long accountId)
    {
        return await db.Budgets.AnyAsync(b => b.Id == budgetId && b.AccountId == accountId);
    }

    public async Task<bool> CategoriesBelongToAccountAsync(IReadOnlyCollection<int> categoryIds, long accountId)
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

    /// <summary>
    /// Gets all active recurring transactions that need processing today.
    /// </summary>
    public async Task<IReadOnlyList<RecurringTransaction>> GetDueTransactionsAsync(RecurrenceEngine engine)
    {
        List<RecurringTransaction> allActive = await db.RecurringTransactions
            .Where(t => t.IsActive)
            .ToListAsync();

        List<RecurringTransaction> dueTransactions = allActive
            .Where(t => engine.ShouldGenerateTransactionToday(t))
            .ToList();

        return dueTransactions;
    }

    /// <summary>
    /// Updates the generation tracking after a transaction is created.
    /// </summary>
    public async Task UpdateGenerationTrackingAsync(long recurringTransactionId)
    {
        RecurringTransaction? entity = await db.RecurringTransactions.FindAsync(recurringTransactionId);
        if (entity is not null)
        {
            entity.LastGeneratedDate = DateTime.UtcNow;
            entity.GeneratedCount++;
            entity.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    private static RecurringTransactionResponse ToResponse(RecurringTransaction entity)
    {
        return new RecurringTransactionResponse
        {
            Id = entity.Id,
            AccountId = entity.AccountId,
            BudgetId = entity.BudgetId,
            Description = entity.Description,
            Amount = entity.Amount,
            CategoryIds = entity.CategoryIds,
            Frequency = entity.Frequency,
            Interval = entity.Interval,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            MaxOccurrences = entity.MaxOccurrences,
            DaysOfWeek = entity.DaysOfWeek,
            DayOfMonth = entity.DayOfMonth,
            LastGeneratedDate = entity.LastGeneratedDate,
            GeneratedCount = entity.GeneratedCount,
            IsActive = entity.IsActive,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc
        };
    }
}
