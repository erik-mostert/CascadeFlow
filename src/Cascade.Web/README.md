# Cascade.Web

React-based visualization dashboard for CascadeFlow telemetry.

## Overview

This is the web frontend for CascadeFlow, providing real-time visualization of NServiceBus message flows, system topology, and performance metrics.

## Features

- **Message Flows**: View correlated message flows with visual graphs showing message paths
- **System Topology**: Interactive graph of endpoint connections and message types
- **Impact Analysis**: Identify high-impact endpoints and message multipliers
- **Dashboard**: Real-time metrics including throughput, failure rates, and slowest handlers
- **API Key Management**: Create and manage API keys for telemetry authentication

## Technology Stack

- React 19
- TypeScript 5.9
- Vite 7
- Tailwind CSS
- SignalR (real-time updates)
- Cytoscape (topology graphs)
- Recharts (dashboard charts)

## Development

```bash
# Install dependencies
npm install

# Start development server
npm run dev

# Build for production
npm run build

# Run linting
npm run lint
```

The development server runs at `http://localhost:5173` and proxies API requests to `http://localhost:5100`.

## Production Build

The production build outputs to `../Cascade.Collector/wwwroot/`, so the Collector serves both the API and the static frontend.

```bash
npm run build
```

## Configuration

The app connects to the Collector API at the same origin by default. For development, API requests are proxied via Vite's dev server configuration in `vite.config.ts`.
