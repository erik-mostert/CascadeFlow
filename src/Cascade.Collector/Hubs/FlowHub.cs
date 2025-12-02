using Microsoft.AspNetCore.SignalR;
using Cascade.Collector.Services;

namespace Cascade.Collector.Hubs;

/// <summary>
/// SignalR hub for real-time flow and topology updates.
/// </summary>
public class FlowHub : Hub
{
  private readonly IFlowAggregator _flowAggregator;
  private readonly ITopologyAggregator _topologyAggregator;

  public FlowHub(IFlowAggregator flowAggregator, ITopologyAggregator topologyAggregator)
  {
    _flowAggregator = flowAggregator;
    _topologyAggregator = topologyAggregator;
  }

  /// <summary>
  /// Called when a client connects. Sends current state.
  /// </summary>
  public override async Task OnConnectedAsync()
  {
    Console.WriteLine($"Client connected: {Context.ConnectionId}");

    // Send current state to the new connection
    var flows = _flowAggregator.GetActiveFlows();
    var topology = _topologyAggregator.GetTopology();

    await Clients.Caller.SendAsync("InitialState", flows);
    await Clients.Caller.SendAsync("TopologyUpdated", topology);

    await base.OnConnectedAsync();
  }

  public override Task OnDisconnectedAsync(Exception? exception)
  {
    Console.WriteLine($"Client disconnected: {Context.ConnectionId}");
    return base.OnDisconnectedAsync(exception);
  }

  /// <summary>
  /// Subscribe to updates for a specific flow.
  /// </summary>
  public async Task SubscribeToFlow(string correlationId)
  {
    await Groups.AddToGroupAsync(Context.ConnectionId, $"flow-{correlationId}");
    Console.WriteLine($"Client {Context.ConnectionId} subscribed to flow {correlationId}");
  }

  /// <summary>
  /// Unsubscribe from a specific flow.
  /// </summary>
  public async Task UnsubscribeFromFlow(string correlationId)
  {
    await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"flow-{correlationId}");
  }
}