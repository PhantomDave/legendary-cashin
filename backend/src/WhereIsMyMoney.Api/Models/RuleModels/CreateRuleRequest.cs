namespace WhereIsMyMoney.Api.Models.RuleModels;

public sealed class CreateRuleRequest
{
    public long AccountId { get; set; }
    public required string Name { get; set; }
    public MatchType MatchType { get; set; }
    public required string DescriptionPattern { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public long? BudgetId { get; set; }
    public int[]? DaysOfWeek { get; set; }
    public int? DayOfMonth { get; set; }
    public required int[] CategoryIds { get; set; }
}
