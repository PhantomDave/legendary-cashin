using Microsoft.EntityFrameworkCore;
using WhereIsMyMoney.Api.Data;
using WhereIsMyMoney.Api.Models;
using WhereIsMyMoney.Api.Models.CategoryModels;
using WhereIsMyMoney.Api.Models.RuleModels;
using WhereIsMyMoney.Api.Models.TransactionModels;
using RuleMatchType = WhereIsMyMoney.Api.Models.RuleModels.MatchType;

namespace WhereIsMyMoney.Api.Services;

public sealed class RuleStore(AppDbContext db, RuleEngine engine)
    : IStore<RuleResponse, CreateRuleRequest, UpdateRuleRequest>
{
    public async Task<RuleResponse?> GetAsync(long id)
    {
        Rule? rule = await db.Rules.FindAsync(id);
        return rule is null ? null : ToResponse(rule);
    }

    public async Task<RuleResponse?> GetByIdAndAccountAsync(long id, long accountId)
    {
        Rule? rule = await db.Rules.FirstOrDefaultAsync(r => r.Id == id && r.AccountId == accountId);
        return rule is null ? null : ToResponse(rule);
    }

    public async Task<IReadOnlyList<RuleResponse>> GetAllAsync()
    {
        return await db.Rules
            .OrderBy(r => r.Priority)
            .Select(r => ToResponse(r))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<RuleResponse>> GetAllByAccountId(long accountId)
    {
        return await db.Rules
            .Where(r => r.AccountId == accountId)
            .OrderBy(r => r.Priority)
            .Select(r => ToResponse(r))
            .ToListAsync();
    }

    public async Task<PaginatedResponse<RuleResponse>> GetAllByAccountIdPaginatedAsync(long accountId, PaginationRequest request)
    {
        return await db.Rules
            .Where(r => r.AccountId == accountId)
            .OrderBy(r => r.Priority)
            .Select(r => ToResponse(r))
            .ToPaginatedResponseAsync(request);
    }

    public async Task<IReadOnlyList<Rule>> GetActiveRulesByAccountIdAsync(long accountId)
    {
        return await db.Rules
            .Where(r => r.AccountId == accountId && r.IsActive)
            .OrderBy(r => r.Priority)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<RuleResponse>> GetActiveResponsesByAccountIdAsync(long accountId)
    {
        return await db.Rules
            .Where(r => r.AccountId == accountId && r.IsActive)
            .OrderBy(r => r.Priority)
            .Select(r => ToResponse(r))
            .ToListAsync();
    }

    public async Task<(bool Success, string? Error)> ValidateRuleAsync(CreateRuleRequest request)
    {
        if (request.MatchType == RuleMatchType.Regex && !RuleEngine.IsValidRegex(request.DescriptionPattern))
            return (false, "Regex pattern is invalid.");

        if (request.CategoryIds.Length == 0)
            return (false, "At least one category is required.");

        int distinctIds = request.CategoryIds.Distinct().Count();
        int existingCount = await db.Categories
            .Where(c => c.AccountId == request.AccountId && request.CategoryIds.Contains(c.Id))
            .CountAsync();

        if (existingCount != distinctIds)
            return (false, "One or more category IDs are invalid or do not belong to this account.");

        if (request.BudgetId.HasValue)
        {
            bool budgetExists = await db.Budgets
                .AnyAsync(b => b.Id == request.BudgetId.Value && b.AccountId == request.AccountId);
            if (!budgetExists)
                return (false, "Budget ID is invalid or does not belong to this account.");
        }

        return (true, null);
    }

    public async Task<RuleResponse> CreateAsync(CreateRuleRequest request)
    {
        int maxPriority = await db.Rules
            .Where(r => r.AccountId == request.AccountId)
            .Select(r => (int?)r.Priority)
            .MaxAsync() ?? 0;

        Rule rule = new Rule
        {
            AccountId = request.AccountId,
            Name = request.Name,
            Priority = maxPriority + 1,
            IsActive = true,
            MatchType = request.MatchType,
            DescriptionPattern = request.DescriptionPattern,
            MinAmount = request.MinAmount,
            MaxAmount = request.MaxAmount,
            BudgetId = request.BudgetId,
            DaysOfWeek = request.DaysOfWeek,
            DayOfMonth = request.DayOfMonth,
            CategoryIds = request.CategoryIds,
        };

        db.Rules.Add(rule);
        await db.SaveChangesAsync();
        return ToResponse(rule);
    }

    public async Task<bool> UpdateAsync(long id, UpdateRuleRequest request)
    {
        Rule? rule = await db.Rules.FindAsync(id);
        if (rule is null) return false;

        rule.Name = request.Name;
        rule.MatchType = request.MatchType;
        rule.DescriptionPattern = request.DescriptionPattern;
        rule.MinAmount = request.MinAmount;
        rule.MaxAmount = request.MaxAmount;
        rule.BudgetId = request.BudgetId;
        rule.DaysOfWeek = request.DaysOfWeek;
        rule.DayOfMonth = request.DayOfMonth;
        rule.CategoryIds = request.CategoryIds;
        rule.IsActive = request.IsActive;
        rule.Priority = request.Priority;

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> PatchAsync(long id, long accountId, PatchRuleRequest request)
    {
        Rule? rule = await db.Rules.FirstOrDefaultAsync(r => r.Id == id && r.AccountId == accountId);
        if (rule is null) return false;

        if (request.IsActive.HasValue) rule.IsActive = request.IsActive.Value;
        if (request.Priority.HasValue) rule.Priority = request.Priority.Value;
        if (request.Name is not null) rule.Name = request.Name;
        if (request.MatchType.HasValue) rule.MatchType = request.MatchType.Value;
        if (request.DescriptionPattern is not null) rule.DescriptionPattern = request.DescriptionPattern;
        if (request.MinAmount.HasValue) rule.MinAmount = request.MinAmount;
        if (request.MaxAmount.HasValue) rule.MaxAmount = request.MaxAmount;
        if (request.BudgetId.HasValue) rule.BudgetId = request.BudgetId;
        if (request.DaysOfWeek is not null) rule.DaysOfWeek = request.DaysOfWeek;
        if (request.DayOfMonth.HasValue) rule.DayOfMonth = request.DayOfMonth;
        if (request.CategoryIds is not null) rule.CategoryIds = request.CategoryIds;

        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        Rule? rule = await db.Rules.FindAsync(id);
        if (rule is null) return false;

        long accountId = rule.AccountId;
        db.Rules.Remove(rule);
        await db.SaveChangesAsync();

        // Renumber remaining priorities 1, 2, 3, …
        List<Rule> remaining = await db.Rules
            .Where(r => r.AccountId == accountId)
            .OrderBy(r => r.Priority)
            .ToListAsync();

        for (int i = 0; i < remaining.Count; i++)
            remaining[i].Priority = i + 1;

        await db.SaveChangesAsync();
        return true;
    }

    public async Task ReorderAsync(long accountId, long[] ruleIds)
    {
        List<Rule> rules = await db.Rules
            .Where(r => r.AccountId == accountId && ruleIds.Contains(r.Id))
            .ToListAsync();

        Dictionary<long, Rule> ruleMap = rules.ToDictionary(r => r.Id);

        for (int i = 0; i < ruleIds.Length; i++)
        {
            if (ruleMap.TryGetValue(ruleIds[i], out Rule? rule))
                rule.Priority = i + 1;
        }

        await db.SaveChangesAsync();
    }

    public async Task<int[]> GetMatchedCategoryIdsAsync(Transaction tx, long accountId)
    {
        IReadOnlyList<Rule> activeRules = await GetActiveRulesByAccountIdAsync(accountId);
        return GetMatchedCategoryIds(activeRules, tx);
    }

    public int[] GetMatchedCategoryIds(IReadOnlyList<Rule> activeRules, Transaction tx)
    {
        HashSet<int> matched = [];
        foreach (Rule rule in activeRules)
        {
            if (engine.Evaluate(rule, tx))
            {
                foreach (int catId in rule.CategoryIds)
                    matched.Add(catId);
            }
        }
        return [.. matched];
    }

    public async Task<int> ApplyRulesToHistoricalAsync(long accountId, DateTime fromDate, DateTime toDate, bool overwrite)
    {
        IReadOnlyList<Rule> activeRules = await GetActiveRulesByAccountIdAsync(accountId);
        if (activeRules.Count == 0) return 0;

        List<Transaction> transactions = await db.Transactions
            .Include(t => t.Categories)
            .Where(t => t.AccountId == accountId && t.Date >= fromDate && t.Date <= toDate)
            .ToListAsync();

        int updated = 0;
        int batchSize = 0;

        foreach (Transaction tx in transactions)
        {
            int[] matchedCategoryIds = GetMatchedCategoryIds(activeRules, tx);
            if (matchedCategoryIds.Length == 0) continue;

            List<int> targetIds;
            if (overwrite)
            {
                targetIds = [.. matchedCategoryIds];
            }
            else
            {
                HashSet<int> existing = tx.Categories.Select(c => c.Id).ToHashSet();
                targetIds = matchedCategoryIds.Where(id => !existing.Contains(id)).Concat(existing).ToList();
            }

            List<Category> categories = await db.Categories
                .Where(c => c.AccountId == accountId && targetIds.Contains(c.Id))
                .ToListAsync();

            tx.Categories = categories;
            updated++;
            batchSize++;

            if (batchSize >= 100)
            {
                await db.SaveChangesAsync();
                batchSize = 0;
            }
        }

        if (batchSize > 0)
            await db.SaveChangesAsync();

        return updated;
    }

    public async Task<int> CountHistoricalMatchAsync(long accountId, DateTime fromDate, DateTime toDate)
    {
        IReadOnlyList<Rule> activeRules = await GetActiveRulesByAccountIdAsync(accountId);
        if (activeRules.Count == 0) return 0;

        List<Transaction> transactions = await db.Transactions
            .Where(t => t.AccountId == accountId && t.Date >= fromDate && t.Date <= toDate)
            .ToListAsync();

        return transactions.Count(tx => GetMatchedCategoryIds(activeRules, tx).Length > 0);
    }

    public async Task<PaginatedResponse<TransactionResponse>> PreviewRuleAsync(long ruleId, long accountId, PaginationRequest request)
    {
        Rule? rule = await db.Rules.FirstOrDefaultAsync(r => r.Id == ruleId && r.AccountId == accountId);
        if (rule is null)
            return new PaginatedResponse<TransactionResponse>([], request.PageNumber, request.PageSize, 0, 0);

        // For exact and partial, push filter to DB; for regex, load all and filter in-memory.
        IQueryable<Transaction> query = db.Transactions
            .Include(t => t.Categories)
            .Where(t => t.AccountId == accountId);

        if (rule.BudgetId.HasValue)
            query = query.Where(t => t.BudgetId == rule.BudgetId.Value);

        if (rule.MinAmount.HasValue)
            query = query.Where(t => t.Amount >= rule.MinAmount.Value);

        if (rule.MaxAmount.HasValue)
            query = query.Where(t => t.Amount <= rule.MaxAmount.Value);

        if (rule.DayOfMonth.HasValue)
            query = query.Where(t => t.Date.Day == rule.DayOfMonth.Value);

        if (rule.MatchType == RuleMatchType.Exact)
            query = query.Where(t => t.Description.ToLower() == rule.DescriptionPattern.ToLower());
        else if (rule.MatchType == RuleMatchType.Partial)
            query = query.Where(t => t.Description.ToLower().Contains(rule.DescriptionPattern.ToLower()));

        List<Transaction> candidates = await query.OrderByDescending(t => t.Date).ThenByDescending(t => t.Id).ToListAsync();

        // Apply remaining in-memory filters (regex, DaysOfWeek)
        List<Transaction> matched = candidates
            .Where(tx => engine.Evaluate(rule, tx))
            .ToList();

        int total = matched.Count;
        int totalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)request.PageSize);
        List<TransactionResponse> page = matched
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(TransactionStore.ToResponse)
            .ToList();

        return new PaginatedResponse<TransactionResponse>(page, request.PageNumber, request.PageSize, total, totalPages);
    }

    internal static RuleResponse ToResponse(Rule rule) =>
        new(rule.Id, rule.AccountId, rule.Name, rule.Priority, rule.IsActive,
            rule.MatchType, rule.DescriptionPattern, rule.MinAmount, rule.MaxAmount,
            rule.BudgetId, rule.DaysOfWeek, rule.DayOfMonth, rule.CategoryIds);
}
