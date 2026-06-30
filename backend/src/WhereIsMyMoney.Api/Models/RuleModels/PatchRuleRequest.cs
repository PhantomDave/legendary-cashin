namespace WhereIsMyMoney.Api.Models.RuleModels;

public sealed class PatchRuleRequest
{
    public bool? IsActive { get; set; }
    public int? Priority { get; set; }
    public string? Name { get; set; }
    public MatchType? MatchType { get; set; }
    public string? DescriptionPattern { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public long? BudgetId { get; set; }
    public int[]? DaysOfWeek { get; set; }
    public int? DayOfMonth { get; set; }
    public int[]? CategoryIds { get; set; }
}
