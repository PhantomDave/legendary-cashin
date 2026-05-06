using Microsoft.AspNetCore.Mvc;
using WhereIsMyMoney.Api.Models;
using WhereIsMyMoney.Api.Models.CategoryModels;
using WhereIsMyMoney.Api.Services;

namespace WhereIsMyMoney.Api.Controllers;

[ApiController]
[Route("categories")]
public class CategoriesController(CategoryStore store) : ApiControllerBase
{
    [HttpGet("{id:int}", Name = "GetCategoryById")]
    public async Task<ActionResult<CategoryResponse>> GetAsync(int id)
    {
        CategoryResponse? category = await store.GetAsync(id);
        return category is null ? NotFound() : Ok(category);
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<CategoryResponse>>> GetAllAsync([FromQuery] PaginationRequest request)
    {
        long accountId = GetAccountId();
        PaginatedResponse<CategoryResponse> categories = await store.GetAllByAccountIdPaginatedAsync(accountId, request);
        return Ok(categories);
    }

    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> CreateAsync([FromBody] CreateCategoryRequest request)
    {
        long accountId = GetAccountId();
        request.AccountId = accountId;
        CategoryResponse created = await store.CreateAsync(request);
        return CreatedAtRoute("GetCategoryById", new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<CategoryResponse>> UpdateAsync(int id, [FromBody] UpdateCategoryRequest request)
    {
        long accountId = GetAccountId();
        CategoryResponse? existing = await store.GetAsync(id);
        if (existing is null || existing.AccountId != accountId)
            return NotFound();

        var updated = existing with { Name = request.Name, Budget = request.Budget };
        bool success = await store.UpdateAsync(id, updated);
        return success ? Ok(updated) : NotFound();
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<CategoryResponse>> DeleteAsync(int id)
    {
        long accountId = GetAccountId();
        CategoryResponse? existing = await store.GetAsync(id);
        if (existing is null || existing.AccountId != accountId)
            return NotFound();

        bool success = await store.DeleteAsync(id);
        return success ? NoContent() : NotFound();
    }

}
