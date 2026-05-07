using Microsoft.EntityFrameworkCore;
using WhereIsMyMoney.Api.Data;
using WhereIsMyMoney.Api.Models;
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

    public async Task<IReadOnlyList<BudgetResponse>> GetAllByAccountId(long accountId)
    {
        return await db.Budgets
            .Where(b => b.AccountId == accountId)
            .Select(b => ToResponse(b))
            .ToListAsync();
    }

    public async Task<PaginatedResponse<BudgetResponse>> GetAllByAccountIdPaginatedAsync(long accountId, PaginationRequest request)
    {
        return await db.Budgets
            .Where(b => b.AccountId == accountId)
            .OrderByDescending(b => b.CreatedAtUtc)
            .ThenByDescending(b => b.Id)
            .Select(b => ToResponse(b))
            .ToPaginatedResponseAsync(request);
    }

    public async Task<BudgetResponse> CreateAsync(BudgetResponse value)
    {
        Budget budget = new Budget
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
        Budget budget = new Budget
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

    internal static BudgetResponse ToResponse(Budget budget)
    {
        return new(budget.Id, budget.AccountId, budget.Name, budget.DefaultCurrency, budget.Amount, budget.CreatedAtUtc);
    }
}
