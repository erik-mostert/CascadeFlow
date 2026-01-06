using Cascade.Collector.Data.Entities;

namespace Cascade.Collector.Services;

/// <summary>
/// Service for managing API keys used to authenticate telemetry submissions.
/// </summary>
public interface IApiKeyService
{
    /// <summary>
    /// Validates an API key and optionally checks endpoint restriction.
    /// </summary>
    /// <param name="apiKey">The plaintext API key to validate.</param>
    /// <param name="endpointName">Optional endpoint name from the telemetry to check against key restrictions.</param>
    /// <returns>True if the key is valid and authorized for the endpoint; otherwise, false.</returns>
    Task<bool> ValidateKeyAsync(string apiKey, string? endpointName = null);

    /// <summary>
    /// Creates a new API key.
    /// </summary>
    /// <param name="name">Human-readable name for the key.</param>
    /// <param name="endpointName">Optional endpoint restriction. If specified, key can only be used by this endpoint.</param>
    /// <returns>The plaintext key (only returned once at creation) and the created entity.</returns>
    Task<(string PlaintextKey, ApiKey Entity)> CreateKeyAsync(string name, string? endpointName = null);

    /// <summary>
    /// Gets all API keys (without the actual key values).
    /// </summary>
    /// <returns>All API keys ordered by creation date descending.</returns>
    Task<IReadOnlyList<ApiKey>> GetAllKeysAsync();

    /// <summary>
    /// Revokes (deactivates) an API key. The key remains in the database but cannot be used.
    /// </summary>
    /// <param name="id">The ID of the key to revoke.</param>
    /// <returns>True if the key was found and revoked; otherwise, false.</returns>
    Task<bool> RevokeKeyAsync(int id);

    /// <summary>
    /// Deletes an API key permanently.
    /// </summary>
    /// <param name="id">The ID of the key to delete.</param>
    /// <returns>True if the key was found and deleted; otherwise, false.</returns>
    Task<bool> DeleteKeyAsync(int id);

    /// <summary>
    /// Updates the LastUsedAt timestamp for a key (called during validation).
    /// </summary>
    /// <param name="keyHash">The hash of the key to update.</param>
    Task UpdateLastUsedAsync(string keyHash);
}
