using Microsoft.EntityFrameworkCore;
using WhereIsMyMoney.Api.Data;
using WhereIsMyMoney.Api.Models.TransactionModels;

namespace WhereIsMyMoney.Api.Services;

/// <summary>
/// Background service that processes due recurring transactions.
/// Runs daily to generate new transaction entries from recurring schedules.
/// </summary>
public class ScheduledTransactionProcessor : BackgroundService
{
    private readonly ILogger<ScheduledTransactionProcessor> _logger;
    private readonly IServiceProvider _serviceProvider;

    public ScheduledTransactionProcessor(
        ILogger<ScheduledTransactionProcessor> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ScheduledTransactionProcessor started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueTransactionsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing scheduled transactions");
            }

            // Run daily at approximately 2 AM UTC
            await WaitUntilNextExecutionAsync(stoppingToken);
        }

        _logger.LogInformation("ScheduledTransactionProcessor stopped.");
    }

    private async Task ProcessDueTransactionsAsync()
    {
        using IServiceScope scope = _serviceProvider.CreateScope();

        AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        TransactionStore transactionStore = scope.ServiceProvider.GetRequiredService<TransactionStore>();
        RecurringTransactionStore recurringStore = scope.ServiceProvider.GetRequiredService<RecurringTransactionStore>();
        BudgetStore budgetStore = scope.ServiceProvider.GetRequiredService<BudgetStore>();
        RecurrenceEngine engine = scope.ServiceProvider.GetRequiredService<RecurrenceEngine>();

        List<RecurringTransaction> dueTransactions = (List<RecurringTransaction>)await recurringStore.GetDueTransactionsAsync(engine);

        if (dueTransactions.Count == 0)
        {
            _logger.LogInformation("No recurring transactions due today.");
            return;
        }

        _logger.LogInformation($"Processing {dueTransactions.Count} due recurring transactions.");

        foreach (RecurringTransaction recurring in dueTransactions)
        {
            try
            {
                // Create transaction from recurring template
                CreateTransactionRequest createRequest = new CreateTransactionRequest
                {
                    Description = recurring.Description,
                    Amount = recurring.Amount,
                    BudgetId = recurring.BudgetId,
                    AccountId = recurring.AccountId,
                    CategoryIds = recurring.CategoryIds.Cast<int>().ToList(),
                    Date = DateTime.UtcNow
                };

                TransactionResponse newTransaction = await transactionStore.CreateAsync(createRequest);

                // Update budget amount
                await budgetStore.UpdateBudgetAmount(recurring.BudgetId, recurring.Amount);

                // Update tracking
                await recurringStore.UpdateGenerationTrackingAsync(recurring.Id);

                _logger.LogInformation(
                    $"Generated transaction {newTransaction.Id} from recurring schedule {recurring.Id} " +
                    $"for account {recurring.AccountId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    $"Failed to generate transaction from recurring schedule {recurring.Id}");
            }
        }

        _logger.LogInformation("Completed processing recurring transactions.");
    }

    private async Task WaitUntilNextExecutionAsync(CancellationToken cancellationToken)
    {
        // Calculate next 2 AM UTC
        DateTime now = DateTime.UtcNow;
        DateTime tomorrow2Am = now.Date.AddDays(1).AddHours(2);

        if (now >= tomorrow2Am)
            tomorrow2Am = tomorrow2Am.AddDays(1);

        TimeSpan delay = tomorrow2Am - now;

        _logger.LogInformation($"Next scheduled transaction processing in {delay.TotalHours:F1} hours.");

        await Task.Delay(delay, cancellationToken);
    }
}
