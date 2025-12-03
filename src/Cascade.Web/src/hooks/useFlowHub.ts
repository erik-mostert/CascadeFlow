import { useState, useEffect, useCallback, useRef } from 'react';
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import type { MessageTelemetry, MessageFlow, SystemTopology } from '../types';

interface FlowHubState {
    connection: HubConnection | null;
    connectionStatus: 'disconnected' | 'connecting' | 'connected' | 'error';
    flows: MessageFlow[];
    topology: SystemTopology | null;
    lastTelemetry: MessageTelemetry | null;
}

interface FlowHubActions {
    subscribeToFlow: (correlationId: string) => Promise<void>;
    unsubscribeFromFlow: (correlationId: string) => Promise<void>;
    clearFlows: () => void;
}

const COLLECTOR_URL = 'http://localhost:5100';

export function useFlowHub(): FlowHubState & FlowHubActions {
    const [connectionStatus, setConnectionStatus] = useState<FlowHubState['connectionStatus']>('disconnected');
    const [flows, setFlows] = useState<MessageFlow[]>([]);
    const [topology, setTopology] = useState<SystemTopology | null>(null);
    const [lastTelemetry, setLastTelemetry] = useState<MessageTelemetry | null>(null);
    const connectionRef = useRef<HubConnection | null>(null);

    useEffect(() => {
        const connection = new HubConnectionBuilder()
            .withUrl(`${COLLECTOR_URL}/hubs/flow`)
            .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
            .configureLogging(LogLevel.Information)
            .build();

        connectionRef.current = connection;

        // Event handlers
        connection.on('InitialState', (initialFlows: MessageFlow[]) => {
            console.log('[SignalR] InitialState received:', initialFlows.length, 'flows');
            setFlows(initialFlows);
        });

        connection.on('TelemetryReceived', (telemetry: MessageTelemetry) => {
            console.log('[SignalR] TelemetryReceived:', telemetry.endpointName, telemetry.messageTypeShort);
            setLastTelemetry(telemetry);
        });

        connection.on('FlowUpdated', (flow: MessageFlow) => {
            console.log('[SignalR] FlowUpdated:', flow.correlationId, flow.messageCount, 'messages');
            setFlows(prev => {
                const existing = prev.findIndex(f => f.correlationId === flow.correlationId);
                if (existing >= 0) {
                    const updated = [...prev];
                    updated[existing] = flow;
                    return updated;
                }
                return [flow, ...prev];
            });
        });

        connection.on('TopologyUpdated', (newTopology: SystemTopology) => {
            console.log('[SignalR] TopologyUpdated:', newTopology.endpointCount, 'endpoints');
            setTopology(newTopology);
        });

        // Connection state handlers
        connection.onreconnecting(() => {
            console.log('[SignalR] Reconnecting...');
            setConnectionStatus('connecting');
        });

        connection.onreconnected(() => {
            console.log('[SignalR] Reconnected');
            setConnectionStatus('connected');
        });

        connection.onclose(() => {
            console.log('[SignalR] Disconnected');
            setConnectionStatus('disconnected');
        });

        // Start connection
        setConnectionStatus('connecting');
        connection.start()
            .then(() => {
                console.log('[SignalR] Connected');
                setConnectionStatus('connected');
            })
            .catch(err => {
                console.error('[SignalR] Connection failed:', err);
                setConnectionStatus('error');
            });

        // Cleanup
        return () => {
            connection.stop();
        };
    }, []);

    const subscribeToFlow = useCallback(async (correlationId: string) => {
        if (connectionRef.current?.state === HubConnectionState.Connected) {
            await connectionRef.current.invoke('SubscribeToFlow', correlationId);
        }
    }, []);

    const unsubscribeFromFlow = useCallback(async (correlationId: string) => {
        if (connectionRef.current?.state === HubConnectionState.Connected) {
            await connectionRef.current.invoke('UnsubscribeFromFlow', correlationId);
        }
    }, []);

    const clearFlows = useCallback(() => {
        setFlows([]);
    }, []);

    return {
        connection: connectionRef.current,
        connectionStatus,
        flows,
        topology,
        lastTelemetry,
        subscribeToFlow,
        unsubscribeFromFlow,
        clearFlows,
    };
}