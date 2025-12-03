import type { MessageFlow } from '../types';

interface FlowListProps {
  flows: MessageFlow[];
  selectedFlowId: string | null;
  onSelectFlow: (correlationId: string) => void;
}

export function FlowList({ flows, selectedFlowId, onSelectFlow }: FlowListProps) {
  return (
    <div className="bg-gray-800 rounded-lg p-4 h-full flex flex-col">
      <h2 className="text-lg font-semibold mb-4">Active Flows</h2>
      
      {flows.length === 0 ? (
        <p className="text-gray-500 text-sm">No flows yet. Send a message to see it here.</p>
      ) : (
        <div className="space-y-2 overflow-y-auto flex-1">
          {flows.map(flow => (
            <div 
              key={flow.correlationId}
              onClick={() => onSelectFlow(flow.correlationId)}
              className={`rounded p-3 cursor-pointer transition-colors ${
                selectedFlowId === flow.correlationId
                  ? 'bg-blue-600 ring-2 ring-blue-400'
                  : 'bg-gray-700 hover:bg-gray-600'
              }`}
            >
              <div className="flex justify-between items-start">
                <div className="text-sm font-mono text-gray-300 truncate flex-1">
                  {flow.correlationId.substring(0, 8)}...
                </div>
                <span className={`text-xs px-2 py-0.5 rounded ml-2 ${
                  flow.hasFailures ? 'bg-red-900 text-red-300' :
                  flow.status === 'Completed' ? 'bg-green-900 text-green-300' :
                  'bg-blue-900 text-blue-300'
                }`}>
                  {flow.messageCount} msgs
                </span>
              </div>
              
              {/* Show message types in flow */}
              <div className="text-xs text-gray-400 mt-1 truncate">
                {[...new Set(flow.messages.map(m => m.messageTypeShort))].join(' → ')}
              </div>
              
              <div className="text-xs text-gray-500 mt-1">
                {new Date(flow.startedAt).toLocaleTimeString()}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}