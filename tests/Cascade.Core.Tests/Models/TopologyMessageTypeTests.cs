using Cascade.Core.Models;

namespace Cascade.Core.Tests.Models;

[TestClass]
public class TopologyMessageTypeTests
{
    #region ShortName Tests

    [TestMethod]
    public void ShortName_WithFullyQualifiedName_ReturnsClassName()
    {
        // Arrange
        var messageType = CreateMessageType(
            fullName: "MyNamespace.SubNamespace.OrderPlaced, MyAssembly, Version=1.0.0.0");

        // Act
        var shortName = messageType.ShortName;

        // Assert
        Assert.AreEqual("OrderPlaced", shortName);
    }

    [TestMethod]
    public void ShortName_WithSimpleTypeName_ReturnsTypeName()
    {
        // Arrange
        var messageType = CreateMessageType(fullName: "OrderPlaced");

        // Act
        var shortName = messageType.ShortName;

        // Assert
        Assert.AreEqual("OrderPlaced", shortName);
    }

    [TestMethod]
    public void ShortName_WithNamespaceOnly_ReturnsClassName()
    {
        // Arrange
        var messageType = CreateMessageType(fullName: "MyNamespace.OrderPlaced");

        // Act
        var shortName = messageType.ShortName;

        // Assert
        Assert.AreEqual("OrderPlaced", shortName);
    }

    [TestMethod]
    public void ShortName_WithAssemblyQualifiedName_IgnoresAssemblyPart()
    {
        // Arrange
        var messageType = CreateMessageType(fullName: "Namespace.ClassName, AssemblyName");

        // Act
        var shortName = messageType.ShortName;

        // Assert
        Assert.AreEqual("ClassName", shortName);
    }

    [TestMethod]
    public void ShortName_WithDeeplyNestedNamespace_ReturnsOnlyClassName()
    {
        // Arrange
        var messageType = CreateMessageType(
            fullName: "Company.Product.Module.Feature.OrderPlaced, Company.Product, Version=1.0.0.0");

        // Act
        var shortName = messageType.ShortName;

        // Assert
        Assert.AreEqual("OrderPlaced", shortName);
    }

    #endregion

    #region Mutable Properties Tests

    [TestMethod]
    public void TimesObserved_CanBeIncremented()
    {
        // Arrange
        var messageType = CreateMessageType();

        // Act
        messageType.TimesObserved++;
        messageType.TimesObserved++;
        messageType.TimesObserved++;

        // Assert
        Assert.AreEqual(3, messageType.TimesObserved);
    }

    [TestMethod]
    public void FirstSeen_CanBeSet()
    {
        // Arrange
        var messageType = CreateMessageType();
        var timestamp = DateTimeOffset.UtcNow.AddDays(-7);

        // Act
        messageType.FirstSeen = timestamp;

        // Assert
        Assert.AreEqual(timestamp, messageType.FirstSeen);
    }

    [TestMethod]
    public void LastSeen_CanBeUpdated()
    {
        // Arrange
        var messageType = CreateMessageType();
        var initialTime = DateTimeOffset.UtcNow.AddHours(-1);
        var updatedTime = DateTimeOffset.UtcNow;
        messageType.LastSeen = initialTime;

        // Act
        messageType.LastSeen = updatedTime;

        // Assert
        Assert.AreEqual(updatedTime, messageType.LastSeen);
    }

    #endregion

    #region Record Behavior Tests

    [TestMethod]
    public void RecordEquality_WithSameFullName_AreEqual()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;
        var messageType1 = new TopologyMessageType
        {
            FullName = "Namespace.TestMessage, Assembly",
            FirstSeen = timestamp,
            LastSeen = timestamp,
            TimesObserved = 10
        };
        var messageType2 = new TopologyMessageType
        {
            FullName = "Namespace.TestMessage, Assembly",
            FirstSeen = timestamp,
            LastSeen = timestamp,
            TimesObserved = 10
        };

        // Act & Assert
        Assert.AreEqual(messageType1, messageType2);
    }

    [TestMethod]
    public void RecordEquality_WithDifferentTimesObserved_AreNotEqual()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;
        var messageType1 = new TopologyMessageType
        {
            FullName = "Namespace.TestMessage, Assembly",
            FirstSeen = timestamp,
            LastSeen = timestamp,
            TimesObserved = 10
        };
        var messageType2 = new TopologyMessageType
        {
            FullName = "Namespace.TestMessage, Assembly",
            FirstSeen = timestamp,
            LastSeen = timestamp,
            TimesObserved = 20
        };

        // Act & Assert
        Assert.AreNotEqual(messageType1, messageType2);
    }

    #endregion

    private static TopologyMessageType CreateMessageType(
        string fullName = "Namespace.TestMessage, Assembly",
        long timesObserved = 0)
    {
        return new TopologyMessageType
        {
            FullName = fullName,
            FirstSeen = DateTimeOffset.UtcNow,
            LastSeen = DateTimeOffset.UtcNow,
            TimesObserved = timesObserved
        };
    }
}
