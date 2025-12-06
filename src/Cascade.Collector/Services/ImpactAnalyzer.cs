using Cascade.Collector.Models;
using Cascade.Core.Enums;
using Cascade.Core.Models;

namespace Cascade.Collector.Services;

public interface IImpactAnalyzer
{
  FlowImpactMetrics AnalyzeFlow(MessageFlow flow);
  Task<SystemImpactSummary> GetSystemImpactSummaryAsync(int flowCount = 100);
  Task<List<MultiplierEndpoint>> GetMultiplierEndpointsAsync(int flowCount = 100);
}

public class ImpactAnalyzer : IImpactAnalyzer
{
  private readonly IFlowAggregator _flowAggregator;
  private readonly ILogger<ImpactAnalyzer> _logger;

  public ImpactAnalyzer(IFlowAggregator flowAggregator, ILogger<ImpactAnalyzer> logger)
  {
    _flowAggregator = flowAggregator;
    _logger = logger;
  }

  public FlowImpactMetrics AnalyzeFlow(MessageFlow flow)
  {
    var metrics = new FlowImpactMetrics
    {
      CorrelationId = flow.CorrelationId,
      TotalMessages = flow.Messages.Count,
      TotalEndpoints = flow.Messages.Select(m => m.EndpointName).Distinct().Count(),
      HasFailures = flow.HasFailures
    };

    // Build message tree using RelatedTo
    var messageTree = BuildMessageTree(flow.Messages);
    metrics.MessageTree = messageTree;
    metrics.MaxDepth = CalculateMaxDepth(messageTree);

    // Calculate endpoint breakdown
    metrics.EndpointBreakdown = CalculateEndpointBreakdown(flow.Messages);

    // Calculate total processing time
    metrics.TotalProcessingTimeMs = flow.Messages
        .Where(m => m.ProcessingDuration.HasValue)
        .Sum(m => m.ProcessingDuration!.Value.TotalMilliseconds);

    return metrics;
  }

  public async Task<SystemImpactSummary> GetSystemImpactSummaryAsync(int flowCount = 100)
  {
    var flows = await GetRecentFlowsAsync(flowCount);

    if (flows.Count == 0)
    {
      return new SystemImpactSummary();
    }

    var analyzedFlows = flows.Select(AnalyzeFlow).ToList();

    var summary = new SystemImpactSummary
    {
      TotalFlowsAnalyzed = analyzedFlows.Count,
      AverageMessagesPerFlow = analyzedFlows.Average(f => f.TotalMessages),
      AverageEndpointsPerFlow = analyzedFlows.Average(f => f.TotalEndpoints),
      AverageDepth = analyzedFlows.Average(f => f.MaxDepth),
      TopMultipliers = await GetMultiplierEndpointsAsync(flowCount),
      HighImpactMessageTypes = GetHighImpactMessageTypes(flows)
    };

    return summary;
  }

  public async Task<List<MultiplierEndpoint>> GetMultiplierEndpointsAsync(int flowCount = 100)
  {
    var flows = await GetRecentFlowsAsync(flowCount);

    if (flows.Count == 0)
    {
      return new List<MultiplierEndpoint>();
    }

    // Aggregate endpoint stats across all flows
    var endpointStats = new Dictionary<string, (int received, int published, int commands, int events, HashSet<string> outputTypes)>();

    foreach (var flow in flows)
    {
      var published = flow.Messages.Where(m => m.Direction == MessageDirection.Outgoing).ToList();
      var handled = flow.Messages.Where(m => m.Direction == MessageDirection.Incoming).ToList();

      foreach (var msg in handled)
      {
        if (!endpointStats.ContainsKey(msg.EndpointName))
        {
          endpointStats[msg.EndpointName] = (0, 0, 0, 0, new HashSet<string>());
        }
        var stats = endpointStats[msg.EndpointName];
        endpointStats[msg.EndpointName] = (stats.received + 1, stats.published, stats.commands, stats.events, stats.outputTypes);
      }

      foreach (var msg in published)
      {
        if (!endpointStats.ContainsKey(msg.EndpointName))
        {
          endpointStats[msg.EndpointName] = (0, 0, 0, 0, new HashSet<string>());
        }
        var stats = endpointStats[msg.EndpointName];

        var commands = stats.commands + (msg.Intent == MessageIntent.Send ? 1 : 0);
        var events = stats.events + (msg.Intent == MessageIntent.Publish ? 1 : 0);

        stats.outputTypes.Add(msg.MessageTypeShort);
        endpointStats[msg.EndpointName] = (stats.received, stats.published + 1, commands, events, stats.outputTypes);
      }
    }

    // Calculate multipliers
    var multipliers = endpointStats
        .Where(kvp => kvp.Value.received > 0)
        .Select(kvp => new MultiplierEndpoint
        {
          EndpointName = kvp.Key,
          TotalReceived = kvp.Value.received,
          TotalPublished = kvp.Value.published,
          CommandsSent = kvp.Value.commands,
          EventsPublished = kvp.Value.events,
          MultiplierRatio = (double)kvp.Value.published / kvp.Value.received,
          EventMultiplierRatio = (double)kvp.Value.events / kvp.Value.received,
          SampleSize = flows.Count,
          CommonOutputMessages = kvp.Value.outputTypes.Take(5).ToList()
        })
        .OrderByDescending(m => m.EventMultiplierRatio)
        .ToList();

    return multipliers;
  }

  private List<MessageImpact> BuildMessageTree(ICollection<MessageTelemetry> messages)
  {
    var published = messages.Where(m => m.Direction == Core.Enums.MessageDirection.Outgoing).ToList();
    var handled = messages.Where(m => m.Direction == Core.Enums.MessageDirection.Incoming).ToList();

    // Group by messageId
    var messageGroups = published
        .GroupBy(m => m.MessageId)
        .ToDictionary(g => g.Key, g => g.First());

    // Find handlers for each message
    var handlersByMessageId = handled
        .GroupBy(m => m.MessageId)
        .ToDictionary(g => g.Key, g => g.ToList());

    // Build parent-child relationships using RelatedTo
    var childrenByParent = published
        .Where(m => !string.IsNullOrEmpty(m.RelatedTo))
        .GroupBy(m => m.RelatedTo!)
        .ToDictionary(g => g.Key, g => g.ToList());

    // Find root messages (no RelatedTo or RelatedTo not in our message set)
    var rootMessages = published
        .Where(m => string.IsNullOrEmpty(m.RelatedTo) || !messageGroups.ContainsKey(m.RelatedTo))
        .ToList();

    // Build tree recursively
    return rootMessages.Select(m => BuildMessageImpactNode(m, messageGroups, handlersByMessageId, childrenByParent, 0)).ToList();
  }

  private MessageImpact BuildMessageImpactNode(
      MessageTelemetry message,
      Dictionary<string, MessageTelemetry> messageGroups,
      Dictionary<string, List<MessageTelemetry>> handlersByMessageId,
      Dictionary<string, List<MessageTelemetry>> childrenByParent,
      int depth)
  {
    var handlers = handlersByMessageId.GetValueOrDefault(message.MessageId, new List<MessageTelemetry>());
    var children = childrenByParent.GetValueOrDefault(message.MessageId, new List<MessageTelemetry>());

    var childNodes = children
        .Select(c => BuildMessageImpactNode(c, messageGroups, handlersByMessageId, childrenByParent, depth + 1))
        .ToList();

    var impact = new MessageImpact
    {
      MessageId = message.MessageId,
      MessageType = message.MessageTypeShort,
      PublishedBy = message.EndpointName,
      Depth = depth,
      HandledBy = handlers.Select(h => h.EndpointName).Distinct().ToList(),
      Children = childNodes,
      DownstreamMessageCount = childNodes.Sum(c => c.DownstreamMessageCount + 1),
      DownstreamEndpointCount = CountUniqueDownstreamEndpoints(childNodes)
    };

    return impact;
  }

  private int CountUniqueDownstreamEndpoints(List<MessageImpact> children)
  {
    var endpoints = new HashSet<string>();
    CollectEndpoints(children, endpoints);
    return endpoints.Count;
  }

  private void CollectEndpoints(List<MessageImpact> nodes, HashSet<string> endpoints)
  {
    foreach (var node in nodes)
    {
      foreach (var handler in node.HandledBy)
      {
        endpoints.Add(handler);
      }
      endpoints.Add(node.PublishedBy);
      CollectEndpoints(node.Children, endpoints);
    }
  }

  private int CalculateMaxDepth(List<MessageImpact> tree)
  {
    if (tree.Count == 0) return 0;
    return tree.Max(m => CalculateNodeDepth(m));
  }

  private int CalculateNodeDepth(MessageImpact node)
  {
    if (node.Children.Count == 0) return node.Depth;
    return Math.Max(node.Depth, node.Children.Max(CalculateNodeDepth));
  }

  private static List<EndpointImpact> CalculateEndpointBreakdown(ICollection<MessageTelemetry> messages)
  {
    var endpoints = messages
        .GroupBy(m => m.EndpointName)
        .Select(g => {
          var received = g.Count(m => m.Direction == MessageDirection.Incoming);
          var published = g.Count(m => m.Direction == MessageDirection.Outgoing);
          var commandsSent = g.Count(m => m.Direction == MessageDirection.Outgoing && m.Intent == MessageIntent.Send);
          var eventsPublished = g.Count(m => m.Direction == MessageDirection.Outgoing && m.Intent == MessageIntent.Publish);
          var repliesSent = g.Count(m => m.Direction == MessageDirection.Outgoing && m.Intent == MessageIntent.Reply);

          return new EndpointImpact
          {
            EndpointName = g.Key,
            MessagesReceived = received,
            MessagesPublished = published,
            CommandsSent = commandsSent,
            EventsPublished = eventsPublished,
            RepliesSent = repliesSent,
            MultiplierRatio = received > 0 ? (double)published / received : 0,
            EventMultiplierRatio = received > 0 ? (double)eventsPublished / received : 0,
            ProcessingTimeMs = g
                  .Where(m => m.ProcessingDuration.HasValue)
                  .Sum(m => m.ProcessingDuration!.Value.TotalMilliseconds),
            HasFailures = g.Any(m => m.Success == false)
          };
        })
        .ToList();

    return endpoints.OrderByDescending(e => e.EventMultiplierRatio).ToList();
  }

  private List<string> GetHighImpactMessageTypes(List<MessageFlow> flows)
  {
    // Find message types that trigger the most downstream messages
    var messageImpact = new Dictionary<string, int>();

    foreach (var flow in flows)
    {
      var metrics = AnalyzeFlow(flow);
      CountMessageTypeImpact(metrics.MessageTree, messageImpact);
    }

    return messageImpact
        .OrderByDescending(kvp => kvp.Value)
        .Take(10)
        .Select(kvp => kvp.Key)
        .ToList();
  }

  private static void CountMessageTypeImpact(List<MessageImpact> nodes, Dictionary<string, int> impact)
  {
    foreach (var node in nodes)
    {
      if (!impact.ContainsKey(node.MessageType))
      {
        impact[node.MessageType] = 0;
      }
      impact[node.MessageType] += node.DownstreamMessageCount;
      CountMessageTypeImpact(node.Children, impact);
    }
  }

  private async Task<List<MessageFlow>> GetRecentFlowsAsync(int count)
  {
    // Get from in-memory first
    var flows = _flowAggregator.GetActiveFlows().ToList();

    // If we need more, try to get from database
    if (flows.Count < count)
    {
      var historyFlows = await _flowAggregator.GetFlowsInTimeRangeAsync(
          DateTimeOffset.UtcNow.AddDays(-7),
          DateTimeOffset.UtcNow,
          count - flows.Count);

      flows.AddRange(historyFlows);
    }

    return flows.Take(count).ToList();
  }
}