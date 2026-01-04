# Cascade

Cascade is a real-time telemetry and visualization platform for NServiceBus-based distributed systems. It captures message flows across microservices, aggregates them into correlated flows, and provides a web-based UI for observing system behavior.

## Features

- **Message Flow Tracking**: Automatically captures incoming and outgoing messages from NServiceBus endpoints, correlating them into end-to-end flows
- **System Topology Discovery**: Builds a live topology map showing how your services communicate
- **Impact Analysis**: Analyzes message propagation to identify high-impact endpoints and message types
- **Real-time Dashboard**: Displays metrics including message throughput, slowest handlers, and failure rates
- **Zero-config Integration**: Drop-in NServiceBus plugin that requires minimal configuration

## Architecture

```
┌────────────────────────┐     ┌────────────────────────┐
│  NServiceBus           │     │  NServiceBus           │
│  Endpoint A            │     │  Endpoint B            │
│  + Cascade.NServiceBus |     │  + Cascade.NServiceBus |
└─────────┬──────────────┘     └──────┬─────────────────┘
          │ HTTP POST                 │ HTTP POST
          │ /api/telemetry            │ /api/telemetry
          ▼                           ▼
       ┌─────────────────────────────────┐
       │       Cascade.Collector         │
       │   (ASP.NET Core + SQL Server)   │
       └─────────────┬───────────────────┘
                     │ SignalR
                     ▼
       ┌─────────────────────────────────┐
       │         Cascade.Web             │
       │      (React + TypeScript)       │
       └─────────────────────────────────┘
```

## Getting Started

### Option 1: Docker (Recommended)

The easiest way to run the Cascade Collector is with Docker Compose:

```bash
docker compose up
```

This starts both the Collector and SQL Server. The collector will be available at `http://localhost:5100`.

To run just the collector (if you have your own SQL Server):

```bash
docker build -t cascade-collector .
docker run -p 5100:8080 -e ConnectionStrings__CascadeDb="Server=host.docker.internal;Database=CascadeCollector;..." cascade-collector
```

### Option 2: Local Development

Prerequisites:
- .NET 10.0 SDK
- SQL Server (LocalDB or SQL Server Express)
- Node.js 18+

```bash
# Start the collector API (database migrations run automatically)
dotnet run --project src/Cascade.Collector
```

The collector will be available at `http://localhost:5100`.

### Running the Web UI

```bash
cd src/Cascade.Web
npm install
npm run dev
```

The web UI will be available at `http://localhost:5173`.

### Integrating with NServiceBus

Choose the package that matches your environment:

| Package | Target Framework | NServiceBus Version |
|---------|------------------|---------------------|
| `CascadeFlow.NServiceBus` | .NET 10+ | 9.x |
| `CascadeFlow.NServiceBus.Framework` | .NET Framework 4.7.2+ | 8.x |

```bash
# For .NET 10+ with NServiceBus 9.x
dotnet add package CascadeFlow.NServiceBus

# For .NET Framework 4.7.2+ with NServiceBus 8.x
dotnet add package CascadeFlow.NServiceBus.Framework
```

**That's it!** The package automatically registers telemetry behaviors via `INeedInitialization`. No code changes required.

#### Optional: Explicit Configuration

```csharp
var endpointConfiguration = new EndpointConfiguration("MyEndpoint");
endpointConfiguration.UseCascade(options =>
{
    options.CollectorUrl = "http://localhost:5100";
});
```

Or use environment variables for zero-code configuration:

- `CASCADE_COLLECTOR_URL`: URL of the Cascade collector
- `CASCADE_ENDPOINT_NAME`: Override the endpoint name (defaults to NServiceBus endpoint name)

## Project Structure

| Project | Description |
|---------|-------------|
| `Cascade.Core` | Shared domain models and enums |
| `Cascade.Collector` | ASP.NET Core API and SignalR hub for receiving and serving telemetry |
| `Cascade.NServiceBus` | NServiceBus 9.x plugin for .NET 10+ |
| `Cascade.NServiceBus.Framework` | NServiceBus 8.x plugin for .NET Framework 4.7.2+ |
| `Cascade.Web` | React-based visualization dashboard |
| `Cascade.Sample.*` | Sample microservices demonstrating integration |

## Resilience

The NServiceBus integration is designed to never impact your services:

- Telemetry dispatch is asynchronous and non-blocking
- If the collector is unavailable, telemetry is silently dropped
- No exceptions propagate to message handlers
- Bounded buffer prevents memory buildup
