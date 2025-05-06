namespace PaymentGateway.Api.Exceptions
{
    public class ApiKeyMissingException : Exception
    {
        public ApiKeyMissingException() : base(ErrorMessages.ApiKeyNotProvided) { }
    }

    public class ApiKeyInvalidException : Exception
    {
        public ApiKeyInvalidException() : base(ErrorMessages.UnauthorizedClient) { }
    }
}
