using Cascade.Collector.Data;
using Cascade.Collector.Data.Entities;
using Cascade.Core.Enums;
using Cascade.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Cascade.Collector.Services;

public class SqlServerFlowAggregator : IFlowAggregator
{
  private readonly InMemoryFlowAggregator _memoryAggregator;
  private readonly IServiceScopeFactory _scopeFactory;
  private readonly ILogger<SqlServerFlowAggregator> _logger;

  public SqlServerFlowAggregator(
      IServiceScopeFactory scopeFactory,
      ILogger<SqlServerFlowAggregator> logger)
  {
    _memoryAggregator = new InMemoryFlowAggregator();
    _scopeFactory = scopeFactory;
    _logger = logger;
  }

  /// <inheritdoc/>
  public MessageFlow AddMessage(MessageTelemetry telemetry)
  {
    // Add to in-memory for real-time updates
    var flow = _memoryAggregator.AddMessage(telemetry);

    // Persist async (fire-and-forget)
    _ = PersistMessageAsync(telemetry);

    return flow;
  }

  /// <inheritdoc/>
  public MessageFlow? GetFlow(string correlationId)
  {
    return _memoryAggregator.GetFlow(correlationId);
  }

  /// <inheritdoc/>
  public IEnumerable<MessageFlow> GetActiveFlows()
  {
    return _memoryAggregator.GetActiveFlows();
  }

  /// <inheritdoc/>
  public async Task<MessageFlow?> GetFlowFromDatabaseAsync(string correlationId)
  {
    using var scope = _scopeFactory.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CascadeDbContext>();

    var messages = await db.Messages
        .Where(m => m.CorrelationId == correlationId)
        .OrderBy(m => m.Timestamp)
        .ToListAsync();

    if (messages.Count == 0)
      return null;

    var flow = new MessageFlow
    {
      CorrelationId = correlationId,
      StartedAt = messages.First().Timestamp,
      Status = FlowStatus.Completed,
      Messages = []
    };

    foreach (var msg in messages)
    {
      flow.Messages.Add(MapToTelemetry(msg));
    }

    if (messages.Any(m => m.Success == false))
    {
      flow.Status = FlowStatus.Failed;
    }

    return flow;
  }

  /// <inheritdoc/>
  public async Task<IEnumerable<MessageFlow>> GetFlowsInTimeRangeAsync(
      DateTimeOffset start,
      DateTimeOffset end,
      int maxResults = 100)
  {
    using var scope = _scopeFactory.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CascadeDbContext>();

    var correlationIds = await db.Messages
        .Where(m => m.Timestamp >= start && m.Timestamp <= end && m.CorrelationId != null)
        .Select(m => m.CorrelationId!)
        .Distinct()
        .Take(maxResults)
        .ToListAsync();

    var flows = new List<MessageFlow>();
    foreach (var correlationId in correlationIds)
    {
      var flow = await GetFlowFromDatabaseAsync(correlationId);
      if (flow != null)
      {
        flows.Add(flow);
      }
    }

    return flows;
  }

  /// <inheritdoc/>
  public async Task<IEnumerable<MessageFlow>> SearchFlowsAsync(
    string? endpoint = null,
    string? messageType = null,
    bool? hasFailures = null,
    int maxResults = 100)
  {
    using var scope = _scopeFactory.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CascadeDbContext>();

    // Build query to find matching correlation IDs
    var query = db.Messages.AsQueryable();

    if (!string.IsNullOrEmpty(endpoint))
    {
      query = query.Where(m => m.EndpointName.Contains(endpoint));
    }

    if (!string.IsNullOrEmpty(messageType))
    {
      query = query.Where(m => m.MessageTypeShort.Contains(messageType));
    }

    if (hasFailures == true)
    {
      query = query.Where(m => m.Success == false);
    }

    var correlationIds = await query
        .Where(m => m.CorrelationId != null)
        .Select(m => m.CorrelationId!)
        .Distinct()
        .Take(maxResults)
        .ToListAsync();

    // Fetch full flows
    var flows = new List<MessageFlow>();
    foreach (var correlationId in correlationIds)
    {
      var flow = await GetFlowFromDatabaseAsync(correlationId);
      if (flow != null)
      {
        // Apply hasFailures filter at flow level if specified as false
        if (hasFailures == false && flow.HasFailures)
          continue;

        flows.Add(flow);
      }
    }

    return flows.OrderByDescending(f => f.StartedAt).Take(maxResults);
  }
  private async Task PersistMessageAsync(MessageTelemetry telemetry)
  {
    try
    {
      using var scope = _scopeFactory.CreateScope();
      var db = scope.ServiceProvider.GetRequiredService<CascadeDbContext>();

      var entity = new StoredMessage
      {
        MessageId = telemetry.MessageId,
        CorrelationId = telemetry.CorrelationId,
        ConversationId = telemetry.ConversationId,
        CausationId = telemetry.CausationId,
        RelatedTo = telemetry.RelatedTo,
        MessageType = telemetry.MessageType,
        MessageTypeShort = telemetry.MessageTypeShort,
        EndpointName = telemetry.EndpointName,
        HostId = telemetry.HostId,
        Direction = (int)telemetry.Direction,
        Timestamp = telemetry.Timestamp,
        ProcessingDuration = telemetry.ProcessingDuration,
        Success = telemetry.Success,
        ExceptionType = telemetry.ExceptionType,
        ExceptionMessage = telemetry.ExceptionMessage,
        OriginatingEndpoint = telemetry.OriginatingEndpoint,
        SagaId = telemetry.SagaId,
        SagaType = telemetry.SagaType,
        RetryCount = telemetry.RetryCount,
        CreatedAt = DateTimeOffset.UtcNow,
        Intent = (int)telemetry.Intent
      };

      db.Messages.Add(entity);
      await db.SaveChangesAsync();
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Failed to persist message {MessageId}", telemetry.MessageId);
    }
  }

  private static MessageTelemetry MapToTelemetry(StoredMessage msg)
  {
    return new MessageTelemetry
    {
      Id = msg.Id.ToString(),
      MessageId = msg.MessageId,
      CorrelationId = msg.CorrelationId,
      ConversationId = msg.ConversationId,
      CausationId = msg.CausationId,
      RelatedTo = msg.RelatedTo,
      MessageType = msg.MessageType,
      EndpointName = msg.EndpointName,
      HostId = msg.HostId,
      Direction = (MessageDirection)msg.Direction,
      Timestamp = msg.Timestamp,
      ProcessingDuration = msg.ProcessingDuration,
      Success = msg.Success,
      ExceptionType = msg.ExceptionType,
      ExceptionMessage = msg.ExceptionMessage,
      OriginatingEndpoint = msg.OriginatingEndpoint,
      SagaId = msg.SagaId,
      SagaType = msg.SagaType,
      RetryCount = msg.RetryCount,
      Intent = (MessageIntent)msg.Intent
    };
  }
}