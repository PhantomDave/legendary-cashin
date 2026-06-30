namespace WhereIsMyMoney.Api.Models.RuleModels;

public record RuleResponse(
    long Id,
    long AccountId,
    string Name,
    int Priority,
    bool IsActive,
    MatchType MatchType,
    string DescriptionPattern,
    decimal? MinAmount,
    decimal? MaxAmount,
    long? BudgetId,
    int[]? DaysOfWeek,
    int? DayOfMonth,
    int[] CategoryIds
);
