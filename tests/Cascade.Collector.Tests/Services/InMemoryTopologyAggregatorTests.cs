using Cascade.Collector.Services;
using Cascade.Collector.Tests.Helpers;
using Cascade.Core.Enums;

namespace Cascade.Collector.Tests.Services;

[TestClass]
public class InMemoryTopologyAggregatorTests
{
    private InMemoryTopologyAggregator _aggregator = null!;

    [TestInitialize]
    public void Setup()
    {
        _aggregator = new InMemoryTopologyAggregator();
        TestDataBuilder.ResetCounter();
    }

    #region RecordMessage - Endpoint Tracking Tests

    [TestMethod]
    public void RecordMessage_WithNewEndpoint_CreatesEndpointEntry()
    {
        // Arrange
        var telemetry = TestDataBuilder.CreateIncomingMessage(endpointName: "NewEndpoint");

        // Act
        _aggregator.RecordMessage(telemetry);
        var topology = _aggregator.GetTopology();

        // Assert
        Assert.IsTrue(topology.Endpoints.ContainsKey("NewEndpoint"));
        Assert.AreEqual("NewEndpoint", topology.Endpoints["NewEndpoint"].Name);
    }

    [TestMethod]
    public void RecordMessage_IncomingMessage_IncrementsMessagesReceived()
    {
        // Arrange
        var telemetry = TestDataBuilder.CreateIncomingMessage(endpointName: "ReceiverEndpoint");

        // Act
        _aggregator.RecordMessage(telemetry);
        var topology = _aggregator.GetTopology();

        // Assert
        Assert.AreEqual(1, topology.Endpoints["ReceiverEndpoint"].MessagesReceived);
        Assert.AreEqual(0, topology.Endpoints["ReceiverEndpoint"].MessagesSent);
    }

    [TestMethod]
    public void RecordMessage_OutgoingMessage_IncrementsMessagesSent()
    {
        // Arrange
        var telemetry = TestDataBuilder.CreateOutgoingMessage(endpointName: "SenderEndpoint");

        // Act
        _aggregator.RecordMessage(telemetry);
        var topology = _aggregator.GetTopology();

        // Assert
        Assert.AreEqual(0, topology.Endpoints["SenderEndpoint"].MessagesReceived);
        Assert.AreEqual(1, topology.Endpoints["SenderEndpoint"].MessagesSent);
    }

    [TestMethod]
    public void RecordMessage_FailedMessage_IncrementsFailures()
    {
        // Arrange
        var telemetry = TestDataBuilder.CreateFailedMessage(endpointName: "FailingEndpoint");

        // Act
        _aggregator.RecordMessage(telemetry);
        var topology = _aggregator.GetTopology();

        // Assert
        Assert.AreEqual(1, topology.Endpoints["FailingEndpoint"].Failures);
    }

    [TestMethod]
    public void RecordMessage_WithProcessingDuration_CalculatesAverageProcessingTime()
    {
        // Arrange
        var telemetry1 = TestDataBuilder.CreateIncomingMessage(
            endpointName: "TimedEndpoint",
            processingDuration: TimeSpan.FromMilliseconds(100));
        var telemetry2 = TestDataBuilder.CreateIncomingMessage(
            endpointName: "TimedEndpoint",
            processingDuration: TimeSpan.FromMilliseconds(200));

        // Act
        _aggregator.RecordMessage(telemetry1);
        _aggregator.RecordMessage(telemetry2);
        var topology = _aggregator.GetTopology();

        // Assert
        var endpoint = topology.Endpoints["TimedEndpoint"];
        Assert.AreEqual(150.0, endpoint.AverageProcessingTimeMs, 0.1);
    }

    [TestMethod]
    public void RecordMessage_TracksMultipleHostIds()
    {
        // Arrange
        var telemetry1 = TestDataBuilder.CreateTelemetry(
            endpointName: "MultiHostEndpoint",
            hostId: "host-1");
        var telemetry2 = TestDataBuilder.CreateTelemetry(
            endpointName: "MultiHostEndpoint",
            hostId: "host-2");

        // Act
        _aggregator.RecordMessage(telemetry1);
        _aggregator.RecordMessage(telemetry2);
        var topology = _aggregator.GetTopology();

        // Assert
        var endpoint = topology.Endpoints["MultiHostEndpoint"];
        Assert.IsTrue(endpoint.HostIds.Contains("host-1"));
        Assert.IsTrue(endpoint.HostIds.Contains("host-2"));
    }

    [TestMethod]
    public void RecordMessage_UpdatesLastSeenTimestamp()
    {
        // Arrange
        var telemetry1 = TestDataBuilder.CreateIncomingMessage(endpointName: "TimeTrackedEndpoint");

        // Act
        _aggregator.RecordMessage(telemetry1);
        var firstSeen = _aggregator.GetTopology().Endpoints["TimeTrackedEndpoint"].LastSeen;

        Thread.Sleep(50);

        var telemetry2 = TestDataBuilder.CreateIncomingMessage(endpointName: "TimeTrackedEndpoint");
        _aggregator.RecordMessage(telemetry2);
        var lastSeen = _aggregator.GetTopology().Endpoints["TimeTrackedEndpoint"].LastSeen;

        // Assert
        Assert.IsTrue(lastSeen > firstSeen);
    }

    #endregion

    #region RecordMessage - Message Type Tracking Tests

    [TestMethod]
    public void RecordMessage_WithNewMessageType_CreatesMessageTypeEntry()
    {
        // Arrange
        var telemetry = TestDataBuilder.CreateIncomingMessage(
            messageType: "Namespace.OrderPlaced, Assembly");

        // Act
        _aggregator.RecordMessage(telemetry);
        var topology = _aggregator.GetTopology();

        // Assert
        Assert.IsTrue(topology.MessageTypes.ContainsKey("Namespace.OrderPlaced, Assembly"));
    }

    [TestMethod]
    public void RecordMessage_WithExistingMessageType_IncrementsTimesObserved()
    {
        // Arrange
        var telemetry1 = TestDataBuilder.CreateIncomingMessage(
            messageType: "Namespace.OrderPlaced, Assembly");
        var telemetry2 = TestDataBuilder.CreateIncomingMessage(
            messageType: "Namespace.OrderPlaced, Assembly");

        // Act
        _aggregator.RecordMessage(telemetry1);
        _aggregator.RecordMessage(telemetry2);
        var topology = _aggregator.GetTopology();

        // Assert
        Assert.AreEqual(2, topology.MessageTypes["Namespace.OrderPlaced, Assembly"].TimesObserved);
    }

    #endregion

    #region RecordMessage - Connection Tracking Tests

    [TestMethod]
    public void RecordMessage_IncomingWithOriginatingEndpoint_CreatesConnection()
    {
        // Arrange
        var telemetry = TestDataBuilder.CreateIncomingMessage(
            endpointName: "TargetEndpoint",
            originatingEndpoint: "SourceEndpoint",
            messageType: "Namespace.TestMessage, Assembly");

        // Act
        _aggregator.RecordMessage(telemetry);
        var topology = _aggregator.GetTopology();

        // Assert
        Assert.AreEqual(1, topology.Connections.Count);
        var connection = topology.Connections[0];
        Assert.AreEqual("SourceEndpoint", connection.SourceEndpoint);
        Assert.AreEqual("TargetEndpoint", connection.TargetEndpoint);
        Assert.AreEqual("Namespace.TestMessage, Assembly", connection.MessageType);
    }

    [TestMethod]
    public void RecordMessage_IncomingWithSameOriginatingEndpoint_DoesNotCreateConnection()
    {
        // Arrange - Self-referencing message (shouldn't create connection)
        var telemetry = TestDataBuilder.CreateIncomingMessage(
            endpointName: "SameEndpoint",
            originatingEndpoint: "SameEndpoint");

        // Act
        _aggregator.RecordMessage(telemetry);
        var topology = _aggregator.GetTopology();

        // Assert
        Assert.AreEqual(0, topology.Connections.Count);
    }

    [TestMethod]
    public void RecordMessage_MultipleMessagesOnSameConnection_IncrementsMessageCount()
    {
        // Arrange
        var telemetry1 = TestDataBuilder.CreateIncomingMessage(
            endpointName: "Target",
            originatingEndpoint: "Source",
            messageType: "Namespace.Message, Assembly");
        var telemetry2 = TestDataBuilder.CreateIncomingMessage(
            endpointName: "Target",
            originatingEndpoint: "Source",
            messageType: "Namespace.Message, Assembly");

        // Act
        _aggregator.RecordMessage(telemetry1);
        _aggregator.RecordMessage(telemetry2);
        var topology = _aggregator.GetTopology();

        // Assert
        Assert.AreEqual(1, topology.Connections.Count);
        Assert.AreEqual(2, topology.Connections[0].MessageCount);
    }

    [TestMethod]
    public void RecordMessage_FailedMessageOnConnection_IncrementsFailureCount()
    {
        // Arrange
        var failedTelemetry = TestDataBuilder.CreateFailedMessage(endpointName: "Target");
        // Manually set originating endpoint for the failed message
        var telemetry = TestDataBuilder.CreateTelemetry(
            endpointName: "Target",
            originatingEndpoint: "Source",
            messageType: "Namespace.FailedMessage, Assembly",
            direction: MessageDirection.Incoming,
            success: false);

        // Act
        _aggregator.RecordMessage(telemetry);
        var topology = _aggregator.GetTopology();

        // Assert
        Assert.AreEqual(1, topology.Connections[0].FailureCount);
    }

    [TestMethod]
    public void RecordMessage_OutgoingMessage_DoesNotCreateConnection()
    {
        // Arrange - Outgoing messages don't have "incoming" relationship
        var telemetry = TestDataBuilder.CreateOutgoingMessage(
            endpointName: "Sender",
            messageType: "Namespace.OutgoingMessage, Assembly");

        // Act
        _aggregator.RecordMessage(telemetry);
        var topology = _aggregator.GetTopology();

        // Assert
        Assert.AreEqual(0, topology.Connections.Count);
    }

    #endregion

    #region GetTopology Tests

    [TestMethod]
    public void GetTopology_WithNoMessages_ReturnsEmptyTopology()
    {
        // Act
        var topology = _aggregator.GetTopology();

        // Assert
        Assert.AreEqual(0, topology.Endpoints.Count);
        Assert.AreEqual(0, topology.MessageTypes.Count);
        Assert.AreEqual(0, topology.Connections.Count);
        Assert.AreEqual(0, topology.TotalMessagesObserved);
    }

    [TestMethod]
    public void GetTopology_TracksTotalMessagesObserved()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            _aggregator.RecordMessage(TestDataBuilder.CreateIncomingMessage());
        }

        // Act
        var topology = _aggregator.GetTopology();

        // Assert
        Assert.AreEqual(10, topology.TotalMessagesObserved);
    }

    [TestMethod]
    public void GetTopology_ReturnsCorrectCounts()
    {
        // Arrange
        _aggregator.RecordMessage(TestDataBuilder.CreateIncomingMessage(
            endpointName: "Endpoint1",
            messageType: "Type1, Assembly"));
        _aggregator.RecordMessage(TestDataBuilder.CreateIncomingMessage(
            endpointName: "Endpoint2",
            messageType: "Type2, Assembly",
            originatingEndpoint: "Endpoint1"));

        // Act
        var topology = _aggregator.GetTopology();

        // Assert
        Assert.AreEqual(2, topology.EndpointCount);
        Assert.AreEqual(2, topology.MessageTypeCount);
        Assert.AreEqual(1, topology.ConnectionCount);
    }

    #endregion

    #region Reset Tests

    [TestMethod]
    public void Reset_ClearsAllData()
    {
        // Arrange
        _aggregator.RecordMessage(TestDataBuilder.CreateIncomingMessage(
            endpointName: "Endpoint1",
            originatingEndpoint: "Endpoint2",
            messageType: "Type1, Assembly"));

        // Verify data exists
        var topologyBefore = _aggregator.GetTopology();
        Assert.IsTrue(topologyBefore.Endpoints.Count > 0);

        // Act
        _aggregator.Reset();
        var topologyAfter = _aggregator.GetTopology();

        // Assert
        Assert.AreEqual(0, topologyAfter.Endpoints.Count);
        Assert.AreEqual(0, topologyAfter.MessageTypes.Count);
        Assert.AreEqual(0, topologyAfter.Connections.Count);
        Assert.AreEqual(0, topologyAfter.TotalMessagesObserved);
    }

    #endregion

    #region Concurrent Access Tests

    [TestMethod]
    public async Task RecordMessage_WithConcurrentAccess_HandlesThreadSafety()
    {
        // Arrange
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            var index = i;
            tasks.Add(Task.Run(() =>
            {
                var telemetry = TestDataBuilder.CreateIncomingMessage(
                    endpointName: "ConcurrentEndpoint",
                    messageType: $"Type{index % 5}, Assembly");
                _aggregator.RecordMessage(telemetry);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        var topology = _aggregator.GetTopology();
        Assert.AreEqual(100, topology.TotalMessagesObserved);
        Assert.IsTrue(topology.Endpoints.ContainsKey("ConcurrentEndpoint"));
        Assert.AreEqual(100, topology.Endpoints["ConcurrentEndpoint"].MessagesReceived);
    }

    [TestMethod]
    public async Task GetTopology_WhileRecording_DoesNotThrow()
    {
        // Arrange
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var recordingTask = Task.Run(async () =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                _aggregator.RecordMessage(TestDataBuilder.CreateIncomingMessage());
                await Task.Delay(1);
            }
        });

        // Act & Assert - Should not throw
        for (int i = 0; i < 50; i++)
        {
            var topology = _aggregator.GetTopology();
            Assert.IsNotNull(topology);
            await Task.Delay(10);
        }

        cts.Cancel();
        await recordingTask;
    }

    #endregion

    #region FailureRate Tests

    [TestMethod]
    public void RecordMessage_CalculatesCorrectFailureRate()
    {
        // Arrange - 3 successes, 1 failure = 25% failure rate
        for (int i = 0; i < 3; i++)
        {
            _aggregator.RecordMessage(TestDataBuilder.CreateIncomingMessage(endpointName: "RateEndpoint"));
        }
        _aggregator.RecordMessage(TestDataBuilder.CreateFailedMessage(endpointName: "RateEndpoint"));

        // Act
        var topology = _aggregator.GetTopology();

        // Assert
        var endpoint = topology.Endpoints["RateEndpoint"];
        Assert.AreEqual(4, endpoint.MessagesReceived);
        Assert.AreEqual(1, endpoint.Failures);
        Assert.AreEqual(0.25, endpoint.FailureRate, 0.001);
    }

    #endregion
}
