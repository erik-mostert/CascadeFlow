# CascadeFlow.Core

Core models and types for the CascadeFlow telemetry platform.

## Overview

This package contains the shared domain models used by CascadeFlow packages:

- **MessageTelemetry** - Telemetry data for individual messages
- **MessageFlow** - Aggregated message flow with correlation
- **SystemTopology** - Endpoint and connection topology data
- **Enums** - MessageDirection, MessageIntent, FlowStatus

## Usage

This package is typically installed automatically as a dependency of `CascadeFlow.NServiceBus`. You don't usually need to install it directly.

```bash
dotnet add package CascadeFlow.NServiceBus
```

## Links

- [GitHub Repository](https://github.com/erik-mostert/Cascade)
- [CascadeFlow.NServiceBus Package](https://www.nuget.org/packages/CascadeFlow.NServiceBus)

## License

MIT License - see [LICENSE](https://github.com/erik-mostert/Cascade/blob/main/LICENSE) for details.
