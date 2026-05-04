using System.Text.Json.Serialization;

namespace WhereIsMyMoney.Import.Models;

// ── ASPSP ─────────────────────────────────────────────────────────────────────

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

// ── Authorization ─────────────────────────────────────────────────────────────

public sealed record Access(
    [property: JsonPropertyName("valid_until")] string ValidUntil,
    [property: JsonPropertyName("balances")] bool Balances = true,
    [property: JsonPropertyName("transactions")] bool Transactions = true);

public sealed record StartAuthorizationRequest(
    [property: JsonPropertyName("access")] Access Access,
    [property: JsonPropertyName("aspsp")] Aspsp Aspsp,
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("redirect_url")] string RedirectUrl,
    [property: JsonPropertyName("psu_type")] string PsuType);

public sealed record StartAuthorizationResponse(
    [property: JsonPropertyName("url")] string Url);

// ── Session ───────────────────────────────────────────────────────────────────

public sealed record AuthorizeSessionRequest(
    [property: JsonPropertyName("code")] string Code);

public sealed record AuthorizeSessionResponse(
    [property: JsonPropertyName("session_id")] string SessionId,
    [property: JsonPropertyName("accounts")] List<AccountResource> Accounts,
    [property: JsonPropertyName("aspsp")] Aspsp Aspsp);

// ── Account ───────────────────────────────────────────────────────────────────

public sealed record AccountIdentification(
    [property: JsonPropertyName("iban")] string? Iban,
    [property: JsonPropertyName("other")] GenericIdentification? Other);

public sealed record GenericIdentification(
    [property: JsonPropertyName("identification")] string? Identification,
    [property: JsonPropertyName("scheme_name")] string? SchemeName);

public sealed record AccountResource(
    [property: JsonPropertyName("uid")] string? Uid,
    [property: JsonPropertyName("account_id")] AccountIdentification? AccountId,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("cash_account_type")] string CashAccountType,
    [property: JsonPropertyName("identification_hash")] string IdentificationHash);

// ── Balance ───────────────────────────────────────────────────────────────────

public sealed record AmountType(
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("amount")] string Amount);

public sealed record BalanceResource(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("balance_amount")] AmountType BalanceAmount,
    [property: JsonPropertyName("balance_type")] string BalanceType);

public sealed record HalBalances(
    [property: JsonPropertyName("balances")] List<BalanceResource> Balances);

// ── Transactions ──────────────────────────────────────────────────────────────

public sealed record TransactionAmount(
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("amount")] string Amount);

public sealed record Transaction(
    [property: JsonPropertyName("transaction_id")] string? TransactionId,
    [property: JsonPropertyName("entry_reference")] string? EntryReference,
    [property: JsonPropertyName("transaction_amount")] TransactionAmount TransactionAmount,
    [property: JsonPropertyName("credit_debit_indicator")] string CreditDebitIndicator,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("booking_date")] string? BookingDate,
    [property: JsonPropertyName("value_date")] string? ValueDate,
    [property: JsonPropertyName("remittance_information")] List<string>? RemittanceInformation,
    [property: JsonPropertyName("creditor_name")] string? CreditorName,
    [property: JsonPropertyName("debtor_name")] string? DebtorName);

public sealed record HalTransactions(
    [property: JsonPropertyName("transactions")] List<Transaction> Transactions,
    [property: JsonPropertyName("continuation_key")] string? ContinuationKey);

// ── Application ───────────────────────────────────────────────────────────────

public sealed record ApplicationResponse(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("redirect_urls")] List<string> RedirectUrls);
