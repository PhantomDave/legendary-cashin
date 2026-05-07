using Microsoft.AspNetCore.Mvc;
using WhereIsMyMoney.Api.Models;
using WhereIsMyMoney.Api.Models.TransactionModels;
using WhereIsMyMoney.Api.Services;

namespace WhereIsMyMoney.Api.Controllers;

[ApiController]
[Route("transactions")]
public sealed class TransactionsController(TransactionStore store, BudgetStore budgetStore) : ApiControllerBase
{
    [HttpGet("metrics")]
    [ProducesResponseType<TransactionMetricsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TransactionMetricsResponse>> GetMetrics([FromQuery] long budgetId)
    {
        long accountId = GetAccountId();

        bool budgetBelongsToAccount = await store.BudgetBelongsToAccountAsync(budgetId, accountId);
        if (!budgetBelongsToAccount)
            return BadRequest(new { message = $"Budget '{budgetId}' is invalid for this account." });

        return Ok(await store.GetMetricsAsync(accountId, budgetId));
    }

    [HttpGet]
    [ProducesResponseType<PaginatedResponse<TransactionResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<TransactionResponse>>> GetMyTransactions([FromQuery] PaginationRequest request)
    {
        long accountId = GetAccountId();
        return Ok(await store.GetAllByAccountIdPaginatedAsync(accountId, request));
    }

    [HttpGet("budget/{id:int}")]
    [ProducesResponseType<PaginatedResponse<TransactionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaginatedResponse<TransactionResponse>>> GetByBudget(int id, [FromQuery] PaginationRequest request)
    {
        long accountId = GetAccountId();
        var transactions = await store.GetByBudgetPaginatedAsync(id, accountId, request);
        return transactions.TotalCount == 0
            ? NotFound(new { message = $"Transactions for budget '{id}' were not found." })
            : Ok(transactions);
    }


    [HttpPost]
    [ProducesResponseType<TransactionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TransactionResponse>> CreateTransaction(CreateTransactionRequest request)
    {
        long accountId = GetAccountId();
        request.AccountId = accountId;

        bool budgetBelongsToAccount = await store.BudgetBelongsToAccountAsync(request.BudgetId, accountId);
        if (!budgetBelongsToAccount)
        {
            return BadRequest(new { message = $"Budget '{request.BudgetId}' is invalid for this account." });
        }

        var transaction = await store.CreateAsync(request);
        await budgetStore.UpdateBudgetAmount(transaction.BudgetId, request.Amount);

        return CreatedAtAction(nameof(GetByBudget), new { id = transaction.BudgetId }, transaction);
    }

    [HttpPatch("{id:int}")]
    [ProducesResponseType<TransactionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionResponse>> PatchTransaction(int id, [FromBody] PatchTransactionRequest request)
    {
        if (request.Description is null &&
            request.Amount is null &&
            request.Date is null &&
            request.CategoryIds is null &&
            request.BudgetId is null)
        {
            return BadRequest(new { message = "At least one field must be provided." });
        }

        if (request.Description is not null && string.IsNullOrWhiteSpace(request.Description))
            return BadRequest(new { message = "Description cannot be empty." });

        long accountId = GetAccountId();
        TransactionResponse? existing = await store.GetByIdAndAccountAsync(id, accountId);
        if (existing is null)
            return NotFound(new { message = $"Transaction '{id}' was not found." });

        if (request.BudgetId.HasValue)
        {
            bool budgetBelongsToAccount = await store.BudgetBelongsToAccountAsync(request.BudgetId.Value, accountId);
            if (!budgetBelongsToAccount)
                return BadRequest(new { message = $"Budget '{request.BudgetId.Value}' is invalid for this account." });
        }

        if (request.CategoryIds is not null)
        {
            bool categoriesBelongToAccount = await store.CategoryIdsBelongToAccountAsync(request.CategoryIds, accountId);
            if (!categoriesBelongToAccount)
                return BadRequest(new { message = "One or more categories are invalid for this account." });
        }

        decimal updatedAmount = request.Amount ?? existing.Amount;
        long updatedBudgetId = request.BudgetId ?? existing.BudgetId;

        TransactionResponse? updated = await store.PatchAsync(id, accountId, request);
        if (updated is null)
            return NotFound(new { message = $"Transaction '{id}' was not found." });

        if (existing.BudgetId == updatedBudgetId)
        {
            decimal delta = updatedAmount - existing.Amount;
            if (delta != 0)
                await budgetStore.UpdateBudgetAmount(existing.BudgetId, delta);
        }
        else
        {
            await budgetStore.UpdateBudgetAmount(existing.BudgetId, -existing.Amount);
            await budgetStore.UpdateBudgetAmount(updatedBudgetId, updatedAmount);
        }

        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteTransaction(int id)
    {
        long accountId = GetAccountId();
        TransactionResponse? existing = await store.GetByIdAndAccountAsync(id, accountId);
        if (existing is null)
            return NotFound(new { message = $"Transaction '{id}' was not found." });

        bool success = await store.DeleteAsync(id);
        if (success)
        {
            await budgetStore.UpdateBudgetAmount(existing.BudgetId, -existing.Amount);
            return NoContent();
        }
        else
        {
            return NotFound(new { message = $"Transaction '{id}' was not found." });
        }
    }
}
