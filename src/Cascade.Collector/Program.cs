using Cascade.Core.Models;
using Cascade.Collector.Services;

var builder = WebApplication.CreateBuilder(args);

// Register services
builder.Services.AddSingleton<IFlowAggregator, InMemoryFlowAggregator>();

// Configure CORS for frontend development
builder.Services.AddCors(options =>
{
  options.AddDefaultPolicy(policy =>
  {
    policy.WithOrigins("http://localhost:5173")
          .AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials();
  });
});

var app = builder.Build();

app.UseCors();

// Health check endpoint
app.MapGet("/api/health", () => new
{
  status = "healthy",
  timestamp = DateTimeOffset.UtcNow
});

// Telemetry ingestion endpoint
app.MapPost("/api/telemetry", (MessageTelemetry telemetry, IFlowAggregator aggregator) =>
{
  var flow = aggregator.AddMessage(telemetry);

  Console.WriteLine($"[{telemetry.Timestamp:HH:mm:ss.fff}] {telemetry.Direction} | {telemetry.EndpointName} | {telemetry.MessageTypeShort} | Flow: {flow.CorrelationId} ({flow.MessageCount} msgs)");

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

// Placeholder for topology
app.MapGet("/api/topology", () => Results.Ok(new SystemTopology()));

Console.WriteLine("Cascade Collector running at http://localhost:5100");

app.Run();