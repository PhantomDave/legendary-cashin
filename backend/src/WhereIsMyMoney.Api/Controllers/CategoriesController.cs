using Microsoft.AspNetCore.Mvc;
using WhereIsMyMoney.Api.Models;
using WhereIsMyMoney.Api.Models.CategoryModels;
using WhereIsMyMoney.Api.Services;

namespace WhereIsMyMoney.Api.Controllers;

[ApiController]
[Route("categories")]
public class CategoriesController(IStore<CategoryResponse, CreateCategoryRequest, CategoryResponse> store) : ApiControllerBase
{
    [HttpGet("{id:long}")]
    public async Task<ActionResult<CategoryResponse>> GetAsync(long id)
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
        return CreatedAtAction(nameof(GetAsync), new { id = created.Id }, created);
    }


}
