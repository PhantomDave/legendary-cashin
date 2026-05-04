using System.ComponentModel.DataAnnotations;

namespace WhereIsMyMoney.Api;

public sealed class CreateCashinRequest
{
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "Amount must be greater than zero.")]
    public decimal Amount { get; init; }

    [CurrencyCode]
    public string Currency { get; init; } = string.Empty;

    [CashinReference]
    public string Reference { get; init; } = string.Empty;
}

