using System.Net;
using System.Text.Json;
using RecipeSugesstionApp.DTOs;

namespace RecipeSugesstionApp.Middleware
{
    /// <summary>
    /// Global exception handler. Catches unhandled exceptions and returns a
    /// consistent <see cref="ErrorResponse"/> JSON payload.
    /// </summary>
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _env;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public ExceptionMiddleware(
            RequestDelegate next,
            ILogger<ExceptionMiddleware> logger,
            IHostEnvironment env)
        {
            _next   = next;
            _logger = logger;
            _env    = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);

            var (statusCode, response) = ex switch
            {
                ArgumentException or InvalidOperationException =>
                    ((int)HttpStatusCode.BadRequest,
                     new ErrorResponse { Message = ex.Message }),

                UnauthorizedAccessException =>
                    ((int)HttpStatusCode.Unauthorized,
                     new ErrorResponse { Message = "Unauthorized." }),

                KeyNotFoundException =>
                    ((int)HttpStatusCode.NotFound,
                     new ErrorResponse { Message = ex.Message }),

                _ =>
                    ((int)HttpStatusCode.InternalServerError,
                     new ErrorResponse
                     {
                         Message = _env.IsDevelopment()
                             ? ex.Message
                             : "An internal server error occurred. Please try again later.",
                         Detail = _env.IsDevelopment() ? ex.StackTrace : null
                     })
            };

            context.Response.ContentType = "application/json";
            context.Response.StatusCode  = statusCode;

            var json = JsonSerializer.Serialize(response, _jsonOptions);
            await context.Response.WriteAsync(json);
        }
    }
}
