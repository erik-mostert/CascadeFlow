# CascadeFlow.NServiceBus

Zero-config telemetry for NServiceBus endpoints. Automatically captures message flows, endpoint topology, and performance metrics.

## Installation

```bash
dotnet add package CascadeFlow.NServiceBus --prerelease
```

## Quick Start

**That's it!** Once installed, the package automatically registers telemetry behaviors via `INeedInitialization`. No code changes required.

By default, telemetry is sent to `http://localhost:5100`.

## Running the Collector

Start the Cascade Collector using Docker (zero-config with SQLite):

```bash
docker run -d -p 5100:8080 ghcr.io/erik-mostert/cascade-collector:main
```

That's it! The collector uses SQLite by default - no database setup required.

### Persist Data Across Restarts

Mount a volume to keep your data:

```bash
docker run -d -p 5100:8080 -v cascade-data:/data ghcr.io/erik-mostert/cascade-collector:main
```

### Use SQL Server (Optional)

For production scale, provide a SQL Server connection string:

```bash
docker run -d -p 5100:8080 \
  -e "ConnectionStrings__CascadeDb=Server=your-server;Database=CascadeCollector;User Id=sa;Password=YourPassword;TrustServerCertificate=True;" \
  ghcr.io/erik-mostert/cascade-collector:main
```

## Configuration

### Environment Variables (Zero-Config)

| Variable | Default | Description |
|----------|---------|-------------|
| `CASCADE_COLLECTOR_URL` | `http://localhost:5100` | Collector API URL |
| `CASCADE_ENDPOINT_NAME` | Auto-detected | Endpoint identifier |
| `CASCADE_HOST_ID` | Machine name | Host/instance identifier |
| `CASCADE_ENABLED` | `true` | Set to `false` to disable |

### Explicit Configuration

```csharp
var endpointConfiguration = new EndpointConfiguration("MyEndpoint");

endpointConfiguration.UseCascade(options =>
{
    options.CollectorUrl = "http://localhost:5100";
    options.EndpointName = "MyEndpoint";
    options.HostId = "instance-1";
    options.IncludeHeaders = true;
    options.BufferSize = 1000;
});
```

### Disable Telemetry

```csharp
endpointConfiguration.DisableCascade();
```

Or via environment variable:

```bash
CASCADE_ENABLED=false
```

## What Gets Captured

- Message send/receive events
- Processing duration
- Success/failure status
- Message headers and correlation IDs
- Endpoint topology (who talks to whom)

## Links

- [GitHub Repository](https://github.com/erik-mostert/Cascade)
- [Documentation](https://github.com/erik-mostert/Cascade#readme)
- [Report Issues](https://github.com/erik-mostert/Cascade/issues)
