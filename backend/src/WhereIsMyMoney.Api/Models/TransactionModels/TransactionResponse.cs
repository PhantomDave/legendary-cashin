namespace WhereIsMyMoney.Api.Models.TransactionModels;

public record TransactionResponse(
    int Id,
    long AccountId,
    string Description,
    decimal Amount,
    DateTime Date,
    List<int> CategoryIds
);
