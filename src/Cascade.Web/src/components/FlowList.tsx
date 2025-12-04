import { useState } from 'react';
import type { MessageFlow } from '../types';
import { FlowFilters, type FlowFilterParams } from './FlowFilters';
import { searchFlows, getFlowHistory } from '../services/api';

interface FlowListProps {
  flows: MessageFlow[];
  selectedFlowId: string | null;
  onSelectFlow: (correlationId: string) => void;
  onFlowsLoaded?: (flows: MessageFlow[]) => void;
}

export function FlowList({ flows, selectedFlowId, onSelectFlow, onFlowsLoaded }: FlowListProps) {
  const [isSearching, setIsSearching] = useState(false);
  const [searchResults, setSearchResults] = useState<MessageFlow[] | null>(null);
  const [searchError, setSearchError] = useState<string | null>(null);

  const handleSearch = async (filters: FlowFilterParams) => {
    // If no filters, clear search results and show live flows
    if (!filters.endpoint && !filters.messageType && filters.hasFailures === undefined && !filters.startTime) {
      setSearchResults(null);
      setSearchError(null);
      return;
    }

    setIsSearching(true);
    setSearchError(null);

    try {
      let results: MessageFlow[];
      
      if (filters.startTime) {
        // Use history endpoint for time-based queries
        results = await getFlowHistory(filters.startTime, filters.endTime);
        
        // Apply additional filters client-side
        if (filters.endpoint) {
          results = results.filter(f => 
            f.messages.some(m => m.endpointName.toLowerCase().includes(filters.endpoint!.toLowerCase()))
          );
        }
        if (filters.messageType) {
          results = results.filter(f => 
            f.messages.some(m => m.messageTypeShort.toLowerCase().includes(filters.messageType!.toLowerCase()))
          );
        }
        if (filters.hasFailures !== undefined) {
          results = results.filter(f => f.hasFailures === filters.hasFailures);
        }
      } else {
        // Use search endpoint
        results = await searchFlows({
          endpoint: filters.endpoint,
          messageType: filters.messageType,
          hasFailures: filters.hasFailures,
        });
      }

      setSearchResults(results);
      onFlowsLoaded?.(results);
    } catch (err) {
      setSearchError(err instanceof Error ? err.message : 'Search failed');
    } finally {
      setIsSearching(false);
    }
  };

  const displayFlows = searchResults ?? flows;

  return (
    <div className="bg-gray-800 rounded-lg p-4 h-full flex flex-col">
      <h2 className="text-lg font-semibold mb-3">
        {searchResults ? `Search Results (${searchResults.length})` : 'Active Flows'}
      </h2>
      
      <FlowFilters onSearch={handleSearch} isLoading={isSearching} />

      {searchError && (
        <div className="bg-red-900/50 border border-red-500 rounded p-2 mb-3 text-sm text-red-300">
          {searchError}
        </div>
      )}

      {displayFlows.length === 0 ? (
        <p className="text-gray-500 text-sm">
          {searchResults ? 'No flows match your search criteria.' : 'No flows yet. Send a message to see it here.'}
        </p>
      ) : (
        <div className="space-y-2 overflow-y-auto flex-1">
          {displayFlows.map(flow => (
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