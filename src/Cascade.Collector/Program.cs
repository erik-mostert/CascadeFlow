using Cascade.Collector.Data;
using Cascade.Collector.Filters;
using Cascade.Collector.Hubs;
using Cascade.Collector.Models;
using Cascade.Collector.Services;
using Cascade.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Determine database provider
var connectionString = builder.Configuration.GetConnectionString("CascadeDb");
var useSqlite = string.IsNullOrEmpty(connectionString);

if (useSqlite)
{
    // Default: SQLite for zero-config setup
    var dataPath = Environment.GetEnvironmentVariable("CASCADE_DATA_PATH") ?? "/data";
    Directory.CreateDirectory(dataPath);
    var sqliteConnectionString = $"Data Source={Path.Combine(dataPath, "cascade.db")}";

    builder.Services.AddDbContext<CascadeDbContext>(options =>
        options.UseSqlite(sqliteConnectionString));

    Console.WriteLine($"[Cascade] Using SQLite database at {dataPath}/cascade.db");
}
else
{
    // SQL Server for production/scale
    builder.Services.AddDbContext<CascadeDbContext>(options =>
        options.UseSqlServer(connectionString));

    Console.WriteLine("[Cascade] Using SQL Server database");
}

// Register database provider flag for query optimization
builder.Services.AddSingleton(new DatabaseProviderInfo { UseSqlite = useSqlite });

// Register services
builder.Services.AddSingleton<IFlowAggregator, SqlServerFlowAggregator>();
builder.Services.AddSingleton<ITopologyAggregator, SqlServerTopologyAggregator>();
builder.Services.AddScoped<IImpactAnalyzer, ImpactAnalyzer>();
builder.Services.AddScoped<IApiKeyService, ApiKeyService>();
builder.Services.AddSignalR();

// Configure CORS
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (corsOrigins is { Length: > 0 })
        {
            policy.WithOrigins(corsOrigins);
        }
        else
        {
            // Default: allow any origin (for private network deployments)
            policy.SetIsOriginAllowed(_ => true);
        }

        policy.WithMethods("GET", "POST", "DELETE", "OPTIONS")
              .WithHeaders("Content-Type", "X-API-Key", "X-Requested-With", "X-SignalR-User-Agent")
              .AllowCredentials();
    });
});

var app = builder.Build();

// Initialize database on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CascadeDbContext>();

    if (useSqlite)
    {
        // SQLite: Create database if it doesn't exist
        Console.WriteLine("Initializing SQLite database...");
        db.Database.EnsureCreated();
        Console.WriteLine("SQLite database ready.");
    }
    else
    {
        // SQL Server: Apply migrations
        Console.WriteLine("Applying database migrations...");
        db.Database.Migrate();
        Console.WriteLine("Database migrations applied successfully.");
    }
}

app.UseCors();

// Serve embedded UI
app.UseDefaultFiles();
app.UseStaticFiles();

// Map SignalR hub
app.MapHub<FlowHub>("/hubs/flow");

// Health check endpoint
app.MapGet("/api/health", () => new
{
  status = "healthy",
  timestamp = DateTimeOffset.UtcNow
});

// Telemetry ingestion endpoint (protected by API key)
app.MapPost("/api/telemetry", async (
    MessageTelemetry telemetry,
    IFlowAggregator flowAggregator,
    ITopologyAggregator topologyAggregator,
    IHubContext<FlowHub> hubContext) =>
{
  // Update aggregators
  var flow = flowAggregator.AddMessage(telemetry);
  topologyAggregator.RecordMessage(telemetry);

  Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {telemetry.Direction} | {telemetry.EndpointName} | {telemetry.MessageTypeShort} | Flow: {flow.CorrelationId} ({flow.MessageCount} msgs)");

  // Broadcast to all connected clients
  await hubContext.Clients.All.SendAsync("TelemetryReceived", telemetry);
  await hubContext.Clients.All.SendAsync("FlowUpdated", flow);
  await hubContext.Clients.All.SendAsync("TopologyUpdated", topologyAggregator.GetTopology());

  return Results.Ok(new { received = true, id = telemetry.Id, flowId = flow.CorrelationId });
})
.AddEndpointFilter<ApiKeyAuthenticationFilter>();

// Flow endpoints
app.MapGet("/api/flows", (IFlowAggregator aggregator) =>
    Results.Ok(aggregator.GetActiveFlows()));

app.MapGet("/api/flows/{correlationId}", (string correlationId, IFlowAggregator aggregator) =>
{
  var flow = aggregator.GetFlow(correlationId);
  return flow is not null ? Results.Ok(flow) : Results.NotFound();
});

// Topology endpoints
app.MapGet("/api/topology", (ITopologyAggregator aggregator) =>
    Results.Ok(aggregator.GetTopology()));

app.MapPost("/api/topology/reset", (ITopologyAggregator aggregator) =>
{
  aggregator.Reset();
  return Results.Ok(new { reset = true });
});

// Historical flow queries
app.MapGet("/api/flows/history", async (
    IFlowAggregator aggregator,
    [FromQuery] DateTimeOffset? start,
    [FromQuery] DateTimeOffset? end,
    [FromQuery] int maxResults = 100) =>
{
  var startTime = start ?? DateTimeOffset.UtcNow.AddHours(-1);
  var endTime = end ?? DateTimeOffset.UtcNow;

  var flows = await aggregator.GetFlowsInTimeRangeAsync(startTime, endTime, maxResults);
  return Results.Ok(flows);
});

app.MapGet("/api/flows/search", async (
    IFlowAggregator aggregator,
    [FromQuery] string? endpoint,
    [FromQuery] string? messageType,
    [FromQuery] bool? hasFailures,
    [FromQuery] int maxResults = 100) =>
{
  var flows = await aggregator.SearchFlowsAsync(endpoint, messageType, hasFailures, maxResults);
  return Results.Ok(flows);
});

app.MapGet("/api/flows/{correlationId}/full", async (
    IFlowAggregator aggregator,
    string correlationId) =>
{
  // Try memory first, then database
  var flow = aggregator.GetFlow(correlationId)
      ?? await aggregator.GetFlowFromDatabaseAsync(correlationId);

  return flow is not null ? Results.Ok(flow) : Results.NotFound();
});

// Message statistics endpoint
app.MapGet("/api/stats", async (
    IFlowAggregator flowAggregator,
    ITopologyAggregator topologyAggregator,
    [FromQuery] DateTimeOffset? since) =>
{
  var topology = topologyAggregator.GetTopology();
  var activeFlows = flowAggregator.GetActiveFlows().ToList();

  return Results.Ok(new
  {
    ActiveFlows = activeFlows.Count,
    TotalEndpoints = topology.EndpointCount,
    TotalConnections = topology.ConnectionCount,
    TotalMessages = topology.TotalMessagesObserved,
    FailedFlows = activeFlows.Count(f => f.HasFailures),
    LastUpdated = topology.LastUpdated
  });
});

// Impact analysis endpoints
app.MapGet("/api/impact/{correlationId}", async (
    string correlationId,
    IFlowAggregator flowAggregator,
    IImpactAnalyzer impactAnalyzer) =>
{
  var flow = flowAggregator.GetFlow(correlationId)
      ?? await flowAggregator.GetFlowFromDatabaseAsync(correlationId);

  if (flow is null)
  {
    return Results.NotFound();
  }

  var metrics = impactAnalyzer.AnalyzeFlow(flow);
  return Results.Ok(metrics);
});

app.MapGet("/api/impact/summary", async (
    IImpactAnalyzer impactAnalyzer,
    int? flowCount) =>
{
  var summary = await impactAnalyzer.GetSystemImpactSummaryAsync(flowCount ?? 100);
  return Results.Ok(summary);
});

app.MapGet("/api/impact/multipliers", async (
    IImpactAnalyzer impactAnalyzer,
    int? flowCount) =>
{
  var multipliers = await impactAnalyzer.GetMultiplierEndpointsAsync(flowCount ?? 100);
  return Results.Ok(multipliers);
});

// Dashboard statistics endpoints
app.MapGet("/api/dashboard/stats", async (
    IFlowAggregator flowAggregator,
    CascadeDbContext db,
    DatabaseProviderInfo dbProvider) =>
{
  var now = DateTimeOffset.UtcNow;
  var last24h = now.AddHours(-24);
  var lastHour = now.AddHours(-1);

  int totalMessages, messagesLast24h, messagesLastHour, totalFailures, failuresLast24h;

  if (dbProvider.UseSqlite)
  {
    // SQLite: fetch all and filter client-side (DateTimeOffset not translatable)
    var allMessages = await db.Messages.ToListAsync();
    totalMessages = allMessages.Count;
    messagesLast24h = allMessages.Count(m => m.CreatedAt >= last24h);
    messagesLastHour = allMessages.Count(m => m.CreatedAt >= lastHour);
    totalFailures = allMessages.Count(m => m.Success == false);
    failuresLast24h = allMessages.Count(m => m.Success == false && m.CreatedAt >= last24h);
  }
  else
  {
    // SQL Server: use efficient server-side queries
    totalMessages = await db.Messages.CountAsync();
    messagesLast24h = await db.Messages.CountAsync(m => m.CreatedAt >= last24h);
    messagesLastHour = await db.Messages.CountAsync(m => m.CreatedAt >= lastHour);
    totalFailures = await db.Messages.CountAsync(m => m.Success == false);
    failuresLast24h = await db.Messages.CountAsync(m => m.Success == false && m.CreatedAt >= last24h);
  }

  var activeFlows = flowAggregator.GetActiveFlows().Count();

  return Results.Ok(new
  {
    totalMessages,
    messagesLast24h,
    messagesLastHour,
    totalFailures,
    failuresLast24h,
    failureRate = totalMessages > 0 ? (double)totalFailures / totalMessages * 100 : 0,
    activeFlows,
    timestamp = now
  });
});

app.MapGet("/api/dashboard/messages-over-time", async (
    CascadeDbContext db,
    DatabaseProviderInfo dbProvider,
    int? hours) =>
{
  var hoursToQuery = hours ?? 24;
  var startTime = DateTimeOffset.UtcNow.AddHours(-hoursToQuery);

  List<Cascade.Collector.Data.Entities.StoredMessage> messages;
  if (dbProvider.UseSqlite)
  {
    // SQLite: fetch all and filter client-side
    var allMessages = await db.Messages.ToListAsync();
    messages = allMessages.Where(m => m.CreatedAt >= startTime).ToList();
  }
  else
  {
    // SQL Server: use server-side filtering
    messages = await db.Messages.Where(m => m.CreatedAt >= startTime).ToListAsync();
  }

  var grouped = messages
      .GroupBy(m => new { m.CreatedAt.Year, m.CreatedAt.Month, m.CreatedAt.Day, m.CreatedAt.Hour })
      .Select(g => new
      {
        timestamp = new DateTime(g.Key.Year, g.Key.Month, g.Key.Day, g.Key.Hour, 0, 0).ToString("yyyy-MM-dd HH:mm"),
        hour = $"{g.Key.Hour:00}:00",
        count = g.Count(),
        failures = g.Count(m => m.Success == false)
      })
      .OrderBy(g => g.timestamp)
      .ToList();

  return Results.Ok(grouped);
});

app.MapGet("/api/dashboard/top-endpoints", async (
    CascadeDbContext db,
    DatabaseProviderInfo dbProvider,
    int? limit) =>
{
  var take = limit ?? 10;
  var last24h = DateTimeOffset.UtcNow.AddHours(-24);

  List<Cascade.Collector.Data.Entities.StoredMessage> messages;
  if (dbProvider.UseSqlite)
  {
    var allMessages = await db.Messages.ToListAsync();
    messages = allMessages.Where(m => m.CreatedAt >= last24h).ToList();
  }
  else
  {
    messages = await db.Messages.Where(m => m.CreatedAt >= last24h).ToListAsync();
  }

  var endpoints = messages
      .GroupBy(m => m.EndpointName)
      .Select(g => new
      {
        endpoint = g.Key,
        messageCount = g.Count(),
        failures = g.Count(m => m.Success == false),
        avgProcessingMs = g.Where(m => m.ProcessingDuration != null)
              .Select(m => m.ProcessingDuration!.Value.TotalMilliseconds)
              .DefaultIfEmpty(0)
              .Average()
      })
      .OrderByDescending(e => e.messageCount)
      .Take(take)
      .ToList();

  return Results.Ok(endpoints);
});

app.MapGet("/api/dashboard/slowest-handlers", async (
    CascadeDbContext db,
    DatabaseProviderInfo dbProvider,
    int? limit) =>
{
  var take = limit ?? 10;
  var last24h = DateTimeOffset.UtcNow.AddHours(-24);

  List<Cascade.Collector.Data.Entities.StoredMessage> messages;
  if (dbProvider.UseSqlite)
  {
    var allMessages = await db.Messages.ToListAsync();
    messages = allMessages
        .Where(m => m.CreatedAt >= last24h && m.Direction == 0 && m.ProcessingDuration != null)
        .ToList();
  }
  else
  {
    messages = await db.Messages
        .Where(m => m.CreatedAt >= last24h && m.Direction == 0 && m.ProcessingDuration != null)
        .ToListAsync();
  }

  var handlers = messages
      .GroupBy(m => new { m.EndpointName, m.MessageTypeShort })
      .Select(g => new
      {
        endpoint = g.Key.EndpointName,
        messageType = g.Key.MessageTypeShort,
        avgProcessingMs = g.Average(m => m.ProcessingDuration!.Value.TotalMilliseconds),
        maxProcessingMs = g.Max(m => m.ProcessingDuration!.Value.TotalMilliseconds),
        count = g.Count()
      })
      .OrderByDescending(h => h.avgProcessingMs)
      .Take(take)
      .ToList();

  return Results.Ok(handlers);
});

app.MapGet("/api/dashboard/failure-rate-over-time", async (
    CascadeDbContext db,
    DatabaseProviderInfo dbProvider,
    int? hours) =>
{
  var hoursToQuery = hours ?? 24;
  var startTime = DateTimeOffset.UtcNow.AddHours(-hoursToQuery);

  List<Cascade.Collector.Data.Entities.StoredMessage> messages;
  if (dbProvider.UseSqlite)
  {
    var allMessages = await db.Messages.ToListAsync();
    messages = allMessages.Where(m => m.CreatedAt >= startTime).ToList();
  }
  else
  {
    messages = await db.Messages.Where(m => m.CreatedAt >= startTime).ToListAsync();
  }

  var result = messages
      .GroupBy(m => new { m.CreatedAt.Year, m.CreatedAt.Month, m.CreatedAt.Day, m.CreatedAt.Hour })
      .Select(g => new
      {
        timestamp = new DateTime(g.Key.Year, g.Key.Month, g.Key.Day, g.Key.Hour, 0, 0).ToString("yyyy-MM-dd HH:mm"),
        hour = $"{g.Key.Hour:00}:00",
        total = g.Count(),
        failures = g.Count(m => m.Success == false),
        failureRate = g.Count() > 0 ? (double)g.Count(m => m.Success == false) / g.Count() * 100 : 0
      })
      .OrderBy(g => g.timestamp)
      .ToList();

  return Results.Ok(result);
});
// API Key management endpoints (protected by admin key)
app.MapGet("/api/keys", async (IApiKeyService apiKeyService) =>
{
    var keys = await apiKeyService.GetAllKeysAsync();
    var response = keys.Select(k => new ApiKeyResponse(
        k.Id,
        k.KeyPrefix,
        k.Name,
        k.EndpointName,
        k.CreatedAt,
        k.LastUsedAt,
        k.IsActive
    ));
    return Results.Ok(response);
}).AddEndpointFilter<AdminKeyAuthenticationFilter>();

app.MapPost("/api/keys", async (
    CreateApiKeyRequest request,
    IApiKeyService apiKeyService) =>
{
    if (string.IsNullOrWhiteSpace(request.Name))
    {
        return Results.BadRequest(new { error = "Name is required" });
    }

    var (plaintextKey, entity) = await apiKeyService.CreateKeyAsync(request.Name, request.EndpointName);

    var response = new CreateApiKeyResponse(
        entity.Id,
        plaintextKey, // Only time the key is returned
        entity.KeyPrefix,
        entity.Name,
        entity.EndpointName,
        entity.CreatedAt,
        entity.IsActive
    );

    return Results.Created($"/api/keys/{entity.Id}", response);
}).AddEndpointFilter<AdminKeyAuthenticationFilter>();

app.MapPost("/api/keys/{id:int}/revoke", async (int id, IApiKeyService apiKeyService) =>
{
    var success = await apiKeyService.RevokeKeyAsync(id);
    return success
        ? Results.Ok(new { revoked = true, id })
        : Results.NotFound(new { error = "API key not found" });
}).AddEndpointFilter<AdminKeyAuthenticationFilter>();

app.MapDelete("/api/keys/{id:int}", async (int id, IApiKeyService apiKeyService) =>
{
    var success = await apiKeyService.DeleteKeyAsync(id);
    return success
        ? Results.Ok(new { deleted = true, id })
        : Results.NotFound(new { error = "API key not found" });
}).AddEndpointFilter<AdminKeyAuthenticationFilter>();

// SPA fallback - serves index.html for non-API routes (must be last)
app.MapFallbackToFile("index.html");

Console.WriteLine("Cascade Collector running at http://localhost:5100");
Console.WriteLine("SignalR hub available at http://localhost:5100/hubs/flow");

app.Run();

/// <summary>
/// Configuration class to indicate which database provider is in use.
/// Used to optimize queries for each provider.
/// </summary>
public class DatabaseProviderInfo
{
    public bool UseSqlite { get; init; }
}