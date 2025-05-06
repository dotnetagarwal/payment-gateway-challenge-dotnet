namespace PaymentGateway.Api.Models.Responses
{
    //TODO Needs to use it in Exception Handling and validations
    public class ErrorResponse    {
        public string ErrorMessage { get; init; } = default!;
        public string StatusCode { get; init; } = default!;
        public string? Details { get; init; } 
    }
}
