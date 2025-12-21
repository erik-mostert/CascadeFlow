using Cascade.Collector.Services;
using Cascade.Collector.Tests.Helpers;
using Cascade.Core.Enums;
using Cascade.Core.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cascade.Collector.Tests.Services;

[TestClass]
public class ImpactAnalyzerTests
{
    private Mock<IFlowAggregator> _flowAggregatorMock = null!;
    private Mock<ILogger<ImpactAnalyzer>> _loggerMock = null!;
    private ImpactAnalyzer _analyzer = null!;

    [TestInitialize]
    public void Setup()
    {
        _flowAggregatorMock = new Mock<IFlowAggregator>();
        _loggerMock = new Mock<ILogger<ImpactAnalyzer>>();
        _analyzer = new ImpactAnalyzer(_flowAggregatorMock.Object, _loggerMock.Object);
        TestDataBuilder.ResetCounter();
    }

    #region AnalyzeFlow - Basic Tests

    [TestMethod]
    public void AnalyzeFlow_WithEmptyFlow_ReturnsBasicMetrics()
    {
        // Arrange
        var flow = TestDataBuilder.CreateFlow(correlationId: "empty-flow");

        // Act
        var metrics = _analyzer.AnalyzeFlow(flow);

        // Assert
        Assert.AreEqual("empty-flow", metrics.CorrelationId);
        Assert.AreEqual(0, metrics.TotalMessages);
        Assert.AreEqual(0, metrics.TotalEndpoints);
        Assert.IsFalse(metrics.HasFailures);
    }

    [TestMethod]
    public void AnalyzeFlow_CountsMessagesCorrectly()
    {
        // Arrange
        var messages = new[]
        {
            TestDataBuilder.CreateIncomingMessage(),
            TestDataBuilder.CreateIncomingMessage(),
            TestDataBuilder.CreateOutgoingMessage()
        };
        var flow = TestDataBuilder.CreateFlow(correlationId: "count-flow", messages: messages);

        // Act
        var metrics = _analyzer.AnalyzeFlow(flow);

        // Assert
        Assert.AreEqual(3, metrics.TotalMessages);
    }

    [TestMethod]
    public void AnalyzeFlow_CountsUniqueEndpoints()
    {
        // Arrange
        var messages = new[]
        {
            TestDataBuilder.CreateIncomingMessage(endpointName: "Endpoint1"),
            TestDataBuilder.CreateIncomingMessage(endpointName: "Endpoint2"),
            TestDataBuilder.CreateIncomingMessage(endpointName: "Endpoint1") // Duplicate
        };
        var flow = TestDataBuilder.CreateFlow(messages: messages);

        // Act
        var metrics = _analyzer.AnalyzeFlow(flow);

        // Assert
        Assert.AreEqual(2, metrics.TotalEndpoints);
    }

    [TestMethod]
    public void AnalyzeFlow_DetectsFailures()
    {
        // Arrange
        var messages = new[]
        {
            TestDataBuilder.CreateIncomingMessage(),
            TestDataBuilder.CreateFailedMessage()
        };
        var flow = TestDataBuilder.CreateFlow(messages: messages);

        // Act
        var metrics = _analyzer.AnalyzeFlow(flow);

        // Assert
        Assert.IsTrue(metrics.HasFailures);
    }

    [TestMethod]
    public void AnalyzeFlow_CalculatesTotalProcessingTime()
    {
        // Arrange
        var messages = new[]
        {
            TestDataBuilder.CreateIncomingMessage(processingDuration: TimeSpan.FromMilliseconds(100)),
            TestDataBuilder.CreateIncomingMessage(processingDuration: TimeSpan.FromMilliseconds(200)),
            TestDataBuilder.CreateIncomingMessage(processingDuration: TimeSpan.FromMilliseconds(50))
        };
        var flow = TestDataBuilder.CreateFlow(messages: messages);

        // Act
        var metrics = _analyzer.AnalyzeFlow(flow);

        // Assert
        Assert.AreEqual(350.0, metrics.TotalProcessingTimeMs, 0.1);
    }

    #endregion

    #region AnalyzeFlow - Endpoint Breakdown Tests

    [TestMethod]
    public void AnalyzeFlow_CalculatesEndpointBreakdown()
    {
        // Arrange
        var messages = new[]
        {
            TestDataBuilder.CreateIncomingMessage(endpointName: "Handler"),
            TestDataBuilder.CreateOutgoingMessage(endpointName: "Handler", intent: MessageIntent.Publish),
            TestDataBuilder.CreateOutgoingMessage(endpointName: "Handler", intent: MessageIntent.Publish)
        };
        var flow = TestDataBuilder.CreateFlow(messages: messages);

        // Act
        var metrics = _analyzer.AnalyzeFlow(flow);

        // Assert
        Assert.HasCount(1, metrics.EndpointBreakdown);
        var breakdown = metrics.EndpointBreakdown[0];
        Assert.AreEqual("Handler", breakdown.EndpointName);
        Assert.AreEqual(1, breakdown.MessagesReceived);
        Assert.AreEqual(2, breakdown.MessagesPublished);
        Assert.AreEqual(2, breakdown.EventsPublished);
    }

    [TestMethod]
    public void AnalyzeFlow_CalculatesMultiplierRatio()
    {
        // Arrange - 1 received, 3 published = 3x multiplier
        var messages = new[]
        {
            TestDataBuilder.CreateIncomingMessage(endpointName: "Multiplier"),
            TestDataBuilder.CreateOutgoingMessage(endpointName: "Multiplier", intent: MessageIntent.Send),
            TestDataBuilder.CreateOutgoingMessage(endpointName: "Multiplier", intent: MessageIntent.Publish),
            TestDataBuilder.CreateOutgoingMessage(endpointName: "Multiplier", intent: MessageIntent.Publish)
        };
        var flow = TestDataBuilder.CreateFlow(messages: messages);

        // Act
        var metrics = _analyzer.AnalyzeFlow(flow);

        // Assert
        var breakdown = metrics.EndpointBreakdown.First(e => e.EndpointName == "Multiplier");
        Assert.AreEqual(3.0, breakdown.MultiplierRatio, 0.01);
        Assert.AreEqual(2.0, breakdown.EventMultiplierRatio, 0.01);
    }

    [TestMethod]
    public void AnalyzeFlow_BreakdownIncludesProcessingTime()
    {
        // Arrange
        var messages = new[]
        {
            TestDataBuilder.CreateIncomingMessage(
                endpointName: "TimedEndpoint",
                processingDuration: TimeSpan.FromMilliseconds(150))
        };
        var flow = TestDataBuilder.CreateFlow(messages: messages);

        // Act
        var metrics = _analyzer.AnalyzeFlow(flow);

        // Assert
        var breakdown = metrics.EndpointBreakdown[0];
        Assert.AreEqual(150.0, breakdown.ProcessingTimeMs, 0.1);
    }

    [TestMethod]
    public void AnalyzeFlow_BreakdownDetectsEndpointFailures()
    {
        // Arrange
        var messages = new[]
        {
            TestDataBuilder.CreateIncomingMessage(endpointName: "HealthyEndpoint"),
            TestDataBuilder.CreateFailedMessage(endpointName: "FailingEndpoint")
        };
        var flow = TestDataBuilder.CreateFlow(messages: messages);

        // Act
        var metrics = _analyzer.AnalyzeFlow(flow);

        // Assert
        var healthy = metrics.EndpointBreakdown.First(e => e.EndpointName == "HealthyEndpoint");
        var failing = metrics.EndpointBreakdown.First(e => e.EndpointName == "FailingEndpoint");
        Assert.IsFalse(healthy.HasFailures);
        Assert.IsTrue(failing.HasFailures);
    }

    #endregion

    #region AnalyzeFlow - Message Tree Tests

    [TestMethod]
    public void AnalyzeFlow_BuildsMessageTree()
    {
        // Arrange - Simple tree: Root -> Child
        var rootMessageId = "root-msg";
        var childMessageId = "child-msg";

        var messages = new[]
        {
            TestDataBuilder.CreateTelemetry(
                messageId: rootMessageId,
                endpointName: "Publisher",
                direction: MessageDirection.Outgoing,
                messageType: "RootMessage, Assembly"),
            TestDataBuilder.CreateTelemetry(
                messageId: childMessageId,
                endpointName: "Handler",
                direction: MessageDirection.Outgoing,
                relatedTo: rootMessageId,
                messageType: "ChildMessage, Assembly")
        };
        var flow = TestDataBuilder.CreateFlow(messages: messages);

        // Act
        var metrics = _analyzer.AnalyzeFlow(flow);

        // Assert
        Assert.IsNotNull(metrics.MessageTree);
        Assert.IsGreaterThanOrEqualTo(1, metrics.MessageTree.Count);
    }

    [TestMethod]
    public void AnalyzeFlow_CalculatesMaxDepth()
    {
        // Arrange - Linear chain: Root -> Child1 -> Child2 (depth = 2)
        var msg1Id = "msg-1";
        var msg2Id = "msg-2";
        var msg3Id = "msg-3";

        var messages = new[]
        {
            TestDataBuilder.CreateTelemetry(
                messageId: msg1Id,
                endpointName: "Endpoint1",
                direction: MessageDirection.Outgoing),
            TestDataBuilder.CreateTelemetry(
                messageId: msg2Id,
                endpointName: "Endpoint2",
                direction: MessageDirection.Outgoing,
                relatedTo: msg1Id),
            TestDataBuilder.CreateTelemetry(
                messageId: msg3Id,
                endpointName: "Endpoint3",
                direction: MessageDirection.Outgoing,
                relatedTo: msg2Id)
        };
        var flow = TestDataBuilder.CreateFlow(messages: messages);

        // Act
        var metrics = _analyzer.AnalyzeFlow(flow);

        // Assert
        Assert.AreEqual(2, metrics.MaxDepth);
    }

    #endregion

    #region GetSystemImpactSummaryAsync Tests

    [TestMethod]
    public async Task GetSystemImpactSummaryAsync_WithNoFlows_ReturnsEmptySummary()
    {
        // Arrange
        _flowAggregatorMock.Setup(x => x.GetActiveFlows())
            .Returns([]);
        _flowAggregatorMock.Setup(x => x.GetFlowsInTimeRangeAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<int>()))
            .ReturnsAsync([]);

        // Act
        var summary = await _analyzer.GetSystemImpactSummaryAsync();

        // Assert
        Assert.AreEqual(0, summary.TotalFlowsAnalyzed);
    }

    [TestMethod]
    public async Task GetSystemImpactSummaryAsync_CalculatesAverages()
    {
        // Arrange
        var flow1 = TestDataBuilder.CreateFlow(messages:
        [
            TestDataBuilder.CreateIncomingMessage(endpointName: "E1"),
            TestDataBuilder.CreateIncomingMessage(endpointName: "E2")
        ]);
        var flow2 = TestDataBuilder.CreateFlow(messages:
        [
            TestDataBuilder.CreateIncomingMessage(endpointName: "E1"),
            TestDataBuilder.CreateIncomingMessage(endpointName: "E2"),
            TestDataBuilder.CreateIncomingMessage(endpointName: "E3"),
            TestDataBuilder.CreateIncomingMessage(endpointName: "E4")
        ]);

        _flowAggregatorMock.Setup(x => x.GetActiveFlows())
            .Returns([flow1, flow2]);
        _flowAggregatorMock.Setup(x => x.GetFlowsInTimeRangeAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<int>()))
            .ReturnsAsync([]);

        // Act
        var summary = await _analyzer.GetSystemImpactSummaryAsync(flowCount: 10);

        // Assert
        Assert.AreEqual(2, summary.TotalFlowsAnalyzed);
        Assert.AreEqual(3.0, summary.AverageMessagesPerFlow, 0.1); // (2 + 4) / 2 = 3
        Assert.AreEqual(3.0, summary.AverageEndpointsPerFlow, 0.1); // (2 + 4) / 2 = 3
    }

    #endregion

    #region GetMultiplierEndpointsAsync Tests

    [TestMethod]
    public async Task GetMultiplierEndpointsAsync_WithNoFlows_ReturnsEmptyList()
    {
        // Arrange
        _flowAggregatorMock.Setup(x => x.GetActiveFlows())
            .Returns([]);
        _flowAggregatorMock.Setup(x => x.GetFlowsInTimeRangeAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<int>()))
            .ReturnsAsync([]);

        // Act
        var multipliers = await _analyzer.GetMultiplierEndpointsAsync();

        // Assert
        Assert.IsEmpty(multipliers);
    }

    [TestMethod]
    public async Task GetMultiplierEndpointsAsync_IdentifiesHighMultiplierEndpoints()
    {
        // Arrange - Endpoint receives 1, publishes 5 events
        var flow = TestDataBuilder.CreateFlow(messages:
        [
            TestDataBuilder.CreateIncomingMessage(endpointName: "HighMultiplier"),
            TestDataBuilder.CreateOutgoingMessage(endpointName: "HighMultiplier", intent: MessageIntent.Publish),
            TestDataBuilder.CreateOutgoingMessage(endpointName: "HighMultiplier", intent: MessageIntent.Publish),
            TestDataBuilder.CreateOutgoingMessage(endpointName: "HighMultiplier", intent: MessageIntent.Publish),
            TestDataBuilder.CreateOutgoingMessage(endpointName: "HighMultiplier", intent: MessageIntent.Publish),
            TestDataBuilder.CreateOutgoingMessage(endpointName: "HighMultiplier", intent: MessageIntent.Publish)
        ]);

        _flowAggregatorMock.Setup(x => x.GetActiveFlows())
            .Returns([flow]);
        _flowAggregatorMock.Setup(x => x.GetFlowsInTimeRangeAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<int>()))
            .ReturnsAsync([]);

        // Act
        var multipliers = await _analyzer.GetMultiplierEndpointsAsync();

        // Assert
        Assert.HasCount(1, multipliers);
        var multiplier = multipliers[0];
        Assert.AreEqual("HighMultiplier", multiplier.EndpointName);
        Assert.AreEqual(1, multiplier.TotalReceived);
        Assert.AreEqual(5, multiplier.TotalPublished);
        Assert.AreEqual(5, multiplier.EventsPublished);
        Assert.AreEqual(5.0, multiplier.MultiplierRatio, 0.01);
        Assert.AreEqual(5.0, multiplier.EventMultiplierRatio, 0.01);
    }

    [TestMethod]
    public async Task GetMultiplierEndpointsAsync_OrdersByEventMultiplierRatioDescending()
    {
        // Arrange
        var flow = TestDataBuilder.CreateFlow(messages:
        [
            // LowMultiplier: 1 received, 1 published = 1x
            TestDataBuilder.CreateIncomingMessage(endpointName: "LowMultiplier"),
            TestDataBuilder.CreateOutgoingMessage(endpointName: "LowMultiplier", intent: MessageIntent.Publish),

            // HighMultiplier: 1 received, 3 published = 3x
            TestDataBuilder.CreateIncomingMessage(endpointName: "HighMultiplier"),
            TestDataBuilder.CreateOutgoingMessage(endpointName: "HighMultiplier", intent: MessageIntent.Publish),
            TestDataBuilder.CreateOutgoingMessage(endpointName: "HighMultiplier", intent: MessageIntent.Publish),
            TestDataBuilder.CreateOutgoingMessage(endpointName: "HighMultiplier", intent: MessageIntent.Publish)
        ]);

        _flowAggregatorMock.Setup(x => x.GetActiveFlows())
            .Returns([flow]);
        _flowAggregatorMock.Setup(x => x.GetFlowsInTimeRangeAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<int>()))
            .ReturnsAsync([]);

        // Act
        var multipliers = await _analyzer.GetMultiplierEndpointsAsync();

        // Assert
        Assert.HasCount(2, multipliers);
        Assert.AreEqual("HighMultiplier", multipliers[0].EndpointName); // Higher ratio first
        Assert.AreEqual("LowMultiplier", multipliers[1].EndpointName);
    }

    [TestMethod]
    public async Task GetMultiplierEndpointsAsync_TracksCommandsAndEventsSeparately()
    {
        // Arrange
        var flow = TestDataBuilder.CreateFlow(messages:
        [
            TestDataBuilder.CreateIncomingMessage(endpointName: "MixedEndpoint"),
            TestDataBuilder.CreateOutgoingMessage(endpointName: "MixedEndpoint", intent: MessageIntent.Send),
            TestDataBuilder.CreateOutgoingMessage(endpointName: "MixedEndpoint", intent: MessageIntent.Send),
            TestDataBuilder.CreateOutgoingMessage(endpointName: "MixedEndpoint", intent: MessageIntent.Publish),
            TestDataBuilder.CreateOutgoingMessage(endpointName: "MixedEndpoint", intent: MessageIntent.Publish),
            TestDataBuilder.CreateOutgoingMessage(endpointName: "MixedEndpoint", intent: MessageIntent.Publish)
        ]);

        _flowAggregatorMock.Setup(x => x.GetActiveFlows())
            .Returns([flow]);
        _flowAggregatorMock.Setup(x => x.GetFlowsInTimeRangeAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<int>()))
            .ReturnsAsync([]);

        // Act
        var multipliers = await _analyzer.GetMultiplierEndpointsAsync();

        // Assert
        var endpoint = multipliers[0];
        Assert.AreEqual(2, endpoint.CommandsSent);
        Assert.AreEqual(3, endpoint.EventsPublished);
        Assert.AreEqual(5, endpoint.TotalPublished);
    }

    #endregion

    #region Integration Tests

    [TestMethod]
    public async Task FullFlowAnalysis_IntegrationTest()
    {
        // Arrange - Simulate a realistic flow
        var orderId = Guid.NewGuid().ToString();
        var placeOrderMsgId = "place-order-1";
        var orderPlacedMsgId = "order-placed-1";
        var billingMsgId = "billing-1";
        var shippingMsgId = "shipping-1";

        var messages = new[]
        {
            // OrderService receives PlaceOrder command
            TestDataBuilder.CreateTelemetry(
                messageId: placeOrderMsgId,
                correlationId: orderId,
                endpointName: "OrderService",
                direction: MessageDirection.Incoming,
                messageType: "Commands.PlaceOrder, Contracts",
                processingDuration: TimeSpan.FromMilliseconds(50)),

            // OrderService publishes OrderPlaced event
            TestDataBuilder.CreateTelemetry(
                messageId: orderPlacedMsgId,
                correlationId: orderId,
                endpointName: "OrderService",
                direction: MessageDirection.Outgoing,
                messageType: "Events.OrderPlaced, Contracts",
                relatedTo: placeOrderMsgId,
                intent: MessageIntent.Publish),

            // BillingService handles OrderPlaced
            TestDataBuilder.CreateTelemetry(
                messageId: billingMsgId,
                correlationId: orderId,
                endpointName: "BillingService",
                direction: MessageDirection.Incoming,
                messageType: "Events.OrderPlaced, Contracts",
                processingDuration: TimeSpan.FromMilliseconds(100),
                originatingEndpoint: "OrderService"),

            // ShippingService handles OrderPlaced
            TestDataBuilder.CreateTelemetry(
                messageId: shippingMsgId,
                correlationId: orderId,
                endpointName: "ShippingService",
                direction: MessageDirection.Incoming,
                messageType: "Events.OrderPlaced, Contracts",
                processingDuration: TimeSpan.FromMilliseconds(75),
                originatingEndpoint: "OrderService")
        };

        var flow = TestDataBuilder.CreateFlow(correlationId: orderId, messages: messages);

        _flowAggregatorMock.Setup(x => x.GetActiveFlows())
            .Returns([flow]);
        _flowAggregatorMock.Setup(x => x.GetFlowsInTimeRangeAsync(
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<int>()))
            .ReturnsAsync([]);

        // Act
        var metrics = _analyzer.AnalyzeFlow(flow);
        var summary = await _analyzer.GetSystemImpactSummaryAsync();
        var multipliers = await _analyzer.GetMultiplierEndpointsAsync();

        // Assert - Flow metrics
        Assert.AreEqual(4, metrics.TotalMessages);
        Assert.AreEqual(3, metrics.TotalEndpoints); // OrderService, BillingService, ShippingService
        Assert.IsFalse(metrics.HasFailures);
        Assert.AreEqual(225.0, metrics.TotalProcessingTimeMs, 0.1); // 50 + 100 + 75

        // Assert - Endpoint breakdown
        var orderService = metrics.EndpointBreakdown.First(e => e.EndpointName == "OrderService");
        Assert.AreEqual(1, orderService.MessagesReceived);
        Assert.AreEqual(1, orderService.MessagesPublished);
        Assert.AreEqual(1, orderService.EventsPublished);

        // Assert - Summary
        Assert.AreEqual(1, summary.TotalFlowsAnalyzed);

        // Assert - Multipliers (OrderService is the only one that publishes events)
        var orderMultiplier = multipliers.FirstOrDefault(m => m.EndpointName == "OrderService");
        Assert.IsNotNull(orderMultiplier);
        Assert.AreEqual(1.0, orderMultiplier.EventMultiplierRatio, 0.01);
    }

    #endregion
}
