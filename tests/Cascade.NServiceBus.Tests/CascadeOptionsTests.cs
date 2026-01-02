namespace Cascade.NServiceBus.Tests;

[TestClass]
public class CascadeOptionsTests
{
    #region Default Value Tests

    [TestMethod]
    public void CollectorUrl_DefaultsToLocalhost5100()
    {
        // Arrange & Act
        var options = new CascadeOptions();

        // Assert
        Assert.AreEqual("http://localhost:5100", options.CollectorUrl);
    }

    [TestMethod]
    public void EndpointName_DefaultsToNull()
    {
        // Arrange & Act
        var options = new CascadeOptions();

        // Assert
        Assert.IsNull(options.EndpointName);
    }

    [TestMethod]
    public void HostId_DefaultsToNull()
    {
        // Arrange & Act
        var options = new CascadeOptions();

        // Assert
        Assert.IsNull(options.HostId);
    }

    [TestMethod]
    public void IncludeHeaders_DefaultsToTrue()
    {
        // Arrange & Act
        var options = new CascadeOptions();

        // Assert
        Assert.IsTrue(options.IncludeHeaders);
    }

    [TestMethod]
    public void BufferSize_DefaultsTo1000()
    {
        // Arrange & Act
        var options = new CascadeOptions();

        // Assert
        Assert.AreEqual(1000, options.BufferSize);
    }

    #endregion

    #region Property Assignment Tests

    [TestMethod]
    public void CollectorUrl_CanBeChanged()
    {
        // Arrange
        var options = new CascadeOptions();

        // Act
        options.CollectorUrl = "http://collector.example.com:8080";

        // Assert
        Assert.AreEqual("http://collector.example.com:8080", options.CollectorUrl);
    }

    [TestMethod]
    public void EndpointName_CanBeSet()
    {
        // Arrange
        var options = new CascadeOptions();

        // Act
        options.EndpointName = "MyService";

        // Assert
        Assert.AreEqual("MyService", options.EndpointName);
    }

    [TestMethod]
    public void HostId_CanBeSet()
    {
        // Arrange
        var options = new CascadeOptions();

        // Act
        options.HostId = "host-123";

        // Assert
        Assert.AreEqual("host-123", options.HostId);
    }

    [TestMethod]
    public void IncludeHeaders_CanBeDisabled()
    {
        // Arrange
        var options = new CascadeOptions();

        // Act
        options.IncludeHeaders = false;

        // Assert
        Assert.IsFalse(options.IncludeHeaders);
    }

    [TestMethod]
    public void BufferSize_CanBeChanged()
    {
        // Arrange
        var options = new CascadeOptions();

        // Act
        options.BufferSize = 5000;

        // Assert
        Assert.AreEqual(5000, options.BufferSize);
    }

    #endregion

    #region Initialization Tests

    [TestMethod]
    public void Options_CanBeInitializedWithObjectInitializer()
    {
        // Arrange & Act
        var options = new CascadeOptions
        {
            CollectorUrl = "http://custom:9000",
            EndpointName = "TestEndpoint",
            HostId = "test-host",
            IncludeHeaders = false,
            BufferSize = 500
        };

        // Assert
        Assert.AreEqual("http://custom:9000", options.CollectorUrl);
        Assert.AreEqual("TestEndpoint", options.EndpointName);
        Assert.AreEqual("test-host", options.HostId);
        Assert.IsFalse(options.IncludeHeaders);
        Assert.AreEqual(500, options.BufferSize);
    }

    #endregion
}
