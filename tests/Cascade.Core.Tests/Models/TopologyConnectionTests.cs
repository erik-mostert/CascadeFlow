using Cascade.Core.Models;

namespace Cascade.Core.Tests.Models;

[TestClass]
public class TopologyConnectionTests
{
    #region MessageTypeShort Tests

    [TestMethod]
    public void MessageTypeShort_WithFullyQualifiedName_ReturnsClassName()
    {
        // Arrange
        var connection = CreateConnection(
            messageType: "MyNamespace.SubNamespace.OrderPlaced, MyAssembly, Version=1.0.0.0");

        // Act
        var shortName = connection.MessageTypeShort;

        // Assert
        Assert.AreEqual("OrderPlaced", shortName);
    }

    [TestMethod]
    public void MessageTypeShort_WithSimpleTypeName_ReturnsTypeName()
    {
        // Arrange
        var connection = CreateConnection(messageType: "OrderPlaced");

        // Act
        var shortName = connection.MessageTypeShort;

        // Assert
        Assert.AreEqual("OrderPlaced", shortName);
    }

    [TestMethod]
    public void MessageTypeShort_WithNamespaceOnly_ReturnsClassName()
    {
        // Arrange
        var connection = CreateConnection(messageType: "MyNamespace.OrderPlaced");

        // Act
        var shortName = connection.MessageTypeShort;

        // Assert
        Assert.AreEqual("OrderPlaced", shortName);
    }

    #endregion

    #region FailureRate Tests

    [TestMethod]
    public void FailureRate_WithNoMessages_ReturnsZero()
    {
        // Arrange
        var connection = CreateConnection(messageCount: 0, failureCount: 0);

        // Act
        var rate = connection.FailureRate;

        // Assert
        Assert.AreEqual(0, rate);
    }

    [TestMethod]
    public void FailureRate_WithNoFailures_ReturnsZero()
    {
        // Arrange
        var connection = CreateConnection(messageCount: 100, failureCount: 0);

        // Act
        var rate = connection.FailureRate;

        // Assert
        Assert.AreEqual(0, rate);
    }

    [TestMethod]
    public void FailureRate_CalculatesCorrectPercentage()
    {
        // Arrange - 10% failure rate
        var connection = CreateConnection(messageCount: 100, failureCount: 10);

        // Act
        var rate = connection.FailureRate;

        // Assert
        Assert.AreEqual(0.1, rate, 0.001);
    }

    #endregion

    #region Id Tests

    [TestMethod]
    public void Id_GeneratesCorrectFormat()
    {
        // Arrange
        var connection = CreateConnection(
            sourceEndpoint: "OrderService",
            targetEndpoint: "BillingService",
            messageType: "Namespace.OrderPlaced, Assembly");

        // Act
        var id = connection.Id;

        // Assert
        Assert.AreEqual("OrderService|Namespace.OrderPlaced, Assembly|BillingService", id);
    }

    [TestMethod]
    public void Id_IsConsistentForSameConnection()
    {
        // Arrange
        var connection = CreateConnection();

        // Act
        var id1 = connection.Id;
        var id2 = connection.Id;

        // Assert
        Assert.AreEqual(id1, id2);
    }

    [TestMethod]
    public void Id_DiffersForDifferentConnections()
    {
        // Arrange
        var connection1 = CreateConnection(sourceEndpoint: "A", targetEndpoint: "B");
        var connection2 = CreateConnection(sourceEndpoint: "A", targetEndpoint: "C");

        // Assert
        Assert.AreNotEqual(connection1.Id, connection2.Id);
    }

    #endregion

    #region Mutable Properties Tests

    [TestMethod]
    public void MessageCount_CanBeIncremented()
    {
        // Arrange
        var connection = CreateConnection(messageCount: 0);

        // Act
        connection.MessageCount++;

        // Assert
        Assert.AreEqual(1, connection.MessageCount);
    }

    [TestMethod]
    public void FailureCount_CanBeIncremented()
    {
        // Arrange
        var connection = CreateConnection(failureCount: 0);

        // Act
        connection.FailureCount = 5;

        // Assert
        Assert.AreEqual(5, connection.FailureCount);
    }

    [TestMethod]
    public void AverageLatencyMs_CanBeUpdated()
    {
        // Arrange
        var connection = CreateConnection();

        // Act
        connection.AverageLatencyMs = 25.5;

        // Assert
        Assert.AreEqual(25.5, connection.AverageLatencyMs, 0.01);
    }

    #endregion

    private static TopologyConnection CreateConnection(
        string sourceEndpoint = "SourceEndpoint",
        string targetEndpoint = "TargetEndpoint",
        string messageType = "Namespace.TestMessage, Assembly",
        long messageCount = 0,
        long failureCount = 0)
    {
        return new TopologyConnection
        {
            SourceEndpoint = sourceEndpoint,
            TargetEndpoint = targetEndpoint,
            MessageType = messageType,
            MessageCount = messageCount,
            FailureCount = failureCount,
            FirstSeen = DateTimeOffset.UtcNow,
            LastSeen = DateTimeOffset.UtcNow
        };
    }
}
