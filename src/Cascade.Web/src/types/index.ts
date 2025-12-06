export type MessageDirection = 0 | 1;

export type FlowStatus = 'InProgress' | 'Completed' | 'Failed' | 'TimedOut';
export type MessageIntent = 'Unknown' | 'Send' | 'Publish' | 'Reply';

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
  intent?: number; // 0=Unknown, 1=Send, 2=Publish, 3=Reply
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
// Impact Analysis Types
export interface FlowImpactMetrics {
  correlationId: string;
  totalMessages: number;
  totalEndpoints: number;
  maxDepth: number;
  totalProcessingTimeMs: number;
  hasFailures: boolean;
  messageTree: MessageImpact[];
  endpointBreakdown: EndpointImpact[];
}

export interface MessageImpact {
  messageId: string;
  messageType: string;
  publishedBy: string;
  depth: number;
  downstreamMessageCount: number;
  downstreamEndpointCount: number;
  handledBy: string[];
  children: MessageImpact[];
}

export interface EndpointImpact {
  endpointName: string;
  messagesReceived: number;
  messagesPublished: number;
  commandsSent: number;
  eventsPublished: number;
  repliesSent: number;
  multiplierRatio: number;
  eventMultiplierRatio: number;
  processingTimeMs: number;
  hasFailures: boolean;
}

export interface MultiplierEndpoint {
  endpointName: string;
  multiplierRatio: number;
  eventMultiplierRatio: number;
  totalReceived: number;
  totalPublished: number;
  commandsSent: number;
  eventsPublished: number;
  sampleSize: number;
  commonOutputMessages: string[];
}

export interface SystemImpactSummary {
  totalFlowsAnalyzed: number;
  averageMessagesPerFlow: number;
  averageEndpointsPerFlow: number;
  averageDepth: number;
  topMultipliers: MultiplierEndpoint[];
  highImpactMessageTypes: string[];
}

// Dashboard Types
export interface DashboardStats {
  totalMessages: number;
  messagesLast24h: number;
  messagesLastHour: number;
  totalFailures: number;
  failuresLast24h: number;
  failureRate: number;
  activeFlows: number;
  timestamp: string;
}

export interface MessagesOverTime {
  timestamp: string;
  hour: string;
  count: number;
  failures: number;
}

export interface TopEndpoint {
  endpoint: string;
  messageCount: number;
  failures: number;
  avgProcessingMs: number;
}

export interface SlowestHandler {
  endpoint: string;
  messageType: string;
  avgProcessingMs: number;
  maxProcessingMs: number;
  count: number;
}

export interface FailureRateOverTime {
  timestamp: string;
  hour: string;
  total: number;
  failures: number;
  failureRate: number;
}