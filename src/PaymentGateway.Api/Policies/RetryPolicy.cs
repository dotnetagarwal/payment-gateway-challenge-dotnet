using Polly.Extensions.Http;
using Polly;

namespace PaymentGateway.Api.Policies
{
    public static class RetryPolicy
    {
       public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))
                );
        }
    }
}
