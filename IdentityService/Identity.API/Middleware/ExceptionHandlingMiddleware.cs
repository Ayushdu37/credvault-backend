using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Identity.Application.Exceptions;

namespace Identity.API.Middleware
{
    // Global exception handler — catches all unhandled exceptions from controllers/services
    // and returns a consistent JSON error response (ProblemDetails format).
    // This way, controllers never need try/catch blocks.
    public static class ExceptionHandlingMiddleware
    {
        public static void UseGlobalExceptionHandler(this WebApplication app)
        {
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    // Get the exception that was thrown
                    var feature = context.Features.Get<IExceptionHandlerFeature>();
                    if (feature is null) return;

                    var exception = feature.Error;

                    // Map custom exceptions to HTTP status codes
                    var (statusCode, title) = exception switch
                    {
                        NotFoundException => (StatusCodes.Status404NotFound, "Resource Not Found"),
                        ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
                        CustomValidationException => (StatusCodes.Status400BadRequest, "Validation Failed"),
                        UnauthorizedException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
                        _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
                    };

                    // Return a standard ProblemDetails JSON response
                    context.Response.StatusCode = statusCode;
                    context.Response.ContentType = "application/json";

                    await context.Response.WriteAsJsonAsync(new ProblemDetails
                    {
                        Title = title,
                        Status = statusCode,
                        Detail = exception.Message
                    });
                });
            });
        }
    }
}
