using Hotel_Booking.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Hotel_Booking.API.Exceptions
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly IHostEnvironment _env;

        public GlobalExceptionHandler(
            ILogger<GlobalExceptionHandler> logger,
            IHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            var traceId = httpContext.TraceIdentifier;

            var (statusCode, title) = MapException(exception);

          
            if (exception is AppException)
            {
                _logger.LogWarning(
                    "Handled exception. TraceId: {TraceId}, Path: {Path}, Type: {Type}, Message: {Message}",
                    traceId, httpContext.Request.Path, exception.GetType().Name, exception.Message);
            }
            else
            {
                _logger.LogError(
                    exception,
                    "Unhandled exception. TraceId: {TraceId}, Method: {Method}, Path: {Path}",
                    traceId, httpContext.Request.Method, httpContext.Request.Path);
            }

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = BuildDetail(exception, statusCode),
                Instance = httpContext.Request.Path,
                Type = $"https://httpstatuses.io/{statusCode}"
            };

            problemDetails.Extensions["traceId"] = traceId;

            if (exception is ValidationAppException validationEx)
            {
                problemDetails.Extensions["errors"] = validationEx.Errors;
            }

            httpContext.Response.StatusCode = statusCode;
            httpContext.Response.ContentType = "application/problem+json";

            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }

        private static (int StatusCode, string Title) MapException(Exception exception) =>
            exception switch
            {
                NotFoundException => (StatusCodes.Status404NotFound, "Resource Not Found"),
                ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
                ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden"),
                ArgumentException =>(StatusCodes.Status400BadRequest, "Bad Request"),
                ValidationAppException => (StatusCodes.Status400BadRequest, "Validation Failed"),
                BusinessRuleException => (StatusCodes.Status409Conflict, "Business Rule Violation"),
                UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Forbidden"),
                _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
            };

        
        private string BuildDetail(Exception exception, int statusCode)
        {
            if (exception is AppException)
                return exception.Message;

            if (_env.IsDevelopment())
                return exception.Message;

            return "An unexpected error occurred. Please contact support with the trace id.";
        }
    }
}