using static System.Runtime.InteropServices.JavaScript.JSType;

namespace PaymentGateway.Api
{
    public class Constants
    {
        public const string ApiKeyHeaderName = "X-Api-Key";
        public const string ApiKeyValue = "test123"; //TODO This should be moved to aws-secret
        public const string ContentType = "application/json";
        public const string PaymentsRequestUri = "payments";
    }

    public class ErrorMessages
    {
        public const string IncorrectCardNumberLength = "Card number must be between 14 and 19 digits.";
        public const string CardNumberMustBeNumeric = "Card number must be numeric.";
        public const string IncorrectExpiryMonth = "Expiry month must be between 1 and 12.";
        public const string NonSupportedCurrency = "Only USD, EUR, or GBP are supported.";
        public const string IncorrectCvvLength = "CVV must be 3 or 4 digits.";
        public const string InvalidCardExpiryDate = "Card expiry date must be in the future.";
    }
}
