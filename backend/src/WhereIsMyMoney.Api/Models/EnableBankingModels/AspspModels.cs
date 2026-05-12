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
}
