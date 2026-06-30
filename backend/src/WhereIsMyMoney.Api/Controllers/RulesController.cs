using Microsoft.AspNetCore.Mvc;
using WhereIsMyMoney.Api.Models;
using WhereIsMyMoney.Api.Models.RuleModels;
using WhereIsMyMoney.Api.Models.TransactionModels;
using WhereIsMyMoney.Api.Services;

namespace WhereIsMyMoney.Api.Controllers;

[ApiController]
[Route("rules")]
public sealed class RulesController(RuleStore store) : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<RuleResponse>>> GetAllAsync([FromQuery] PaginationRequest request)
    {
        long accountId = GetAccountId();
        PaginatedResponse<RuleResponse> rules = await store.GetAllByAccountIdPaginatedAsync(accountId, request);
        return Ok(rules);
    }

    [HttpGet("active")]
    public async Task<ActionResult<IReadOnlyList<RuleResponse>>> GetActiveAsync()
    {
        long accountId = GetAccountId();
        IReadOnlyList<RuleResponse> rules = await store.GetActiveResponsesByAccountIdAsync(accountId);
        return Ok(rules);
    }

    [HttpGet("{id:long}", Name = "GetRuleById")]
    public async Task<ActionResult<RuleResponse>> GetAsync(long id)
    {
        long accountId = GetAccountId();
        RuleResponse? rule = await store.GetByIdAndAccountAsync(id, accountId);
        return rule is null ? NotFound() : Ok(rule);
    }

    [HttpPost]
    public async Task<ActionResult<RuleResponse>> CreateAsync([FromBody] CreateRuleRequest request)
    {
        long accountId = GetAccountId();
        request.AccountId = accountId;

        (bool valid, string? error) = await store.ValidateRuleAsync(request);
        if (!valid)
            return BadRequest(new { message = error });

        RuleResponse created = await store.CreateAsync(request);
        return CreatedAtRoute("GetRuleById", new { id = created.Id }, created);
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<RuleResponse>> UpdateAsync(long id, [FromBody] UpdateRuleRequest request)
    {
        long accountId = GetAccountId();
        RuleResponse? existing = await store.GetByIdAndAccountAsync(id, accountId);
        if (existing is null) return NotFound();

        // Validate categories and regex using a CreateRuleRequest proxy
        CreateRuleRequest proxy = new CreateRuleRequest
        {
            AccountId = accountId,
            Name = request.Name,
            MatchType = request.MatchType,
            DescriptionPattern = request.DescriptionPattern,
            CategoryIds = request.CategoryIds,
            BudgetId = request.BudgetId,
        };

        (bool valid, string? error) = await store.ValidateRuleAsync(proxy);
        if (!valid)
            return BadRequest(new { message = error });

        bool success = await store.UpdateAsync(id, request);
        if (!success) return NotFound();

        RuleResponse? updated = await store.GetByIdAndAccountAsync(id, accountId);
        return Ok(updated);
    }

    [HttpPatch("{id:long}")]
    public async Task<ActionResult<RuleResponse>> PatchAsync(long id, [FromBody] PatchRuleRequest request)
    {
        long accountId = GetAccountId();
        bool success = await store.PatchAsync(id, accountId, request);
        if (!success) return NotFound();

        RuleResponse? updated = await store.GetByIdAndAccountAsync(id, accountId);
        return Ok(updated);
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteAsync(long id)
    {
        long accountId = GetAccountId();
        RuleResponse? existing = await store.GetByIdAndAccountAsync(id, accountId);
        if (existing is null) return NotFound();

        bool success = await store.DeleteAsync(id);
        return success ? NoContent() : NotFound();
    }

    [HttpPost("reorder")]
    public async Task<IActionResult> ReorderAsync([FromBody] ReorderRulesRequest request)
    {
        long accountId = GetAccountId();
        await store.ReorderAsync(accountId, request.RuleIds);
        return NoContent();
    }

    [HttpPost("{id:long}/preview")]
    public async Task<ActionResult<PaginatedResponse<TransactionResponse>>> PreviewAsync(long id, [FromQuery] PaginationRequest request)
    {
        long accountId = GetAccountId();
        PaginatedResponse<TransactionResponse> result = await store.PreviewRuleAsync(id, accountId, request);
        return Ok(result);
    }

    [HttpPost("apply-existing")]
    public async Task<ActionResult<object>> ApplyToExistingAsync([FromBody] ApplyToExistingRequest request)
    {
        long accountId = GetAccountId();
        int updated = await store.ApplyRulesToHistoricalAsync(accountId, request.FromDate, request.ToDate, request.OverwriteExisting);
        return Ok(new { updated });
    }

    [HttpPost("count-existing")]
    public async Task<ActionResult<object>> CountExistingAsync([FromBody] ApplyToExistingRequest request)
    {
        long accountId = GetAccountId();
        int count = await store.CountHistoricalMatchAsync(accountId, request.FromDate, request.ToDate);
        return Ok(new { count });
    }
}
