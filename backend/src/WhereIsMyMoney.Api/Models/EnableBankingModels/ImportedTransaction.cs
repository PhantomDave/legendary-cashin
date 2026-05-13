namespace WhereIsMyMoney.Api.Models.EnableBankingModels;

/// <summary>
/// Raw transaction data fetched from Enable Banking.
/// Transformation and persistence are handled by the caller.
/// </summary>
public sealed record ImportedTransaction(
    string AccountUid,
    long SessionId,
    long IntegrationId,
    long OwnerAccountId,
    string? TransactionId,
    string? EntryReference,
    string Amount,
    string Currency,
    string CreditDebitIndicator,
    string Status,
    string? BookingDate,
    string? ValueDate,
    string? Description,
    string? CreditorName,
    string? DebtorName);
