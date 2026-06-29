using System.Collections.Concurrent;
using System.Threading.Channels;
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

public sealed record ImportJobStatus(
    Guid JobId,
    long AccountId,
    string Trigger,
    long? SessionId,
    DateTime? From,
    DateTime? To,
    string State,
    DateTime CreatedAtUtc,
    DateTime? StartedAtUtc,
    DateTime? CompletedAtUtc,
    ImportResult? Result,
    string? Error);

internal sealed record ImportJob(
    Guid JobId,
    long AccountId,
    string Trigger,
    long? SessionId,
    DateTime? From,
    DateTime? To,
    DateTime CreatedAtUtc);

// ── Importer ──────────────────────────────────────────────────────────────────

public class EnableBankingImporter(
    ILogger<EnableBankingImporter> logger,
    IServiceProvider serviceProvider) : BackgroundService
{
    private readonly ILogger<EnableBankingImporter> _logger = logger;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly Channel<ImportJob> _importQueue = Channel.CreateUnbounded<ImportJob>();
    private readonly ConcurrentDictionary<Guid, ImportJobStatus> _jobStatuses = new();

    public Guid EnqueueImportRequest(
        DateTime? from,
        DateTime? to,
        long? sessionId,
        long accountId,
        string trigger)
    {
        Guid jobId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;
        ImportJob job = new(jobId, accountId, trigger, sessionId, from, to, now);

        _jobStatuses[jobId] = new ImportJobStatus(
            JobId: jobId,
            AccountId: accountId,
            Trigger: trigger,
            SessionId: sessionId,
            From: from,
            To: to,
            State: "Queued",
            CreatedAtUtc: now,
            StartedAtUtc: null,
            CompletedAtUtc: null,
            Result: null,
            Error: null);

        if (!_importQueue.Writer.TryWrite(job))
        {
            _jobStatuses[jobId] = _jobStatuses[jobId] with
            {
                State = "Failed",
                CompletedAtUtc = DateTime.UtcNow,
                Error = "Failed to enqueue import job."
            };
        }

        return jobId;
    }

    public ImportJobStatus? GetImportJobStatus(Guid jobId)
    {
        return _jobStatuses.TryGetValue(jobId, out ImportJobStatus? status) ? status : null;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EnableBankingImporter started.");

        DateTime nextScheduledRun = GetNextScheduledRunUtc(DateTime.UtcNow);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                TimeSpan delayUntilScheduledRun = nextScheduledRun - DateTime.UtcNow;
                if (delayUntilScheduledRun < TimeSpan.Zero)
                    delayUntilScheduledRun = TimeSpan.Zero;

                Task<bool> waitForQueue = _importQueue.Reader.WaitToReadAsync(stoppingToken).AsTask();
                Task waitForSchedule = Task.Delay(delayUntilScheduledRun, stoppingToken);

                Task completed = await Task.WhenAny(waitForQueue, waitForSchedule);

                if (completed == waitForQueue && await waitForQueue)
                {
                    while (_importQueue.Reader.TryRead(out ImportJob? queuedJob))
                    {
                        await ProcessQueuedJobAsync(queuedJob);
                    }

                    continue;
                }

                await StartImportRequest();
                nextScheduledRun = GetNextScheduledRunUtc(DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Enable Banking import processing");
            }
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
        long? sessionId = null,
        DateTime? to = null)
    {
        if (from.HasValue && to.HasValue && from.Value > to.Value)
            throw new ArgumentException("'from' cannot be after 'to'.", nameof(from));

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

        DateOnly dateTo = DateOnly.FromDateTime((to ?? DateTime.UtcNow));
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

            if (dateFrom > dateTo)
            {
                _logger.LogWarning(
                    "Skipping session {SessionId} because date range is invalid ({From} > {To})",
                    session.Id,
                    dateFrom,
                    dateTo);
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
                    string? continuationKey = null;
                    HashSet<string> seenContinuationKeys = [];
                    int pageNumber = 0;

                    while (true)
                    {
                        pageNumber++;

                        EnableBankingHalTransactions page =
                            await integration.GetTransactionsAsync(uid, dateFrom, dateTo, continuationKey);

                        _logger.LogInformation(
                            "Fetched page {PageNumber} with {Count} transaction(s) for account {Uid} (session {SessionId})",
                            pageNumber, page.Transactions.Count, uid, session.Id);

                        fetched.AddRange(page.Transactions.Select(t => new ImportedTransaction(
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
                            BookingDateTime: t.BookingDateTime,
                            BookingDate: t.BookingDate,
                            ValueDateTime: t.ValueDateTime,
                            ValueDate: t.ValueDate,
                            Description: t.RemittanceInformation?.FirstOrDefault()
                                         ?? t.CreditorName
                                         ?? t.DebtorName,
                            CreditorName: t.CreditorName,
                            DebtorName: t.DebtorName)));

                        if (string.IsNullOrWhiteSpace(page.ContinuationKey))
                            break;

                        if (!seenContinuationKeys.Add(page.ContinuationKey))
                        {
                            _logger.LogWarning(
                                "Stopping pagination for account {Uid} in session {SessionId} because continuation key repeated.",
                                uid, session.Id);
                            break;
                        }

                        continuationKey = page.ContinuationKey;
                    }
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

    private async Task ProcessQueuedJobAsync(ImportJob job)
    {
        _jobStatuses[job.JobId] = _jobStatuses[job.JobId] with
        {
            State = "Running",
            StartedAtUtc = DateTime.UtcNow
        };

        try
        {
            ImportResult result = await StartImportRequest(job.From, job.SessionId, job.To);

            _jobStatuses[job.JobId] = _jobStatuses[job.JobId] with
            {
                State = "Completed",
                CompletedAtUtc = DateTime.UtcNow,
                Result = result,
                Error = null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Queued import job {JobId} failed", job.JobId);

            _jobStatuses[job.JobId] = _jobStatuses[job.JobId] with
            {
                State = "Failed",
                CompletedAtUtc = DateTime.UtcNow,
                Error = ex.Message
            };
        }
    }

    private static DateTime GetNextScheduledRunUtc(DateTime now)
    {
        // Run daily at approximately 2 AM UTC
        DateTime next2Am = now.Date.AddHours(2);
        if (next2Am <= now)
            next2Am = next2Am.AddDays(1);

        return next2Am;
    }
}
