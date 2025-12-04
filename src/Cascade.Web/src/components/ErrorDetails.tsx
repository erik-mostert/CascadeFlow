import type { MessageTelemetry } from '../types';

interface ErrorDetailsProps {
  message: MessageTelemetry;
}

export function ErrorDetails({ message }: ErrorDetailsProps) {
  if (message.success !== false) return null;

  return (
    <div className="bg-red-900/30 border border-red-500 rounded p-3 mt-2">
      <h4 className="text-red-400 font-semibold text-sm mb-2">Error Details</h4>
      <div className="space-y-1 text-sm">
        <div>
          <span className="text-gray-400">Exception: </span>
          <span className="text-red-300">{message.exceptionType || 'Unknown'}</span>
        </div>
        <div>
          <span className="text-gray-400">Message: </span>
          <span className="text-gray-300">{message.exceptionMessage || 'No message'}</span>
        </div>
        {message.retryCount !== undefined && message.retryCount > 0 && (
          <div>
            <span className="text-gray-400">Retry Attempt: </span>
            <span className="text-yellow-400">{message.retryCount}</span>
          </div>
        )}
      </div>
    </div>
  );
}