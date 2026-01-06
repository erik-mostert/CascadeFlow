namespace Cascade.Collector.Models;

/// <summary>
/// Request model for creating a new API key.
/// </summary>
public record CreateApiKeyRequest(
    string Name,
    string? EndpointName = null
);

/// <summary>
/// Response model when an API key is created (includes the plaintext key).
/// </summary>
public record CreateApiKeyResponse(
    int Id,
    string Key,
    string KeyPrefix,
    string Name,
    string? EndpointName,
    DateTimeOffset CreatedAt,
    bool IsActive
);

/// <summary>
/// Response model for listing API keys (excludes the plaintext key).
/// </summary>
public record ApiKeyResponse(
    int Id,
    string KeyPrefix,
    string Name,
    string? EndpointName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastUsedAt,
    bool IsActive
);
