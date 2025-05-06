using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Repositories;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Services
{
    public interface IPaymentsService
    {
        Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest request);
        Task<PaymentResponse?> GetPaymentAsync(Guid id);
    }

    public class PaymentsService : IPaymentsService
    {
        private readonly IBankClient _bankClient;
        private readonly IPaymentsRepository _repository;

        public PaymentsService(
            IBankClient bankClient, IPaymentsRepository repository)
        {
            _bankClient = bankClient;
            _repository = repository;
        }

        public async Task<PaymentResponse> ProcessPaymentAsync(PaymentRequest request)
        {
            // Mask card number
            var lastFour = request.CardNumber[^4..];

            //Prepare bank request
            var bankRequest = new BankRequest(request);

            var bankResponse = await _bankClient.PostPaymentAsync(bankRequest);
            if (bankResponse == null)
            {
                // network error or empty response
                return CreateDeclinedPaymentResponse(request, lastFour);
            }

            var paymentResponse = CreatePaymentResponse(request, lastFour, bankResponse);
            await _repository.AddAsync(paymentResponse);
            return paymentResponse;
        }

        public async Task<PaymentResponse?> GetPaymentAsync(Guid id) => await _repository.GetAsync(id);

        private static PaymentResponse CreateDeclinedPaymentResponse(PaymentRequest request, string lastFour)
        {
            return new PaymentResponse
            {
                Id = Guid.NewGuid(),
                Status = PaymentStatus.Declined,
                CardNumberLastFour = lastFour,
                ExpiryMonth = request.ExpiryMonth,
                ExpiryYear = request.ExpiryYear,
                Currency = request.Currency,
                Amount = request.Amount
            };
        }

        private static PaymentResponse CreatePaymentResponse(PaymentRequest request, string lastFour, BankResponse bankResponse)
        {
            return new PaymentResponse
            {
                Id = Guid.NewGuid(),
                Status = bankResponse.IsAuthorized ? PaymentStatus.Authorized : PaymentStatus.Declined,
                CardNumberLastFour = lastFour,
                ExpiryMonth = request.ExpiryMonth,
                ExpiryYear = request.ExpiryYear,
                Currency = request.Currency,
                Amount = request.Amount
            };
        }
    }
}
