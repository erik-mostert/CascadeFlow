using Cascade.Collector.Data;
using Cascade.Collector.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cascade.Collector.Tests.Services;

[TestClass]
public class ApiKeyServiceTests
{
    private CascadeDbContext _db = null!;
    private Mock<ILogger<ApiKeyService>> _loggerMock = null!;
    private ApiKeyService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<CascadeDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new CascadeDbContext(options);
        _loggerMock = new Mock<ILogger<ApiKeyService>>();
        _service = new ApiKeyService(_db, _loggerMock.Object);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _db.Dispose();
    }

    #region CreateKeyAsync Tests

    [TestMethod]
    public async Task CreateKeyAsync_ReturnsPlaintextKey()
    {
        // Act
        var (plaintextKey, entity) = await _service.CreateKeyAsync("Test Key");

        // Assert
        Assert.IsNotNull(plaintextKey);
        Assert.IsTrue(plaintextKey.StartsWith("csk_"));
        Assert.AreEqual(47, plaintextKey.Length); // "csk_" (4) + 43 base64 chars
    }

    [TestMethod]
    public async Task CreateKeyAsync_StoresHashedKey()
    {
        // Act
        var (plaintextKey, entity) = await _service.CreateKeyAsync("Test Key");

        // Assert
        var storedKey = await _db.ApiKeys.FindAsync(entity.Id);
        Assert.IsNotNull(storedKey);
        Assert.AreNotEqual(plaintextKey, storedKey.KeyHash);
        Assert.AreEqual(64, storedKey.KeyHash.Length); // SHA-256 hex string
    }

    [TestMethod]
    public async Task CreateKeyAsync_StoresKeyPrefix()
    {
        // Act
        var (plaintextKey, entity) = await _service.CreateKeyAsync("Test Key");

        // Assert
        Assert.AreEqual(plaintextKey[..8], entity.KeyPrefix);
    }

    [TestMethod]
    public async Task CreateKeyAsync_WithEndpointName_StoresRestriction()
    {
        // Act
        var (_, entity) = await _service.CreateKeyAsync("Test Key", "MyEndpoint");

        // Assert
        Assert.AreEqual("MyEndpoint", entity.EndpointName);
    }

    [TestMethod]
    public async Task CreateKeyAsync_WithoutEndpointName_StoresNullRestriction()
    {
        // Act
        var (_, entity) = await _service.CreateKeyAsync("Test Key");

        // Assert
        Assert.IsNull(entity.EndpointName);
    }

    [TestMethod]
    public async Task CreateKeyAsync_SetsCreatedAt()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var (_, entity) = await _service.CreateKeyAsync("Test Key");

        // Assert
        var after = DateTimeOffset.UtcNow;
        Assert.IsTrue(entity.CreatedAt >= before && entity.CreatedAt <= after);
    }

    [TestMethod]
    public async Task CreateKeyAsync_SetsIsActiveTrue()
    {
        // Act
        var (_, entity) = await _service.CreateKeyAsync("Test Key");

        // Assert
        Assert.IsTrue(entity.IsActive);
    }

    #endregion

    #region ValidateKeyAsync Tests

    [TestMethod]
    public async Task ValidateKeyAsync_WithValidKey_ReturnsTrue()
    {
        // Arrange
        var (plaintextKey, _) = await _service.CreateKeyAsync("Test Key");

        // Act
        var isValid = await _service.ValidateKeyAsync(plaintextKey);

        // Assert
        Assert.IsTrue(isValid);
    }

    [TestMethod]
    public async Task ValidateKeyAsync_WithInvalidKey_ReturnsFalse()
    {
        // Act
        var isValid = await _service.ValidateKeyAsync("csk_invalid-key-that-does-not-exist");

        // Assert
        Assert.IsFalse(isValid);
    }

    [TestMethod]
    public async Task ValidateKeyAsync_WithEmptyKey_ReturnsFalse()
    {
        // Act
        var isValid = await _service.ValidateKeyAsync("");

        // Assert
        Assert.IsFalse(isValid);
    }

    [TestMethod]
    public async Task ValidateKeyAsync_WithNullKey_ReturnsFalse()
    {
        // Act
        var isValid = await _service.ValidateKeyAsync(null!);

        // Assert
        Assert.IsFalse(isValid);
    }

    [TestMethod]
    public async Task ValidateKeyAsync_WithWhitespaceKey_ReturnsFalse()
    {
        // Act
        var isValid = await _service.ValidateKeyAsync("   ");

        // Assert
        Assert.IsFalse(isValid);
    }

    [TestMethod]
    public async Task ValidateKeyAsync_WithRevokedKey_ReturnsFalse()
    {
        // Arrange
        var (plaintextKey, entity) = await _service.CreateKeyAsync("Test Key");
        await _service.RevokeKeyAsync(entity.Id);

        // Create new service instance to clear cache
        var newService = new ApiKeyService(_db, _loggerMock.Object);

        // Act
        var isValid = await newService.ValidateKeyAsync(plaintextKey);

        // Assert
        Assert.IsFalse(isValid);
    }

    [TestMethod]
    public async Task ValidateKeyAsync_WithEndpointRestriction_MatchingEndpoint_ReturnsTrue()
    {
        // Arrange
        var (plaintextKey, _) = await _service.CreateKeyAsync("Test Key", "AllowedEndpoint");

        // Act
        var isValid = await _service.ValidateKeyAsync(plaintextKey, "AllowedEndpoint");

        // Assert
        Assert.IsTrue(isValid);
    }

    [TestMethod]
    public async Task ValidateKeyAsync_WithEndpointRestriction_NonMatchingEndpoint_ReturnsFalse()
    {
        // Arrange
        var (plaintextKey, _) = await _service.CreateKeyAsync("Test Key", "AllowedEndpoint");

        // Act
        var isValid = await _service.ValidateKeyAsync(plaintextKey, "DifferentEndpoint");

        // Assert
        Assert.IsFalse(isValid);
    }

    [TestMethod]
    public async Task ValidateKeyAsync_WithoutEndpointRestriction_AnyEndpoint_ReturnsTrue()
    {
        // Arrange
        var (plaintextKey, _) = await _service.CreateKeyAsync("Test Key");

        // Act
        var isValid = await _service.ValidateKeyAsync(plaintextKey, "AnyEndpoint");

        // Assert
        Assert.IsTrue(isValid);
    }

    [TestMethod]
    public async Task ValidateKeyAsync_WithEndpointRestriction_CaseInsensitive_ReturnsTrue()
    {
        // Arrange
        var (plaintextKey, _) = await _service.CreateKeyAsync("Test Key", "MyEndpoint");

        // Act
        var isValid = await _service.ValidateKeyAsync(plaintextKey, "myendpoint");

        // Assert
        Assert.IsTrue(isValid);
    }

    [TestMethod]
    public async Task ValidateKeyAsync_CachesValidationResult()
    {
        // Arrange
        var (plaintextKey, _) = await _service.CreateKeyAsync("Test Key");

        // Act - validate twice
        var isValid1 = await _service.ValidateKeyAsync(plaintextKey);
        var isValid2 = await _service.ValidateKeyAsync(plaintextKey);

        // Assert - both should succeed (second from cache)
        Assert.IsTrue(isValid1);
        Assert.IsTrue(isValid2);
    }

    #endregion

    #region RevokeKeyAsync Tests

    [TestMethod]
    public async Task RevokeKeyAsync_DeactivatesKey()
    {
        // Arrange
        var (_, entity) = await _service.CreateKeyAsync("Test Key");

        // Act
        var success = await _service.RevokeKeyAsync(entity.Id);

        // Assert
        Assert.IsTrue(success);
        var storedKey = await _db.ApiKeys.FindAsync(entity.Id);
        Assert.IsFalse(storedKey!.IsActive);
    }

    [TestMethod]
    public async Task RevokeKeyAsync_NonExistentKey_ReturnsFalse()
    {
        // Act
        var success = await _service.RevokeKeyAsync(999);

        // Assert
        Assert.IsFalse(success);
    }

    [TestMethod]
    public async Task RevokeKeyAsync_InvalidatesCacheForKey()
    {
        // Arrange
        var (plaintextKey, entity) = await _service.CreateKeyAsync("Test Key");

        // Validate to populate cache
        await _service.ValidateKeyAsync(plaintextKey);

        // Act
        await _service.RevokeKeyAsync(entity.Id);

        // Assert - should now be invalid
        var isValid = await _service.ValidateKeyAsync(plaintextKey);
        Assert.IsFalse(isValid);
    }

    #endregion

    #region DeleteKeyAsync Tests

    [TestMethod]
    public async Task DeleteKeyAsync_RemovesKey()
    {
        // Arrange
        var (_, entity) = await _service.CreateKeyAsync("Test Key");

        // Act
        var success = await _service.DeleteKeyAsync(entity.Id);

        // Assert
        Assert.IsTrue(success);
        var storedKey = await _db.ApiKeys.FindAsync(entity.Id);
        Assert.IsNull(storedKey);
    }

    [TestMethod]
    public async Task DeleteKeyAsync_NonExistentKey_ReturnsFalse()
    {
        // Act
        var success = await _service.DeleteKeyAsync(999);

        // Assert
        Assert.IsFalse(success);
    }

    #endregion

    #region GetAllKeysAsync Tests

    [TestMethod]
    public async Task GetAllKeysAsync_ReturnsAllKeys()
    {
        // Arrange
        await _service.CreateKeyAsync("Key 1");
        await _service.CreateKeyAsync("Key 2");
        await _service.CreateKeyAsync("Key 3");

        // Act
        var keys = await _service.GetAllKeysAsync();

        // Assert
        Assert.AreEqual(3, keys.Count);
    }

    [TestMethod]
    public async Task GetAllKeysAsync_ReturnsEmptyWhenNoKeys()
    {
        // Act
        var keys = await _service.GetAllKeysAsync();

        // Assert
        Assert.AreEqual(0, keys.Count);
    }

    [TestMethod]
    public async Task GetAllKeysAsync_OrdersByIdDescending()
    {
        // Arrange - create keys sequentially (IDs auto-increment)
        await _service.CreateKeyAsync("First");
        await _service.CreateKeyAsync("Second");
        await _service.CreateKeyAsync("Third");

        // Act
        var keys = await _service.GetAllKeysAsync();

        // Assert - newest (highest ID) first
        Assert.AreEqual("Third", keys[0].Name);
        Assert.AreEqual("Second", keys[1].Name);
        Assert.AreEqual("First", keys[2].Name);
    }

    [TestMethod]
    public async Task GetAllKeysAsync_IncludesRevokedKeys()
    {
        // Arrange
        var (_, entity) = await _service.CreateKeyAsync("Key 1");
        await _service.CreateKeyAsync("Key 2");
        await _service.RevokeKeyAsync(entity.Id);

        // Act
        var keys = await _service.GetAllKeysAsync();

        // Assert
        Assert.AreEqual(2, keys.Count);
        Assert.IsTrue(keys.Any(k => !k.IsActive));
    }

    #endregion

    #region Key Generation Tests

    [TestMethod]
    public async Task CreateKeyAsync_GeneratesUniqueKeys()
    {
        // Act
        var (key1, _) = await _service.CreateKeyAsync("Key 1");
        var (key2, _) = await _service.CreateKeyAsync("Key 2");
        var (key3, _) = await _service.CreateKeyAsync("Key 3");

        // Assert
        Assert.AreNotEqual(key1, key2);
        Assert.AreNotEqual(key2, key3);
        Assert.AreNotEqual(key1, key3);
    }

    #endregion
}
