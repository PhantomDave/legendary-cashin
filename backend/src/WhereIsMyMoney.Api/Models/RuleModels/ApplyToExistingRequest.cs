namespace WhereIsMyMoney.Api.Models.RuleModels;

public sealed record ApplyToExistingRequest(DateTime FromDate, DateTime ToDate, bool OverwriteExisting = false);
