using Cascade.Collector.Data;
using Cascade.Collector.Data.Entities;
using Cascade.Core.Enums;
using Cascade.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace Cascade.Collector.Services;

public class SqlServerTopologyAggregator : ITopologyAggregator
{
    private readonly InMemoryTopologyAggregator _memoryAggregator;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SqlServerTopologyAggregator> _logger;

    public SqlServerTopologyAggregator(
        IServiceScopeFactory scopeFactory,
        ILogger<SqlServerTopologyAggregator> logger)
    {
        _memoryAggregator = new InMemoryTopologyAggregator();
        _scopeFactory = scopeFactory;
        _logger = logger;

        // Load existing data from database on startup
        _ = LoadFromDatabaseAsync();
    }

    public void RecordMessage(MessageTelemetry telemetry)
    {
        // Update in-memory for real-time
        _memoryAggregator.RecordMessage(telemetry);

        // Persist async
        _ = PersistTopologyAsync(telemetry);
    }

    public SystemTopology GetTopology()
    {
        return _memoryAggregator.GetTopology();
    }

    public void Reset()
    {
        _memoryAggregator.Reset();
        _ = ClearDatabaseAsync();
    }

    private async Task LoadFromDatabaseAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CascadeDbContext>();

            // Load endpoints
            var endpoints = await db.Endpoints.ToListAsync();
            foreach (var ep in endpoints)
            {
                var topology = _memoryAggregator.GetTopology();
                topology.Endpoints[ep.Name] = new TopologyEndpoint
                {
                    Name = ep.Name,
                    FirstSeen = ep.FirstSeen,
                    LastSeen = ep.LastSeen,
                    MessagesReceived = ep.MessagesReceived,
                    MessagesSent = ep.MessagesSent,
                    Failures = ep.Failures,
                    AverageProcessingTimeMs = ep.ProcessingTimeCount > 0
                        ? ep.TotalProcessingTimeMs / ep.ProcessingTimeCount
                        : 0
                };
            }

            // Load connections
            var connections = await db.Connections.ToListAsync();
            var topology2 = _memoryAggregator.GetTopology();
            foreach (var conn in connections)
            {
                topology2.Connections.Add(new TopologyConnection
                {
                    SourceEndpoint = conn.SourceEndpoint,
                    TargetEndpoint = conn.TargetEndpoint,
                    MessageType = conn.MessageType,
                    MessageCount = conn.MessageCount,
                    FailureCount = conn.FailureCount,
                    FirstSeen = conn.FirstSeen,
                    LastSeen = conn.LastSeen,
                    AverageLatencyMs = conn.LatencyCount > 0
                        ? conn.TotalLatencyMs / conn.LatencyCount
                        : 0
                });
            }

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Loaded {EndpointCount} endpoints and {ConnectionCount} connections from database",
                    endpoints.Count, connections.Count);
            }
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "Failed to load topology from database");
            }
        }
    }

    private async Task PersistTopologyAsync(MessageTelemetry telemetry)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CascadeDbContext>();

            // Upsert endpoint
            var endpoint = await db.Endpoints.FirstOrDefaultAsync(e => e.Name == telemetry.EndpointName);
            if (endpoint == null)
            {
                endpoint = new StoredEndpoint
                {
                    Name = telemetry.EndpointName,
                    FirstSeen = telemetry.Timestamp,
                    LastSeen = telemetry.Timestamp,
                    MessagesReceived = 0,
                    MessagesSent = 0,
                    Failures = 0
                };
                db.Endpoints.Add(endpoint);
            }

            endpoint.LastSeen = telemetry.Timestamp;

            if (telemetry.Direction == MessageDirection.Incoming)
            {
                endpoint.MessagesReceived++;
                if (telemetry.ProcessingDuration.HasValue)
                {
                    endpoint.TotalProcessingTimeMs += telemetry.ProcessingDuration.Value.TotalMilliseconds;
                    endpoint.ProcessingTimeCount++;
                }

                if (telemetry.Success == false)
                {
                    endpoint.Failures++;
                }
            }
            else
            {
                endpoint.MessagesSent++;
            }

            // Upsert connection if incoming and has originating endpoint
            if (telemetry.Direction == MessageDirection.Incoming &&
                !string.IsNullOrEmpty(telemetry.OriginatingEndpoint) &&
                telemetry.OriginatingEndpoint != telemetry.EndpointName)
            {
                var connection = await db.Connections.FirstOrDefaultAsync(c =>
                    c.SourceEndpoint == telemetry.OriginatingEndpoint &&
                    c.TargetEndpoint == telemetry.EndpointName &&
                    c.MessageType == telemetry.MessageType);

                if (connection == null)
                {
                    connection = new StoredConnection
                    {
                        SourceEndpoint = telemetry.OriginatingEndpoint,
                        TargetEndpoint = telemetry.EndpointName,
                        MessageType = telemetry.MessageType,
                        MessageTypeShort = telemetry.MessageTypeShort ?? "UNKNOWN",
                        MessageCount = 0,
                        FailureCount = 0,
                        FirstSeen = telemetry.Timestamp,
                        LastSeen = telemetry.Timestamp
                    };
                    db.Connections.Add(connection);
                }

                connection.MessageCount++;
                connection.LastSeen = telemetry.Timestamp;

                if (telemetry.Success == false)
                {
                    connection.FailureCount++;
                }
            }

            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "Failed to persist topology for {Endpoint}", telemetry.EndpointName);
            }
        }
    }

    private async Task ClearDatabaseAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CascadeDbContext>();

            await db.Endpoints.ExecuteDeleteAsync();
            await db.Connections.ExecuteDeleteAsync();

            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation("Cleared topology from database");
            }
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "Failed to clear topology from database");
            }
        }
    }
}