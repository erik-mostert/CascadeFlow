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
    const addedNodeIds = new Set<string>();
    const addedEdgeIds = new Set<string>();

    // Sort messages by effective time
    const sortedMessages = [...flow.messages].sort((a, b) => {
        return getEffectiveTime(a) - getEffectiveTime(b);
    });

    const publishedMessages = sortedMessages.filter(m => m.direction === 1);
    const handledMessages = sortedMessages.filter(m => m.direction === 0);

    // Helper to add a node
    const addNode = (id: string, label: string, type: 'endpoint' | 'message', failed: boolean = false) => {
        if (!addedNodeIds.has(id)) {
            nodes.push({
                data: { id, label, type, failed: failed ? 'true' : 'false' },
            });
            addedNodeIds.add(id);
        }
        return id;
    };

    // Helper to add an edge
    const addEdge = (sourceId: string, targetId: string, edgeType: 'published' | 'handled') => {
        const id = `${sourceId}->${targetId}`;
        if (!addedEdgeIds.has(id)) {
            edges.push({
                data: { id, source: sourceId, target: targetId, edgeType },
            });
            addedEdgeIds.add(id);
        }
    };

    // Track which published messages we've processed
    const processedPublished = new Set<string>();

    // Process each handled message and trace back to its publisher
    handledMessages.forEach(handled => {
        const handlerNodeId = `handler-${handled.endpointName}-${handled.messageId}`;
        addNode(handlerNodeId, handled.endpointName, 'endpoint', handled.success === false);

        // Find the published message that this handler received
        const publishedMsg = publishedMessages.find(p => 
            p.messageTypeShort === handled.messageTypeShort &&
            getEffectiveTime(p) < getEffectiveTime(handled)
        );

        if (publishedMsg && !processedPublished.has(publishedMsg.id)) {
            // Create message node
            const messageNodeId = `msg-${publishedMsg.messageId}`;
            addNode(messageNodeId, publishedMsg.messageTypeShort, 'message');

            // Create publisher endpoint node
            const publisherNodeId = `publisher-${publishedMsg.endpointName}-${publishedMsg.messageId}`;
            addNode(publisherNodeId, publishedMsg.endpointName, 'endpoint');

            // Check if this published message was caused by a handled message
            const causingHandler = handledMessages.find(h =>
                h.endpointName === publishedMsg.endpointName &&
                getEffectiveTime(h) <= getEffectiveTime(publishedMsg) &&
                getEffectiveTime(publishedMsg) - getEffectiveTime(h) < 1000 // Within 1 second
            );

            if (causingHandler) {
                // Find what message the causing handler was processing
                const causingPublished = publishedMessages.find(p =>
                    p.messageTypeShort === causingHandler.messageTypeShort &&
                    getEffectiveTime(p) < getEffectiveTime(causingHandler)
                );

                if (causingPublished) {
                    const causingMsgNodeId = `msg-${causingPublished.messageId}`;
                    // Make sure the causing message node exists
                    addNode(causingMsgNodeId, causingPublished.messageTypeShort, 'message');
                    
                    // Edge: causing message → publisher endpoint
                    addEdge(causingMsgNodeId, publisherNodeId, 'handled');
                }
            }

            // Edge: publisher → message
            addEdge(publisherNodeId, messageNodeId, 'published');

            processedPublished.add(publishedMsg.id);
        }

        // Find the message node this handler receives from
        const incomingMsgNode = publishedMessages.find(p =>
            p.messageTypeShort === handled.messageTypeShort &&
            getEffectiveTime(p) < getEffectiveTime(handled)
        );

        if (incomingMsgNode) {
            const messageNodeId = `msg-${incomingMsgNode.messageId}`;
            addNode(messageNodeId, incomingMsgNode.messageTypeShort, 'message');
            addEdge(messageNodeId, handlerNodeId, 'handled');
        }

        // Find messages published by this handler
        const publishedByHandler = publishedMessages.filter(p =>
            p.endpointName === handled.endpointName &&
            getEffectiveTime(p) > getEffectiveTime(handled) &&
            getEffectiveTime(p) - getEffectiveTime(handled) < 1000
        );

        publishedByHandler.forEach(pub => {
            const pubMsgNodeId = `msg-${pub.messageId}`;
            addNode(pubMsgNodeId, pub.messageTypeShort, 'message');
            addEdge(handlerNodeId, pubMsgNodeId, 'published');
            processedPublished.add(pub.id);
        });
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