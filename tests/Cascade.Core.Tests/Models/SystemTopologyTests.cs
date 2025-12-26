using Cascade.Core.Models;

namespace Cascade.Core.Tests.Models;

[TestClass]
public class SystemTopologyTests
{
    #region EndpointCount Tests

    [TestMethod]
    public void EndpointCount_WithNoEndpoints_ReturnsZero()
    {
        // Arrange
        var topology = CreateTopology();

        // Assert
        Assert.AreEqual(0, topology.EndpointCount);
    }

    [TestMethod]
    public void EndpointCount_WithMultipleEndpoints_ReturnsCorrectCount()
    {
        // Arrange
        var topology = CreateTopology();
        topology.Endpoints["OrderService"] = CreateEndpoint("OrderService");
        topology.Endpoints["BillingService"] = CreateEndpoint("BillingService");
        topology.Endpoints["ShippingService"] = CreateEndpoint("ShippingService");

        // Assert
        Assert.AreEqual(3, topology.EndpointCount);
    }

    #endregion

    #region MessageTypeCount Tests

    [TestMethod]
    public void MessageTypeCount_WithNoMessageTypes_ReturnsZero()
    {
        // Arrange
        var topology = CreateTopology();

        // Assert
        Assert.AreEqual(0, topology.MessageTypeCount);
    }

    [TestMethod]
    public void MessageTypeCount_WithMultipleMessageTypes_ReturnsCorrectCount()
    {
        // Arrange
        var topology = CreateTopology();
        topology.MessageTypes["OrderPlaced"] = CreateMessageType("Namespace.OrderPlaced, Assembly");
        topology.MessageTypes["OrderShipped"] = CreateMessageType("Namespace.OrderShipped, Assembly");

        // Assert
        Assert.AreEqual(2, topology.MessageTypeCount);
    }

    #endregion

    #region ConnectionCount Tests

    [TestMethod]
    public void ConnectionCount_WithNoConnections_ReturnsZero()
    {
        // Arrange
        var topology = CreateTopology();

        // Assert
        Assert.AreEqual(0, topology.ConnectionCount);
    }

    [TestMethod]
    public void ConnectionCount_WithMultipleConnections_ReturnsCorrectCount()
    {
        // Arrange
        var topology = CreateTopology();
        topology.Connections.Add(CreateConnection("A", "B"));
        topology.Connections.Add(CreateConnection("B", "C"));
        topology.Connections.Add(CreateConnection("A", "C"));

        // Assert
        Assert.AreEqual(3, topology.ConnectionCount);
    }

    #endregion

    #region Mutable Properties Tests

    [TestMethod]
    public void TotalMessagesObserved_CanBeIncremented()
    {
        // Arrange
        var topology = CreateTopology();

        // Act
        topology.TotalMessagesObserved = 100;
        topology.TotalMessagesObserved++;

        // Assert
        Assert.AreEqual(101, topology.TotalMessagesObserved);
    }

    [TestMethod]
    public void FirstObserved_CanBeSet()
    {
        // Arrange
        var topology = CreateTopology();
        var timestamp = DateTimeOffset.UtcNow.AddDays(-7);

        // Act
        topology.FirstObserved = timestamp;

        // Assert
        Assert.AreEqual(timestamp, topology.FirstObserved);
    }

    [TestMethod]
    public void LastUpdated_CanBeUpdated()
    {
        // Arrange
        var topology = CreateTopology();
        var initialTime = DateTimeOffset.UtcNow.AddHours(-1);
        var updatedTime = DateTimeOffset.UtcNow;
        topology.LastUpdated = initialTime;

        // Act
        topology.LastUpdated = updatedTime;

        // Assert
        Assert.AreEqual(updatedTime, topology.LastUpdated);
    }

    #endregion

    #region Collection Initialization Tests

    [TestMethod]
    public void Endpoints_DefaultsToEmptyDictionary()
    {
        // Arrange
        var topology = new SystemTopology();

        // Assert
        Assert.IsNotNull(topology.Endpoints);
        Assert.AreEqual(0, topology.Endpoints.Count);
    }

    [TestMethod]
    public void MessageTypes_DefaultsToEmptyDictionary()
    {
        // Arrange
        var topology = new SystemTopology();

        // Assert
        Assert.IsNotNull(topology.MessageTypes);
        Assert.AreEqual(0, topology.MessageTypes.Count);
    }

    [TestMethod]
    public void Connections_DefaultsToEmptyList()
    {
        // Arrange
        var topology = new SystemTopology();

        // Assert
        Assert.IsNotNull(topology.Connections);
        Assert.AreEqual(0, topology.Connections.Count);
    }

    #endregion

    #region Collection Modification Tests

    [TestMethod]
    public void Endpoints_CanAddAndRetrieve()
    {
        // Arrange
        var topology = CreateTopology();
        var endpoint = CreateEndpoint("TestService");

        // Act
        topology.Endpoints["TestService"] = endpoint;

        // Assert
        Assert.IsTrue(topology.Endpoints.ContainsKey("TestService"));
        Assert.AreEqual(endpoint, topology.Endpoints["TestService"]);
    }

    [TestMethod]
    public void MessageTypes_CanAddAndRetrieve()
    {
        // Arrange
        var topology = CreateTopology();
        var messageType = CreateMessageType("Namespace.TestMessage, Assembly");

        // Act
        topology.MessageTypes["Namespace.TestMessage, Assembly"] = messageType;

        // Assert
        Assert.IsTrue(topology.MessageTypes.ContainsKey("Namespace.TestMessage, Assembly"));
        Assert.AreEqual(messageType, topology.MessageTypes["Namespace.TestMessage, Assembly"]);
    }

    [TestMethod]
    public void Connections_CanAddAndRetrieve()
    {
        // Arrange
        var topology = CreateTopology();
        var connection = CreateConnection("Source", "Target");

        // Act
        topology.Connections.Add(connection);

        // Assert
        Assert.AreEqual(1, topology.Connections.Count);
        Assert.AreEqual(connection, topology.Connections[0]);
    }

    #endregion

    private static SystemTopology CreateTopology()
    {
        return new SystemTopology
        {
            FirstObserved = DateTimeOffset.UtcNow,
            LastUpdated = DateTimeOffset.UtcNow
        };
    }

    private static TopologyEndpoint CreateEndpoint(string name)
    {
        return new TopologyEndpoint
        {
            Name = name,
            FirstSeen = DateTimeOffset.UtcNow,
            LastSeen = DateTimeOffset.UtcNow
        };
    }

    private static TopologyMessageType CreateMessageType(string fullName)
    {
        return new TopologyMessageType
        {
            FullName = fullName,
            FirstSeen = DateTimeOffset.UtcNow,
            LastSeen = DateTimeOffset.UtcNow
        };
    }

    private static TopologyConnection CreateConnection(
        string source,
        string target,
        string messageType = "Namespace.TestMessage, Assembly")
    {
        return new TopologyConnection
        {
            SourceEndpoint = source,
            TargetEndpoint = target,
            MessageType = messageType,
            FirstSeen = DateTimeOffset.UtcNow,
            LastSeen = DateTimeOffset.UtcNow
        };
    }
}
