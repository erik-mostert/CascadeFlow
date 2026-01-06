using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Cascade.Collector.Data;
using Cascade.Collector.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cascade.Collector.Services;

/// <summary>
/// Service for managing API keys with secure hashing and caching.
/// </summary>
public class ApiKeyService : IApiKeyService
{
    private readonly CascadeDbContext _db;
    private readonly ILogger<ApiKeyService> _logger;

    // Cache validated keys for performance (keys don't change frequently)
    private readonly ConcurrentDictionary<string, (bool IsValid, string? EndpointRestriction)> _validationCache = new();

    public ApiKeyService(CascadeDbContext db, ILogger<ApiKeyService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> ValidateKeyAsync(string apiKey, string? endpointName = null)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return false;

        var keyHash = HashKey(apiKey);

        // Check cache first
        if (_validationCache.TryGetValue(keyHash, out var cached))
        {
            if (!cached.IsValid)
                return false;

            // If endpoint restricted, verify match
            if (cached.EndpointRestriction != null &&
                !string.Equals(cached.EndpointRestriction, endpointName, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("API key restricted to endpoint {Restricted}, but used by {Actual}",
                    cached.EndpointRestriction, endpointName);
                return false;
            }

            // Update last used (fire and forget to not block validation)
            _ = UpdateLastUsedAsync(keyHash);
            return true;
        }

        // Query database
        var key = await _db.ApiKeys
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash && k.IsActive);

        if (key == null)
        {
            _validationCache[keyHash] = (false, null);
            return false;
        }

        // Cache the result
        _validationCache[keyHash] = (true, key.EndpointName);

        // Check endpoint restriction
        if (key.EndpointName != null &&
            !string.Equals(key.EndpointName, endpointName, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("API key {KeyPrefix}... restricted to endpoint {Restricted}, but used by {Actual}",
                key.KeyPrefix, key.EndpointName, endpointName);
            return false;
        }

        // Update last used (fire and forget)
        _ = UpdateLastUsedAsync(keyHash);

        return true;
    }

    /// <inheritdoc />
    public async Task<(string PlaintextKey, ApiKey Entity)> CreateKeyAsync(string name, string? endpointName = null)
    {
        var plaintextKey = GenerateApiKey();
        var keyHash = HashKey(plaintextKey);
        var keyPrefix = plaintextKey[..8];

        var entity = new ApiKey
        {
            KeyHash = keyHash,
            KeyPrefix = keyPrefix,
            Name = name,
            EndpointName = endpointName,
            CreatedAt = DateTimeOffset.UtcNow,
            IsActive = true
        };

        _db.ApiKeys.Add(entity);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created API key {KeyPrefix}... for {Name}{Restriction}",
            keyPrefix, name, endpointName != null ? $" (restricted to {endpointName})" : "");

        return (plaintextKey, entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ApiKey>> GetAllKeysAsync()
    {
        // Order by Id descending (auto-increment ensures same order as CreatedAt)
        return await _db.ApiKeys
            .OrderByDescending(k => k.Id)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<bool> RevokeKeyAsync(int id)
    {
        var key = await _db.ApiKeys.FindAsync(id);
        if (key == null)
            return false;

        key.IsActive = false;
        await _db.SaveChangesAsync();

        // Invalidate cache
        InvalidateCache();

        _logger.LogInformation("Revoked API key {KeyPrefix}... ({Name})", key.KeyPrefix, key.Name);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteKeyAsync(int id)
    {
        var key = await _db.ApiKeys.FindAsync(id);
        if (key == null)
            return false;

        _db.ApiKeys.Remove(key);
        await _db.SaveChangesAsync();

        // Invalidate cache
        InvalidateCache();

        _logger.LogInformation("Deleted API key {KeyPrefix}... ({Name})", key.KeyPrefix, key.Name);
        return true;
    }

    /// <inheritdoc />
    public async Task UpdateLastUsedAsync(string keyHash)
    {
        try
        {
            await _db.ApiKeys
                .Where(k => k.KeyHash == keyHash)
                .ExecuteUpdateAsync(s => s.SetProperty(k => k.LastUsedAt, DateTimeOffset.UtcNow));
        }
        catch (Exception ex)
        {
            // Don't fail validation for tracking errors
            _logger.LogWarning(ex, "Failed to update LastUsedAt for key");
        }
    }

    private void InvalidateCache()
    {
        _validationCache.Clear();
    }

    /// <summary>
    /// Generates a cryptographically secure API key with a recognizable prefix.
    /// Format: csk_ + 43 URL-safe base64 characters (256 bits of entropy)
    /// </summary>
    private static string GenerateApiKey()
    {
        // Generate 32 bytes = 256 bits of randomness
        var bytes = RandomNumberGenerator.GetBytes(32);
        // Convert to URL-safe base64 (43 chars) prefixed with "csk_" (Cascade Secret Key)
        return "csk_" + Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    /// <summary>
    /// Hashes an API key using SHA-256.
    /// </summary>
    private static string HashKey(string apiKey)
    {
        var bytes = Encoding.UTF8.GetBytes(apiKey);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
