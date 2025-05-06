using System.ComponentModel.DataAnnotations;

namespace PaymentGateway.Api.Models.Requests;

public class PaymentRequest
{
    [Required]
    [StringLength(19, MinimumLength = 14, ErrorMessage = ErrorMessages.IncorrectCardNumberLength)]
    [RegularExpression(@"^\d+$", ErrorMessage = ErrorMessages.CardNumberMustBeNumeric)]
    public required string CardNumber { get; init; }

    [Required, Range(1, 12, ErrorMessage = ErrorMessages.IncorrectExpiryMonth)]
    public int ExpiryMonth { get; set; }

    [Required]
    public int ExpiryYear { get; set; }

    [Required]
    [RegularExpression(@"(?i)^(USD|EUR|GBP)$", ErrorMessage = ErrorMessages.NonSupportedCurrency)]
    public required string Currency { get; set; }

    [Required, Range(1, int.MaxValue)]
    public int Amount { get; set; }

    [Required]
    [Range(100, 9999, ErrorMessage = ErrorMessages.IncorrectCvvLength)]
    public int Cvv { get; set; }
}