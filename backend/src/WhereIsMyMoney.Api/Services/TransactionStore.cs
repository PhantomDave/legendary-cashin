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

    internal static TransactionResponse ToResponse(Transaction transaction) =>
        new(transaction.Id, transaction.AccountId, transaction.Description, transaction.Amount, transaction.Date, transaction.BudgetId, transaction.Categories.Select(c => c.Id).ToList());
}
