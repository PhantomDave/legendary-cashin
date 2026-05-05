using Microsoft.EntityFrameworkCore;
using WhereIsMyMoney.Api.Data;
using WhereIsMyMoney.Api.Models.CategoryModels;

namespace WhereIsMyMoney.Api.Services;

public sealed class CategoryStore(AppDbContext db) : IStore<CategoryResponse, CategoryResponse, CategoryResponse>
{
    public async Task<CategoryResponse?> GetAsync(long id)
    {
        Category? category = await db.Categories.FindAsync((int)id);
        return category is null ? null : ToResponse(category);
    }

    public async Task<IReadOnlyList<CategoryResponse>> GetAllAsync()
    {
        return await db.Categories
            .Select(c => ToResponse(c))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<CategoryResponse>> GetByAccountAsync(long accountId)
    {
        return await db.Categories
            .Where(c => c.AccountId == accountId)
            .Select(c => ToResponse(c))
            .ToListAsync();
    }

    public async Task<CategoryResponse> CreateAsync(CategoryResponse value)
    {
        var category = new Category
        {
            Name = value.Name,
            Budget = value.Budget,
            AccountId = value.AccountId
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync();

        return ToResponse(category);
    }

    public async Task<bool> UpdateAsync(long id, CategoryResponse value)
    {
        Category? category = await db.Categories.FindAsync((int)id);
        if (category is null) return false;

        category.Name = value.Name;
        category.Budget = value.Budget;

        db.Categories.Update(category);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        Category? category = await db.Categories.FindAsync((int)id);
        if (category is null) return false;

        db.Categories.Remove(category);
        await db.SaveChangesAsync();
        return true;
    }

    internal static CategoryResponse ToResponse(Category category) =>
        new(category.Id, category.AccountId, category.Name, category.Budget);
}
