namespace PaymentGateway.Api
{
    public class Constants
    {
        public const string ApiKeyHeaderName = "X-Api-Key";
        public const string ApiKeyValue = "test123"; //TODO This should be moved to aws-secret
        public const string ContentType = "application/json";
        public const string PaymentsRequestUri = "payments";
    }
}
