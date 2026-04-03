using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ValidationException = BudgetBuddy.API.VSA.Common.Domain.Exceptions.ValidationException;

namespace BudgetBuddy.API.VSA.Common.Exceptions;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        // Handle cancellation separately - not an error, but expected behavior
        if (exception is OperationCanceledException or TaskCanceledException)
        {
            logger.LogInformation(
                "Request cancelled: {Path} | TraceId: {TraceId}",
                context.Request.Path,
                traceId);

            // Client has disconnected, no need to write response
            // ASP.NET Core will return 499 Client Closed Request
            return true;
        }

        logger.LogError(
            exception,
            "[EXCEPTION] Error occurred: {ExceptionType} - {Message} | TraceId: {TraceId} | Path: {Path}",
            exception.GetType().Name,
            exception.Message,
            traceId,
            context.Request.Path);

        var (statusCode, title, detail, errors) = MapException(exception, environment);

        context.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Title = title,
            Detail = detail,
            Status = statusCode,
            Instance = context.Request.Path,
            Type = GetProblemType(statusCode)
        };

        problemDetails.Extensions.Add("traceId", traceId);

        if (errors != null)
        {
            problemDetails.Extensions.Add("errors", errors);
        }

        if (environment.IsDevelopment())
        {
            problemDetails.Extensions.Add("exceptionType", exception.GetType().FullName);
            problemDetails.Extensions.Add("stackTrace", exception.StackTrace);
            problemDetails.Extensions.Add("innerException", exception.InnerException?.Message);
        }

        await context.Response.WriteAsJsonAsync(problemDetails, cancellationToken: cancellationToken);
        return true;
    }


    private static (int StatusCode, string Title, string Detail, object? Errors) MapException(
        Exception exception,
        IHostEnvironment environment)
    {
        return exception switch
        {
            // FluentValidation exceptions from ValidationBehavior
            FluentValidation.ValidationException fluentValidationEx => (
                StatusCodes.Status400BadRequest,
                "Validation Failed",
                "One or more validation errors occurred.",
                fluentValidationEx.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    )
            ),

            // Custom domain exceptions
            NotFoundException => (
                StatusCodes.Status404NotFound,
                "Resource Not Found",
                exception.Message,
                null
            ),

            // Custom domain exceptions
            ValidationException => (
                StatusCodes.Status400BadRequest,
                "Validation Failed",
                exception.Message,
                null
            ),

            DomainException => (
                StatusCodes.Status400BadRequest,
                "Business Rule Violation",
                exception.Message,
                null
            ),

            // Database unique constraint violations
            DbUpdateException dbEx when dbEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23505" => (
                StatusCodes.Status409Conflict,
                "Duplicate Entry",
                ParseUniqueConstraintMessage(pgEx),
                null
            ),

            // Database foreign key constraint violations
            DbUpdateException dbEx when dbEx.InnerException is PostgresException pgEx && pgEx.SqlState == "23503" => (
                StatusCodes.Status400BadRequest,
                "Invalid Reference",
                environment.IsDevelopment()
                    ? $"Foreign key constraint violation: {pgEx.ConstraintName}. {pgEx.MessageText}"
                    : "The referenced resource does not exist.",
                null
            ),

            // ASP.NET Core built-in exceptions
            BadHttpRequestException badHttpEx => (
                StatusCodes.Status400BadRequest,
                "Bad Request",
                environment.IsDevelopment()
                    ? badHttpEx.Message
                    : "Invalid request body or parameters.",
                null
            ),

            // Unhandled exceptions
            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                environment.IsDevelopment()
                    ? exception.Message
                    : "An unexpected error occurred. Please try again later.",
                null
            )
        };
    }

    private static string GetProblemType(int statusCode)
    {
        return statusCode switch
        {
            StatusCodes.Status400BadRequest => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            StatusCodes.Status404NotFound => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            StatusCodes.Status409Conflict => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
            StatusCodes.Status500InternalServerError => "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            _ => "https://tools.ietf.org/html/rfc7231"
        };
    }

    private static string ParseUniqueConstraintMessage(PostgresException pgEx)
    {
        // Parse constraint name to provide user-friendly message
        var constraintName = pgEx.ConstraintName ?? "";

        return constraintName switch
        {
            "IX_Accounts_Unique" => "An account with this name already exists.",
            "IX_Categories_Unique" => "A category with this name already exists.",
            "IX_CategoryTypes_Unique" => "A type with this name already exists in this category.",
            "IX_Investments_Dedup" => "An identical investment purchase already exists.",
            "IX_Budgets_Unique" => "A budget already exists for this category and period.",
            _ => "A duplicate entry already exists. Please use a different value."
        };
    }
}