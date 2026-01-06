using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Cascade.Core.Models;

namespace Cascade.NServiceBus.Framework.Dispatchers
{
    /// <summary>
    /// Dispatches telemetry to the Cascade collector via HTTP.
    /// Uses a bounded channel to buffer events and sends them in the background.
    /// Never blocks or slows down message processing.
    /// </summary>
    public class HttpTelemetryDispatcher : ITelemetryDispatcher
    {
        private readonly HttpClient _httpClient;
        private readonly string _telemetryUrl;
        private readonly string _apiKey;
        private readonly Channel<MessageTelemetry> _channel;
        private readonly Task _sendTask;
        private readonly CancellationTokenSource _cts;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// The HTTP header name for the API key.
        /// </summary>
        public const string ApiKeyHeaderName = "X-API-Key";

        public HttpTelemetryDispatcher(HttpClient httpClient, CascadeOptions options)
        {
            _httpClient = httpClient;
            _telemetryUrl = options.CollectorUrl.TrimEnd('/') + "/api/telemetry";
            _apiKey = options.ApiKey;
            _cts = new CancellationTokenSource();

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            // Bounded channel - drops oldest if full (never blocks producer)
            _channel = Channel.CreateBounded<MessageTelemetry>(new BoundedChannelOptions(options.BufferSize)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            });

            // Start background sender
            _sendTask = Task.Run(ProcessChannelAsync);
        }

        public async Task DispatchAsync(MessageTelemetry telemetry, CancellationToken ct = default)
        {
            try
            {
                // Non-blocking write to channel
                await _channel.Writer.WriteAsync(telemetry, ct).ConfigureAwait(false);
            }
            catch (ChannelClosedException)
            {
                // Dispatcher is being disposed, ignore
            }
            catch
            {
                // Never throw from telemetry - just drop it
            }
        }

        private async Task ProcessChannelAsync()
        {
            try
            {
                while (await _channel.Reader.WaitToReadAsync(_cts.Token).ConfigureAwait(false))
                {
                    MessageTelemetry telemetry;
                    while (_channel.Reader.TryRead(out telemetry))
                    {
                        await SendTelemetryAsync(telemetry).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
            }
        }

        private async Task SendTelemetryAsync(MessageTelemetry telemetry)
        {
            try
            {
                var json = JsonSerializer.Serialize(telemetry, _jsonOptions);

                using (var request = new HttpRequestMessage(HttpMethod.Post, _telemetryUrl))
                {
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                    // Add API key header if configured
                    if (!string.IsNullOrEmpty(_apiKey))
                    {
                        request.Headers.Add(ApiKeyHeaderName, _apiKey);
                    }

                    using (var response = await _httpClient.SendAsync(request, _cts.Token).ConfigureAwait(false))
                    {
                        // Log failures but don't throw
                        if (!response.IsSuccessStatusCode)
                        {
                            Console.WriteLine("[Cascade] Failed to send telemetry: " + response.StatusCode);
                        }
                    }
                }
            }
            catch (HttpRequestException)
            {
                // Collector unavailable - silently drop
            }
            catch (TaskCanceledException)
            {
                // Request cancelled - silently drop
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Cascade] Unexpected error sending telemetry: " + ex.Message);
            }
        }

        public void Dispose()
        {
            _channel.Writer.Complete();
            _cts.Cancel();

            try
            {
                // Give background task a moment to finish
                _sendTask.Wait(TimeSpan.FromSeconds(2));
            }
            catch
            {
                // Ignore shutdown errors
            }

            _cts.Dispose();
        }
    }
}
