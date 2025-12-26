using Cascade.Core.Enums;
using Cascade.Core.Models;

namespace Cascade.Core.Tests.Models;

[TestClass]
public class MessageFlowTests
{
    #region MessageCount Tests

    [TestMethod]
    public void MessageCount_WithNoMessages_ReturnsZero()
    {
        // Arrange
        var flow = CreateFlow();

        // Assert
        Assert.AreEqual(0, flow.MessageCount);
    }

    [TestMethod]
    public void MessageCount_WithMessages_ReturnsCorrectCount()
    {
        // Arrange
        var flow = CreateFlow(messages:
        [
            CreateTelemetry("msg-1"),
            CreateTelemetry("msg-2"),
            CreateTelemetry("msg-3")
        ]);

        // Assert
        Assert.AreEqual(3, flow.MessageCount);
    }

    #endregion

    #region Duration Tests

    [TestMethod]
    public void Duration_WhenNotCompleted_ReturnsElapsedTime()
    {
        // Arrange
        var startTime = DateTimeOffset.UtcNow.AddMinutes(-5);
        var flow = CreateFlow(startedAt: startTime, completedAt: null);

        // Act
        var duration = flow.Duration;

        // Assert
        Assert.IsTrue(duration.TotalMinutes >= 5);
        Assert.IsTrue(duration.TotalMinutes < 6); // Allow some tolerance
    }

    [TestMethod]
    public void Duration_WhenCompleted_ReturnsExactDuration()
    {
        // Arrange
        var startTime = DateTimeOffset.UtcNow.AddMinutes(-10);
        var endTime = DateTimeOffset.UtcNow.AddMinutes(-5);
        var flow = CreateFlow(startedAt: startTime, completedAt: endTime);

        // Act
        var duration = flow.Duration;

        // Assert
        Assert.AreEqual(5, duration.TotalMinutes, 0.1);
    }

    #endregion

    #region HasFailures Tests

    [TestMethod]
    public void HasFailures_WithNoFailures_ReturnsFalse()
    {
        // Arrange
        var flow = CreateFlow(messages:
        [
            CreateTelemetry("msg-1", success: true),
            CreateTelemetry("msg-2", success: true)
        ]);

        // Assert
        Assert.IsFalse(flow.HasFailures);
    }

    [TestMethod]
    public void HasFailures_WithOneFailure_ReturnsTrue()
    {
        // Arrange
        var flow = CreateFlow(messages:
        [
            CreateTelemetry("msg-1", success: true),
            CreateTelemetry("msg-2", success: false)
        ]);

        // Assert
        Assert.IsTrue(flow.HasFailures);
    }

    [TestMethod]
    public void HasFailures_WithAllFailures_ReturnsTrue()
    {
        // Arrange
        var flow = CreateFlow(messages:
        [
            CreateTelemetry("msg-1", success: false),
            CreateTelemetry("msg-2", success: false)
        ]);

        // Assert
        Assert.IsTrue(flow.HasFailures);
    }

    [TestMethod]
    public void HasFailures_WithNullSuccess_ReturnsFalse()
    {
        // Arrange - Success = null means not yet determined (in-flight message)
        var flow = CreateFlow(messages:
        [
            CreateTelemetry("msg-1", success: null)
        ]);

        // Assert
        Assert.IsFalse(flow.HasFailures);
    }

    [TestMethod]
    public void HasFailures_WithEmptyMessages_ReturnsFalse()
    {
        // Arrange
        var flow = CreateFlow();

        // Assert
        Assert.IsFalse(flow.HasFailures);
    }

    #endregion

    #region Status Tests

    [TestMethod]
    public void Status_DefaultsToInProgress()
    {
        // Arrange
        var flow = CreateFlow();

        // Assert
        Assert.AreEqual(FlowStatus.InProgress, flow.Status);
    }

    [TestMethod]
    public void Status_CanBeSetToCompleted()
    {
        // Arrange
        var flow = CreateFlow(status: FlowStatus.Completed);

        // Assert
        Assert.AreEqual(FlowStatus.Completed, flow.Status);
    }

    [TestMethod]
    public void Status_CanBeSetToFailed()
    {
        // Arrange
        var flow = CreateFlow(status: FlowStatus.Failed);

        // Assert
        Assert.AreEqual(FlowStatus.Failed, flow.Status);
    }

    [TestMethod]
    public void Status_CanBeSetToTimedOut()
    {
        // Arrange
        var flow = CreateFlow(status: FlowStatus.TimedOut);

        // Assert
        Assert.AreEqual(FlowStatus.TimedOut, flow.Status);
    }

    #endregion

    #region Record Behavior Tests

    [TestMethod]
    public void Messages_ListIsMutable()
    {
        // Arrange
        var flow = CreateFlow();

        // Act
        flow.Messages.Add(CreateTelemetry("new-msg"));

        // Assert
        Assert.AreEqual(1, flow.MessageCount);
    }

    [TestMethod]
    public void CompletedAt_CanBeUpdated()
    {
        // Arrange
        var flow = CreateFlow();
        var completedAt = DateTimeOffset.UtcNow;

        // Act
        flow.CompletedAt = completedAt;

        // Assert
        Assert.AreEqual(completedAt, flow.CompletedAt);
    }

    #endregion

    private static MessageFlow CreateFlow(
        string correlationId = "test-correlation",
        DateTimeOffset? startedAt = null,
        DateTimeOffset? completedAt = null,
        FlowStatus status = FlowStatus.InProgress,
        List<MessageTelemetry>? messages = null)
    {
        return new MessageFlow
        {
            CorrelationId = correlationId,
            StartedAt = startedAt ?? DateTimeOffset.UtcNow,
            CompletedAt = completedAt,
            Status = status,
            Messages = messages ?? []
        };
    }

    private static MessageTelemetry CreateTelemetry(
        string messageId,
        bool? success = true)
    {
        return new MessageTelemetry
        {
            Id = $"telemetry-{messageId}",
            MessageId = messageId,
            MessageType = "Namespace.TestMessage, Assembly",
            EndpointName = "TestEndpoint",
            HostId = "test-host",
            Direction = MessageDirection.Incoming,
            Timestamp = DateTimeOffset.UtcNow,
            Success = success
        };
    }
}
