import { useEffect, useRef } from 'react';
import cytoscape, { type Core } from 'cytoscape';
import dagre from 'cytoscape-dagre';
import type { SystemTopology } from '../types';

cytoscape.use(dagre);

interface TopologyGraphProps {
  topology: SystemTopology;
}

export function TopologyGraph({ topology }: TopologyGraphProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const cyRef = useRef<Core | null>(null);

  useEffect(() => {
    if (!containerRef.current) return;

    const elements = buildTopologyElements(topology);

    const cy = cytoscape({
      container: containerRef.current,
      elements,
      style: [
        // Endpoint nodes
        {
          selector: 'node',
          style: {
            'label': 'data(label)',
            'text-valign': 'center',
            'text-halign': 'center',
            'background-color': '#1e3a5f',
            'color': '#fff',
            'font-size': '14px',
            'font-weight': 'bold',
            'text-wrap': 'wrap',
            'text-max-width': '120px',
            'width': 'label',
            'height': 'label',
            'padding': '20px',
            'shape': 'roundrectangle',
            'border-width': 3,
            'border-color': '#3b82f6',
          },
        },
        // Nodes with failures
        {
          selector: 'node[hasFailures="true"]',
          style: {
            'border-color': '#ef4444',
            'border-width': 4,
          },
        },
        // Edge styles
        {
          selector: 'edge',
          style: {
            'width': 'data(weight)',
            'line-color': '#4b5563',
            'target-arrow-color': '#4b5563',
            'target-arrow-shape': 'triangle',
            'curve-style': 'bezier',
            'label': 'data(label)',
            'font-size': '10px',
            'color': '#9ca3af',
            'text-rotation': 'autorotate',
            'text-margin-y': -10,
            'text-background-color': '#1f2937',
            'text-background-opacity': 0.8,
            'text-background-padding': '3px',
          },
        },
        // Edges with high traffic
        {
          selector: 'edge[highTraffic="true"]',
          style: {
            'line-color': '#3b82f6',
            'target-arrow-color': '#3b82f6',
          },
        },
        // Edges with failures
        {
          selector: 'edge[hasFailures="true"]',
          style: {
            'line-color': '#ef4444',
            'target-arrow-color': '#ef4444',
          },
        },
      ],
      layout: {
        name: 'dagre',
        rankDir: 'LR',
        nodeSep: 80,
        rankSep: 150,
        padding: 50,
      } as cytoscape.LayoutOptions,
      userZoomingEnabled: true,
      userPanningEnabled: true,
      boxSelectionEnabled: false,
      minZoom: 0.3,
      maxZoom: 2,
    });

    cyRef.current = cy;
    cy.fit(undefined, 50);

    return () => {
      cy.destroy();
    };
  }, [topology]);

  return (
    <div 
      ref={containerRef} 
      className="w-full h-full bg-gray-900 rounded"
    />
  );
}

function buildTopologyElements(topology: SystemTopology) {
  const nodes: { data: Record<string, string> }[] = [];
  const edges: { data: Record<string, string | number> }[] = [];

  // Find max message count for scaling edge widths
  const maxMessages = Math.max(
    ...topology.connections.map(c => c.messageCount),
    1
  );

  // Add endpoint nodes
  Object.values(topology.endpoints).forEach(endpoint => {
    nodes.push({
      data: {
        id: endpoint.name,
        label: `${endpoint.name}\n↓${endpoint.messagesReceived} ↑${endpoint.messagesSent}`,
        hasFailures: endpoint.failures > 0 ? 'true' : 'false',
      },
    });
  });

  // Add connection edges
  topology.connections.forEach(conn => {
    // Scale edge width between 2 and 8 based on message count
    const weight = 2 + (conn.messageCount / maxMessages) * 6;
    
    edges.push({
      data: {
        id: `${conn.sourceEndpoint}-${conn.messageTypeShort}-${conn.targetEndpoint}`,
        source: conn.sourceEndpoint,
        target: conn.targetEndpoint,
        label: `${conn.messageTypeShort} (${conn.messageCount})`,
        weight: weight,
        highTraffic: conn.messageCount > maxMessages * 0.5 ? 'true' : 'false',
        hasFailures: conn.failureCount > 0 ? 'true' : 'false',
      },
    });
  });

  return [...nodes, ...edges];
}