namespace WhereIsMyMoney.Api.Models.EnableBankingModels;

public record CreateEnableBankingRequest
{
    public required string ApplicationId { get; init; }
    public required string Certificate { get; init; }
    public string? Asps { get; init; } // Comma-separated list of ASPS (e.g., "EXAMPLE_ASPS_ID_1,EXAMPLE_ASPS_ID_2")
}
