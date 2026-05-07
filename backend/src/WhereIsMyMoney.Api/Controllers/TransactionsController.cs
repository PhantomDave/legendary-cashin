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

    [HttpGet("budget/{id:long}/monthly-summary")]
    [ProducesResponseType<IReadOnlyList<MonthlySummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<MonthlySummaryResponse>>> GetMonthlySummary(long id)
    {
        long accountId = GetAccountId();

        bool budgetBelongsToAccount = await store.BudgetBelongsToAccountAsync(id, accountId);
        if (!budgetBelongsToAccount)
            return BadRequest(new { message = $"Budget '{id}' is invalid for this account." });

        return Ok(await store.GetMonthlySummaryAsync(accountId, id));
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
        PaginatedResponse<TransactionResponse> transactions = await store.GetByBudgetPaginatedAsync(id, accountId, request);
        return transactions.TotalCount == 0
            ? NotFound(new { message = $"Transactions for budget '{id}' were not found." })
            : Ok(transactions);
    }

    [HttpGet("month")]
    [ProducesResponseType<IReadOnlyList<TransactionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TransactionResponse>>> GetMonthTransactions(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        long accountId = GetAccountId();

        long? budgetId = await store.GetLatestBudgetIdForAccountAsync(accountId);
        if (!budgetId.HasValue)
            return NotFound(new { message = "No budget found for this account." });

        return await GetMonthTransactionsInternal(accountId, budgetId.Value, from, to);
    }

    [HttpGet("budget/{budgetId:long}/month")]
    [ProducesResponseType<IReadOnlyList<TransactionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TransactionResponse>>> GetMonthTransactionsByBudget(
        long budgetId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        long accountId = GetAccountId();
        return await GetMonthTransactionsInternal(accountId, budgetId, from, to);
    }

    private async Task<ActionResult<IReadOnlyList<TransactionResponse>>> GetMonthTransactionsInternal(
        long accountId,
        long budgetId,
        DateTime? from,
        DateTime? to)
    {
        bool budgetBelongsToAccount = await store.BudgetBelongsToAccountAsync(budgetId, accountId);
        if (!budgetBelongsToAccount)
            return BadRequest(new { message = $"Budget '{budgetId}' is invalid for this account." });

        DateTime now = DateTime.UtcNow;
        DateTime startOfCurrentMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime endOfCurrentMonth = startOfCurrentMonth.AddMonths(1).AddTicks(-1);

        DateTime normalizedFrom = NormalizeUtc(from) ?? startOfCurrentMonth;
        DateTime normalizedTo = NormalizeUtc(to) ?? endOfCurrentMonth;

        if (normalizedFrom > normalizedTo)
            return BadRequest(new { message = "'from' must be less than or equal to 'to'." });

        IReadOnlyList<TransactionResponse> transactions = await store.GetByBudgetAndDateRangeAsync(
            accountId,
            budgetId,
            normalizedFrom,
            normalizedTo);

        return Ok(transactions);
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

        TransactionResponse transaction = await store.CreateAsync(request);
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

    /// <summary>
    /// Creates a new recurring transaction schedule.
    /// </summary>
    [HttpPost("recurring")]
    [ProducesResponseType<RecurringTransactionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RecurringTransactionResponse>> CreateRecurringTransaction(
        [FromBody] CreateRecurringTransactionRequest request)
    {
        long accountId = GetAccountId();
        request.AccountId = accountId;

        RecurringTransactionStore recurringStore = HttpContext.RequestServices.GetRequiredService<RecurringTransactionStore>();
        RecurrenceEngine engine = HttpContext.RequestServices.GetRequiredService<RecurrenceEngine>();

        // Validate budget belongs to account
        bool budgetBelongsToAccount = await recurringStore.BudgetBelongsToAccountAsync(request.BudgetId, accountId);
        if (!budgetBelongsToAccount)
            return BadRequest(new { message = $"Budget '{request.BudgetId}' is invalid for this account." });

        // Validate categories belong to account
        if (request.CategoryIds.Count > 0)
        {
            bool categoriesBelongToAccount = await recurringStore.CategoriesBelongToAccountAsync(request.CategoryIds, accountId);
            if (!categoriesBelongToAccount)
                return BadRequest(new { message = "One or more categories are invalid for this account." });
        }

        // Validate date range
        if (request.EndDate.HasValue && request.EndDate <= request.StartDate)
            return BadRequest(new { message = "End date must be after start date." });

        // Validate recurrence pattern
        if (request.Interval < 1)
            return BadRequest(new { message = "Interval must be at least 1." });

        RecurringTransactionResponse recurring = await recurringStore.CreateAsync(request);

        // Preview next occurrences for client - preview only, not stored
        List<DateTime> previewList = engine.GetAllFutureOccurrences(
            new RecurringTransaction
            {
                Description = request.Description,
                Frequency = request.Frequency,
                Interval = request.Interval,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                MaxOccurrences = request.MaxOccurrences,
                DaysOfWeek = request.DaysOfWeek,
                DayOfMonth = request.DayOfMonth,
                LastGeneratedDate = null,
                GeneratedCount = 0,
                IsActive = true,
                Amount = request.Amount,
                CategoryIds = request.CategoryIds,
                AccountId = accountId,
                BudgetId = request.BudgetId,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                Id = 0
            },
            3);

        return CreatedAtAction(nameof(GetRecurringTransaction), new { id = recurring.Id }, recurring);
    }

    /// <summary>
    /// Gets a specific recurring transaction schedule.
    /// </summary>
    [HttpGet("recurring/{id:long}")]
    [ProducesResponseType<RecurringTransactionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecurringTransactionResponse>> GetRecurringTransaction(
        long id,
        RecurringTransactionStore recurringStore)
    {
        long accountId = GetAccountId();
        RecurringTransactionResponse? recurring = await recurringStore.GetByIdAndAccountAsync(id, accountId);

        if (recurring is null)
            return NotFound(new { message = $"Recurring transaction '{id}' not found." });

        return Ok(recurring);
    }

    /// <summary>
    /// Lists all recurring transaction schedules for the account.
    /// </summary>
    [HttpGet("recurring")]
    [ProducesResponseType<IReadOnlyList<RecurringTransactionResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RecurringTransactionResponse>>> GetRecurringTransactions(
        RecurringTransactionStore recurringStore)
    {
        long accountId = GetAccountId();
        IReadOnlyList<RecurringTransactionResponse> recurring = await recurringStore.GetAllByAccountAsync(accountId);

        return Ok(recurring);
    }

    /// <summary>
    /// Gets recurring transaction schedules for a specific budget (paginated).
    /// </summary>
    [HttpGet("recurring/budget/{budgetId:long}")]
    [ProducesResponseType<PaginatedResponse<RecurringTransactionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginatedResponse<RecurringTransactionResponse>>> GetRecurringTransactionsByBudget(
        long budgetId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 15)
    {
        long accountId = GetAccountId();
        RecurringTransactionStore recurringStore = HttpContext.RequestServices.GetRequiredService<RecurringTransactionStore>();

        // Validate budget belongs to account
        bool budgetBelongsToAccount = await recurringStore.BudgetBelongsToAccountAsync(budgetId, accountId);
        if (!budgetBelongsToAccount)
            return BadRequest(new { message = $"Budget '{budgetId}' is invalid for this account." });

        PaginationRequest request = new PaginationRequest { PageNumber = pageNumber, PageSize = pageSize };
        PaginatedResponse<RecurringTransactionResponse> recurring =
            await recurringStore.GetByBudgetPaginatedAsync(budgetId, accountId, request);

        return Ok(recurring);
    }

    /// <summary>
    /// Updates an existing recurring transaction schedule.
    /// </summary>
    [HttpPatch("recurring/{id:long}")]
    [ProducesResponseType<RecurringTransactionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecurringTransactionResponse>> UpdateRecurringTransaction(
        long id,
        UpdateRecurringTransactionRequest request,
        RecurringTransactionStore recurringStore)
    {
        long accountId = GetAccountId();

        RecurringTransactionResponse? existing = await recurringStore.GetByIdAndAccountAsync(id, accountId);
        if (existing is null)
            return NotFound(new { message = $"Recurring transaction '{id}' not found." });

        try
        {
            RecurringTransactionResponse updated = await recurringStore.UpdateAsync(id, accountId, request);
            return Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Recurring transaction '{id}' not found." });
        }
    }

    /// <summary>
    /// Deletes a recurring transaction schedule.
    /// </summary>
    [HttpDelete("recurring/{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRecurringTransaction(
        long id,
        RecurringTransactionStore recurringStore)
    {
        long accountId = GetAccountId();
        bool deleted = await recurringStore.DeleteAsync(id, accountId);

        if (!deleted)
            return NotFound(new { message = $"Recurring transaction '{id}' not found." });

        return NoContent();
    }

    /// <summary>
    /// Gets a preview of the next N occurrences for a recurring transaction.
    /// </summary>
    [HttpPost("recurring/preview")]
    [ProducesResponseType<List<DateTime>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<List<DateTime>> PreviewRecurringOccurrences(
        CreateRecurringTransactionRequest request,
        RecurrenceEngine engine)
    {
        try
        {
            RecurringTransaction tempRecurring = new RecurringTransaction
            {
                Frequency = request.Frequency,
                Description = request.Description,
                Interval = request.Interval,
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                MaxOccurrences = request.MaxOccurrences,
                DaysOfWeek = request.DaysOfWeek,
                DayOfMonth = request.DayOfMonth,
                LastGeneratedDate = null,
                GeneratedCount = 0,
                IsActive = true
            };

            List<DateTime> occurrences = engine.GetAllFutureOccurrences(tempRecurring, 12);
            return Ok(occurrences);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private static DateTime? NormalizeUtc(DateTime? value)
    {
        if (!value.HasValue)
            return null;

        DateTime dateTime = value.Value;
        if (dateTime.Kind == DateTimeKind.Unspecified)
            return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);

        return dateTime.ToUniversalTime();
    }
}
