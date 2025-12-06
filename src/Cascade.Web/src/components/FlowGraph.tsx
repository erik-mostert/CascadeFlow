import { useEffect, useRef, useImperativeHandle, forwardRef } from "react";
import cytoscape, { type Core } from "cytoscape";
import dagre from "cytoscape-dagre";
import type { MessageFlow } from "../types";

// Register the dagre layout
cytoscape.use(dagre);

interface FlowGraphProps {
    flow: MessageFlow;
    expanded?: boolean;
}

export interface FlowGraphRef {
    exportPng: () => string | null;
}

export const FlowGraph = forwardRef<FlowGraphRef, FlowGraphProps>(
    function FlowGraph({ flow, expanded = false }, ref) {
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

            const elements = buildGraphElements(flow);

            const cy = cytoscape({
                container: containerRef.current,
                elements,
                style: [
                    // Base node styles
                    {
                        selector: "node",
                        style: {
                            "label": "data(label)",
                            "text-valign": "center",
                            "text-halign": "center",
                            "color": "#e2e8f0",
                            "font-size": "10px",
                            "font-weight": "normal",
                            "text-wrap": "wrap",
                            "text-max-width": "100px",
                            "width": "label",
                            "height": "label",
                            "padding": "14px",
                            "shape": "round-rectangle",
                            "background-color": "#334155",
                            "border-width": 1,
                            "border-color": "#475569",
                        },
                    },
                    // Endpoint nodes - Service style
                    {
                        selector: 'node[type="endpoint"]',
                        style: {
                            "background-color": "#0f172a",
                            "border-color": "#38bdf8",
                            "border-width": 2,
                            "font-weight": "bold",
                            "font-size": "11px",
                            "color": "#f0f9ff",
                            "shape": "rectangle",
                            "padding": "18px",
                        },
                    },
                    // Message nodes - Event/Command style
                    {
                        selector: 'node[type="message"]',
                        style: {
                            "background-color": "#1e293b",
                            "border-color": "#64748b",
                            "border-width": 1,
                            "font-size": "9px",
                            "color": "#cbd5e1",
                            "shape": "rectangle",
                            "padding": "10px",
                        },
                    },
                    // Failed nodes
                    {
                        selector: 'node[failed="true"]',
                        style: {
                            "background-color": "#7f1d1d",
                            "border-color": "#f87171",
                            "border-width": 2,
                            "color": "#fecaca",
                        },
                    },
                    // Slow nodes
                    {
                        selector: 'node[slow="true"]',
                        style: {
                            "border-color": "#fbbf24",
                            "border-width": 2,
                        },
                    },
                    // Base edge styles - Orthogonal connectors
                    {
                        selector: "edge",
                        style: {
                            "width": 2,
                            "line-color": "#475569",
                            "target-arrow-color": "#475569",
                            "target-arrow-shape": "triangle",
                            "arrow-scale": 1,
                            "curve-style": "straight",
                            "taxi-direction": "rightward",
                            "taxi-turn": "50px",
                        },
                    },
                    // Published edges - Blue
                    {
                        selector: 'edge[edgeType="published"]',
                        style: {
                            "line-color": "#0ea5e9",
                            "target-arrow-color": "#0ea5e9",
                            "width": 2,
                        },
                    },
                    // Handled edges - Green
                    {
                        selector: 'edge[edgeType="handled"]',
                        style: {
                            "line-color": "#10b981",
                            "target-arrow-color": "#10b981",
                            "width": 2,
                        },
                    },
                ],
                layout: {
                    name: "dagre",
                    rankDir: "LR",
                    nodeSep: expanded ? 100 : 70,
                    rankSep: expanded ? 150 : 100,
                    padding: 40,
                } as cytoscape.LayoutOptions,
                userZoomingEnabled: true,
                userPanningEnabled: true,
                boxSelectionEnabled: false,
                minZoom: 0.3,
                maxZoom: 2.5,
            });

            cyRef.current = cy;

            // Fit to container
            cy.fit(undefined, 30);

            return () => {
                cy.destroy();
            };
        }, [flow, expanded]);

        return (
            <div
                ref={containerRef}
                className={`w-full bg-gray-900 rounded overflow-hidden ${expanded ? "h-[70vh]" : "min-h-[200px] max-h-[200px]"
                    }`}
            />
        );
    }
);

interface GraphNode {
    data: {
        id: string;
        label: string;
        type: "endpoint" | "message";
        failed?: string;
        slow?: string;
    };
}

interface GraphEdge {
    data: {
        id: string;
        source: string;
        target: string;
        label?: string;
        edgeType?: "published" | "handled";
    };
}

function buildGraphElements(flow: MessageFlow): (GraphNode | GraphEdge)[] {
    const nodes: GraphNode[] = [];
    const edges: GraphEdge[] = [];
    const addedNodeIds = new Set<string>();
    const addedEdgeIds = new Set<string>();

    const addNode = (
        id: string,
        label: string,
        type: "endpoint" | "message",
        failed: boolean = false,
        slow: boolean = false
    ): string => {
        if (!addedNodeIds.has(id)) {
            nodes.push({
                data: { id, label, type, failed: failed ? "true" : "false", slow: slow ? "true" : "false" },
            });
            addedNodeIds.add(id);
        }
        return id;
    };

    const addEdge = (
        sourceId: string,
        targetId: string,
        edgeType: "published" | "handled"
    ) => {
        const id = `${sourceId}->${targetId}`;
        if (!addedEdgeIds.has(id)) {
            edges.push({
                data: { id, source: sourceId, target: targetId, edgeType },
            });
            addedEdgeIds.add(id);
        }
    };

    // Helper to build label with retry count and duration
    const buildHandlerLabel = (
        endpointName: string,
        retryCount?: number,
        processingDuration?: string
    ): string => {
        let label = endpointName;

        if (processingDuration) {
            const ms = parseDuration(processingDuration);
            if (ms >= 1000) {
                label += `\n${(ms / 1000).toFixed(2)}s`;
            } else {
                label += `\n${ms.toFixed(0)}ms`;
            }
        }

        if (retryCount !== undefined && retryCount > 0) {
            label += `\n(retry #${retryCount})`;
        }

        return label;
    };

    // Sort by effective time
    const sortedMessages = [...flow.messages].sort(
        (a, b) => getEffectiveTime(a) - getEffectiveTime(b)
    );

    const publishedMsgs = sortedMessages.filter((m) => m.direction === 1);
    const handledMsgs = sortedMessages.filter((m) => m.direction === 0);

    // Map messageId -> handlers for that message
    const handlersByMessageId = new Map<string, typeof handledMsgs>();
    handledMsgs.forEach((h) => {
        if (!handlersByMessageId.has(h.messageId)) {
            handlersByMessageId.set(h.messageId, []);
        }
        handlersByMessageId.get(h.messageId)!.push(h);
    });

    // Track handler node IDs so we can connect child messages to them
    const handlerNodeIds = new Map<string, string>(); // "endpoint-messageId" -> nodeId

    // Process each published message
    publishedMsgs.forEach((pub) => {
        const msgNodeId = addNode(
            `msg-${pub.messageId}`,
            pub.messageTypeShort,
            "message"
        );

        // Determine the source of this publish
        if (pub.relatedTo && pub.relatedTo !== pub.messageId) {
            // This was published while handling another message
            const causingHandler = handledMsgs.find(
                (h) =>
                    h.messageId === pub.relatedTo && h.endpointName === pub.endpointName
            );

            if (causingHandler) {
                const handlerKey = `${causingHandler.endpointName}-${causingHandler.messageId}`;
                let handlerNodeId = handlerNodeIds.get(handlerKey);

                if (!handlerNodeId) {
                    // Create the handler node if it doesn't exist
                    handlerNodeId = addNode(
                        `hdl-${handlerKey}`,
                        buildHandlerLabel(
                            causingHandler.endpointName,
                            causingHandler.retryCount,
                            causingHandler.processingDuration
                        ),
                        "endpoint",
                        causingHandler.success === false,
                        isSlow(causingHandler.processingDuration)
                    );
                    handlerNodeIds.set(handlerKey, handlerNodeId);
                }

                // Connect: handler → message
                addEdge(handlerNodeId, msgNodeId, "published");
            } else {
                // Fallback: create publisher endpoint
                const publisherNodeId = addNode(
                    `pub-${pub.endpointName}-${pub.messageId}`,
                    pub.endpointName,
                    "endpoint"
                );
                addEdge(publisherNodeId, msgNodeId, "published");
            }
        } else {
            // Root publish (SendLocal, initial Send, etc.)
            const publisherNodeId = addNode(
                `pub-${pub.endpointName}-${pub.messageId}`,
                pub.endpointName,
                "endpoint"
            );
            addEdge(publisherNodeId, msgNodeId, "published");
        }

        // Connect this message to all its handlers (fan-out)
        const handlers = handlersByMessageId.get(pub.messageId) || [];
        handlers.forEach((handler) => {
            const handlerKey = `${handler.endpointName}-${handler.messageId}`;
            let handlerNodeId = handlerNodeIds.get(handlerKey);

            if (!handlerNodeId) {
                handlerNodeId = addNode(
                    `hdl-${handlerKey}`,
                    buildHandlerLabel(
                        handler.endpointName,
                        handler.retryCount,
                        handler.processingDuration
                    ),
                    "endpoint",
                    handler.success === false,
                    isSlow(handler.processingDuration)
                );
                handlerNodeIds.set(handlerKey, handlerNodeId);
            }

            // Connect: message → handler
            addEdge(msgNodeId, handlerNodeId, "handled");
        });
    });

    return [...nodes, ...edges];
}

const SLOW_THRESHOLD_MS = 100;

function isSlow(processingDuration?: string): boolean {
    if (!processingDuration) return false;
    return parseDuration(processingDuration) > SLOW_THRESHOLD_MS;
}

function getEffectiveTime(msg: MessageFlow["messages"][0]): number {
    const timestamp = new Date(msg.timestamp).getTime();

    if (msg.direction === 0 && msg.processingDuration) {
        const durationMs = parseDuration(msg.processingDuration);
        return timestamp - durationMs;
    }

    return timestamp;
}

function parseDuration(duration: string): number {
    const parts = duration.split(":");
    if (parts.length !== 3) return 0;

    const hours = parseInt(parts[0]) || 0;
    const minutes = parseInt(parts[1]) || 0;
    const seconds = parseFloat(parts[2]) || 0;

    return (hours * 3600 + minutes * 60 + seconds) * 1000;
}