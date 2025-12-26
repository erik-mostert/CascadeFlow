using Cascade.Core.Enums;
using Cascade.Core.Models;

namespace Cascade.Core.Tests.Models;

[TestClass]
public class MessageTelemetryTests
{
    #region MessageTypeShort Tests

    [TestMethod]
    public void MessageTypeShort_WithFullyQualifiedName_ReturnsClassName()
    {
        // Arrange
        var telemetry = CreateTelemetry(messageType: "MyNamespace.SubNamespace.OrderPlaced, MyAssembly, Version=1.0.0.0");

        // Act
        var shortName = telemetry.MessageTypeShort;

        // Assert
        Assert.AreEqual("OrderPlaced", shortName);
    }

    [TestMethod]
    public void MessageTypeShort_WithSimpleTypeName_ReturnsTypeName()
    {
        // Arrange
        var telemetry = CreateTelemetry(messageType: "OrderPlaced");

        // Act
        var shortName = telemetry.MessageTypeShort;

        // Assert
        Assert.AreEqual("OrderPlaced", shortName);
    }

    [TestMethod]
    public void MessageTypeShort_WithNamespaceOnly_ReturnsClassName()
    {
        // Arrange
        var telemetry = CreateTelemetry(messageType: "MyNamespace.OrderPlaced");

        // Act
        var shortName = telemetry.MessageTypeShort;

        // Assert
        Assert.AreEqual("OrderPlaced", shortName);
    }

    [TestMethod]
    public void MessageTypeShort_WithAssemblyQualifiedName_IgnoresAssemblyPart()
    {
        // Arrange
        var telemetry = CreateTelemetry(messageType: "Namespace.ClassName, AssemblyName");

        // Act
        var shortName = telemetry.MessageTypeShort;

        // Assert
        Assert.AreEqual("ClassName", shortName);
    }

    #endregion

    #region Record Equality Tests

    [TestMethod]
    public void Equality_WithSameValues_AreEqual()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;
        var telemetry1 = CreateTelemetry(
            id: "id-1",
            messageId: "msg-1",
            messageType: "Type",
            endpointName: "Endpoint",
            hostId: "host",
            timestamp: timestamp);
        var telemetry2 = CreateTelemetry(
            id: "id-1",
            messageId: "msg-1",
            messageType: "Type",
            endpointName: "Endpoint",
            hostId: "host",
            timestamp: timestamp);

        // Act & Assert
        Assert.AreEqual(telemetry1, telemetry2);
    }

    [TestMethod]
    public void Equality_WithDifferentId_AreNotEqual()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;
        var telemetry1 = CreateTelemetry(id: "id-1", timestamp: timestamp);
        var telemetry2 = CreateTelemetry(id: "id-2", timestamp: timestamp);

        // Act & Assert
        Assert.AreNotEqual(telemetry1, telemetry2);
    }

    #endregion

    #region Optional Properties Tests

    [TestMethod]
    public void CorrelationId_WhenNull_RemainsNull()
    {
        // Arrange
        var telemetry = CreateTelemetry(correlationId: null);

        // Assert
        Assert.IsNull(telemetry.CorrelationId);
    }

    [TestMethod]
    public void ProcessingDuration_WhenSet_ReturnsValue()
    {
        // Arrange
        var duration = TimeSpan.FromMilliseconds(150);
        var telemetry = CreateTelemetry(processingDuration: duration);

        // Assert
        Assert.AreEqual(duration, telemetry.ProcessingDuration);
    }

    [TestMethod]
    public void Success_WhenFalse_IndicatesFailure()
    {
        // Arrange
        var telemetry = CreateTelemetry(success: false, exceptionType: "System.Exception");

        // Assert
        Assert.IsFalse(telemetry.Success);
        Assert.AreEqual("System.Exception", telemetry.ExceptionType);
    }

    #endregion

    #region Intent Tests

    [TestMethod]
    public void Intent_DefaultsToUnknown()
    {
        // Arrange
        var telemetry = CreateTelemetry();

        // Assert
        Assert.AreEqual(MessageIntent.Unknown, telemetry.Intent);
    }

    [TestMethod]
    public void Intent_CanBeSetToPublish()
    {
        // Arrange
        var telemetry = CreateTelemetry(intent: MessageIntent.Publish);

        // Assert
        Assert.AreEqual(MessageIntent.Publish, telemetry.Intent);
    }

    #endregion

    private static MessageTelemetry CreateTelemetry(
        string id = "test-id",
        string messageId = "msg-id",
        string? correlationId = null,
        string messageType = "Namespace.TestMessage, Assembly",
        string endpointName = "TestEndpoint",
        string hostId = "test-host",
        MessageDirection direction = MessageDirection.Incoming,
        DateTimeOffset? timestamp = null,
        TimeSpan? processingDuration = null,
        bool? success = true,
        string? exceptionType = null,
        MessageIntent intent = MessageIntent.Unknown)
    {
        return new MessageTelemetry
        {
            Id = id,
            MessageId = messageId,
            CorrelationId = correlationId,
            MessageType = messageType,
            EndpointName = endpointName,
            HostId = hostId,
            Direction = direction,
            Timestamp = timestamp ?? DateTimeOffset.UtcNow,
            ProcessingDuration = processingDuration,
            Success = success,
            ExceptionType = exceptionType,
            Intent = intent
        };
    }
}
