# CascadeFlow.NServiceBus.Framework

Zero-config telemetry for NServiceBus 8.x endpoints on .NET Framework 4.7.2+.

## Installation

```powershell
Install-Package CascadeFlow.NServiceBus.Framework
```

## Quick Start

**That's it!** Once installed, the package automatically registers telemetry behaviors via `INeedInitialization`. No code changes required.

By default, telemetry is sent to `http://localhost:5100`.

## Requirements

- .NET Framework 4.7.2 or later
- NServiceBus 8.x

## For .NET Core / .NET 5+

Use the `CascadeFlow.NServiceBus` package instead, which targets modern .NET with NServiceBus 9.x.

## Configuration

### Environment Variables (Zero-Config)

| Variable | Default | Description |
|----------|---------|-------------|
| `CASCADE_COLLECTOR_URL` | `http://localhost:5100` | Collector API URL |
| `CASCADE_ENDPOINT_NAME` | Auto-detected | Endpoint identifier |
| `CASCADE_HOST_ID` | Machine name | Host/instance identifier |
| `CASCADE_ENABLED` | `true` | Set to `false` to disable |
| `CASCADE_API_KEY` | *(none)* | API key for authentication (if collector requires it) |

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
    options.ApiKey = "csk_your-api-key"; // If collector requires authentication
});
```

### Disable Telemetry

```csharp
endpointConfiguration.DisableCascade();
```

## Links

- [GitHub Repository](https://github.com/erik-mostert/Cascade)
- [Modern .NET Package](https://www.nuget.org/packages/CascadeFlow.NServiceBus)
