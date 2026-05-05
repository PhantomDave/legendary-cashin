namespace WhereIsMyMoney.Api.Models.TransactionModels;

public record TransactionMetricsResponse(
    decimal YearToDate,
    decimal? YearToDateTrend,
    decimal MonthToDate,
    decimal? MonthToDateTrend,
    decimal PredictedEndOfMonth,
    decimal? PredictedEndOfMonthTrend,
    decimal Balance30DaysAgo,
    decimal? Balance30DaysAgoTrend
);
