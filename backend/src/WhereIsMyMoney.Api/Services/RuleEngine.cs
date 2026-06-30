using System.Text.RegularExpressions;
using WhereIsMyMoney.Api.Models.RuleModels;
using WhereIsMyMoney.Api.Models.TransactionModels;
using RuleMatchType = WhereIsMyMoney.Api.Models.RuleModels.MatchType;

namespace WhereIsMyMoney.Api.Services;

public sealed class RuleEngine
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);

    public bool Evaluate(Rule rule, Transaction tx)
    {
        if (!MatchesDescription(tx.Description, rule.DescriptionPattern, rule.MatchType))
            return false;

        if (!IsAmountInRange(tx.Amount, rule.MinAmount, rule.MaxAmount))
            return false;

        if (rule.BudgetId.HasValue && tx.BudgetId != rule.BudgetId.Value)
            return false;

        if (!IsDayOfWeekMatch(tx.Date, rule.DaysOfWeek))
            return false;

        if (!IsDayOfMonthMatch(tx.Date, rule.DayOfMonth))
            return false;

        return true;
    }

    internal static bool MatchesDescription(string description, string pattern, RuleMatchType matchType)
    {
        return matchType switch
        {
            RuleMatchType.Exact => string.Equals(description, pattern, StringComparison.OrdinalIgnoreCase),
            RuleMatchType.Partial => description.Contains(pattern, StringComparison.OrdinalIgnoreCase),
            RuleMatchType.Regex => MatchesRegex(description, pattern),
            _ => false,
        };
    }

    internal static bool IsAmountInRange(decimal amount, decimal? min, decimal? max)
    {
        if (min.HasValue && amount < min.Value)
            return false;
        if (max.HasValue && amount > max.Value)
            return false;
        return true;
    }

    internal static bool IsDayOfWeekMatch(DateTime date, int[]? daysOfWeek)
    {
        if (daysOfWeek is null || daysOfWeek.Length == 0)
            return true;
        return daysOfWeek.Contains((int)date.DayOfWeek);
    }

    internal static bool IsDayOfMonthMatch(DateTime date, int? dayOfMonth)
    {
        if (!dayOfMonth.HasValue)
            return true;
        return date.Day == dayOfMonth.Value;
    }

    internal static bool IsValidRegex(string pattern)
    {
        try
        {
            _ = new Regex(pattern, RegexOptions.None, RegexTimeout);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool MatchesRegex(string input, string pattern)
    {
        try
        {
            return Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase, RegexTimeout);
        }
        catch
        {
            return false;
        }
    }
}
