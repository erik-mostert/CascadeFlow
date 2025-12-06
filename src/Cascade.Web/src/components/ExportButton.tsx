import { useState } from 'react';

interface ExportButtonProps {
  onExport: () => void;
  label?: string;
  icon?: 'download' | 'image' | 'json' | 'csv';
  disabled?: boolean;
}

export function ExportButton({ onExport, label = 'Export', icon = 'download', disabled = false }: ExportButtonProps) {
  const [isExporting, setIsExporting] = useState(false);

  const handleClick = async () => {
    setIsExporting(true);
    try {
      await onExport();
    } finally {
      setTimeout(() => setIsExporting(false), 500);
    }
  };

  return (
    <button
      onClick={handleClick}
      disabled={disabled || isExporting}
      className="flex items-center gap-1.5 bg-gray-700 hover:bg-gray-600 disabled:bg-gray-800 disabled:text-gray-500 px-2 py-1 rounded text-sm transition-colors"
      title={label}
    >
      {isExporting ? (
        <div className="w-4 h-4 border-2 border-gray-400 border-t-transparent rounded-full animate-spin" />
      ) : (
        <ExportIcon type={icon} />
      )}
      <span>{label}</span>
    </button>
  );
}

function ExportIcon({ type }: { type: 'download' | 'image' | 'json' | 'csv' }) {
  switch (type) {
    case 'image':
      return (
        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <rect x="3" y="3" width="18" height="18" rx="2" strokeWidth="2" />
          <circle cx="8.5" cy="8.5" r="1.5" fill="currentColor" />
          <path strokeWidth="2" d="M21 15l-5-5L5 21" />
        </svg>
      );
    case 'json':
      return (
        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeWidth="2" strokeLinecap="round" d="M8 3H6a2 2 0 00-2 2v14a2 2 0 002 2h12a2 2 0 002-2V5a2 2 0 00-2-2h-2" />
          <path strokeWidth="2" d="M9 12h6M9 16h4" />
        </svg>
      );
    case 'csv':
      return (
        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeWidth="2" strokeLinecap="round" d="M3 10h18M3 14h18M8 6v12M16 6v12" />
        </svg>
      );
    default:
      return (
        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4" />
        </svg>
      );
  }
}