using Cascade.Core.Models;

namespace Cascade.Core.Tests.Models;

[TestClass]
public class TopologyEndpointTests
{
    #region FailureRate Tests

    [TestMethod]
    public void FailureRate_WithNoMessagesReceived_ReturnsZero()
    {
        // Arrange
        var endpoint = CreateEndpoint(messagesReceived: 0, failures: 0);

        // Act
        var rate = endpoint.FailureRate;

        // Assert
        Assert.AreEqual(0, rate);
    }

    [TestMethod]
    public void FailureRate_WithNoFailures_ReturnsZero()
    {
        // Arrange
        var endpoint = CreateEndpoint(messagesReceived: 100, failures: 0);

        // Act
        var rate = endpoint.FailureRate;

        // Assert
        Assert.AreEqual(0, rate);
    }

    [TestMethod]
    public void FailureRate_WithAllFailures_ReturnsOne()
    {
        // Arrange
        var endpoint = CreateEndpoint(messagesReceived: 100, failures: 100);

        // Act
        var rate = endpoint.FailureRate;

        // Assert
        Assert.AreEqual(1.0, rate);
    }

    [TestMethod]
    public void FailureRate_CalculatesCorrectPercentage()
    {
        // Arrange - 25% failure rate
        var endpoint = CreateEndpoint(messagesReceived: 100, failures: 25);

        // Act
        var rate = endpoint.FailureRate;

        // Assert
        Assert.AreEqual(0.25, rate, 0.001);
    }

    [TestMethod]
    public void FailureRate_WithOddNumbers_CalculatesCorrectly()
    {
        // Arrange - 1/3 failure rate
        var endpoint = CreateEndpoint(messagesReceived: 3, failures: 1);

        // Act
        var rate = endpoint.FailureRate;

        // Assert
        Assert.AreEqual(0.333, rate, 0.01);
    }

    #endregion

    #region HostIds Tests

    [TestMethod]
    public void HostIds_DefaultsToEmpty()
    {
        // Arrange
        var endpoint = CreateEndpoint();

        // Assert
        Assert.AreEqual(0, endpoint.HostIds.Count);
    }

    [TestMethod]
    public void HostIds_CanAddMultipleHosts()
    {
        // Arrange
        var endpoint = CreateEndpoint();

        // Act
        endpoint.HostIds.Add("host-1");
        endpoint.HostIds.Add("host-2");
        endpoint.HostIds.Add("host-3");

        // Assert
        Assert.AreEqual(3, endpoint.HostIds.Count);
        Assert.IsTrue(endpoint.HostIds.Contains("host-1"));
        Assert.IsTrue(endpoint.HostIds.Contains("host-2"));
        Assert.IsTrue(endpoint.HostIds.Contains("host-3"));
    }

    [TestMethod]
    public void HostIds_DuplicatesAreIgnored()
    {
        // Arrange
        var endpoint = CreateEndpoint();

        // Act
        endpoint.HostIds.Add("host-1");
        endpoint.HostIds.Add("host-1"); // Duplicate

        // Assert
        Assert.AreEqual(1, endpoint.HostIds.Count);
    }

    #endregion

    #region Mutable Properties Tests

    [TestMethod]
    public void MessagesReceived_CanBeIncremented()
    {
        // Arrange
        var endpoint = CreateEndpoint(messagesReceived: 0);

        // Act
        endpoint.MessagesReceived++;
        endpoint.MessagesReceived++;

        // Assert
        Assert.AreEqual(2, endpoint.MessagesReceived);
    }

    [TestMethod]
    public void MessagesSent_CanBeIncremented()
    {
        // Arrange
        var endpoint = CreateEndpoint();

        // Act
        endpoint.MessagesSent = 5;

        // Assert
        Assert.AreEqual(5, endpoint.MessagesSent);
    }

    [TestMethod]
    public void AverageProcessingTimeMs_CanBeUpdated()
    {
        // Arrange
        var endpoint = CreateEndpoint();

        // Act
        endpoint.AverageProcessingTimeMs = 150.5;

        // Assert
        Assert.AreEqual(150.5, endpoint.AverageProcessingTimeMs, 0.01);
    }

    [TestMethod]
    public void LastSeen_CanBeUpdated()
    {
        // Arrange
        var endpoint = CreateEndpoint();
        var newTime = DateTimeOffset.UtcNow.AddHours(1);

        // Act
        endpoint.LastSeen = newTime;

        // Assert
        Assert.AreEqual(newTime, endpoint.LastSeen);
    }

    #endregion

    private static TopologyEndpoint CreateEndpoint(
        string name = "TestEndpoint",
        long messagesReceived = 0,
        long messagesSent = 0,
        long failures = 0,
        double averageProcessingTimeMs = 0)
    {
        return new TopologyEndpoint
        {
            Name = name,
            FirstSeen = DateTimeOffset.UtcNow,
            LastSeen = DateTimeOffset.UtcNow,
            MessagesReceived = messagesReceived,
            MessagesSent = messagesSent,
            Failures = failures,
            AverageProcessingTimeMs = averageProcessingTimeMs
        };
    }
}
