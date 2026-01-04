using Cascade.Core.Models;

namespace Cascade.Core.Tests.Models;

[TestClass]
public class FlowNodeTests
{
    #region Required Properties Tests

    [TestMethod]
    public void Id_IsRequired()
    {
        // Arrange & Act
        var node = CreateNode(id: "node-123");

        // Assert
        Assert.AreEqual("node-123", node.Id);
    }

    [TestMethod]
    public void Label_IsRequired()
    {
        // Arrange & Act
        var node = CreateNode(label: "OrderService");

        // Assert
        Assert.AreEqual("OrderService", node.Label);
    }

    [TestMethod]
    public void MessageType_IsRequired()
    {
        // Arrange & Act
        var node = CreateNode(messageType: "Namespace.OrderPlaced, Assembly");

        // Assert
        Assert.AreEqual("Namespace.OrderPlaced, Assembly", node.MessageType);
    }

    [TestMethod]
    public void Timestamp_IsRequired()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        var node = CreateNode(timestamp: timestamp);

        // Assert
        Assert.AreEqual(timestamp, node.Timestamp);
    }

    [TestMethod]
    public void Success_IsRequired()
    {
        // Arrange & Act
        var successNode = CreateNode(success: true);
        var failureNode = CreateNode(success: false);

        // Assert
        Assert.IsTrue(successNode.Success);
        Assert.IsFalse(failureNode.Success);
    }

    #endregion

    #region Optional Properties Tests

    [TestMethod]
    public void Duration_WhenNotSet_IsNull()
    {
        // Arrange & Act
        var node = CreateNode(duration: null);

        // Assert
        Assert.IsNull(node.Duration);
    }

    [TestMethod]
    public void Duration_WhenSet_ReturnsValue()
    {
        // Arrange
        var duration = TimeSpan.FromMilliseconds(250);

        // Act
        var node = CreateNode(duration: duration);

        // Assert
        Assert.AreEqual(duration, node.Duration);
    }

    #endregion

    private static FlowNode CreateNode(
        string id = "test-node",
        string label = "TestEndpoint",
        string messageType = "Namespace.TestMessage, Assembly",
        DateTimeOffset? timestamp = null,
        bool success = true,
        TimeSpan? duration = null)
    {
        return new FlowNode
        {
            Id = id,
            Label = label,
            MessageType = messageType,
            Timestamp = timestamp ?? DateTimeOffset.UtcNow,
            Success = success,
            Duration = duration
        };
    }
}
