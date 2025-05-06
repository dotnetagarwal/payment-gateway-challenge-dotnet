using PaymentGateway.Api.Exceptions;

namespace PaymentGateway.Api.Middlewares
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception of type {ExceptionType} at {Path}. Message: {Message}", ex.GetType().Name, context.Request.Path, ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = Constants.ContentType;

            var (statusCode, message) = exception switch
            {
                ApiKeyMissingException => (StatusCodes.Status401Unauthorized, exception.Message),
                ApiKeyInvalidException => (StatusCodes.Status401Unauthorized, exception.Message),
                ServiceUnavailableException =>(StatusCodes.Status503ServiceUnavailable, exception.Message),
                _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
            };

            context.Response.StatusCode = statusCode;

            var errorResponse = new
            {
                statusCode,
                message
            };

            return context.Response.WriteAsJsonAsync(errorResponse);
        }
    }

}
