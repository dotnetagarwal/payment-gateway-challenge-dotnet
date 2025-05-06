namespace PaymentGateway.Api.Exceptions
{
    public class ServiceUnavailableException : Exception
    {
        public ServiceUnavailableException() : base(ErrorMessages.BankCurrentlyUnavailable) { }
    }
}
