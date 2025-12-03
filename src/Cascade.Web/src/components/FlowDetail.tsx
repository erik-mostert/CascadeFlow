import type { MessageFlow } from '../types';
import { FlowGraph } from './FlowGraph';

interface FlowDetailProps {
    flow: MessageFlow | null;
}

export function FlowDetail({ flow }: FlowDetailProps) {
    if (!flow) {
        return (
            <div className="bg-gray-800 rounded-lg p-4 h-full flex items-center justify-center">
                <p className="text-gray-500">Select a flow to view details</p>
            </div>
        );
    }

    return (
        <div className="bg-gray-800 rounded-lg p-4 h-full flex flex-col overflow-hidden">
            <div className="flex justify-between items-start mb-4">
                <h2 className="text-lg font-semibold">Flow Details</h2>
                <span className={`text-xs px-2 py-1 rounded ${flow.hasFailures ? 'bg-red-900 text-red-300' :
                    flow.status === 'Completed' ? 'bg-green-900 text-green-300' :
                        'bg-blue-900 text-blue-300'
                    }`}>
                    {flow.status}
                </span>
            </div>

            {/* Flow Summary */}
            <div className="bg-gray-700 rounded p-3 mb-4 text-sm">
                <div className="grid grid-cols-2 gap-2">
                    <div><span className="text-gray-400">Messages:</span> {flow.messageCount}</div>
                    <div><span className="text-gray-400">Duration:</span> {formatDuration(flow.startedAt, flow.completedAt)}</div>
                    <div className="col-span-2">
                        <span className="text-gray-400">CorrelationId:</span>
                        <span className="font-mono text-xs ml-2">{flow.correlationId}</span>
                    </div>
                </div>
            </div>

            {/* Flow Graph */}
            <div className="h-[200px] mb-4 flex-shrink-0 overflow-hidden relative">
                <FlowGraph flow={flow} />
            </div>

            {/* Message Timeline */}
            <h3 className="text-sm font-semibold mb-2 text-gray-300 flex-shrink-0">Message Timeline</h3>
            <div className="flex-1 overflow-y-auto space-y-2 min-h-0">
                {sortMessagesByFlow(flow.messages).map((msg, index) => (
                    <div
                        key={msg.id}
                        className={`rounded p-2 text-sm border-l-4 ${msg.success === false
                            ? 'bg-red-900/30 border-red-500'
                            : msg.direction === 0
                                ? 'bg-gray-700 border-green-500'
                                : 'bg-gray-700 border-blue-500'
                            }`}
                    >
                        <div className="flex justify-between items-start">
                            <div className="flex items-center gap-2">
                                <span className="text-xs text-gray-500">{index + 1}</span>
                                <span className={`text-xs px-1.5 py-0.5 rounded ${msg.direction === 0 ? 'bg-green-900 text-green-300' : 'bg-blue-900 text-blue-300'
                                    }`}>
                                    {msg.direction === 0 ? '↓ Handled' : '↑ Published'}
                                </span>
                                <span className="font-medium">{msg.endpointName}</span>
                            </div>
                            <span className="text-xs text-gray-500">
                                {new Date(msg.timestamp).toLocaleTimeString('en-US', {
                                    hour: '2-digit',
                                    minute: '2-digit',
                                    second: '2-digit',
                                    fractionalSecondDigits: 3
                                })}
                            </span>
                        </div>
                        <div className="text-gray-300 mt-1">
                            {msg.messageTypeShort}
                        </div>
                        {msg.processingDuration && (
                            <div className="text-xs text-gray-500 mt-1">
                                Processing: {msg.processingDuration}
                            </div>
                        )}
                        {msg.success === false && (
                            <div className="text-xs text-red-400 mt-1">
                                ✗ {msg.exceptionType}: {msg.exceptionMessage}
                            </div>
                        )}
                    </div>
                ))}
            </div>
        </div>
    );
}

function sortMessagesByFlow(messages: MessageFlow['messages']): MessageFlow['messages'] {
  return [...messages].sort((a, b) => {
    const timeA = getEffectiveTime(a);
    const timeB = getEffectiveTime(b);
    
    const timeDiff = timeA - timeB;
    if (Math.abs(timeDiff) > 1) return timeDiff;
    
    // Tiebreaker: use message ID for consistency
    return a.messageId.localeCompare(b.messageId);
  });
}

function getEffectiveTime(msg: MessageFlow['messages'][0]): number {
  const timestamp = new Date(msg.timestamp).getTime();
  
  // For handled messages, calculate when handler STARTED (not ended)
  if (msg.direction === 0 && msg.processingDuration) {
    const durationMs = parseDuration(msg.processingDuration);
    return timestamp - durationMs;
  }
  
  return timestamp;
}

function parseDuration(duration: string): number {
  // Parse "00:00:00.0524720" format
  const parts = duration.split(':');
  if (parts.length !== 3) return 0;
  
  const hours = parseInt(parts[0]) || 0;
  const minutes = parseInt(parts[1]) || 0;
  const seconds = parseFloat(parts[2]) || 0;
  
  return (hours * 3600 + minutes * 60 + seconds) * 1000;
}

function formatDuration(start: string, end?: string): string {
    const startTime = new Date(start).getTime();
    const endTime = end ? new Date(end).getTime() : Date.now();
    const ms = endTime - startTime;

    if (ms < 1000) return `${ms}ms`;
    if (ms < 60000) return `${(ms / 1000).toFixed(1)}s`;
    return `${(ms / 60000).toFixed(1)}m`;
}