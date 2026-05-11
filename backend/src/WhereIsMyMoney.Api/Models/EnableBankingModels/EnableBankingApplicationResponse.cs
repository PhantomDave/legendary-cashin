using System.Text.Json.Serialization;

namespace WhereIsMyMoney.Api.Models.EnableBankingModels
{
    public sealed record EnableBankingApplicationResponse(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("description")] string? Description,
        [property: JsonPropertyName("redirect_urls")] List<string> RedirectUrls);
}
