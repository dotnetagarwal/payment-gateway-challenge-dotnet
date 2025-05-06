using PaymentGateway.Api.Exceptions;

namespace PaymentGateway.Api.Middlewares
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;

        public ApiKeyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.Headers.TryGetValue(Constants.ApiKeyHeaderName, out var extractedApiKey))
                throw new ApiKeyMissingException();

            if (!string.Equals(extractedApiKey, Constants.ApiKeyValue))
                throw new ApiKeyInvalidException();

            await _next(context);
        }
    }
}
