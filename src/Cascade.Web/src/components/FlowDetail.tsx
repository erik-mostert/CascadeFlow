import { useState } from "react";
import type { MessageFlow } from "../types";
import { FlowGraph } from "./FlowGraph";
import { Modal } from "./Modal";
import { ErrorDetails } from "./ErrorDetails";

interface FlowDetailProps {
  flow: MessageFlow | null;
  isLoading?: boolean;
}

export function FlowDetail({ flow, isLoading = false }: FlowDetailProps) {
  const [isGraphExpanded, setIsGraphExpanded] = useState(false);

  if (isLoading) {
    return (
      <div className="bg-gray-800 rounded-lg p-4 h-full flex items-center justify-center">
        <div className="flex flex-col items-center gap-3">
          <div className="w-8 h-8 border-2 border-blue-500 border-t-transparent rounded-full animate-spin"></div>
          <p className="text-gray-400">Loading flow details...</p>
        </div>
      </div>
    );
  }
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
        <span
          className={`text-xs px-2 py-1 rounded ${
            flow.hasFailures
              ? "bg-red-900 text-red-300"
              : flow.status === "Completed"
              ? "bg-green-900 text-green-300"
              : "bg-blue-900 text-blue-300"
          }`}
        >
          {flow.status}
        </span>
      </div>

      {/* Flow Summary */}
      <div className="bg-gray-700 rounded p-3 mb-4 text-sm flex-shrink-0">
        <div className="grid grid-cols-2 gap-2">
          <div>
            <span className="text-gray-400">Messages:</span> {flow.messageCount}
          </div>
          <div>
            <span className="text-gray-400">Duration:</span>{" "}
            {formatDuration(flow.startedAt, flow.completedAt)}
          </div>
          <div>
            <span className="text-gray-400">Started:</span>{" "}
            {new Date(flow.startedAt).toLocaleString("en-US", {
              month: "short",
              day: "numeric",
              hour: "2-digit",
              minute: "2-digit",
              second: "2-digit",
            })}
          </div>
          <div className="col-span-2">
            <span className="text-gray-400">CorrelationId:</span>
            <span className="font-mono text-xs ml-2">{flow.correlationId}</span>
          </div>
        </div>
      </div>

      {/* Flow Graph with expand button */}
      <div className="h-[200px] mb-4 flex-shrink-0 relative">
        <FlowGraph flow={flow} />
        <button
          onClick={() => setIsGraphExpanded(true)}
          className="absolute top-2 right-2 bg-gray-700 hover:bg-gray-600 text-gray-300 hover:text-white p-1.5 rounded transition-colors"
          title="Expand graph"
        >
          <svg
            className="w-4 h-4"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth={2}
              d="M4 8V4m0 0h4M4 4l5 5m11-1V4m0 0h-4m4 0l-5 5M4 16v4m0 0h4m-4 0l5-5m11 5l-5-5m5 5v-4m0 4h-4"
            />
          </svg>
        </button>
        {/* Mini legend */}
        <div className="absolute bottom-2 left-2 flex gap-3 text-xs bg-gray-800/80 rounded px-2 py-1">
          <div className="flex items-center gap-1">
            <div className="w-3 h-3 rounded bg-blue-600 border border-blue-400"></div>
            <span className="text-gray-400">Published</span>
          </div>
          <div className="flex items-center gap-1">
            <div className="w-3 h-3 rounded bg-green-600 border border-green-400"></div>
            <span className="text-gray-400">Handled</span>
          </div>
          <div className="flex items-center gap-1">
            <div className="w-3 h-3 rounded border-2 border-yellow-500"></div>
            <span className="text-gray-400">Slow</span>
          </div>
          <div className="flex items-center gap-1">
            <div className="w-3 h-3 rounded bg-red-900 border border-red-500"></div>
            <span className="text-gray-400">Failed</span>
          </div>
        </div>
      </div>

      {/* Expanded Graph Modal */}
      <Modal
        isOpen={isGraphExpanded}
        onClose={() => setIsGraphExpanded(false)}
        title={`Flow Graph - ${flow.correlationId.substring(0, 8)}...`}
      >
        <FlowGraph flow={flow} expanded />
      </Modal>

      {/* Message Timeline */}
      <h3 className="text-sm font-semibold mb-2 text-gray-300 flex-shrink-0">
        Message Timeline
      </h3>
      <div className="flex-1 overflow-y-auto space-y-2 min-h-0">
        {sortMessagesByFlow(flow.messages).map((msg, index) => (
          <div
            key={msg.id}
            className={`rounded p-2 text-sm border-l-4 ${
              msg.success === false
                ? "bg-red-900/30 border-red-500"
                : msg.direction === 0
                ? "bg-gray-700 border-green-500"
                : "bg-gray-700 border-blue-500"
            }`}
          >
            <div className="flex justify-between items-start">
              <div className="flex items-center gap-2">
                <span className="text-xs text-gray-500">{index + 1}</span>
                <span
                  className={`text-xs px-1.5 py-0.5 rounded ${
                    msg.direction === 0
                      ? "bg-green-900 text-green-300"
                      : "bg-blue-900 text-blue-300"
                  }`}
                >
                  {msg.direction === 0 ? "↓ Handled" : "↑ Published"}
                </span>
                <span className="font-medium">{msg.endpointName}</span>
              </div>
              <span className="text-xs text-gray-500">
                {new Date(msg.timestamp).toLocaleTimeString("en-US", {
                  hour: "2-digit",
                  minute: "2-digit",
                  second: "2-digit",
                  fractionalSecondDigits: 3,
                })}
              </span>
            </div>
            <div className="text-gray-300 mt-1">{msg.messageTypeShort}</div>
            {msg.processingDuration && (
              <div className="text-xs text-gray-500 mt-1">
                Processing: {msg.processingDuration}
              </div>
            )}
            {/* Error details for failed messages */}
            <ErrorDetails message={msg} />
          </div>
        ))}
      </div>
    </div>
  );
}

function sortMessagesByFlow(
  messages: MessageFlow["messages"]
): MessageFlow["messages"] {
  return [...messages].sort((a, b) => {
    return getEffectiveTime(a) - getEffectiveTime(b);
  });
}

function getEffectiveTime(msg: MessageFlow["messages"][0]): number {
  const timestamp = new Date(msg.timestamp).getTime();

  if (msg.direction === 0 && msg.processingDuration) {
    const durationMs = parseDuration(msg.processingDuration);
    return timestamp - durationMs;
  }

  return timestamp;
}

function parseDuration(duration: string): number {
  const parts = duration.split(":");
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
function formatMessageTimestamp(dateString: string): string {
  const date = new Date(dateString);
  const now = new Date();
  const isToday = date.toDateString() === now.toDateString();

  if (isToday) {
    return date.toLocaleTimeString("en-US", {
      hour: "2-digit",
      minute: "2-digit",
      second: "2-digit",
      fractionalSecondDigits: 3,
    });
  }

  return date.toLocaleDateString("en-US", {
    month: "short",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
  });
}
