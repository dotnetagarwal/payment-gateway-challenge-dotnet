using System.ComponentModel.DataAnnotations;

namespace PaymentGateway.Api.Models.Requests;

public class PaymentRequest
{
    [Required]
    [StringLength(19, MinimumLength = 14, ErrorMessage = "Card number must be between 14 and 19 digits.")]
    [RegularExpression(@"^\d+$", ErrorMessage = "Card number must be numeric.")]
    public required string CardNumber { get; init; }

    [Required, Range(1, 12, ErrorMessage = "Expiry month must be between 1 and 12.")]
    public int ExpiryMonth { get; set; }

    [Required]
    public int ExpiryYear { get; set; }

    [Required]
    [RegularExpression(@"(?i)^(USD|EUR|GBP)$", ErrorMessage = "Only USD, EUR, or GBP are supported.")]
    public required string Currency { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int Amount { get; set; }

    [Required]
    [Range(100, 9999, ErrorMessage = "CVV must be 3 or 4 digits.")]
    public int Cvv { get; set; }
}