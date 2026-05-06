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
}
