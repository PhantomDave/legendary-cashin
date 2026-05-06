using Microsoft.EntityFrameworkCore;
using WhereIsMyMoney.Api.Data;
using WhereIsMyMoney.Api.Models;
using WhereIsMyMoney.Api.Models.CategoryModels;

namespace WhereIsMyMoney.Api.Services;

public sealed class CategoryStore(AppDbContext db) : IStore<CategoryResponse, CreateCategoryRequest, CategoryResponse>
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

    public async Task<IReadOnlyList<CategoryResponse>> GetAllByAccountId(long accountId)
    {
        return await db.Categories
            .Where(c => c.AccountId == accountId)
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id)
            .Select(c => ToResponse(c))
            .ToListAsync();
    }

    public async Task<PaginatedResponse<CategoryResponse>> GetAllByAccountIdPaginatedAsync(long accountId, PaginationRequest request)
    {
        return await db.Categories
            .Where(c => c.AccountId == accountId)
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id)
            .Select(c => ToResponse(c))
            .ToPaginatedResponseAsync(request);
    }

    public async Task<CategoryResponse> CreateAsync(CreateCategoryRequest request)
    {
        var category = new Category
        {
            Name = request.Name,
            Budget = request.Budget,
            AccountId = request.AccountId
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
