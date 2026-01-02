interface ConnectionStatusProps {
  status: 'disconnected' | 'connecting' | 'connected' | 'error';
}

export function ConnectionStatus({ status }: ConnectionStatusProps) {
  if (status === 'connecting') {
    return (
      <div className="fixed inset-0 bg-gray-900/80 flex items-center justify-center z-50">
        <div className="bg-gray-800 rounded-lg p-6 text-center">
          <div className="animate-spin w-8 h-8 border-4 border-blue-500 border-t-transparent rounded-full mx-auto mb-4"></div>
          <p className="text-gray-300">Connecting to Cascade Collector...</p>
          <p className="text-gray-500 text-sm mt-2">{window.location.origin}</p>
        </div>
      </div>
    );
  }

  if (status === 'error') {
    return (
      <div className="fixed inset-0 bg-gray-900/80 flex items-center justify-center z-50">
        <div className="bg-gray-800 rounded-lg p-6 text-center max-w-md">
          <div className="text-red-500 text-4xl mb-4">⚠</div>
          <p className="text-gray-300 font-semibold">Connection Failed</p>
          <p className="text-gray-500 text-sm mt-2">
            Could not connect to the Cascade Collector at {window.location.origin}
          </p>
          <p className="text-gray-500 text-sm mt-4">
            Make sure the collector is running:
          </p>
          <code className="block bg-gray-900 rounded p-2 mt-2 text-sm text-green-400">
            cd src/Cascade.Collector && dotnet run
          </code>
        </div>
      </div>
    );
  }

  return null;
}