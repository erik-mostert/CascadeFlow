export function downloadFile(content: string, filename: string, mimeType: string) {
  const blob = new Blob([content], { type: mimeType });
  const url = URL.createObjectURL(blob);
  
  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  
  URL.revokeObjectURL(url);
}

export function downloadPng(dataUrl: string, filename: string) {
  const link = document.createElement('a');
  link.href = dataUrl;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
}

export function flowToCsv(flow: {
  correlationId: string;
  messages: Array<{
    messageId: string;
    messageType: string;
    messageTypeShort: string;
    endpointName: string;
    direction: number;
    timestamp: string;
    processingDuration?: string;
    success?: boolean;
    exceptionType?: string;
    exceptionMessage?: string;
  }>;
}): string {
  const headers = [
    'CorrelationId',
    'MessageId',
    'MessageType',
    'Endpoint',
    'Direction',
    'Timestamp',
    'ProcessingDuration',
    'Success',
    'ExceptionType',
    'ExceptionMessage'
  ];

  const rows = flow.messages.map(msg => [
    flow.correlationId,
    msg.messageId,
    msg.messageTypeShort,
    msg.endpointName,
    msg.direction === 0 ? 'Incoming' : 'Outgoing',
    msg.timestamp,
    msg.processingDuration ?? '',
    msg.success?.toString() ?? '',
    msg.exceptionType ?? '',
    `"${(msg.exceptionMessage ?? '').replace(/"/g, '""')}"`
  ]);

  return [
    headers.join(','),
    ...rows.map(row => row.join(','))
  ].join('\n');
}