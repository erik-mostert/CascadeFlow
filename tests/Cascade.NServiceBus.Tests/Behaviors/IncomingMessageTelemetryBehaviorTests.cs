using Cascade.Core.Enums;
using Cascade.Core.Models;
using Cascade.NServiceBus.Behaviors;
using Cascade.NServiceBus.Dispatchers;
using Moq;
using NServiceBus.Pipeline;
using CascadeMessageIntent = Cascade.Core.Enums.MessageIntent;

namespace Cascade.NServiceBus.Tests.Behaviors;

[TestClass]
public class IncomingMessageTelemetryBehaviorTests
{
    private Mock<ITelemetryDispatcher> _dispatcherMock = null!;
    private CascadeOptions _options = null!;
    private IncomingMessageTelemetryBehavior _behavior = null!;

    [TestInitialize]
    public void Setup()
    {
        _dispatcherMock = new Mock<ITelemetryDispatcher>();
        _options = new CascadeOptions
        {
            EndpointName = "TestEndpoint",
            HostId = "test-host",
            IncludeHeaders = true
        };
        _behavior = new IncomingMessageTelemetryBehavior(_dispatcherMock.Object, _options);
    }

    #region Success Path Tests

    [TestMethod]
    public async Task Invoke_OnSuccess_DispatchesTelemetryWithSuccessTrue()
    {
        // Arrange
        var context = CreateMockContext();
        MessageTelemetry? capturedTelemetry = null;
        _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<MessageTelemetry>(), It.IsAny<CancellationToken>()))
            .Callback<MessageTelemetry, CancellationToken>((t, _) => capturedTelemetry = t)
            .Returns(Task.CompletedTask);

        // Act
        await _behavior.Invoke(context.Object, () => Task.CompletedTask);

        // Assert
        Assert.IsNotNull(capturedTelemetry);
        Assert.IsTrue(capturedTelemetry.Success);
        Assert.IsNull(capturedTelemetry.ExceptionType);
        Assert.IsNull(capturedTelemetry.ExceptionMessage);
    }

    [TestMethod]
    public async Task Invoke_CapturesProcessingDuration()
    {
        // Arrange
        var context = CreateMockContext();
        MessageTelemetry? capturedTelemetry = null;
        _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<MessageTelemetry>(), It.IsAny<CancellationToken>()))
            .Callback<MessageTelemetry, CancellationToken>((t, _) => capturedTelemetry = t)
            .Returns(Task.CompletedTask);

        // Act
        await _behavior.Invoke(context.Object, async () => await Task.Delay(50));

        // Assert
        Assert.IsNotNull(capturedTelemetry);
        Assert.IsTrue(capturedTelemetry.ProcessingDuration?.TotalMilliseconds >= 50);
    }

    [TestMethod]
    public async Task Invoke_SetsDirectionToIncoming()
    {
        // Arrange
        var context = CreateMockContext();
        MessageTelemetry? capturedTelemetry = null;
        _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<MessageTelemetry>(), It.IsAny<CancellationToken>()))
            .Callback<MessageTelemetry, CancellationToken>((t, _) => capturedTelemetry = t)
            .Returns(Task.CompletedTask);

        // Act
        await _behavior.Invoke(context.Object, () => Task.CompletedTask);

        // Assert
        Assert.IsNotNull(capturedTelemetry);
        Assert.AreEqual(MessageDirection.Incoming, capturedTelemetry.Direction);
    }

    #endregion

    #region Exception Handling Tests

    [TestMethod]
    public async Task Invoke_OnException_DispatchesTelemetryWithSuccessFalse()
    {
        // Arrange
        var context = CreateMockContext();
        MessageTelemetry? capturedTelemetry = null;
        _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<MessageTelemetry>(), It.IsAny<CancellationToken>()))
            .Callback<MessageTelemetry, CancellationToken>((t, _) => capturedTelemetry = t)
            .Returns(Task.CompletedTask);

        // Act & Assert
        try
        {
            await _behavior.Invoke(context.Object, () => throw new InvalidOperationException("Test error"));
            Assert.Fail("Expected InvalidOperationException to be thrown");
        }
        catch (InvalidOperationException)
        {
            // Expected
        }

        Assert.IsNotNull(capturedTelemetry);
        Assert.IsFalse(capturedTelemetry.Success);
    }

    [TestMethod]
    public async Task Invoke_OnException_CapturesExceptionDetails()
    {
        // Arrange
        var context = CreateMockContext();
        MessageTelemetry? capturedTelemetry = null;
        _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<MessageTelemetry>(), It.IsAny<CancellationToken>()))
            .Callback<MessageTelemetry, CancellationToken>((t, _) => capturedTelemetry = t)
            .Returns(Task.CompletedTask);

        // Act
        try
        {
            await _behavior.Invoke(context.Object, () => throw new ArgumentException("Invalid argument"));
        }
        catch (ArgumentException)
        {
            // Expected
        }

        // Assert
        Assert.IsNotNull(capturedTelemetry);
        Assert.AreEqual("System.ArgumentException", capturedTelemetry.ExceptionType);
        Assert.AreEqual("Invalid argument", capturedTelemetry.ExceptionMessage);
    }

    [TestMethod]
    public async Task Invoke_OnException_RethrowsException()
    {
        // Arrange
        var context = CreateMockContext();
        var expectedException = new InvalidOperationException("Original exception");

        // Act & Assert
        try
        {
            await _behavior.Invoke(context.Object, () => throw expectedException);
            Assert.Fail("Expected InvalidOperationException to be thrown");
        }
        catch (InvalidOperationException thrownException)
        {
            Assert.AreSame(expectedException, thrownException);
        }
    }

    #endregion

    #region Header Extraction Tests

    [TestMethod]
    public async Task Invoke_ExtractsMessageId()
    {
        // Arrange
        var context = CreateMockContext(messageId: "unique-message-id");
        MessageTelemetry? capturedTelemetry = null;
        _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<MessageTelemetry>(), It.IsAny<CancellationToken>()))
            .Callback<MessageTelemetry, CancellationToken>((t, _) => capturedTelemetry = t)
            .Returns(Task.CompletedTask);

        // Act
        await _behavior.Invoke(context.Object, () => Task.CompletedTask);

        // Assert
        Assert.IsNotNull(capturedTelemetry);
        Assert.AreEqual("unique-message-id", capturedTelemetry.MessageId);
    }

    [TestMethod]
    public async Task Invoke_ExtractsCorrelationId()
    {
        // Arrange
        var headers = new Dictionary<string, string>
        {
            { "NServiceBus.CorrelationId", "corr-123" }
        };
        var context = CreateMockContext(headers: headers);
        MessageTelemetry? capturedTelemetry = null;
        _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<MessageTelemetry>(), It.IsAny<CancellationToken>()))
            .Callback<MessageTelemetry, CancellationToken>((t, _) => capturedTelemetry = t)
            .Returns(Task.CompletedTask);

        // Act
        await _behavior.Invoke(context.Object, () => Task.CompletedTask);

        // Assert
        Assert.IsNotNull(capturedTelemetry);
        Assert.AreEqual("corr-123", capturedTelemetry.CorrelationId);
    }

    [TestMethod]
    public async Task Invoke_ExtractsMessageType()
    {
        // Arrange
        var headers = new Dictionary<string, string>
        {
            { "NServiceBus.EnclosedMessageTypes", "MyNamespace.OrderPlaced, MyAssembly" }
        };
        var context = CreateMockContext(headers: headers);
        MessageTelemetry? capturedTelemetry = null;
        _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<MessageTelemetry>(), It.IsAny<CancellationToken>()))
            .Callback<MessageTelemetry, CancellationToken>((t, _) => capturedTelemetry = t)
            .Returns(Task.CompletedTask);

        // Act
        await _behavior.Invoke(context.Object, () => Task.CompletedTask);

        // Assert
        Assert.IsNotNull(capturedTelemetry);
        Assert.AreEqual("MyNamespace.OrderPlaced, MyAssembly", capturedTelemetry.MessageType);
    }

    [TestMethod]
    public async Task Invoke_ExtractsRetryCount()
    {
        // Arrange
        var headers = new Dictionary<string, string>
        {
            { "NServiceBus.Retries", "3" }
        };
        var context = CreateMockContext(headers: headers);
        MessageTelemetry? capturedTelemetry = null;
        _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<MessageTelemetry>(), It.IsAny<CancellationToken>()))
            .Callback<MessageTelemetry, CancellationToken>((t, _) => capturedTelemetry = t)
            .Returns(Task.CompletedTask);

        // Act
        await _behavior.Invoke(context.Object, () => Task.CompletedTask);

        // Assert
        Assert.IsNotNull(capturedTelemetry);
        Assert.AreEqual(3, capturedTelemetry.RetryCount);
    }

    [TestMethod]
    public async Task Invoke_ExtractsSagaInfo()
    {
        // Arrange
        var headers = new Dictionary<string, string>
        {
            { "NServiceBus.SagaId", "saga-456" },
            { "NServiceBus.SagaType", "OrderSaga" }
        };
        var context = CreateMockContext(headers: headers);
        MessageTelemetry? capturedTelemetry = null;
        _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<MessageTelemetry>(), It.IsAny<CancellationToken>()))
            .Callback<MessageTelemetry, CancellationToken>((t, _) => capturedTelemetry = t)
            .Returns(Task.CompletedTask);

        // Act
        await _behavior.Invoke(context.Object, () => Task.CompletedTask);

        // Assert
        Assert.IsNotNull(capturedTelemetry);
        Assert.AreEqual("saga-456", capturedTelemetry.SagaId);
        Assert.AreEqual("OrderSaga", capturedTelemetry.SagaType);
    }

    #endregion

    #region Message Intent Tests

    [TestMethod]
    public async Task Invoke_ParsesSendIntent()
    {
        // Arrange
        var headers = new Dictionary<string, string>
        {
            { "NServiceBus.MessageIntent", "Send" }
        };
        var context = CreateMockContext(headers: headers);
        MessageTelemetry? capturedTelemetry = null;
        _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<MessageTelemetry>(), It.IsAny<CancellationToken>()))
            .Callback<MessageTelemetry, CancellationToken>((t, _) => capturedTelemetry = t)
            .Returns(Task.CompletedTask);

        // Act
        await _behavior.Invoke(context.Object, () => Task.CompletedTask);

        // Assert
        Assert.IsNotNull(capturedTelemetry);
        Assert.AreEqual(CascadeMessageIntent.Send, capturedTelemetry.Intent);
    }

    [TestMethod]
    public async Task Invoke_ParsesPublishIntent()
    {
        // Arrange
        var headers = new Dictionary<string, string>
        {
            { "NServiceBus.MessageIntent", "Publish" }
        };
        var context = CreateMockContext(headers: headers);
        MessageTelemetry? capturedTelemetry = null;
        _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<MessageTelemetry>(), It.IsAny<CancellationToken>()))
            .Callback<MessageTelemetry, CancellationToken>((t, _) => capturedTelemetry = t)
            .Returns(Task.CompletedTask);

        // Act
        await _behavior.Invoke(context.Object, () => Task.CompletedTask);

        // Assert
        Assert.IsNotNull(capturedTelemetry);
        Assert.AreEqual(CascadeMessageIntent.Publish, capturedTelemetry.Intent);
    }

    [TestMethod]
    public async Task Invoke_ParsesReplyIntent()
    {
        // Arrange
        var headers = new Dictionary<string, string>
        {
            { "NServiceBus.MessageIntent", "Reply" }
        };
        var context = CreateMockContext(headers: headers);
        MessageTelemetry? capturedTelemetry = null;
        _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<MessageTelemetry>(), It.IsAny<CancellationToken>()))
            .Callback<MessageTelemetry, CancellationToken>((t, _) => capturedTelemetry = t)
            .Returns(Task.CompletedTask);

        // Act
        await _behavior.Invoke(context.Object, () => Task.CompletedTask);

        // Assert
        Assert.IsNotNull(capturedTelemetry);
        Assert.AreEqual(CascadeMessageIntent.Reply, capturedTelemetry.Intent);
    }

    [TestMethod]
    public async Task Invoke_ParsesUnknownIntentForMissingHeader()
    {
        // Arrange
        var context = CreateMockContext(); // No intent header
        MessageTelemetry? capturedTelemetry = null;
        _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<MessageTelemetry>(), It.IsAny<CancellationToken>()))
            .Callback<MessageTelemetry, CancellationToken>((t, _) => capturedTelemetry = t)
            .Returns(Task.CompletedTask);

        // Act
        await _behavior.Invoke(context.Object, () => Task.CompletedTask);

        // Assert
        Assert.IsNotNull(capturedTelemetry);
        Assert.AreEqual(CascadeMessageIntent.Unknown, capturedTelemetry.Intent);
    }

    #endregion

    #region Options Configuration Tests

    [TestMethod]
    public async Task Invoke_UsesEndpointNameFromOptions()
    {
        // Arrange
        var options = new CascadeOptions { EndpointName = "CustomEndpoint" };
        var behavior = new IncomingMessageTelemetryBehavior(_dispatcherMock.Object, options);
        var context = CreateMockContext();
        MessageTelemetry? capturedTelemetry = null;
        _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<MessageTelemetry>(), It.IsAny<CancellationToken>()))
            .Callback<MessageTelemetry, CancellationToken>((t, _) => capturedTelemetry = t)
            .Returns(Task.CompletedTask);

        // Act
        await behavior.Invoke(context.Object, () => Task.CompletedTask);

        // Assert
        Assert.IsNotNull(capturedTelemetry);
        Assert.AreEqual("CustomEndpoint", capturedTelemetry.EndpointName);
    }

    [TestMethod]
    public async Task Invoke_UsesHostIdFromOptions()
    {
        // Arrange
        var options = new CascadeOptions { HostId = "custom-host" };
        var behavior = new IncomingMessageTelemetryBehavior(_dispatcherMock.Object, options);
        var context = CreateMockContext();
        MessageTelemetry? capturedTelemetry = null;
        _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<MessageTelemetry>(), It.IsAny<CancellationToken>()))
            .Callback<MessageTelemetry, CancellationToken>((t, _) => capturedTelemetry = t)
            .Returns(Task.CompletedTask);

        // Act
        await behavior.Invoke(context.Object, () => Task.CompletedTask);

        // Assert
        Assert.IsNotNull(capturedTelemetry);
        Assert.AreEqual("custom-host", capturedTelemetry.HostId);
    }

    [TestMethod]
    public async Task Invoke_IncludesHeadersWhenEnabled()
    {
        // Arrange
        var options = new CascadeOptions { IncludeHeaders = true };
        var behavior = new IncomingMessageTelemetryBehavior(_dispatcherMock.Object, options);
        var headers = new Dictionary<string, string>
        {
            { "CustomHeader", "CustomValue" }
        };
        var context = CreateMockContext(headers: headers);
        MessageTelemetry? capturedTelemetry = null;
        _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<MessageTelemetry>(), It.IsAny<CancellationToken>()))
            .Callback<MessageTelemetry, CancellationToken>((t, _) => capturedTelemetry = t)
            .Returns(Task.CompletedTask);

        // Act
        await behavior.Invoke(context.Object, () => Task.CompletedTask);

        // Assert
        Assert.IsNotNull(capturedTelemetry);
        Assert.IsNotNull(capturedTelemetry.Headers);
        Assert.IsTrue(capturedTelemetry.Headers.ContainsKey("CustomHeader"));
    }

    [TestMethod]
    public async Task Invoke_ExcludesHeadersWhenDisabled()
    {
        // Arrange
        var options = new CascadeOptions { IncludeHeaders = false };
        var behavior = new IncomingMessageTelemetryBehavior(_dispatcherMock.Object, options);
        var headers = new Dictionary<string, string>
        {
            { "CustomHeader", "CustomValue" }
        };
        var context = CreateMockContext(headers: headers);
        MessageTelemetry? capturedTelemetry = null;
        _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<MessageTelemetry>(), It.IsAny<CancellationToken>()))
            .Callback<MessageTelemetry, CancellationToken>((t, _) => capturedTelemetry = t)
            .Returns(Task.CompletedTask);

        // Act
        await behavior.Invoke(context.Object, () => Task.CompletedTask);

        // Assert
        Assert.IsNotNull(capturedTelemetry);
        Assert.IsNull(capturedTelemetry.Headers);
    }

    [TestMethod]
    public async Task Invoke_UsesMachineNameWhenHostIdNotSet()
    {
        // Arrange
        var options = new CascadeOptions { HostId = null };
        var behavior = new IncomingMessageTelemetryBehavior(_dispatcherMock.Object, options);
        var context = CreateMockContext();
        MessageTelemetry? capturedTelemetry = null;
        _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<MessageTelemetry>(), It.IsAny<CancellationToken>()))
            .Callback<MessageTelemetry, CancellationToken>((t, _) => capturedTelemetry = t)
            .Returns(Task.CompletedTask);

        // Act
        await behavior.Invoke(context.Object, () => Task.CompletedTask);

        // Assert
        Assert.IsNotNull(capturedTelemetry);
        Assert.AreEqual(Environment.MachineName, capturedTelemetry.HostId);
    }

    #endregion

    #region Helper Methods

    private static Mock<IIncomingPhysicalMessageContext> CreateMockContext(
        string messageId = "test-message-id",
        Dictionary<string, string>? headers = null)
    {
        var context = new Mock<IIncomingPhysicalMessageContext>();

        headers ??= new Dictionary<string, string>();

        context.Setup(c => c.MessageId).Returns(messageId);
        context.Setup(c => c.MessageHeaders).Returns(headers);
        context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        return context;
    }

    #endregion
}
