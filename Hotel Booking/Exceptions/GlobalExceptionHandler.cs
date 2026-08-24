using Microsoft.AspNetCore.Diagnostics;
using System.Net;
using System.Text.Json;

namespace Hotel_Booking.API.Exceptions
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(
            ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(
                exception,
                "Unhandled exception occurred. Request: {Method} {Path}",
                httpContext.Request.Method,
                httpContext.Request.Path);

            var response = httpContext.Response;

            switch (exception)
            {
                case KeyNotFoundException:
                    response.StatusCode = StatusCodes.Status404NotFound;
                    break;

                case ArgumentException:
                    response.StatusCode = StatusCodes.Status400BadRequest;
                    break;

                case UnauthorizedAccessException:
                    response.StatusCode = StatusCodes.Status401Unauthorized;
                    break;

                default:
                    response.StatusCode = StatusCodes.Status500InternalServerError;
                    break;
            }

            var result = new
            {
                statusCode = response.StatusCode,
                message = exception switch
                {
                    KeyNotFoundException => exception.Message,
                    ArgumentException => exception.Message,
                    UnauthorizedAccessException => "You are not authorized.",
                    _ => "An unexpected error occurred."
                }
            };

            response.ContentType = "application/json";

            await response.WriteAsync(
                JsonSerializer.Serialize(result),
                cancellationToken);

            return true;
        }
    }
}