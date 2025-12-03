import type { SystemTopology } from '../types';
import { TopologyGraph } from './TopologyGraph';

interface TopologyViewProps {
  topology: SystemTopology | null;
}

export function TopologyView({ topology }: TopologyViewProps) {
  if (!topology || Object.keys(topology.endpoints).length === 0) {
    return (
      <div className="bg-gray-800 rounded-lg p-4 h-full flex items-center justify-center">
        <div className="text-center">
          <p className="text-gray-500 mb-2">No topology data yet</p>
          <p className="text-gray-600 text-sm">Send some messages to discover your system topology</p>
        </div>
      </div>
    );
  }

  return (
    <div className="bg-gray-800 rounded-lg p-4 h-full flex flex-col">
      <div className="flex justify-between items-start mb-4">
        <div>
          <h2 className="text-lg font-semibold">System Topology</h2>
          <p className="text-sm text-gray-400 mt-1">
            Live architecture diagram built from observed message traffic
          </p>
        </div>
        <div className="text-sm text-gray-400">
          Last updated: {new Date(topology.lastUpdated).toLocaleTimeString()}
        </div>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-4 gap-4 mb-4">
        <div className="bg-gray-700 rounded p-3 text-center">
          <div className="text-2xl font-bold text-blue-400">{topology.endpointCount}</div>
          <div className="text-xs text-gray-400">Endpoints</div>
        </div>
        <div className="bg-gray-700 rounded p-3 text-center">
          <div className="text-2xl font-bold text-blue-400">{topology.connectionCount}</div>
          <div className="text-xs text-gray-400">Connections</div>
        </div>
        <div className="bg-gray-700 rounded p-3 text-center">
          <div className="text-2xl font-bold text-blue-400">{Object.keys(topology.messageTypes).length}</div>
          <div className="text-xs text-gray-400">Message Types</div>
        </div>
        <div className="bg-gray-700 rounded p-3 text-center">
          <div className="text-2xl font-bold text-green-400">{topology.totalMessagesObserved}</div>
          <div className="text-xs text-gray-400">Total Messages</div>
        </div>
      </div>

      {/* Legend */}
      <div className="flex items-center gap-6 mb-4 text-xs text-gray-400 bg-gray-700/50 rounded px-3 py-2">
        <span className="font-semibold text-gray-300">Legend:</span>
        <div className="flex items-center gap-2">
          <div className="w-4 h-4 rounded bg-blue-900 border-2 border-blue-500"></div>
          <span>Endpoint (service)</span>
        </div>
        <div className="flex items-center gap-2">
          <div className="w-8 h-0.5 bg-blue-500"></div>
          <span>→</span>
          <span>Message flow (thicker = more messages)</span>
        </div>
        <div className="flex items-center gap-2">
          <div className="w-4 h-4 rounded bg-gray-700 border-2 border-red-500"></div>
          <span>Endpoint with failures</span>
        </div>
      </div>

      {/* Graph */}
      <div className="flex-1 min-h-0">
        <TopologyGraph topology={topology} />
      </div>

      {/* Endpoint List */}
      <div className="mt-4 flex-shrink-0">
        <h3 className="text-sm font-semibold mb-2 text-gray-300">Discovered Endpoints</h3>
        <div className="flex flex-wrap gap-2">
          {Object.values(topology.endpoints).map(endpoint => (
            <div 
              key={endpoint.name}
              className={`rounded px-3 py-2 text-sm ${
                endpoint.failures > 0 
                  ? 'bg-red-900/50 border border-red-500' 
                  : 'bg-gray-700'
              }`}
            >
              <div className="font-medium">{endpoint.name}</div>
              <div className="text-xs text-gray-400">
                ↓ {endpoint.messagesReceived} handled · ↑ {endpoint.messagesSent} published
                {endpoint.failures > 0 && (
                  <span className="text-red-400 ml-2">· {endpoint.failures} failed</span>
                )}
              </div>
              <div className="text-xs text-gray-500">
                Avg processing: {endpoint.averageProcessingTimeMs.toFixed(1)}ms
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}