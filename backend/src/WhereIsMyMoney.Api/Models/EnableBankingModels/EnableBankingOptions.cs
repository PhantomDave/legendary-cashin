namespace WhereIsMyMoney.Api.Models.EnableBankingModels;

public sealed class EnableBankingOptions
{
    public string RedirectUrl { get; set; } = string.Empty;
    public string PsuType { get; set; } = "personal";
    public int CallbackTimeout { get; set; } = 300;
    public int MaxConsentValidity { get; set; } = 90;
}

