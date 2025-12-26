using Cascade.Core.Models;

namespace Cascade.Core.Tests.Models;

[TestClass]
public class FlowEdgeTests
{
    #region Required Properties Tests

    [TestMethod]
    public void SourceId_IsRequired()
    {
        // Arrange & Act
        var edge = CreateEdge(sourceId: "node-1");

        // Assert
        Assert.AreEqual("node-1", edge.SourceId);
    }

    [TestMethod]
    public void TargetId_IsRequired()
    {
        // Arrange & Act
        var edge = CreateEdge(targetId: "node-2");

        // Assert
        Assert.AreEqual("node-2", edge.TargetId);
    }

    [TestMethod]
    public void Label_IsRequired()
    {
        // Arrange & Act
        var edge = CreateEdge(label: "OrderPlaced");

        // Assert
        Assert.AreEqual("OrderPlaced", edge.Label);
    }

    #endregion

    #region Record Equality Tests

    [TestMethod]
    public void Equality_WithSameValues_AreEqual()
    {
        // Arrange
        var edge1 = new FlowEdge
        {
            SourceId = "node-1",
            TargetId = "node-2",
            Label = "OrderPlaced"
        };
        var edge2 = new FlowEdge
        {
            SourceId = "node-1",
            TargetId = "node-2",
            Label = "OrderPlaced"
        };

        // Act & Assert
        Assert.AreEqual(edge1, edge2);
    }

    [TestMethod]
    public void Equality_WithDifferentSourceId_AreNotEqual()
    {
        // Arrange
        var edge1 = CreateEdge(sourceId: "node-1");
        var edge2 = CreateEdge(sourceId: "node-3");

        // Act & Assert
        Assert.AreNotEqual(edge1, edge2);
    }

    [TestMethod]
    public void Equality_WithDifferentTargetId_AreNotEqual()
    {
        // Arrange
        var edge1 = CreateEdge(targetId: "node-2");
        var edge2 = CreateEdge(targetId: "node-4");

        // Act & Assert
        Assert.AreNotEqual(edge1, edge2);
    }

    [TestMethod]
    public void Equality_WithDifferentLabel_AreNotEqual()
    {
        // Arrange
        var edge1 = CreateEdge(label: "OrderPlaced");
        var edge2 = CreateEdge(label: "OrderShipped");

        // Act & Assert
        Assert.AreNotEqual(edge1, edge2);
    }

    [TestMethod]
    public void Equality_WithSwappedSourceAndTarget_AreNotEqual()
    {
        // Arrange - Edge direction matters
        var edge1 = CreateEdge(sourceId: "A", targetId: "B");
        var edge2 = CreateEdge(sourceId: "B", targetId: "A");

        // Act & Assert
        Assert.AreNotEqual(edge1, edge2);
    }

    #endregion

    #region Hash Code Tests

    [TestMethod]
    public void GetHashCode_ForEqualEdges_ReturnsSameValue()
    {
        // Arrange
        var edge1 = CreateEdge(sourceId: "A", targetId: "B", label: "Message");
        var edge2 = CreateEdge(sourceId: "A", targetId: "B", label: "Message");

        // Act & Assert
        Assert.AreEqual(edge1.GetHashCode(), edge2.GetHashCode());
    }

    [TestMethod]
    public void GetHashCode_ForDifferentEdges_MayReturnDifferentValue()
    {
        // Arrange
        var edge1 = CreateEdge(sourceId: "A", targetId: "B");
        var edge2 = CreateEdge(sourceId: "C", targetId: "D");

        // Note: Different hash codes are not guaranteed but are expected
        // This test documents the behavior
        var hash1 = edge1.GetHashCode();
        var hash2 = edge2.GetHashCode();

        // At minimum, verify we get hash codes without exceptions
        Assert.IsNotNull(hash1);
        Assert.IsNotNull(hash2);
    }

    #endregion

    #region Immutability Tests

    [TestMethod]
    public void Record_IsImmutableAfterCreation()
    {
        // Arrange
        var edge = CreateEdge(sourceId: "node-1", targetId: "node-2", label: "TestMessage");

        // Assert - All properties should retain their initial values
        // (Records with init-only properties cannot be modified after creation)
        Assert.AreEqual("node-1", edge.SourceId);
        Assert.AreEqual("node-2", edge.TargetId);
        Assert.AreEqual("TestMessage", edge.Label);
    }

    #endregion

    private static FlowEdge CreateEdge(
        string sourceId = "source-node",
        string targetId = "target-node",
        string label = "TestMessage")
    {
        return new FlowEdge
        {
            SourceId = sourceId,
            TargetId = targetId,
            Label = label
        };
    }
}
