using System.Text.Json.Serialization;

namespace WhereIsMyMoney.Api.Models.EnableBankingModels
{
    public sealed record Aspsp(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("country")] string Country);

    public sealed record GetAspspsResponse(
        [property: JsonPropertyName("aspsps")] List<AspspData> Aspsps);

    public sealed record AspspData(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("country")] string Country,
        [property: JsonPropertyName("logo")] string? Logo,
        [property: JsonPropertyName("bic")] string? Bic,
        [property: JsonPropertyName("maximum_consent_validity")] int MaximumConsentValidity);

    public sealed record StartBankAuthApiResponse(
        [property: JsonPropertyName("url")] string Url,
        [property: JsonPropertyName("authorization_id")] string AuthorizationId);

    public sealed record AuthorizeSessionApiResponse(
        [property: JsonPropertyName("session_id")] string SessionId,
        [property: JsonPropertyName("accounts")] List<SessionAccountData> Accounts,
        [property: JsonPropertyName("aspsp")] Aspsp Aspsp,
        [property: JsonPropertyName("access")] AccessData Access);

    public sealed record SessionAccountData(
        [property: JsonPropertyName("uid")] string? Uid,
        [property: JsonPropertyName("identification_hash")] string IdentificationHash);

    public sealed record AccessData(
        [property: JsonPropertyName("valid_until")] DateTime ValidUntil);

    // ── Transactions ──────────────────────────────────────────────────────────

    public sealed record EnableBankingTransactionAmount(
        [property: JsonPropertyName("currency")] string Currency,
        [property: JsonPropertyName("amount")] string Amount);

    public sealed record EnableBankingTransaction(
        [property: JsonPropertyName("transaction_id")] string? TransactionId,
        [property: JsonPropertyName("entry_reference")] string? EntryReference,
        [property: JsonPropertyName("transaction_amount")] EnableBankingTransactionAmount TransactionAmount,
        [property: JsonPropertyName("credit_debit_indicator")] string CreditDebitIndicator,
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("booking_date_time")] string? BookingDateTime,
        [property: JsonPropertyName("booking_date")] string? BookingDate,
        [property: JsonPropertyName("value_date_time")] string? ValueDateTime,
        [property: JsonPropertyName("value_date")] string? ValueDate,
        [property: JsonPropertyName("remittance_information")] List<string>? RemittanceInformation,
        [property: JsonPropertyName("creditor_name")] string? CreditorName,
        [property: JsonPropertyName("debtor_name")] string? DebtorName);

    public sealed record EnableBankingHalTransactions(
        [property: JsonPropertyName("transactions")] List<EnableBankingTransaction> Transactions,
        [property: JsonPropertyName("continuation_key")] string? ContinuationKey);
}
