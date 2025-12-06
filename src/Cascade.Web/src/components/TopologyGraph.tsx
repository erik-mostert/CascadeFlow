import { useEffect, useRef, forwardRef, useImperativeHandle } from 'react';
import cytoscape, { type Core } from 'cytoscape';
import dagre from 'cytoscape-dagre';
import type { SystemTopology } from '../types';

cytoscape.use(dagre);

interface TopologyGraphProps {
    topology: SystemTopology;
}

export interface TopologyGraphRef {
    exportPng: () => string | null;
}

export const TopologyGraph = forwardRef<TopologyGraphRef, TopologyGraphProps>(
    function TopologyGraph({ topology }, ref) {
        const containerRef = useRef<HTMLDivElement>(null);
        const cyRef = useRef<Core | null>(null);

        useImperativeHandle(ref, () => ({
            exportPng: () => {
                if (cyRef.current) {
                    return cyRef.current.png({
                        output: 'base64uri',
                        bg: '#111827',
                        scale: 2,
                        full: true
                    });
                }
                return null;
            }
        }));

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
                            'color': '#f0f9ff',
                            'font-size': '12px',
                            'font-weight': 'bold',
                            'text-wrap': 'wrap',
                            'text-max-width': '120px',
                            'width': 'label',
                            'height': 'label',
                            'padding': '24px',
                            'shape': 'rectangle',
                            'background-color': '#0f172a',
                            'border-width': 2,
                            'border-color': '#38bdf8',
                        },
                    },
                    // Nodes with failures
                    {
                        selector: 'node[hasFailures="true"]',
                        style: {
                            'background-color': '#7f1d1d',
                            'border-color': '#f87171',
                        },
                    },
                    // Slow nodes
                    {
                        selector: 'node[slow="true"]',
                        style: {
                            'border-color': '#fbbf24',
                        },
                    },
                    // Edge styles
                    {
                        selector: 'edge',
                        style: {
                            'width': 'data(weight)',
                            'line-color': '#0ea5e9',
                            'target-arrow-color': '#0ea5e9',
                            'target-arrow-shape': 'triangle',
                            'arrow-scale': 1.2,
                            'curve-style': 'straight',
                            'taxi-direction': 'rightward',
                            'taxi-turn': '70px',
                            'label': 'data(label)',
                            'font-size': '9px',
                            'font-weight': 'normal',
                            'color': '#94a3b8',
                            'text-background-color': '#0f172a',
                            'text-background-opacity': 0.9,
                            'text-background-padding': '4px',
                            'text-background-shape': 'roundrectangle',
                        },
                    },
                    // Edges with failures
                    {
                        selector: 'edge[hasFailures="true"]',
                        style: {
                            'line-color': '#f87171',
                            'target-arrow-color': '#f87171',
                        },
                    },
                ],
                layout: {
                    name: 'dagre',
                    rankDir: 'LR',
                    nodeSep: 120,
                    rankSep: 200,
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
);

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
        const avgMs = endpoint.averageProcessingTimeMs;
        let durationLabel = '';
        if (avgMs > 0) {
            if (avgMs >= 1000) {
                durationLabel = `\nAvg: ${(avgMs / 1000).toFixed(2)}s`;
            } else {
                durationLabel = `\nAvg: ${avgMs.toFixed(0)}ms`;
            }
        }

        nodes.push({
            data: {
                id: endpoint.name,
                label: `${endpoint.name}\n↓${endpoint.messagesReceived} ↑${endpoint.messagesSent}${durationLabel}`,
                hasFailures: endpoint.failures > 0 ? 'true' : 'false',
                slow: avgMs > 100 ? 'true' : 'false',
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