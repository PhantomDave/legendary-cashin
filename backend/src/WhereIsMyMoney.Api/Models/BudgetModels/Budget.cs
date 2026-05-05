namespace WhereIsMyMoney.Api.Data;

public sealed class Budget
{
    public long Id { get; set; }
    public long AccountId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DefaultCurrency { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
