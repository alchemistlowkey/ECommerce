using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace ECommerce.Middleware;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger
        )
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
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message) = exception switch
        {
            KeyNotFoundException =>
                (HttpStatusCode.NotFound, exception.Message),

            InvalidOperationException =>
                (HttpStatusCode.BadRequest, exception.Message),

            UnauthorizedAccessException =>
                (HttpStatusCode.Unauthorized, exception.Message),

            ArgumentException =>
                (HttpStatusCode.BadRequest, exception.Message),

            _ =>
                (HttpStatusCode.InternalServerError,
                 "An unexpected error occurred. Please try again later.")
        };

        context.Response.StatusCode = (int)statusCode;

        var errorDetails = new ErrorDetails
        {
            StatusCode = (int)statusCode,
            Message = message
        };

        await context.Response.WriteAsync(errorDetails.ToString());
    }
}
