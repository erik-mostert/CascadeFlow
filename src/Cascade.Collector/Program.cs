using Cascade.Collector.Data;
using Cascade.Collector.Hubs;
using Cascade.Collector.Services;
using Cascade.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add database context
var connectionString = builder.Configuration.GetConnectionString("CascadeDb")
    ?? "Server=localhost;Database=CascadeCollector;Trusted_Connection=True;TrustServerCertificate=True;";

builder.Services.AddDbContext<CascadeDbContext>(options =>
    options.UseSqlServer(connectionString));

// Register services
builder.Services.AddSingleton<IFlowAggregator, SqlServerFlowAggregator>();
builder.Services.AddSingleton<ITopologyAggregator, SqlServerTopologyAggregator>();
builder.Services.AddScoped<IImpactAnalyzer, ImpactAnalyzer>();
builder.Services.AddSignalR();

// Configure CORS for frontend development
builder.Services.AddCors(options =>
{
  options.AddDefaultPolicy(policy =>
  {
    policy.WithOrigins("http://localhost:5173", "null")
          .AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials();
  });
});

var app = builder.Build();

// Apply pending database migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CascadeDbContext>();
    Console.WriteLine("Applying database migrations...");
    db.Database.Migrate();
    Console.WriteLine("Database migrations applied successfully.");
}

app.UseCors();

// Map SignalR hub
app.MapHub<FlowHub>("/hubs/flow");

// Health check endpoint
app.MapGet("/api/health", () => new
{
  status = "healthy",
  timestamp = DateTimeOffset.UtcNow
});

// Telemetry ingestion endpoint
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
});

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
    CascadeDbContext db) =>
{
  var now = DateTimeOffset.UtcNow;
  var last24h = now.AddHours(-24);
  var lastHour = now.AddHours(-1);

  var totalMessages = await db.Messages.CountAsync();
  var messagesLast24h = await db.Messages.CountAsync(m => m.CreatedAt >= last24h);
  var messagesLastHour = await db.Messages.CountAsync(m => m.CreatedAt >= lastHour);
  var totalFailures = await db.Messages.CountAsync(m => m.Success == false);
  var failuresLast24h = await db.Messages.CountAsync(m => m.Success == false && m.CreatedAt >= last24h);

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
    int? hours) =>
{
  var hoursToQuery = hours ?? 24;
  var startTime = DateTimeOffset.UtcNow.AddHours(-hoursToQuery);

  var messages = await db.Messages
      .Where(m => m.CreatedAt >= startTime)
      .ToListAsync();

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
    int? limit) =>
{
  var take = limit ?? 10;
  var last24h = DateTimeOffset.UtcNow.AddHours(-24);

  var messages = await db.Messages
      .Where(m => m.CreatedAt >= last24h)
      .ToListAsync();

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
    int? limit) =>
{
  var take = limit ?? 10;
  var last24h = DateTimeOffset.UtcNow.AddHours(-24);

  var messages = await db.Messages
      .Where(m => m.CreatedAt >= last24h && m.Direction == 0 && m.ProcessingDuration != null)
      .ToListAsync();

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
    int? hours) =>
{
  var hoursToQuery = hours ?? 24;
  var startTime = DateTimeOffset.UtcNow.AddHours(-hoursToQuery);

  var messages = await db.Messages
      .Where(m => m.CreatedAt >= startTime)
      .ToListAsync();

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
Console.WriteLine("Cascade Collector running at http://localhost:5100");
Console.WriteLine("SignalR hub available at http://localhost:5100/hubs/flow");

app.Run();