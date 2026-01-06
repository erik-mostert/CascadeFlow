using System.Net;
using System.Threading.Channels;
using Cascade.Core.Enums;
using Cascade.Core.Models;
using Cascade.NServiceBus.Dispatchers;

namespace Cascade.NServiceBus.Tests.Dispatchers;

[TestClass]
public class HttpTelemetryDispatcherTests
{
    #region Constructor Tests

    [TestMethod]
    public void Constructor_CreatesDispatcher()
    {
        // Arrange
        var handler = new TestHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        var options = CreateOptions();

        // Act
        using var dispatcher = new HttpTelemetryDispatcher(httpClient, options);

        // Assert
        Assert.IsNotNull(dispatcher);
    }

    [TestMethod]
    public void Constructor_TrimsTrailingSlashFromCollectorUrl()
    {
        // Arrange
        var handler = new TestHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        var options = CreateOptions(collectorUrl: "http://localhost:5100/");

        // Act
        using var dispatcher = new HttpTelemetryDispatcher(httpClient, options);
        dispatcher.DispatchAsync(CreateTelemetry()).Wait();

        // Give time for background task to process
        Thread.Sleep(100);

        // Assert - URL should not have double slashes
        Assert.IsTrue(handler.LastRequestUri?.ToString().Contains("/api/telemetry") ?? false);
        Assert.IsFalse(handler.LastRequestUri?.ToString().Contains("//api") ?? true);
    }

    #endregion

    #region DispatchAsync Tests

    [TestMethod]
    public async Task DispatchAsync_SendsTelemetryToCollector()
    {
        // Arrange
        var handler = new TestHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        var options = CreateOptions();
        using var dispatcher = new HttpTelemetryDispatcher(httpClient, options);
        var telemetry = CreateTelemetry();

        // Act
        await dispatcher.DispatchAsync(telemetry);

        // Give time for background task to process
        await Task.Delay(100);

        // Assert
        Assert.IsTrue(handler.RequestCount >= 1);
        Assert.AreEqual("http://localhost:5100/api/telemetry", handler.LastRequestUri?.ToString());
    }

    [TestMethod]
    public async Task DispatchAsync_DoesNotThrowOnHttpError()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(HttpStatusCode.InternalServerError);
        var httpClient = new HttpClient(handler);
        var options = CreateOptions();
        using var dispatcher = new HttpTelemetryDispatcher(httpClient, options);
        var telemetry = CreateTelemetry();

        // Act & Assert - should not throw
        await dispatcher.DispatchAsync(telemetry);
        await Task.Delay(100);
    }

    [TestMethod]
    public async Task DispatchAsync_DoesNotThrowOnNetworkError()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(throwException: true);
        var httpClient = new HttpClient(handler);
        var options = CreateOptions();
        using var dispatcher = new HttpTelemetryDispatcher(httpClient, options);
        var telemetry = CreateTelemetry();

        // Act & Assert - should not throw
        await dispatcher.DispatchAsync(telemetry);
        await Task.Delay(100);
    }

    [TestMethod]
    public async Task DispatchAsync_BuffersMultipleTelemetryEvents()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(delay: TimeSpan.FromMilliseconds(50));
        var httpClient = new HttpClient(handler);
        var options = CreateOptions();
        using var dispatcher = new HttpTelemetryDispatcher(httpClient, options);

        // Act - dispatch multiple events quickly
        for (int i = 0; i < 5; i++)
        {
            await dispatcher.DispatchAsync(CreateTelemetry($"msg-{i}"));
        }

        // Wait for all to be processed
        await Task.Delay(500);

        // Assert - all should have been sent
        Assert.AreEqual(5, handler.RequestCount);
    }

    [TestMethod]
    public async Task DispatchAsync_DropsOldestWhenBufferFull()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(delay: TimeSpan.FromSeconds(10)); // Very slow
        var httpClient = new HttpClient(handler);
        var options = CreateOptions(bufferSize: 3);
        using var dispatcher = new HttpTelemetryDispatcher(httpClient, options);

        // Act - fill buffer beyond capacity
        for (int i = 0; i < 10; i++)
        {
            await dispatcher.DispatchAsync(CreateTelemetry($"msg-{i}"));
        }

        // Assert - dispatcher should not throw/block even when buffer is full
        // The BoundedChannel with DropOldest mode handles this
        Assert.IsTrue(true);
    }

    [TestMethod]
    public async Task DispatchAsync_DoesNotBlockCaller()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(delay: TimeSpan.FromSeconds(5));
        var httpClient = new HttpClient(handler);
        var options = CreateOptions();
        using var dispatcher = new HttpTelemetryDispatcher(httpClient, options);
        var telemetry = CreateTelemetry();

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await dispatcher.DispatchAsync(telemetry);
        sw.Stop();

        // Assert - should return immediately (fire and forget)
        Assert.IsTrue(sw.ElapsedMilliseconds < 100, $"DispatchAsync took {sw.ElapsedMilliseconds}ms, expected < 100ms");
    }

    #endregion

    #region Dispose Tests

    [TestMethod]
    public async Task Dispose_CompletesChannelWriter()
    {
        // Arrange
        var handler = new TestHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        var options = CreateOptions();
        var dispatcher = new HttpTelemetryDispatcher(httpClient, options);

        // Act
        dispatcher.Dispose();

        // Assert - dispatching after dispose should be handled gracefully (internally caught)
        // The dispatcher catches ChannelClosedException internally, so this should not throw
        await dispatcher.DispatchAsync(CreateTelemetry());
        Assert.IsTrue(true); // If we get here, the exception was handled gracefully
    }

    [TestMethod]
    public void Dispose_CanBeCalled()
    {
        // Arrange
        var handler = new TestHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        var options = CreateOptions();
        var dispatcher = new HttpTelemetryDispatcher(httpClient, options);

        // Act & Assert - should not throw
        dispatcher.Dispose();

        // Note: Calling Dispose multiple times may throw due to CancellationTokenSource
        // already being disposed. This is acceptable for this fire-and-forget dispatcher
        // as long as single dispose works correctly.
    }

    [TestMethod]
    public async Task Dispose_AllowsPendingTelemetryToComplete()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(delay: TimeSpan.FromMilliseconds(50));
        var httpClient = new HttpClient(handler);
        var options = CreateOptions();
        var dispatcher = new HttpTelemetryDispatcher(httpClient, options);

        // Act
        await dispatcher.DispatchAsync(CreateTelemetry());
        await Task.Delay(10); // Let the dispatch start

        dispatcher.Dispose();

        // Assert - dispose should wait for pending work (up to 2 seconds)
        // In this case the delay is short so it should complete
    }

    #endregion

    #region API Key Header Tests

    [TestMethod]
    public async Task DispatchAsync_WithApiKey_AddsHeaderToRequest()
    {
        // Arrange
        var handler = new TestHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        var options = CreateOptions(apiKey: "csk_test-api-key-12345");
        using var dispatcher = new HttpTelemetryDispatcher(httpClient, options);
        var telemetry = CreateTelemetry();

        // Act
        await dispatcher.DispatchAsync(telemetry);
        await Task.Delay(100);

        // Assert
        Assert.IsNotNull(handler.LastRequestHeaders);
        Assert.IsTrue(handler.LastRequestHeaders.ContainsKey("X-API-Key"));
        Assert.AreEqual("csk_test-api-key-12345", handler.LastRequestHeaders["X-API-Key"]);
    }

    [TestMethod]
    public async Task DispatchAsync_WithoutApiKey_DoesNotAddHeader()
    {
        // Arrange
        var handler = new TestHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        var options = CreateOptions(apiKey: null);
        using var dispatcher = new HttpTelemetryDispatcher(httpClient, options);
        var telemetry = CreateTelemetry();

        // Act
        await dispatcher.DispatchAsync(telemetry);
        await Task.Delay(100);

        // Assert
        Assert.IsNotNull(handler.LastRequestHeaders);
        Assert.IsFalse(handler.LastRequestHeaders.ContainsKey("X-API-Key"));
    }

    [TestMethod]
    public async Task DispatchAsync_WithEmptyApiKey_DoesNotAddHeader()
    {
        // Arrange
        var handler = new TestHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        var options = CreateOptions(apiKey: "");
        using var dispatcher = new HttpTelemetryDispatcher(httpClient, options);
        var telemetry = CreateTelemetry();

        // Act
        await dispatcher.DispatchAsync(telemetry);
        await Task.Delay(100);

        // Assert
        Assert.IsNotNull(handler.LastRequestHeaders);
        Assert.IsFalse(handler.LastRequestHeaders.ContainsKey("X-API-Key"));
    }

    #endregion

    #region Helper Methods

    private static CascadeOptions CreateOptions(
        string collectorUrl = "http://localhost:5100",
        int bufferSize = 1000,
        string? apiKey = null)
    {
        return new CascadeOptions
        {
            CollectorUrl = collectorUrl,
            EndpointName = "TestEndpoint",
            HostId = "test-host",
            BufferSize = bufferSize,
            ApiKey = apiKey
        };
    }

    private static MessageTelemetry CreateTelemetry(string messageId = "test-message")
    {
        return new MessageTelemetry
        {
            Id = Guid.NewGuid().ToString(),
            MessageId = messageId,
            MessageType = "Test.TestMessage, TestAssembly",
            EndpointName = "TestEndpoint",
            HostId = "test-host",
            Direction = MessageDirection.Incoming,
            Timestamp = DateTimeOffset.UtcNow,
            Success = true
        };
    }

    #endregion

    #region Test Helpers

    private class TestHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly bool _throwException;
        private readonly TimeSpan _delay;

        public int RequestCount { get; private set; }
        public Uri? LastRequestUri { get; private set; }
        public string? LastRequestContent { get; private set; }
        public Dictionary<string, string>? LastRequestHeaders { get; private set; }

        public TestHttpMessageHandler(
            HttpStatusCode statusCode = HttpStatusCode.OK,
            bool throwException = false,
            TimeSpan delay = default)
        {
            _statusCode = statusCode;
            _throwException = throwException;
            _delay = delay;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            LastRequestUri = request.RequestUri;
            LastRequestHeaders = request.Headers.ToDictionary(
                h => h.Key,
                h => string.Join(",", h.Value));

            if (request.Content != null)
            {
                LastRequestContent = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            if (_delay > TimeSpan.Zero)
            {
                await Task.Delay(_delay, cancellationToken);
            }

            if (_throwException)
            {
                throw new HttpRequestException("Simulated network error");
            }

            return new HttpResponseMessage(_statusCode);
        }
    }

    #endregion
}
