using System.Collections.Concurrent;

namespace WhereIsMyMoney.Api;

public sealed class CashinStore
{
    private readonly ConcurrentDictionary<Guid, CashinResponse> cashins = new();

    public IReadOnlyCollection<CashinResponse> GetAll()
    {
        return cashins.Values
            .OrderByDescending(cashin => cashin.CreatedAtUtc)
            .ToArray();
    }

    public CashinResponse? Get(Guid id)
    {
        return cashins.TryGetValue(id, out var cashin) ? cashin : null;
    }

    public CashinResponse Create(CreateCashinRequest request)
    {
        var cashin = new CashinResponse(
            Guid.NewGuid(),
            request.Amount,
            request.Currency.Trim().ToUpperInvariant(),
            request.Reference.Trim(),
            "Received",
            DateTimeOffset.UtcNow);

        cashins[cashin.Id] = cashin;

        return cashin;
    }
}

