using System.ComponentModel.DataAnnotations;

namespace WhereIsMyMoney.Api
{
    public sealed class CreateAccountRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 1)]
        public string Username { get; init; } = null!;

        [Required]
        [EmailAddress]
        public string Email { get; init; } = null!;

        [Required]
        [StringLength(100, MinimumLength = 8)]
        public string Password { get; init; } = null!;
    }

}
