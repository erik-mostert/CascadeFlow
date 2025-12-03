import { useEffect, useRef } from 'react';
import cytoscape, { type Core } from 'cytoscape';
import dagre from 'cytoscape-dagre';
import type { MessageFlow } from '../types';

// Register the dagre layout
cytoscape.use(dagre);

interface FlowGraphProps {
    flow: MessageFlow;
}

export function FlowGraph({ flow }: FlowGraphProps) {
    const containerRef = useRef<HTMLDivElement>(null);
    const cyRef = useRef<Core | null>(null);

    useEffect(() => {
        if (!containerRef.current) return;

        const elements = buildGraphElements(flow);

        const cy = cytoscape({
            container: containerRef.current,
            elements,
            style: [
                // Node styles
                {
                    selector: 'node',
                    style: {
                        'label': 'data(label)',
                        'text-valign': 'center',
                        'text-halign': 'center',
                        'background-color': '#374151',
                        'color': '#fff',
                        'font-size': '12px',
                        'text-wrap': 'wrap',
                        'text-max-width': '100px',
                        'width': 'label',
                        'height': 'label',
                        'padding': '12px',
                        'shape': 'roundrectangle',
                        'border-width': 2,
                        'border-color': '#4b5563',
                    },
                },
                // Endpoint nodes
                {
                    selector: 'node[type="endpoint"]',
                    style: {
                        'background-color': '#1e3a5f',
                        'border-color': '#3b82f6',
                        'font-weight': 'bold',
                    },
                },
                // Message nodes
                {
                    selector: 'node[type="message"]',
                    style: {
                        'background-color': '#374151',
                        'border-color': '#6b7280',
                        'font-size': '10px',
                    },
                },
                // Failed message nodes
                {
                    selector: 'node[failed="true"]',
                    style: {
                        'background-color': '#7f1d1d',
                        'border-color': '#ef4444',
                    },
                },
                // Edge styles
                {
                    selector: 'edge',
                    style: {
                        'width': 2,
                        'line-color': '#4b5563',
                        'target-arrow-color': '#4b5563',
                        'target-arrow-shape': 'triangle',
                        'curve-style': 'bezier',
                        'label': 'data(label)',
                        'font-size': '9px',
                        'color': '#9ca3af',
                        'text-rotation': 'autorotate',
                        'text-margin-y': -10,
                    },
                },
                // Published edges (endpoint → message) - Blue
                {
                    selector: 'edge[edgeType="published"]',
                    style: {
                        'line-color': '#3b82f6',
                        'target-arrow-color': '#3b82f6',
                        'width': 3,
                    },
                },
                // Handled edges (message → endpoint) - Green
                {
                    selector: 'edge[edgeType="handled"]',
                    style: {
                        'line-color': '#22c55e',
                        'target-arrow-color': '#22c55e',
                        'width': 3,
                    },
                },
            ],
            layout: {
                name: 'dagre',
                rankDir: 'LR', // Left to right
                nodeSep: 60,
                rankSep: 100,
                padding: 30,
            } as cytoscape.LayoutOptions,
            userZoomingEnabled: true,
            userPanningEnabled: true,
            boxSelectionEnabled: false,
        });

        cyRef.current = cy;

        // Fit to container
        cy.fit(undefined, 30);

        return () => {
            cy.destroy();
        };
    }, [flow]);

    return (
        <div
            ref={containerRef}
            className="w-full h-full min-h-[200px] max-h-[200px] bg-gray-900 rounded overflow-hidden"
        />
    );
}

interface GraphNode {
    data: {
        id: string;
        label: string;
        type: 'endpoint' | 'message';
        failed?: string;
    };
}

interface GraphEdge {
    data: {
        id: string;
        source: string;
        target: string;
        label?: string;
        edgeType?: 'published' | 'handled';
    };
}

function buildGraphElements(flow: MessageFlow): (GraphNode | GraphEdge)[] {
    const nodes: GraphNode[] = [];
    const edges: GraphEdge[] = [];

    // Sort messages by effective time
    const sortedMessages = [...flow.messages].sort((a, b) => {
        return getEffectiveTime(a) - getEffectiveTime(b);
    });

    // Build a linear sequence of steps
    const steps: { type: 'endpoint' | 'message'; label: string; failed: boolean }[] = [];

    let lastEndpoint: string | null = null;

    sortedMessages.forEach(msg => {
        if (msg.direction === 1) {
            // Published message - add endpoint only if different from last
            if (msg.endpointName !== lastEndpoint) {
                steps.push({ type: 'endpoint', label: msg.endpointName, failed: false });
                lastEndpoint = msg.endpointName;
            }
            // Add the message
            steps.push({ type: 'message', label: msg.messageTypeShort, failed: false });
        } else {
            // Handled message - always add endpoint (message flows TO here)
            steps.push({ type: 'endpoint', label: msg.endpointName, failed: msg.success === false });
            lastEndpoint = msg.endpointName;
        }
    });

    // Create nodes for each step
    steps.forEach((step, index) => {
        nodes.push({
            data: {
                id: `step-${index}`,
                label: step.label,
                type: step.type,
                failed: step.failed ? 'true' : 'false',
            },
        });

        // Create edge from previous step
        if (index > 0) {
            const prevStep = steps[index - 1];
            // endpoint → message = published (blue), message → endpoint = handled (green)
            const edgeType = prevStep.type === 'endpoint' ? 'published' : 'handled';

            edges.push({
                data: {
                    id: `edge-${index - 1}-${index}`,
                    source: `step-${index - 1}`,
                    target: `step-${index}`,
                    edgeType: edgeType,
                },
            });
        }
    });

    return [...nodes, ...edges];
}

function getEffectiveTime(msg: MessageFlow['messages'][0]): number {
    const timestamp = new Date(msg.timestamp).getTime();

    if (msg.direction === 0 && msg.processingDuration) {
        const durationMs = parseDuration(msg.processingDuration);
        return timestamp - durationMs;
    }

    return timestamp;
}

function parseDuration(duration: string): number {
    const parts = duration.split(':');
    if (parts.length !== 3) return 0;

    const hours = parseInt(parts[0]) || 0;
    const minutes = parseInt(parts[1]) || 0;
    const seconds = parseFloat(parts[2]) || 0;

    return (hours * 3600 + minutes * 60 + seconds) * 1000;
}