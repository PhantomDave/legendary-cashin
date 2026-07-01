namespace WhereIsMyMoney.Api.Models.RuleModels;

public sealed class Rule
{
    public long Id { get; set; }
    public long AccountId { get; set; }
    public required string Name { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; } = true;
    public MatchType MatchType { get; set; }
    public required string DescriptionPattern { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public long? BudgetId { get; set; }
    public int[]? DaysOfWeek { get; set; }
    public int? DayOfMonth { get; set; }
    public int[] CategoryIds { get; set; } = [];
}

public enum MatchType
{
    Exact,
    Partial,
    Regex,
}
