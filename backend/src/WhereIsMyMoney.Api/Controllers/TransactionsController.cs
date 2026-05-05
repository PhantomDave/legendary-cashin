using Microsoft.AspNetCore.Mvc;
using WhereIsMyMoney.Api.Models.TransactionModels;
using WhereIsMyMoney.Api.Services;

namespace WhereIsMyMoney.Api.Controllers;

[ApiController]
[Route("transactions")]
public sealed class TransactionsController(TransactionStore store, BudgetStore budgetStore) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TransactionResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TransactionResponse>>> GetMyTransactions()
    {
        long accountId = GetAccountId();
        return Ok(await store.GetByAccountAsync(accountId));
    }

    [HttpGet("budget/{id:int}")]
    [ProducesResponseType<TransactionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionResponse>> GetByBudget(int id)
    {
        long accountId = GetAccountId();
        var transactions = await store.GetByBudgetAsync(id, accountId);
        return transactions is null || !transactions.Any()
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
