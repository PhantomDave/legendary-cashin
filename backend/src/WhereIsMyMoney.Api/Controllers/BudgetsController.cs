using Microsoft.AspNetCore.Mvc;
using WhereIsMyMoney.Api.Models;
using WhereIsMyMoney.Api.Models.BudgetModels;
using WhereIsMyMoney.Api.Services;

namespace WhereIsMyMoney.Api.Controllers;

[ApiController]
[Route("budgets")]
public sealed class BudgetsController(BudgetStore store) : ApiControllerBase
{
    [HttpGet("{id:long}")]
    [ProducesResponseType<BudgetResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BudgetResponse>> GetById(long id)
    {
        BudgetResponse? budget = await store.GetAsync(id);
        return budget is null
            ? NotFound(new { message = $"Budget '{id}' was not found." })
            : Ok(budget);
    }

    [HttpGet]
    [ProducesResponseType<PaginatedResponse<BudgetResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<BudgetResponse>>> GetMyBudgets([FromQuery] PaginationRequest request)
    {
        long accountId = GetAccountId();
        return Ok(await store.GetAllByAccountIdPaginatedAsync(accountId, request));
    }

    [HttpPost]
    [ProducesResponseType<BudgetResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BudgetResponse>> Create([FromBody] CreateBudgetRequest request)
    {
        long accountId = GetAccountId();
        BudgetResponse created = await store.CreateAsync(accountId, request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id)
    {
        bool deleted = await store.DeleteAsync(id);
        return deleted ? NoContent() : NotFound(new { message = $"Budget '{id}' was not found." });
    }
}
