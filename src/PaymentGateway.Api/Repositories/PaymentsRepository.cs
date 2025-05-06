using System.Collections.Concurrent;

using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Repositories
{
    public interface IPaymentsRepository
    {
        Task AddAsync(PaymentResponse payment);

        Task<PaymentResponse?> GetAsync(Guid id);
    }

    public class PaymentsRepository : IPaymentsRepository
    {
        private readonly ConcurrentDictionary<Guid, PaymentResponse> _payments = new();

        public Task AddAsync(PaymentResponse payment)
        {
            _payments[payment.Id] = payment;
            return Task.CompletedTask;
        }

        public Task<PaymentResponse?> GetAsync(Guid id)
        {
            _payments.TryGetValue(id, out var payment);
            return Task.FromResult(payment);
        }
    }
}
