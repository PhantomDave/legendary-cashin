using System.ComponentModel.DataAnnotations;

namespace WhereIsMyMoney.Api
{
    public sealed class CashinReferenceAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is not string reference || string.IsNullOrWhiteSpace(reference))
            {
                return new ValidationResult("Reference is required.");
            }

            return reference.Trim().Length <= 100
                ? ValidationResult.Success
                : new ValidationResult("Reference must be 100 characters or fewer.");
        }
    }

}
