using Cascade.Collector.Data;
using Cascade.Collector.Hubs;
using Cascade.Collector.Services;
using Cascade.Core.Models;
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

Console.WriteLine("Cascade Collector running at http://localhost:5100");
Console.WriteLine("SignalR hub available at http://localhost:5100/hubs/flow");

app.Run();