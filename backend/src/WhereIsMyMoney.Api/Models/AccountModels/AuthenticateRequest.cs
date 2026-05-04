using System.ComponentModel.DataAnnotations;

namespace WhereIsMyMoney.Api.Models.AccountModels;

public sealed class AuthenticateRequest : IValidatableObject
{
    [EmailAddress]
    public string? Email { get; init; }

    [StringLength(100, MinimumLength = 1)]
    public string? Username { get; init; }

    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Password { get; init; } = null!;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        bool hasEmail = !string.IsNullOrWhiteSpace(Email);
        bool hasUsername = !string.IsNullOrWhiteSpace(Username);

        if (hasEmail == hasUsername)
        {
            yield return new ValidationResult(
                "Provide either username or email, but not both.",
                [nameof(Email), nameof(Username)]);
        }
    }
}
