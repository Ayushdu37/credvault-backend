using CreditManagement.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CreditManagement.API.Middleware;

// Global exception handler — same pattern as Identity Service
// Maps custom exceptions to HTTP status codes with ProblemDetails JSON response
public static class ExceptionHandlingMiddleware
{
    public static void UseGlobalExceptionHandler(this WebApplication app)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var feature = context.Features.Get<IExceptionHandlerFeature>();
                if (feature is null) return;

                var exception = feature.Error;

                var (statusCode, title) = exception switch
                {
                    NotFoundException => (StatusCodes.Status404NotFound, "Resource Not Found"),
                    ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
                    CustomValidationException => (StatusCodes.Status400BadRequest, "Validation Failed"),
                    UnauthorizedException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
                    _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
                };

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
