namespace Cascade.Collector.Data.Entities;

/// <summary>
/// Represents an API key for authenticating telemetry submissions.
/// </summary>
public class ApiKey
{
    public int Id { get; set; }

    /// <summary>
    /// The SHA-256 hash of the API key. Never store plaintext keys.
    /// </summary>
    public required string KeyHash { get; set; }

    /// <summary>
    /// A prefix of the key (first 8 chars) for identification without exposing the full key.
    /// </summary>
    public required string KeyPrefix { get; set; }

    /// <summary>
    /// Human-readable name or description for this key.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Optional: Restrict this key to a specific endpoint name.
    /// If null, the key can be used by any endpoint.
    /// </summary>
    public string? EndpointName { get; set; }

    /// <summary>
    /// When the key was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When the key was last used for authentication.
    /// </summary>
    public DateTimeOffset? LastUsedAt { get; set; }

    /// <summary>
    /// Whether the key is active and can be used for authentication.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
