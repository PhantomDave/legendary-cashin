using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WhereIsMyMoney.Api.Models.EnableBankingModels;

namespace WhereIsMyMoney.Api.Services;

// ── Result types ──────────────────────────────────────────────────────────────

public sealed record SessionImportResult(
    long SessionId,
    string AspspName,
    int Fetched,
    int Inserted,
    int Skipped);

public sealed record ImportResult(IReadOnlyList<SessionImportResult> Sessions)
{
    public int TotalFetched => Sessions.Sum(s => s.Fetched);
    public int TotalInserted => Sessions.Sum(s => s.Inserted);
    public int TotalSkipped => Sessions.Sum(s => s.Skipped);
}

// ── Importer ──────────────────────────────────────────────────────────────────

public class EnableBankingImporter(
    ILogger<EnableBankingImporter> logger,
    IServiceProvider serviceProvider) : BackgroundService
{
    private readonly ILogger<EnableBankingImporter> _logger = logger;
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EnableBankingImporter started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await StartImportRequest();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during scheduled Enable Banking import");
            }

            await WaitUntilNextExecutionAsync(stoppingToken);
        }

        _logger.LogInformation("EnableBankingImporter stopped.");
    }

    /// <summary>
    /// Fetches transactions from Enable Banking, saves new ones, and returns per-session stats.
    /// When <paramref name="sessionId"/> is provided only that session is processed;
    /// otherwise every active session across all accounts is processed.
    /// <paramref name="from"/> overrides the per-session <c>LastImportAtUtc</c> as the start date.
    /// Sessions whose <c>LastImportAtUtc</c> is null are skipped when <paramref name="from"/> is also null.
    /// </summary>
    public async Task<ImportResult> StartImportRequest(
        DateTime? from = null,
        long? sessionId = null)
    {
        using IServiceScope scope = _serviceProvider.CreateScope();
        EnableBankingStore ebStore = scope.ServiceProvider.GetRequiredService<EnableBankingStore>();
        TransactionStore txStore = scope.ServiceProvider.GetRequiredService<TransactionStore>();

        // Resolve which sessions to process
        IReadOnlyList<EnableBankingBankSession> sessions;
        if (sessionId.HasValue)
        {
            EnableBankingBankSession? single = await ebStore.GetBankSessionAsync(sessionId.Value);
            sessions = single is not null ? [single] : [];
        }
        else
        {
            sessions = await ebStore.GetAllBankSessionsAsync();
        }

        if (sessions.Count == 0)
        {
            _logger.LogInformation("No Enable Banking sessions found to import.");
            return new ImportResult([]);
        }

        DateOnly dateTo = DateOnly.FromDateTime(DateTime.UtcNow);
        List<SessionImportResult> results = [];

        foreach (EnableBankingBankSession session in sessions)
        {
            // ── Determine start date ────────────────────────────────────────
            DateOnly dateFrom;
            if (from.HasValue)
            {
                dateFrom = DateOnly.FromDateTime(from.Value);
            }
            else if (session.LastImportAtUtc.HasValue)
            {
                dateFrom = DateOnly.FromDateTime(session.LastImportAtUtc.Value);
            }
            else
            {
                _logger.LogInformation(
                    "Session {SessionId} has never been imported and no explicit 'from' was provided — skipping",
                    session.Id);
                continue;
            }

            _logger.LogInformation(
                "Importing session {SessionId} ({Aspsp}) — range {From} → {To}",
                session.Id, session.AspspName, dateFrom, dateTo);

            // ── Resolve integration ─────────────────────────────────────────
            EnableBankingIntegration? integration =
                await ebStore.GetIntegrationById(session.AccountId, session.IntegrationId);

            if (integration is null)
            {
                _logger.LogWarning(
                    "Integration {IntegrationId} not found for session {SessionId} — skipping",
                    session.IntegrationId, session.Id);
                continue;
            }

            // ── Resolve default budget ──────────────────────────────────────
            long? budgetId = await txStore.GetLatestBudgetIdForAccountAsync(session.AccountId);
            if (budgetId is null)
            {
                _logger.LogWarning(
                    "Account {AccountId} has no budgets — skipping session {SessionId}",
                    session.AccountId, session.Id);
                continue;
            }

            // ── Fetch transactions from every linked account ─────────────────
            List<string> accountUids =
                JsonSerializer.Deserialize<List<string>>(session.AccountsJson) ?? [];

            List<ImportedTransaction> fetched = [];

            foreach (string uid in accountUids)
            {
                try
                {
                    EnableBankingHalTransactions result =
                        await integration.GetTransactionsAsync(uid, dateFrom, dateTo);

                    _logger.LogInformation(
                        "Fetched {Count} transaction(s) for account {Uid} (session {SessionId})",
                        result.Transactions.Count, uid, session.Id);

                    fetched.AddRange(result.Transactions.Select(t => new ImportedTransaction(
                        AccountUid: uid,
                        SessionId: session.Id,
                        IntegrationId: session.IntegrationId,
                        OwnerAccountId: session.AccountId,
                        TransactionId: t.TransactionId,
                        EntryReference: t.EntryReference,
                        Amount: t.TransactionAmount.Amount,
                        Currency: t.TransactionAmount.Currency,
                        CreditDebitIndicator: t.CreditDebitIndicator,
                        Status: t.Status,
                        BookingDate: t.BookingDate,
                        ValueDate: t.ValueDate,
                        Description: t.RemittanceInformation?.FirstOrDefault()
                                     ?? t.CreditorName
                                     ?? t.DebtorName,
                        CreditorName: t.CreditorName,
                        DebtorName: t.DebtorName)));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Failed to fetch transactions for account {Uid} in session {SessionId}",
                        uid, session.Id);
                }
            }

            // ── Save with dedup ─────────────────────────────────────────────
            (int inserted, int skipped) = await txStore.ImportFromEnableBankingAsync(fetched, budgetId.Value);

            _logger.LogInformation(
                "Session {SessionId}: {Fetched} fetched, {Inserted} inserted, {Skipped} skipped",
                session.Id, fetched.Count, inserted, skipped);

            // ── Stamp the session ───────────────────────────────────────────
            await ebStore.UpdateLastImportAtUtcAsync(session.Id, DateTime.UtcNow);

            results.Add(new SessionImportResult(
                SessionId: session.Id,
                AspspName: session.AspspName,
                Fetched: fetched.Count,
                Inserted: inserted,
                Skipped: skipped));
        }

        ImportResult summary = new(results);
        _logger.LogInformation(
            "Enable Banking import complete — {TotalFetched} fetched, {TotalInserted} inserted, {TotalSkipped} skipped",
            summary.TotalFetched, summary.TotalInserted, summary.TotalSkipped);

        return summary;
    }

    private async Task WaitUntilNextExecutionAsync(CancellationToken cancellationToken)
    {
        DateTime now = DateTime.UtcNow;
        // Run daily at approximately 2 AM UTC
        DateTime next2Am = now.Date.AddHours(2);
        if (next2Am <= now)
            next2Am = next2Am.AddDays(1);

        TimeSpan delay = next2Am - now;
        _logger.LogInformation("Next Enable Banking import in {Hours:F1} hours.", delay.TotalHours);

        await Task.Delay(delay, cancellationToken);
    }
}
