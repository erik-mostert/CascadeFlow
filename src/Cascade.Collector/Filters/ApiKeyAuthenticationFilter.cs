using Cascade.Collector.Services;
using Cascade.Core.Models;

namespace Cascade.Collector.Filters;

/// <summary>
/// Endpoint filter that validates API key authentication for the telemetry endpoint.
/// </summary>
public class ApiKeyAuthenticationFilter : IEndpointFilter
{
    /// <summary>
    /// The HTTP header name for the API key.
    /// </summary>
    public const string ApiKeyHeaderName = "X-API-Key";

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();

        // Check if authentication is required (opt-in via configuration)
        var requireAuth = configuration.GetValue("Cascade:RequireApiKey", false);
        if (!requireAuth)
        {
            return await next(context);
        }

        // Check for API key in header
        if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKeyHeader) ||
            string.IsNullOrWhiteSpace(apiKeyHeader))
        {
            return Results.Json(
                new { error = "API key required", message = "Include X-API-Key header with a valid API key" },
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var apiKey = apiKeyHeader.ToString();

        // Get endpoint name from the telemetry payload if available
        string? endpointName = null;
        var telemetryArg = context.Arguments.FirstOrDefault(a => a is MessageTelemetry) as MessageTelemetry;
        if (telemetryArg != null)
        {
            endpointName = telemetryArg.EndpointName;
        }

        // Validate the key
        var apiKeyService = context.HttpContext.RequestServices.GetRequiredService<IApiKeyService>();
        var isValid = await apiKeyService.ValidateKeyAsync(apiKey, endpointName);
        if (!isValid)
        {
            return Results.Json(
                new { error = "Invalid API key", message = "The provided API key is invalid or has been revoked" },
                statusCode: StatusCodes.Status401Unauthorized);
        }

        return await next(context);
    }
}
