export type MessageDirection = 0 | 1;

export type FlowStatus = 'InProgress' | 'Completed' | 'Failed' | 'TimedOut';

export interface MessageTelemetry {
  id: string;
  messageId: string;
  correlationId?: string;
  conversationId?: string;
  causationId?: string;
  relatedTo?: string;
  messageType: string;
  messageTypeShort: string;
  endpointName: string;
  hostId: string;
  direction: MessageDirection;
  timestamp: string;
  processingDuration?: string;
  success?: boolean;
  exceptionType?: string;
  exceptionMessage?: string;
  originatingEndpoint?: string;
  sagaId?: string;
  sagaType?: string;
  retryCount?: number;
}

export interface MessageFlow {
  correlationId: string;
  startedAt: string;
  completedAt?: string;
  messages: MessageTelemetry[];
  status: FlowStatus;
  messageCount: number;
  hasFailures: boolean;
}

export interface TopologyEndpoint {
  name: string;
  firstSeen: string;
  lastSeen: string;
  messagesReceived: number;
  messagesSent: number;
  failures: number;
  averageProcessingTimeMs: number;
  hostIds: string[];
  failureRate: number;
}

export interface TopologyConnection {
  sourceEndpoint: string;
  targetEndpoint: string;
  messageType: string;
  messageTypeShort: string;
  messageCount: number;
  firstSeen: string;
  lastSeen: string;
  failureCount: number;
  failureRate: number;
}

export interface SystemTopology {
  endpoints: Record<string, TopologyEndpoint>;
  messageTypes: Record<string, unknown>;
  connections: TopologyConnection[];
  firstObserved: string;
  lastUpdated: string;
  totalMessagesObserved: number;
  endpointCount: number;
  connectionCount: number;
}