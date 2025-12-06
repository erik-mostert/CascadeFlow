import { useState, useEffect } from 'react';
import { ImpactTree } from './ImpactTree';
import { Tooltip } from './Tooltip';
import { getFlowImpact } from '../services/api';
import type { FlowImpactMetrics } from '../types';

interface FlowImpactPanelProps {
  correlationId: string;
}

export function FlowImpactPanel({ correlationId }: FlowImpactPanelProps) {
  const [impact, setImpact] = useState<FlowImpactMetrics | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadImpact();
  }, [correlationId]);

  const loadImpact = async () => {
    setIsLoading(true);
    setError(null);
    
    try {
      const data = await getFlowImpact(correlationId);
      setImpact(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load impact');
    } finally {
      setIsLoading(false);
    }
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-4">
        <div className="w-5 h-5 border-2 border-blue-500 border-t-transparent rounded-full animate-spin"></div>
      </div>
    );
  }

  if (error || !impact) {
    return (
      <div className="text-red-400 text-sm py-2">
        {error || 'Failed to load impact data'}
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {/* Impact Stats */}
      <div className="grid grid-cols-4 gap-2 text-sm">
        <div className="bg-gray-700 rounded p-2 text-center relative">
          <div className="absolute top-1 right-1">
            <Tooltip content="Total number of messages (commands, events, replies) in this flow." />
          </div>
          <div className="text-lg font-bold text-blue-400">{impact.totalMessages}</div>
          <div className="text-gray-400 text-xs">Messages</div>
        </div>
        <div className="bg-gray-700 rounded p-2 text-center relative">
          <div className="absolute top-1 right-1">
            <Tooltip content="Number of distinct services that participated in processing this flow." />
          </div>
          <div className="text-lg font-bold text-green-400">{impact.totalEndpoints}</div>
          <div className="text-gray-400 text-xs">Endpoints</div>
        </div>
        <div className="bg-gray-700 rounded p-2 text-center relative">
          <div className="absolute top-1 right-1">
            <Tooltip content="The deepest level of the message tree. Represents how many sequential message hops occurred from the initial trigger to the final message." />
          </div>
          <div className="text-lg font-bold text-orange-400">{impact.maxDepth}</div>
          <div className="text-gray-400 text-xs">Max Depth</div>
        </div>
        <div className="bg-gray-700 rounded p-2 text-center relative">
          <div className="absolute top-1 right-1">
            <Tooltip content="Sum of all handler processing times in this flow. Does not include queue wait times or network latency." />
          </div>
          <div className="text-lg font-bold text-cyan-400">{impact.totalProcessingTimeMs.toFixed(0)}ms</div>
          <div className="text-gray-400 text-xs">Total Time</div>
        </div>
      </div>

      {/* Endpoint Breakdown */}
      <div>
        <h4 className="text-sm font-semibold text-gray-300 mb-2">Endpoint Breakdown</h4>
        <div className="space-y-1">
          {impact.endpointBreakdown.map((ep) => (
            <div
              key={ep.endpointName}
              className={`flex items-center justify-between text-sm bg-gray-700 rounded px-2 py-1.5 ${
                ep.hasFailures ? 'border-l-2 border-red-500' : ''
              }`}
            >
              <span className="text-green-400">{ep.endpointName}</span>
              <div className="flex items-center gap-3 text-gray-400">
                <span>↓{ep.messagesReceived}</span>
                <span>↑{ep.messagesPublished}</span>
                {ep.multiplierRatio > 1 && (
                  <span className="text-orange-400">{ep.multiplierRatio.toFixed(1)}x</span>
                )}
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Message Tree */}
      <div>
        <h4 className="text-sm font-semibold text-gray-300 mb-2">Message Tree</h4>
        <div className="bg-gray-900 rounded p-2 max-h-[300px] overflow-auto">
          <ImpactTree tree={impact.messageTree} />
        </div>
      </div>
    </div>
  );
}