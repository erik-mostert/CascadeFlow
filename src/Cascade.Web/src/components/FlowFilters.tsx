import { useState } from 'react';

interface FlowFiltersProps {
  onSearch: (filters: FlowFilterParams) => void;
  isLoading?: boolean;
}

export interface FlowFilterParams {
  endpoint?: string;
  messageType?: string;
  hasFailures?: boolean;
  startTime?: string;
  endTime?: string;
}

export function FlowFilters({ onSearch, isLoading }: FlowFiltersProps) {
  const [endpoint, setEndpoint] = useState('');
  const [messageType, setMessageType] = useState('');
  const [hasFailures, setHasFailures] = useState<boolean | undefined>(undefined);
  const [timeRange, setTimeRange] = useState('1h');

  const handleSearch = () => {
    const now = new Date();
    let startTime: string | undefined;
    
    switch (timeRange) {
      case '15m':
        startTime = new Date(now.getTime() - 15 * 60 * 1000).toISOString();
        break;
      case '1h':
        startTime = new Date(now.getTime() - 60 * 60 * 1000).toISOString();
        break;
      case '24h':
        startTime = new Date(now.getTime() - 24 * 60 * 60 * 1000).toISOString();
        break;
      case 'all':
        startTime = undefined;
        break;
    }

    onSearch({
      endpoint: endpoint || undefined,
      messageType: messageType || undefined,
      hasFailures,
      startTime,
      endTime: now.toISOString(),
    });
  };

  const handleReset = () => {
    setEndpoint('');
    setMessageType('');
    setHasFailures(undefined);
    setTimeRange('1h');
    onSearch({});
  };

  return (
    <div className="bg-gray-700 rounded-lg p-3 mb-4">
      <div className="flex flex-wrap gap-3 items-end">
        {/* Endpoint filter */}
        <div className="flex-1 min-w-[150px]">
          <label className="block text-xs text-gray-400 mb-1">Endpoint</label>
          <input
            type="text"
            value={endpoint}
            onChange={(e) => setEndpoint(e.target.value)}
            placeholder="e.g. OrderService"
            className="w-full bg-gray-800 border border-gray-600 rounded px-2 py-1 text-sm focus:outline-none focus:border-blue-500"
          />
        </div>

        {/* Message type filter */}
        <div className="flex-1 min-w-[150px]">
          <label className="block text-xs text-gray-400 mb-1">Message Type</label>
          <input
            type="text"
            value={messageType}
            onChange={(e) => setMessageType(e.target.value)}
            placeholder="e.g. OrderPlaced"
            className="w-full bg-gray-800 border border-gray-600 rounded px-2 py-1 text-sm focus:outline-none focus:border-blue-500"
          />
        </div>

        {/* Time range filter */}
        <div className="min-w-[100px]">
          <label className="block text-xs text-gray-400 mb-1">Time Range</label>
          <select
            value={timeRange}
            onChange={(e) => setTimeRange(e.target.value)}
            className="w-full bg-gray-800 border border-gray-600 rounded px-2 py-1 text-sm focus:outline-none focus:border-blue-500"
          >
            <option value="15m">Last 15 min</option>
            <option value="1h">Last hour</option>
            <option value="24h">Last 24 hours</option>
            <option value="all">All time</option>
          </select>
        </div>

        {/* Failures filter */}
        <div className="min-w-[100px]">
          <label className="block text-xs text-gray-400 mb-1">Status</label>
          <select
            value={hasFailures === undefined ? '' : hasFailures.toString()}
            onChange={(e) => {
              if (e.target.value === '') setHasFailures(undefined);
              else setHasFailures(e.target.value === 'true');
            }}
            className="w-full bg-gray-800 border border-gray-600 rounded px-2 py-1 text-sm focus:outline-none focus:border-blue-500"
          >
            <option value="">All</option>
            <option value="true">Failed only</option>
            <option value="false">Successful only</option>
          </select>
        </div>

        {/* Buttons */}
        <div className="flex gap-2">
          <button
            onClick={handleSearch}
            disabled={isLoading}
            className="bg-blue-600 hover:bg-blue-700 disabled:bg-blue-800 px-3 py-1 rounded text-sm transition-colors"
          >
            {isLoading ? 'Searching...' : 'Search'}
          </button>
          <button
            onClick={handleReset}
            className="bg-gray-600 hover:bg-gray-500 px-3 py-1 rounded text-sm transition-colors"
          >
            Reset
          </button>
        </div>
      </div>
    </div>
  );
}