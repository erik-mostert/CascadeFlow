using System.Net.Http.Json;
using System.Threading.Channels;
using Cascade.Core.Models;

namespace Cascade.NServiceBus.Dispatchers;

/// <summary>
/// Dispatches telemetry to the Cascade collector via HTTP.
/// Uses a bounded channel to buffer events and sends them in the background.
/// Never blocks or slows down message processing.
/// </summary>
public class HttpTelemetryDispatcher : ITelemetryDispatcher
{
  private readonly HttpClient _httpClient;
  private readonly string _telemetryUrl;
  private readonly Channel<MessageTelemetry> _channel;
  private readonly Task _sendTask;
  private readonly CancellationTokenSource _cts;

  public HttpTelemetryDispatcher(HttpClient httpClient, CascadeOptions options)
  {
    _httpClient = httpClient;
    _telemetryUrl = $"{options.CollectorUrl.TrimEnd('/')}/api/telemetry";
    _cts = new CancellationTokenSource();

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
      await foreach (var telemetry in _channel.Reader.ReadAllAsync(_cts.Token))
      {
        await SendTelemetryAsync(telemetry);
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
      using var response = await _httpClient.PostAsJsonAsync(_telemetryUrl, telemetry, _cts.Token);

      // Log failures but don't throw
      if (!response.IsSuccessStatusCode)
      {
        Console.WriteLine($"[Cascade] Failed to send telemetry: {response.StatusCode}");
      }
    }
    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
    {
      // Collector unavailable - silently drop
      // In production, you might want to log this occasionally
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[Cascade] Unexpected error sending telemetry: {ex.Message}");
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