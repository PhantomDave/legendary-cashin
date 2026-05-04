using System.ComponentModel.DataAnnotations;

namespace WhereIsMyMoney.Api
{
    public sealed class CurrencyCodeAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not string currency || string.IsNullOrWhiteSpace(currency))
            {
                return new ValidationResult("Currency is required.");
            }

            return currency.Trim().Length is 3
                ? ValidationResult.Success
                : new ValidationResult("Currency must be a 3-letter ISO code.");
        }
    }

}
