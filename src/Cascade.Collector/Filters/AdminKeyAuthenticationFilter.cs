using System.Security.Cryptography;

namespace Cascade.Collector.Filters;

/// <summary>
/// Endpoint filter that validates admin key authentication for management endpoints.
/// </summary>
public class AdminKeyAuthenticationFilter : IEndpointFilter
{
    /// <summary>
    /// The HTTP header name for the admin key.
    /// </summary>
    public const string AdminKeyHeaderName = "X-Admin-Key";

    /// <summary>
    /// The configuration key for the admin key.
    /// </summary>
    public const string AdminKeyConfigName = "Cascade:AdminKey";

    public ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();

        // Get the configured admin key
        var configuredAdminKey = configuration.GetValue<string>(AdminKeyConfigName);

        // If no admin key is configured, allow access (for development/backwards compatibility)
        if (string.IsNullOrWhiteSpace(configuredAdminKey))
        {
            return next(context);
        }

        // Check for admin key in header
        if (!context.HttpContext.Request.Headers.TryGetValue(AdminKeyHeaderName, out var adminKeyHeader) ||
            string.IsNullOrWhiteSpace(adminKeyHeader))
        {
            return ValueTask.FromResult<object?>(Results.Json(
                new { error = "Admin key required", message = "Include X-Admin-Key header with a valid admin key" },
                statusCode: StatusCodes.Status401Unauthorized));
        }

        var providedKey = adminKeyHeader.ToString();

        // Validate the key using constant-time comparison to prevent timing attacks
        if (!CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(configuredAdminKey),
            System.Text.Encoding.UTF8.GetBytes(providedKey)))
        {
            return ValueTask.FromResult<object?>(Results.Json(
                new { error = "Invalid admin key", message = "The provided admin key is invalid" },
                statusCode: StatusCodes.Status401Unauthorized));
        }

        return next(context);
    }
}
