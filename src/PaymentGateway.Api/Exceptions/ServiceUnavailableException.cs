namespace PaymentGateway.Api.Exceptions
{
    public class ServiceUnavailableException : Exception
    {
        public ServiceUnavailableException() : base("Acquiring bank is currently unavailable.") { }
    }
}
