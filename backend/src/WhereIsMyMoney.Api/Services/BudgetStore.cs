using Microsoft.EntityFrameworkCore;
using WhereIsMyMoney.Api.Data;
using WhereIsMyMoney.Api.Models.BudgetModels;

namespace WhereIsMyMoney.Api.Services;

public sealed class BudgetStore(AppDbContext db) : IStore<BudgetResponse, BudgetResponse, BudgetResponse>
{
    public async Task<BudgetResponse?> GetAsync(long id)
    {
        Budget? budget = await db.Budgets.FindAsync(id);
        return budget is null ? null : ToResponse(budget);
    }

    public async Task<IReadOnlyList<BudgetResponse>> GetAllAsync()
    {
        return await db.Budgets
            .Select(b => ToResponse(b))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<BudgetResponse>> GetByAccountAsync(long accountId)
    {
        return await db.Budgets
            .Where(b => b.AccountId == accountId)
            .Select(b => ToResponse(b))
            .ToListAsync();
    }

    public async Task<BudgetResponse> CreateAsync(BudgetResponse value)
    {
        var budget = new Budget
        {
            AccountId = value.AccountId,
            Name = value.Name,
            DefaultCurrency = value.DefaultCurrency,
            Amount = value.Amount,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        db.Budgets.Add(budget);
        await db.SaveChangesAsync();

        return ToResponse(budget);
    }

    public async Task<bool> UpdateAsync(long id, BudgetResponse value)
    {
        Budget? budget = await db.Budgets.FindAsync(id);
        if (budget is null) return false;

        budget.Name = value.Name;
        budget.DefaultCurrency = value.DefaultCurrency;
        budget.Amount = value.Amount;

        db.Budgets.Update(budget);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        Budget? budget = await db.Budgets.FindAsync(id);
        if (budget is null) return false;

        db.Budgets.Remove(budget);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<BudgetResponse> CreateAsync(long accountId, CreateBudgetRequest request)
    {
        var budget = new Budget
        {
            AccountId = accountId,
            Name = request.Name,
            DefaultCurrency = request.DefaultCurrency,
            Amount = request.Amount,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        db.Budgets.Add(budget);
        await db.SaveChangesAsync();

        return ToResponse(budget);
    }

    public async Task<bool> UpdateBudgetAmount(long budgetId, decimal amount)
    {
        Budget? budget = await db.Budgets.FindAsync(budgetId);
        if (budget is null) return false;

        budget.Amount += amount;

        db.Budgets.Update(budget);
        await db.SaveChangesAsync();
        return true;
    }

    internal static BudgetResponse ToResponse(Budget budget) =>
        new(budget.Id, budget.AccountId, budget.Name, budget.DefaultCurrency, budget.Amount, budget.CreatedAtUtc);
}
