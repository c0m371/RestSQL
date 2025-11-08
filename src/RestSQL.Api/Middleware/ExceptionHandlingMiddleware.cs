using Microsoft.AspNetCore.Mvc;

namespace RestSQL.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, IWebHostEnvironment env, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _env = env;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception while processing request {method} {path}", context.Request.Method, context.Request.Path);
            await HandleExceptionAsync(context, ex, _env.IsDevelopment()).ConfigureAwait(false);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception ex, bool includeDetails)
    {
        ProblemDetails problem;

        if (ex is ArgumentException argEx)
        {
            problem = new ProblemDetails
            {
                Title = "Invalid argument",
                Detail = includeDetails ? argEx.Message : "One or more request arguments are invalid.",
                Status = StatusCodes.Status400BadRequest
            };
        }
        else
        {
            problem = new ProblemDetails
            {
                Title = "An unexpected error occurred",
                Detail = includeDetails ? ex.Message : "Internal server error",
                Status = StatusCodes.Status500InternalServerError
            };
        }

        if (!string.IsNullOrEmpty(context.TraceIdentifier))
            problem.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        return context.Response.WriteAsJsonAsync(problem);
    }
}
