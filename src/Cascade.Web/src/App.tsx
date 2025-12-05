import { useState, useEffect, useMemo, useRef } from 'react';
import { useFlowHub } from './hooks/useFlowHub';
import { FlowList } from './components/FlowList';
import { FlowDetail } from './components/FlowDetail';
import { TopologyView } from './components/TopologyView';
import { ConnectionStatus } from './components/ConnectionStatus';
import { getFlowById } from './services/api';
import type { MessageFlow } from './types';

type ViewMode = 'flows' | 'topology';

function App() {
  const { connectionStatus, flows, topology, clearFlows } = useFlowHub();
  const [selectedFlowId, setSelectedFlowId] = useState<string | null>(null);
  const [fetchedFlow, setFetchedFlow] = useState<MessageFlow | null>(null);
  const [fetchedFlowId, setFetchedFlowId] = useState<string | null>(null);
  const [isLoadingFlow, setIsLoadingFlow] = useState(false);
  const [viewMode, setViewMode] = useState<ViewMode>('flows');

  // Derive the effective selected ID - fall back to first flow if none selected
  const effectiveSelectedId = selectedFlowId ?? (flows.length > 0 ? flows[0].correlationId : null);

  // Find flow in memory
  const memoryFlow = useMemo(() => 
    flows.find(f => f.correlationId === effectiveSelectedId) ?? null,
    [flows, effectiveSelectedId]
  );

  // Determine if we need to fetch
  const needsFetch = effectiveSelectedId !== null && memoryFlow === null && fetchedFlowId !== effectiveSelectedId;

  // Fetch from API only when needed
  useEffect(() => {
  if (!needsFetch || !effectiveSelectedId) {
    return;
  }

  let cancelled = false;
  setIsLoadingFlow(true);
  
  const flowIdToFetch = effectiveSelectedId;
  
  getFlowById(flowIdToFetch)
    .then(flow => {
      if (!cancelled) {
        setFetchedFlow(flow);
        setFetchedFlowId(flowIdToFetch);
        setIsLoadingFlow(false);
      }
    })
    .catch(err => {
      console.error('Failed to fetch flow:', err);
      if (!cancelled) {
        setFetchedFlow(null);
        setFetchedFlowId(flowIdToFetch);
        setIsLoadingFlow(false);
      }
    });

  return () => {
    cancelled = true;
  };
}, [needsFetch, effectiveSelectedId]);

  // Use memory flow if available, otherwise use fetched flow (if it matches current selection)
  const selectedFlow = memoryFlow ?? (fetchedFlowId === effectiveSelectedId ? fetchedFlow : null);
  
  // Show loading if we need to fetch or are currently fetching
  const showLoading = isLoadingFlow || (needsFetch && !selectedFlow);

  return (
    <div className="h-screen flex flex-col bg-gray-900 text-white">
      {/* Connection overlay */}
      <ConnectionStatus status={connectionStatus} />

      {/* Header */}
      <header className="bg-gray-800 border-b border-gray-700 px-6 py-4 flex-shrink-0">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-4">
            <h1 className="text-2xl font-bold text-blue-400">Cascade</h1>
            <span className="text-xs text-gray-500 bg-gray-700 px-2 py-1 rounded">
              NServiceBus Flow Visualization
            </span>
          </div>

          {/* View Toggle */}
          <div className="flex gap-2">
            <button
              onClick={() => setViewMode('flows')}
              className={`px-4 py-2 rounded transition-colors ${viewMode === 'flows'
                  ? 'bg-blue-600 text-white'
                  : 'bg-gray-700 text-gray-300 hover:bg-gray-600'
                }`}
            >
              Flows
            </button>
            <button
              onClick={() => setViewMode('topology')}
              className={`px-4 py-2 rounded transition-colors ${viewMode === 'topology'
                  ? 'bg-blue-600 text-white'
                  : 'bg-gray-700 text-gray-300 hover:bg-gray-600'
                }`}
            >
              Topology
            </button>
          </div>

          <div className="flex items-center gap-4">
            {/* Clear button */}
            {flows.length > 0 && (
              <button
                onClick={() => {
                  clearFlows();
                  setSelectedFlowId(null);
                }}
                className="text-xs text-gray-400 hover:text-white transition-colors"
              >
                Clear Flows
              </button>
            )}

            {/* Connection status */}
            <div className="flex items-center gap-2">
              <span className={`w-2 h-2 rounded-full ${connectionStatus === 'connected' ? 'bg-green-500' :
                  connectionStatus === 'connecting' ? 'bg-yellow-500 animate-pulse' :
                    connectionStatus === 'error' ? 'bg-red-500' :
                      'bg-gray-500'
                }`}></span>
              <span className="text-sm text-gray-400 capitalize">{connectionStatus}</span>
            </div>
          </div>
        </div>
      </header>

      {/* Stats Bar */}
      <div className="bg-gray-800 border-b border-gray-700 px-6 py-2 flex gap-6 text-sm flex-shrink-0">
        <div>
          <span className="text-gray-400">Flows: </span>
          <span className="text-white font-medium">{flows.length}</span>
        </div>
        <div>
          <span className="text-gray-400">Endpoints: </span>
          <span className="text-white font-medium">{topology?.endpointCount ?? 0}</span>
        </div>
        <div>
          <span className="text-gray-400">Connections: </span>
          <span className="text-white font-medium">{topology?.connectionCount ?? 0}</span>
        </div>
        <div>
          <span className="text-gray-400">Total Messages: </span>
          <span className="text-white font-medium">{topology?.totalMessagesObserved ?? 0}</span>
        </div>
      </div>

      {/* Main Content */}
      <main className="flex-1 p-4 overflow-hidden min-h-0">
        {viewMode === 'flows' ? (
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-4 h-full">
            {/* Flow List - Left Panel */}
            <div className="lg:col-span-1 h-full overflow-hidden">
              <FlowList
                flows={flows}
                selectedFlowId={effectiveSelectedId}
                onSelectFlow={setSelectedFlowId}
                onFlowsLoaded={(loadedFlows) => {
                  // Auto-select first result if current selection not in results
                  if (loadedFlows.length > 0 && !loadedFlows.find(f => f.correlationId === effectiveSelectedId)) {
                    setSelectedFlowId(loadedFlows[0].correlationId);
                  }
                }}
              />
            </div>

            {/* Flow Detail - Right Panel */}
            <div className="lg:col-span-2 h-full overflow-hidden">
              <FlowDetail flow={selectedFlow} isLoading={showLoading} />
            </div>
          </div>
        ) : (
          <div className="h-full">
            <TopologyView topology={topology} />
          </div>
        )}
      </main>

      {/* Footer */}
      <footer className="bg-gray-800 border-t border-gray-700 px-6 py-2 flex-shrink-0">
        <div className="flex justify-between text-xs text-gray-500">
          <span>Cascade - Real-time NServiceBus Message Flow Visualization</span>
          <span>Phase 1 POC</span>
        </div>
      </footer>
    </div>
  );
}

export default App;