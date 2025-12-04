import { useEffect, useRef } from "react";
import cytoscape, { type Core } from "cytoscape";
import dagre from "cytoscape-dagre";
import type { MessageFlow } from "../types";

// Register the dagre layout
cytoscape.use(dagre);

interface FlowGraphProps {
  flow: MessageFlow;
  expanded?: boolean;
}

export function FlowGraph({ flow, expanded = false }: FlowGraphProps) {
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
          selector: "node",
          style: {
            label: "data(label)",
            "text-valign": "center",
            "text-halign": "center",
            "background-color": "#374151",
            color: "#fff",
            "font-size": "12px",
            "text-wrap": "wrap",
            "text-max-width": "100px",
            width: "label",
            height: "label",
            padding: "12px",
            shape: "roundrectangle",
            "border-width": 2,
            "border-color": "#4b5563",
          },
        },
        // Endpoint nodes
        {
          selector: 'node[type="endpoint"]',
          style: {
            "background-color": "#1e3a5f",
            "border-color": "#3b82f6",
            "font-weight": "bold",
          },
        },
        // Message nodes
        {
          selector: 'node[type="message"]',
          style: {
            "background-color": "#374151",
            "border-color": "#6b7280",
            "font-size": "10px",
          },
        },
        // Failed message nodes
        {
          selector: 'node[failed="true"]',
          style: {
            "background-color": "#7f1d1d",
            "border-color": "#ef4444",
          },
        },
        // Edge styles
        {
          selector: "edge",
          style: {
            width: 2,
            "line-color": "#4b5563",
            "target-arrow-color": "#4b5563",
            "target-arrow-shape": "triangle",
            "curve-style": "bezier",
            label: "data(label)",
            "font-size": "9px",
            color: "#9ca3af",
            "text-rotation": "autorotate",
            "text-margin-y": -10,
          },
        },
        // Published edges (endpoint → message) - Blue
        {
          selector: 'edge[edgeType="published"]',
          style: {
            "line-color": "#3b82f6",
            "target-arrow-color": "#3b82f6",
            width: 3,
          },
        },
        // Handled edges (message → endpoint) - Green
        {
          selector: 'edge[edgeType="handled"]',
          style: {
            "line-color": "#22c55e",
            "target-arrow-color": "#22c55e",
            width: 3,
          },
        },
      ],
      layout: {
        name: "dagre",
        rankDir: "LR", // Left to right
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
  }, [flow, expanded]);

  return (
    <div
      ref={containerRef}
      className={`w-full bg-gray-900 rounded overflow-hidden ${
        expanded ? "h-[70vh]" : "min-h-[200px] max-h-[200px]"
      }`}
    />
  );
}

interface GraphNode {
  data: {
    id: string;
    label: string;
    type: "endpoint" | "message";
    failed?: string;
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
    failed: boolean = false
  ): string => {
    if (!addedNodeIds.has(id)) {
      nodes.push({
        data: { id, label, type, failed: failed ? "true" : "false" },
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
            causingHandler.endpointName,
            "endpoint",
            causingHandler.success === false
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
          handler.endpointName,
          "endpoint",
          handler.success === false
        );
        handlerNodeIds.set(handlerKey, handlerNodeId);
      }

      // Connect: message → handler
      addEdge(msgNodeId, handlerNodeId, "handled");
    });
  });

  return [...nodes, ...edges];
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
