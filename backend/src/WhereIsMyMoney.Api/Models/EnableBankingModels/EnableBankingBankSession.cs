namespace WhereIsMyMoney.Api.Models.EnableBankingModels;

public class EnableBankingBankSession
{
    public long Id { get; set; }
    public long IntegrationId { get; set; }
    public long AccountId { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public string AspspName { get; set; } = string.Empty;
    public string AspspCountry { get; set; } = string.Empty;
    public DateTime ValidUntil { get; set; }
    public string AccountsJson { get; set; } = "[]"; // JSON array of account UIDs
    public DateTime CreatedAtUtc { get; set; }
}
