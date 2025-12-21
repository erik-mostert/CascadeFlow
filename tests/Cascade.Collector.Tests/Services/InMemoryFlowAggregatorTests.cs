using Cascade.Collector.Services;
using Cascade.Collector.Tests.Helpers;
using Cascade.Core.Enums;

namespace Cascade.Collector.Tests.Services;

[TestClass]
public class InMemoryFlowAggregatorTests
{
    private InMemoryFlowAggregator _aggregator = null!;

    [TestInitialize]
    public void Setup()
    {
        _aggregator = new InMemoryFlowAggregator();
        TestDataBuilder.ResetCounter();
    }

    #region AddMessage Tests

    [TestMethod]
    public void AddMessage_WithNewCorrelationId_CreatesNewFlow()
    {
        // Arrange
        var telemetry = TestDataBuilder.CreateIncomingMessage(correlationId: "test-correlation-1");

        // Act
        var flow = _aggregator.AddMessage(telemetry);

        // Assert
        Assert.IsNotNull(flow);
        Assert.AreEqual("test-correlation-1", flow.CorrelationId);
        Assert.AreEqual(1, flow.MessageCount);
        Assert.AreEqual(FlowStatus.InProgress, flow.Status);
    }

    [TestMethod]
    public void AddMessage_WithExistingCorrelationId_AddsToExistingFlow()
    {
        // Arrange
        var telemetry1 = TestDataBuilder.CreateIncomingMessage(
            correlationId: "shared-correlation",
            endpointName: "Endpoint1");
        var telemetry2 = TestDataBuilder.CreateIncomingMessage(
            correlationId: "shared-correlation",
            endpointName: "Endpoint2");

        // Act
        _aggregator.AddMessage(telemetry1);
        var flow = _aggregator.AddMessage(telemetry2);

        // Assert
        Assert.AreEqual(2, flow.MessageCount);
        Assert.IsTrue(flow.Messages.Any(m => m.EndpointName == "Endpoint1"));
        Assert.IsTrue(flow.Messages.Any(m => m.EndpointName == "Endpoint2"));
    }

    [TestMethod]
    public void AddMessage_WithNullCorrelationId_UsesMessageIdAsCorrelation()
    {
        // Arrange
        var telemetry = TestDataBuilder.CreateTelemetry(
            messageId: "unique-message-id",
            correlationId: null);

        // Act
        var flow = _aggregator.AddMessage(telemetry);

        // Assert
        Assert.AreEqual("unique-message-id", flow.CorrelationId);
    }

    [TestMethod]
    public void AddMessage_WithFailedMessage_SetsFlowStatusToFailed()
    {
        // Arrange
        var telemetry = TestDataBuilder.CreateFailedMessage(correlationId: "failing-flow");

        // Act
        var flow = _aggregator.AddMessage(telemetry);

        // Assert
        Assert.AreEqual(FlowStatus.Failed, flow.Status);
        Assert.IsTrue(flow.HasFailures);
    }

    [TestMethod]
    public void AddMessage_WithFailedMessageAfterSuccessful_SetsFlowStatusToFailed()
    {
        // Arrange
        var successTelemetry = TestDataBuilder.CreateIncomingMessage(correlationId: "mixed-flow");
        var failedTelemetry = TestDataBuilder.CreateFailedMessage(correlationId: "mixed-flow");

        // Act
        _aggregator.AddMessage(successTelemetry);
        var flow = _aggregator.AddMessage(failedTelemetry);

        // Assert
        Assert.AreEqual(FlowStatus.Failed, flow.Status);
        Assert.IsTrue(flow.HasFailures);
    }

    #endregion

    #region GetFlow Tests

    [TestMethod]
    public void GetFlow_WithExistingCorrelationId_ReturnsFlow()
    {
        // Arrange
        var telemetry = TestDataBuilder.CreateIncomingMessage(correlationId: "existing-flow");
        _aggregator.AddMessage(telemetry);

        // Act
        var flow = _aggregator.GetFlow("existing-flow");

        // Assert
        Assert.IsNotNull(flow);
        Assert.AreEqual("existing-flow", flow.CorrelationId);
    }

    [TestMethod]
    public void GetFlow_WithNonExistingCorrelationId_ReturnsNull()
    {
        // Act
        var flow = _aggregator.GetFlow("non-existing-flow");

        // Assert
        Assert.IsNull(flow);
    }

    #endregion

    #region GetActiveFlows Tests

    [TestMethod]
    public void GetActiveFlows_WithNoFlows_ReturnsEmptyCollection()
    {
        // Act
        var flows = _aggregator.GetActiveFlows();

        // Assert
        Assert.IsFalse(flows.Any());
    }

    [TestMethod]
    public void GetActiveFlows_WithMultipleFlows_ReturnsFlowsOrderedByStartTime()
    {
        // Arrange
        var telemetry1 = TestDataBuilder.CreateIncomingMessage(correlationId: "flow-1");
        var telemetry2 = TestDataBuilder.CreateIncomingMessage(correlationId: "flow-2");
        var telemetry3 = TestDataBuilder.CreateIncomingMessage(correlationId: "flow-3");

        _aggregator.AddMessage(telemetry1);
        Thread.Sleep(10); // Ensure different timestamps
        _aggregator.AddMessage(telemetry2);
        Thread.Sleep(10);
        _aggregator.AddMessage(telemetry3);

        // Act
        var flows = _aggregator.GetActiveFlows().ToList();

        // Assert
        Assert.AreEqual(3, flows.Count);
        // Most recent first
        Assert.AreEqual("flow-3", flows[0].CorrelationId);
        Assert.AreEqual("flow-2", flows[1].CorrelationId);
        Assert.AreEqual("flow-1", flows[2].CorrelationId);
    }

    [TestMethod]
    public void GetActiveFlows_LimitsResultsTo100()
    {
        // Arrange
        for (int i = 0; i < 150; i++)
        {
            var telemetry = TestDataBuilder.CreateIncomingMessage(correlationId: $"flow-{i}");
            _aggregator.AddMessage(telemetry);
        }

        // Act
        var flows = _aggregator.GetActiveFlows().ToList();

        // Assert
        Assert.AreEqual(100, flows.Count);
    }

    #endregion

    #region GetFlowFromDatabaseAsync Tests

    [TestMethod]
    public async Task GetFlowFromDatabaseAsync_WithExistingFlow_ReturnsFlow()
    {
        // Arrange
        var telemetry = TestDataBuilder.CreateIncomingMessage(correlationId: "db-flow");
        _aggregator.AddMessage(telemetry);

        // Act
        var flow = await _aggregator.GetFlowFromDatabaseAsync("db-flow");

        // Assert
        Assert.IsNotNull(flow);
        Assert.AreEqual("db-flow", flow.CorrelationId);
    }

    [TestMethod]
    public async Task GetFlowFromDatabaseAsync_WithNonExistingFlow_ReturnsNull()
    {
        // Act
        var flow = await _aggregator.GetFlowFromDatabaseAsync("non-existing");

        // Assert
        Assert.IsNull(flow);
    }

    #endregion

    #region GetFlowsInTimeRangeAsync Tests

    [TestMethod]
    public async Task GetFlowsInTimeRangeAsync_WithFlowsInRange_ReturnsMatchingFlows()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var telemetry = TestDataBuilder.CreateIncomingMessage(correlationId: "in-range-flow");
        _aggregator.AddMessage(telemetry);

        // Act
        var flows = await _aggregator.GetFlowsInTimeRangeAsync(
            now.AddMinutes(-1),
            now.AddMinutes(1),
            maxResults: 10);

        // Assert
        Assert.IsTrue(flows.Any(f => f.CorrelationId == "in-range-flow"));
    }

    [TestMethod]
    public async Task GetFlowsInTimeRangeAsync_RespectsMaxResults()
    {
        // Arrange
        for (int i = 0; i < 20; i++)
        {
            var telemetry = TestDataBuilder.CreateIncomingMessage(correlationId: $"range-flow-{i}");
            _aggregator.AddMessage(telemetry);
        }

        var now = DateTimeOffset.UtcNow;

        // Act
        var flows = await _aggregator.GetFlowsInTimeRangeAsync(
            now.AddMinutes(-1),
            now.AddMinutes(1),
            maxResults: 5);

        // Assert
        Assert.AreEqual(5, flows.Count());
    }

    #endregion

    #region SearchFlowsAsync Tests

    [TestMethod]
    public async Task SearchFlowsAsync_WithEndpointFilter_ReturnsMatchingFlows()
    {
        // Arrange
        var telemetry1 = TestDataBuilder.CreateIncomingMessage(
            correlationId: "search-1",
            endpointName: "OrderService");
        var telemetry2 = TestDataBuilder.CreateIncomingMessage(
            correlationId: "search-2",
            endpointName: "BillingService");

        _aggregator.AddMessage(telemetry1);
        _aggregator.AddMessage(telemetry2);

        // Act
        var flows = await _aggregator.SearchFlowsAsync(endpoint: "Order");

        // Assert
        var flowList = flows.ToList();
        Assert.AreEqual(1, flowList.Count);
        Assert.AreEqual("search-1", flowList[0].CorrelationId);
    }

    [TestMethod]
    public async Task SearchFlowsAsync_WithMessageTypeFilter_ReturnsMatchingFlows()
    {
        // Arrange
        var telemetry1 = TestDataBuilder.CreateIncomingMessage(
            correlationId: "type-search-1",
            messageType: "Namespace.PlaceOrder, Assembly");
        var telemetry2 = TestDataBuilder.CreateIncomingMessage(
            correlationId: "type-search-2",
            messageType: "Namespace.CancelOrder, Assembly");

        _aggregator.AddMessage(telemetry1);
        _aggregator.AddMessage(telemetry2);

        // Act
        var flows = await _aggregator.SearchFlowsAsync(messageType: "PlaceOrder");

        // Assert
        var flowList = flows.ToList();
        Assert.AreEqual(1, flowList.Count);
        Assert.AreEqual("type-search-1", flowList[0].CorrelationId);
    }

    [TestMethod]
    public async Task SearchFlowsAsync_WithHasFailuresFilter_ReturnsMatchingFlows()
    {
        // Arrange
        var successTelemetry = TestDataBuilder.CreateIncomingMessage(correlationId: "success-flow");
        var failedTelemetry = TestDataBuilder.CreateFailedMessage(correlationId: "failed-flow");

        _aggregator.AddMessage(successTelemetry);
        _aggregator.AddMessage(failedTelemetry);

        // Act
        var failedFlows = await _aggregator.SearchFlowsAsync(hasFailures: true);
        var successFlows = await _aggregator.SearchFlowsAsync(hasFailures: false);

        // Assert
        Assert.AreEqual(1, failedFlows.Count());
        Assert.AreEqual("failed-flow", failedFlows.First().CorrelationId);

        Assert.AreEqual(1, successFlows.Count());
        Assert.AreEqual("success-flow", successFlows.First().CorrelationId);
    }

    [TestMethod]
    public async Task SearchFlowsAsync_WithCombinedFilters_ReturnsMatchingFlows()
    {
        // Arrange
        var telemetry1 = TestDataBuilder.CreateIncomingMessage(
            correlationId: "combo-1",
            endpointName: "OrderService",
            messageType: "Namespace.PlaceOrder, Assembly");
        var telemetry2 = TestDataBuilder.CreateIncomingMessage(
            correlationId: "combo-2",
            endpointName: "BillingService",
            messageType: "Namespace.PlaceOrder, Assembly");

        _aggregator.AddMessage(telemetry1);
        _aggregator.AddMessage(telemetry2);

        // Act
        var flows = await _aggregator.SearchFlowsAsync(
            endpoint: "Order",
            messageType: "PlaceOrder");

        // Assert
        var flowList = flows.ToList();
        Assert.AreEqual(1, flowList.Count);
        Assert.AreEqual("combo-1", flowList[0].CorrelationId);
    }

    [TestMethod]
    public async Task SearchFlowsAsync_IsCaseInsensitive()
    {
        // Arrange
        var telemetry = TestDataBuilder.CreateIncomingMessage(
            correlationId: "case-test",
            endpointName: "OrderService");

        _aggregator.AddMessage(telemetry);

        // Act
        var flows = await _aggregator.SearchFlowsAsync(endpoint: "orderservice");

        // Assert
        Assert.AreEqual(1, flows.Count());
    }

    #endregion

    #region Concurrent Access Tests

    [TestMethod]
    public async Task AddMessage_WithConcurrentAccess_HandlesThreadSafety()
    {
        // Arrange
        var correlationId = "concurrent-flow";
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                var telemetry = TestDataBuilder.CreateIncomingMessage(
                    correlationId: correlationId,
                    endpointName: $"Endpoint-{Thread.CurrentThread.ManagedThreadId}");
                _aggregator.AddMessage(telemetry);
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        var flow = _aggregator.GetFlow(correlationId);
        Assert.IsNotNull(flow);
        Assert.AreEqual(100, flow.MessageCount);
    }

    #endregion
}
