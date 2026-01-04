using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NServiceBus.Pipeline;
using Cascade.Core.Enums;
using Cascade.Core.Models;
using Cascade.NServiceBus.Framework;
using Cascade.NServiceBus.Framework.Behaviors;
using Cascade.NServiceBus.Framework.Dispatchers;
using CascadeMessageIntent = Cascade.Core.Enums.MessageIntent;

namespace Cascade.NServiceBus.Framework.Tests.Behaviors
{
    [TestClass]
    public class OutgoingMessageTelemetryBehaviorTests
    {
        private Mock<ITelemetryDispatcher> _dispatcherMock;
        private CascadeOptions _options;
        private OutgoingMessageTelemetryBehavior _behavior;

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
            _behavior = new OutgoingMessageTelemetryBehavior(_dispatcherMock.Object, _options);
        }

        #region Basic Behavior Tests

        [TestMethod]
        public async Task Invoke_DispatchesTelemetryAfterNext()
        {
            // Arrange
            var context = CreateMockContext();
            var nextCalled = false;
            MessageTelemetry capturedTelemetry = null;
            _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<MessageTelemetry>(), It.IsAny<CancellationToken>()))
                .Callback<MessageTelemetry, CancellationToken>((t, _) => capturedTelemetry = t)
                .Returns(Task.CompletedTask);

            // Act
            await _behavior.Invoke(context.Object, () =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            });

            // Assert
            Assert.IsTrue(nextCalled, "Next delegate should be called");
            Assert.IsNotNull(capturedTelemetry);
        }

        [TestMethod]
        public async Task Invoke_SetsDirectionToOutgoing()
        {
            // Arrange
            var context = CreateMockContext();
            MessageTelemetry capturedTelemetry = null;
            _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<MessageTelemetry>(), It.IsAny<CancellationToken>()))
                .Callback<MessageTelemetry, CancellationToken>((t, _) => capturedTelemetry = t)
                .Returns(Task.CompletedTask);

            // Act
            await _behavior.Invoke(context.Object, () => Task.CompletedTask);

            // Assert
            Assert.IsNotNull(capturedTelemetry);
            Assert.AreEqual(MessageDirection.Outgoing, capturedTelemetry.Direction);
        }

        [TestMethod]
        public async Task Invoke_AlwaysSetsSuccessToTrue()
        {
            // Arrange
            var context = CreateMockContext();
            MessageTelemetry capturedTelemetry = null;
            _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<MessageTelemetry>(), It.IsAny<CancellationToken>()))
                .Callback<MessageTelemetry, CancellationToken>((t, _) => capturedTelemetry = t)
                .Returns(Task.CompletedTask);

            // Act
            await _behavior.Invoke(context.Object, () => Task.CompletedTask);

            // Assert
            Assert.IsNotNull(capturedTelemetry);
            Assert.IsTrue(capturedTelemetry.Success);
        }

        [TestMethod]
        public async Task Invoke_DoesNotSetProcessingDuration()
        {
            // Arrange - Outgoing messages don't track processing duration
            var context = CreateMockContext();
            MessageTelemetry capturedTelemetry = null;
            _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<MessageTelemetry>(), It.IsAny<CancellationToken>()))
                .Callback<MessageTelemetry, CancellationToken>((t, _) => capturedTelemetry = t)
                .Returns(Task.CompletedTask);

            // Act
            await _behavior.Invoke(context.Object, async () => await Task.Delay(50));

            // Assert
            Assert.IsNotNull(capturedTelemetry);
            Assert.IsFalse(capturedTelemetry.ProcessingDuration.HasValue);
        }

        [TestMethod]
        public async Task Invoke_GeneratesUniqueId()
        {
            // Arrange
            var context = CreateMockContext();
            MessageTelemetry capturedTelemetry = null;
            _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<MessageTelemetry>(), It.IsAny<CancellationToken>()))
                .Callback<MessageTelemetry, CancellationToken>((t, _) => capturedTelemetry = t)
                .Returns(Task.CompletedTask);

            // Act
            await _behavior.Invoke(context.Object, () => Task.CompletedTask);

            // Assert
            Assert.IsNotNull(capturedTelemetry);
            Assert.IsFalse(string.IsNullOrEmpty(capturedTelemetry.Id));
            Guid parsedGuid;
            Assert.IsTrue(Guid.TryParse(capturedTelemetry.Id, out parsedGuid));
        }

        #endregion

        #region Header Extraction Tests

        [TestMethod]
        public async Task Invoke_ExtractsMessageId()
        {
            // Arrange
            var context = CreateMockContext(messageId: "outgoing-message-id");
            MessageTelemetry capturedTelemetry = null;
            _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<MessageTelemetry>(), It.IsAny<CancellationToken>()))
                .Callback<MessageTelemetry, CancellationToken>((t, _) => capturedTelemetry = t)
                .Returns(Task.CompletedTask);

            // Act
            await _behavior.Invoke(context.Object, () => Task.CompletedTask);

            // Assert
            Assert.IsNotNull(capturedTelemetry);
            Assert.AreEqual("outgoing-message-id", capturedTelemetry.MessageId);
        }

        [TestMethod]
        public async Task Invoke_ExtractsCorrelationId()
        {
            // Arrange
            var headers = new Dictionary<string, string>
            {
                { "NServiceBus.CorrelationId", "out-corr-123" }
            };
            var context = CreateMockContext(headers: headers);
            MessageTelemetry capturedTelemetry = null;
            _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<MessageTelemetry>(), It.IsAny<CancellationToken>()))
                .Callback<MessageTelemetry, CancellationToken>((t, _) => capturedTelemetry = t)
                .Returns(Task.CompletedTask);

            // Act
            await _behavior.Invoke(context.Object, () => Task.CompletedTask);

            // Assert
            Assert.IsNotNull(capturedTelemetry);
            Assert.AreEqual("out-corr-123", capturedTelemetry.CorrelationId);
        }

        [TestMethod]
        public async Task Invoke_ExtractsConversationId()
        {
            // Arrange
            var headers = new Dictionary<string, string>
            {
                { "NServiceBus.ConversationId", "conv-789" }
            };
            var context = CreateMockContext(headers: headers);
            MessageTelemetry capturedTelemetry = null;
            _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<MessageTelemetry>(), It.IsAny<CancellationToken>()))
                .Callback<MessageTelemetry, CancellationToken>((t, _) => capturedTelemetry = t)
                .Returns(Task.CompletedTask);

            // Act
            await _behavior.Invoke(context.Object, () => Task.CompletedTask);

            // Assert
            Assert.IsNotNull(capturedTelemetry);
            Assert.AreEqual("conv-789", capturedTelemetry.ConversationId);
        }

        [TestMethod]
        public async Task Invoke_ExtractsMessageType()
        {
            // Arrange
            var headers = new Dictionary<string, string>
            {
                { "NServiceBus.EnclosedMessageTypes", "MyNamespace.OrderCreated, MyAssembly" }
            };
            var context = CreateMockContext(headers: headers);
            MessageTelemetry capturedTelemetry = null;
            _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<MessageTelemetry>(), It.IsAny<CancellationToken>()))
                .Callback<MessageTelemetry, CancellationToken>((t, _) => capturedTelemetry = t)
                .Returns(Task.CompletedTask);

            // Act
            await _behavior.Invoke(context.Object, () => Task.CompletedTask);

            // Assert
            Assert.IsNotNull(capturedTelemetry);
            Assert.AreEqual("MyNamespace.OrderCreated, MyAssembly", capturedTelemetry.MessageType);
        }

        [TestMethod]
        public async Task Invoke_ExtractsOriginatingEndpoint()
        {
            // Arrange
            var headers = new Dictionary<string, string>
            {
                { "NServiceBus.OriginatingEndpoint", "SourceService" }
            };
            var context = CreateMockContext(headers: headers);
            MessageTelemetry capturedTelemetry = null;
            _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<MessageTelemetry>(), It.IsAny<CancellationToken>()))
                .Callback<MessageTelemetry, CancellationToken>((t, _) => capturedTelemetry = t)
                .Returns(Task.CompletedTask);

            // Act
            await _behavior.Invoke(context.Object, () => Task.CompletedTask);

            // Assert
            Assert.IsNotNull(capturedTelemetry);
            Assert.AreEqual("SourceService", capturedTelemetry.OriginatingEndpoint);
        }

        [TestMethod]
        public async Task Invoke_DefaultsMessageTypeToUnknown()
        {
            // Arrange - No EnclosedMessageTypes header
            var context = CreateMockContext();
            MessageTelemetry capturedTelemetry = null;
            _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<MessageTelemetry>(), It.IsAny<CancellationToken>()))
                .Callback<MessageTelemetry, CancellationToken>((t, _) => capturedTelemetry = t)
                .Returns(Task.CompletedTask);

            // Act
            await _behavior.Invoke(context.Object, () => Task.CompletedTask);

            // Assert
            Assert.IsNotNull(capturedTelemetry);
            Assert.AreEqual("Unknown", capturedTelemetry.MessageType);
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
            MessageTelemetry capturedTelemetry = null;
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
            MessageTelemetry capturedTelemetry = null;
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
            MessageTelemetry capturedTelemetry = null;
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
            MessageTelemetry capturedTelemetry = null;
            _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<MessageTelemetry>(), It.IsAny<CancellationToken>()))
                .Callback<MessageTelemetry, CancellationToken>((t, _) => capturedTelemetry = t)
                .Returns(Task.CompletedTask);

            // Act
            await _behavior.Invoke(context.Object, () => Task.CompletedTask);

            // Assert
            Assert.IsNotNull(capturedTelemetry);
            Assert.AreEqual(CascadeMessageIntent.Unknown, capturedTelemetry.Intent);
        }

        [TestMethod]
        public async Task Invoke_ParsesUnknownIntentForInvalidValue()
        {
            // Arrange
            var headers = new Dictionary<string, string>
            {
                { "NServiceBus.MessageIntent", "InvalidIntent" }
            };
            var context = CreateMockContext(headers: headers);
            MessageTelemetry capturedTelemetry = null;
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
            var options = new CascadeOptions { EndpointName = "OutgoingEndpoint" };
            var behavior = new OutgoingMessageTelemetryBehavior(_dispatcherMock.Object, options);
            var context = CreateMockContext();
            MessageTelemetry capturedTelemetry = null;
            _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<MessageTelemetry>(), It.IsAny<CancellationToken>()))
                .Callback<MessageTelemetry, CancellationToken>((t, _) => capturedTelemetry = t)
                .Returns(Task.CompletedTask);

            // Act
            await behavior.Invoke(context.Object, () => Task.CompletedTask);

            // Assert
            Assert.IsNotNull(capturedTelemetry);
            Assert.AreEqual("OutgoingEndpoint", capturedTelemetry.EndpointName);
        }

        [TestMethod]
        public async Task Invoke_UsesHostIdFromOptions()
        {
            // Arrange
            var options = new CascadeOptions { HostId = "outgoing-host" };
            var behavior = new OutgoingMessageTelemetryBehavior(_dispatcherMock.Object, options);
            var context = CreateMockContext();
            MessageTelemetry capturedTelemetry = null;
            _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<MessageTelemetry>(), It.IsAny<CancellationToken>()))
                .Callback<MessageTelemetry, CancellationToken>((t, _) => capturedTelemetry = t)
                .Returns(Task.CompletedTask);

            // Act
            await behavior.Invoke(context.Object, () => Task.CompletedTask);

            // Assert
            Assert.IsNotNull(capturedTelemetry);
            Assert.AreEqual("outgoing-host", capturedTelemetry.HostId);
        }

        [TestMethod]
        public async Task Invoke_IncludesHeadersWhenEnabled()
        {
            // Arrange
            var options = new CascadeOptions { IncludeHeaders = true };
            var behavior = new OutgoingMessageTelemetryBehavior(_dispatcherMock.Object, options);
            var headers = new Dictionary<string, string>
            {
                { "CustomHeader", "OutgoingValue" }
            };
            var context = CreateMockContext(headers: headers);
            MessageTelemetry capturedTelemetry = null;
            _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<MessageTelemetry>(), It.IsAny<CancellationToken>()))
                .Callback<MessageTelemetry, CancellationToken>((t, _) => capturedTelemetry = t)
                .Returns(Task.CompletedTask);

            // Act
            await behavior.Invoke(context.Object, () => Task.CompletedTask);

            // Assert
            Assert.IsNotNull(capturedTelemetry);
            Assert.IsNotNull(capturedTelemetry.Headers);
            Assert.IsTrue(capturedTelemetry.Headers.ContainsKey("CustomHeader"));
            Assert.AreEqual("OutgoingValue", capturedTelemetry.Headers["CustomHeader"]);
        }

        [TestMethod]
        public async Task Invoke_ExcludesHeadersWhenDisabled()
        {
            // Arrange
            var options = new CascadeOptions { IncludeHeaders = false };
            var behavior = new OutgoingMessageTelemetryBehavior(_dispatcherMock.Object, options);
            var headers = new Dictionary<string, string>
            {
                { "CustomHeader", "OutgoingValue" }
            };
            var context = CreateMockContext(headers: headers);
            MessageTelemetry capturedTelemetry = null;
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
            var behavior = new OutgoingMessageTelemetryBehavior(_dispatcherMock.Object, options);
            var context = CreateMockContext();
            MessageTelemetry capturedTelemetry = null;
            _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<MessageTelemetry>(), It.IsAny<CancellationToken>()))
                .Callback<MessageTelemetry, CancellationToken>((t, _) => capturedTelemetry = t)
                .Returns(Task.CompletedTask);

            // Act
            await behavior.Invoke(context.Object, () => Task.CompletedTask);

            // Assert
            Assert.IsNotNull(capturedTelemetry);
            Assert.AreEqual(Environment.MachineName, capturedTelemetry.HostId);
        }

        [TestMethod]
        public async Task Invoke_UsesUnknownWhenEndpointNameNotSet()
        {
            // Arrange
            var options = new CascadeOptions { EndpointName = null };
            var behavior = new OutgoingMessageTelemetryBehavior(_dispatcherMock.Object, options);
            var context = CreateMockContext();
            MessageTelemetry capturedTelemetry = null;
            _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<MessageTelemetry>(), It.IsAny<CancellationToken>()))
                .Callback<MessageTelemetry, CancellationToken>((t, _) => capturedTelemetry = t)
                .Returns(Task.CompletedTask);

            // Act
            await behavior.Invoke(context.Object, () => Task.CompletedTask);

            // Assert
            Assert.IsNotNull(capturedTelemetry);
            Assert.AreEqual("Unknown", capturedTelemetry.EndpointName);
        }

        #endregion

        #region Timestamp Tests

        [TestMethod]
        public async Task Invoke_SetsTimestamp()
        {
            // Arrange
            var context = CreateMockContext();
            MessageTelemetry capturedTelemetry = null;
            _dispatcherMock.Setup(d => d.DispatchAsync(It.IsAny<MessageTelemetry>(), It.IsAny<CancellationToken>()))
                .Callback<MessageTelemetry, CancellationToken>((t, _) => capturedTelemetry = t)
                .Returns(Task.CompletedTask);
            var beforeInvoke = DateTimeOffset.UtcNow;

            // Act
            await _behavior.Invoke(context.Object, () => Task.CompletedTask);
            var afterInvoke = DateTimeOffset.UtcNow;

            // Assert
            Assert.IsNotNull(capturedTelemetry);
            Assert.IsTrue(capturedTelemetry.Timestamp >= beforeInvoke);
            Assert.IsTrue(capturedTelemetry.Timestamp <= afterInvoke);
        }

        #endregion

        #region Helper Methods

        private static Mock<IOutgoingPhysicalMessageContext> CreateMockContext(
            string messageId = "test-outgoing-message-id",
            Dictionary<string, string> headers = null)
        {
            var context = new Mock<IOutgoingPhysicalMessageContext>();

            if (headers == null)
            {
                headers = new Dictionary<string, string>();
            }

            context.Setup(c => c.MessageId).Returns(messageId);
            context.Setup(c => c.Headers).Returns(headers);
            context.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

            return context;
        }

        #endregion
    }
}
