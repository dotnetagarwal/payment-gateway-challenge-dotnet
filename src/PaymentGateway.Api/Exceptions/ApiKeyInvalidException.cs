namespace PaymentGateway.Api.Exceptions
{
    public class ApiKeyMissingException : Exception
    {
        public ApiKeyMissingException() : base("API Key was not provided.") { }
    }

    public class ApiKeyInvalidException : Exception
    {
        public ApiKeyInvalidException() : base("Unauthorized client.") { }
    }
}
