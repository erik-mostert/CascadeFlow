import type { 
  MessageFlow, 
  SystemTopology, 
  FlowImpactMetrics, 
  SystemImpactSummary, 
  MultiplierEndpoint 
} from '../types';

const API_BASE = 'http://localhost:5100/api';

export interface SearchParams {
  endpoint?: string;
  messageType?: string;
  hasFailures?: boolean;
  startTime?: string;
  endTime?: string;
  maxResults?: number;
}

export interface Stats {
  activeFlows: number;
  totalEndpoints: number;
  totalConnections: number;
  totalMessages: number;
  failedFlows: number;
  lastUpdated: string;
}

export async function searchFlows(params: SearchParams): Promise<MessageFlow[]> {
  const searchParams = new URLSearchParams();
  
  if (params.endpoint) searchParams.set('endpoint', params.endpoint);
  if (params.messageType) searchParams.set('messageType', params.messageType);
  if (params.hasFailures !== undefined) searchParams.set('hasFailures', params.hasFailures.toString());
  if (params.maxResults) searchParams.set('maxResults', params.maxResults.toString());

  const url = `${API_BASE}/flows/search?${searchParams}`;
  const response = await fetch(url);
  
  if (!response.ok) {
    throw new Error(`Search failed: ${response.statusText}`);
  }
  
  return response.json();
}

export async function getFlowHistory(start?: string, end?: string, maxResults = 100): Promise<MessageFlow[]> {
  const searchParams = new URLSearchParams();
  
  if (start) searchParams.set('start', start);
  if (end) searchParams.set('end', end);
  searchParams.set('maxResults', maxResults.toString());

  const url = `${API_BASE}/flows/history?${searchParams}`;
  const response = await fetch(url);
  
  if (!response.ok) {
    throw new Error(`History fetch failed: ${response.statusText}`);
  }
  
  return response.json();
}

export async function getFlowById(correlationId: string): Promise<MessageFlow | null> {
  const response = await fetch(`${API_BASE}/flows/${correlationId}/full`);
  
  if (response.status === 404) {
    return null;
  }
  
  if (!response.ok) {
    throw new Error(`Flow fetch failed: ${response.statusText}`);
  }
  
  return response.json();
}

export async function getStats(): Promise<Stats> {
  const response = await fetch(`${API_BASE}/stats`);
  
  if (!response.ok) {
    throw new Error(`Stats fetch failed: ${response.statusText}`);
  }
  
  return response.json();
}

export async function getTopology(): Promise<SystemTopology> {
  const response = await fetch(`${API_BASE}/topology`);
  
  if (!response.ok) {
    throw new Error(`Topology fetch failed: ${response.statusText}`);
  }
  
  return response.json();
}
export async function getFlowImpact(correlationId: string): Promise<FlowImpactMetrics | null> {
  const response = await fetch(`${API_BASE}/impact/${correlationId}`);
  
  if (response.status === 404) {
    return null;
  }
  
  if (!response.ok) {
    throw new Error(`Impact fetch failed: ${response.statusText}`);
  }
  
  return response.json();
}

export async function getImpactSummary(flowCount = 100): Promise<SystemImpactSummary> {
  const response = await fetch(`${API_BASE}/impact/summary?flowCount=${flowCount}`);
  
  if (!response.ok) {
    throw new Error(`Impact summary fetch failed: ${response.statusText}`);
  }
  
  return response.json();
}

export async function getMultiplierEndpoints(flowCount = 100): Promise<MultiplierEndpoint[]> {
  const response = await fetch(`${API_BASE}/impact/multipliers?flowCount=${flowCount}`);
  
  if (!response.ok) {
    throw new Error(`Multipliers fetch failed: ${response.statusText}`);
  }
  
  return response.json();
}
