namespace WhereIsMyMoney.Api.Models.TransactionModels;

public record MonthlySummaryResponse(
    int Year,
    int Month,
    decimal Income,
    decimal Expenses
);
