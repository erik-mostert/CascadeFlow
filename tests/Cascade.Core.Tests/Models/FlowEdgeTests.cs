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
