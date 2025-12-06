import { useState, useEffect } from 'react';
import { ImpactTree } from './ImpactTree';
import { Tooltip } from './Tooltip';
import { getImpactSummary, getMultiplierEndpoints } from '../services/api';
import type { SystemImpactSummary, MultiplierEndpoint } from '../types';

export function ImpactView() {
    const [summary, setSummary] = useState<SystemImpactSummary | null>(null);
    const [multipliers, setMultipliers] = useState<MultiplierEndpoint[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        loadData();
    }, []);

    const loadData = async () => {
        setIsLoading(true);
        setError(null);

        try {
            const [summaryData, multipliersData] = await Promise.all([
                getImpactSummary(100),
                getMultiplierEndpoints(100)
            ]);

            setSummary(summaryData);
            setMultipliers(multipliersData);
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Failed to load impact data');
        } finally {
            setIsLoading(false);
        }
    };

    if (isLoading) {
        return (
            <div className="bg-gray-800 rounded-lg p-4 h-full flex items-center justify-center">
                <div className="flex flex-col items-center gap-3">
                    <div className="w-8 h-8 border-2 border-blue-500 border-t-transparent rounded-full animate-spin"></div>
                    <p className="text-gray-400">Loading impact analysis...</p>
                </div>
            </div>
        );
    }

    if (error) {
        return (
            <div className="bg-gray-800 rounded-lg p-4 h-full">
                <div className="bg-red-900/50 border border-red-500 rounded p-4">
                    <p className="text-red-300">{error}</p>
                    <button
                        onClick={loadData}
                        className="mt-2 bg-red-600 hover:bg-red-700 px-3 py-1 rounded text-sm"
                    >
                        Retry
                    </button>
                </div>
            </div>
        );
    }

    return (
        <div className="bg-gray-800 rounded-lg p-4 h-full overflow-auto">
            <div className="flex items-center justify-between mb-4">
                <div>
                    <h2 className="text-xl font-semibold">Impact Analysis</h2>
                    <p className="text-gray-400 text-sm">
                        System-wide message flow impact and multiplier analysis
                    </p>
                </div>
                <button
                    onClick={loadData}
                    className="bg-gray-700 hover:bg-gray-600 px-3 py-1.5 rounded text-sm transition-colors"
                >
                    Refresh
                </button>
            </div>

            {/* Summary Stats */}
            {summary && (
                <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
                    <StatCard
                        label="Flows Analyzed"
                        value={summary.totalFlowsAnalyzed}
                        tooltip="The number of message flows examined to calculate these metrics. A flow is a complete chain of messages triggered by a single initiating command or event."
                    />
                    <StatCard
                        label="Avg Messages/Flow"
                        value={`~${Math.round(summary.averageMessagesPerFlow)}`}
                        tooltip={`On average, each flow generates approximately ${Math.round(summary.averageMessagesPerFlow)} messages. This is calculated by dividing total messages by total flows (${summary.averageMessagesPerFlow.toFixed(2)} exact average).`}
                    />
                    <StatCard
                        label="Avg Endpoints/Flow"
                        value={`~${Math.round(summary.averageEndpointsPerFlow)}`}
                        tooltip={`On average, each flow involves approximately ${Math.round(summary.averageEndpointsPerFlow)} distinct services. Exact average: ${summary.averageEndpointsPerFlow.toFixed(2)}.`}
                    />
                    <StatCard
                        label="Avg Depth"
                        value={`~${Math.round(summary.averageDepth)}`}
                        tooltip={`On average, message chains go ${Math.round(summary.averageDepth)} levels deep. Depth represents sequential message hops from trigger to final message. Exact average: ${summary.averageDepth.toFixed(2)}.`}
                    />
                </div>
            )}

            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                {/* Multiplier Endpoints */}
                <div className="bg-gray-900 rounded-lg p-4">
                    <h3 className="text-lg font-semibold mb-1 flex items-center gap-2">
                        <span className="text-orange-400">⚡</span>
                        Multiplier Endpoints
                        <Tooltip content="Endpoints that publish more messages than they receive. These are 'amplifiers' in your system - a single incoming message triggers multiple outgoing messages. High multipliers can indicate fan-out patterns or potential message storms." />
                    </h3>
                    <p className="text-gray-400 text-sm mb-4">
                        Endpoints that produce more messages than they receive
                    </p>

                    {multipliers.length === 0 ? (
                        <p className="text-gray-500 text-sm">No multiplier endpoints found</p>
                    ) : (
                        <div className="space-y-3">
                            {multipliers.slice(0, 10).map((endpoint) => (
                                <MultiplierCard key={endpoint.endpointName} endpoint={endpoint} />
                            ))}
                        </div>
                    )}
                </div>

                {/* High Impact Message Types */}
                {summary && summary.highImpactMessageTypes.length > 0 && (
                    <div className="bg-gray-900 rounded-lg p-4">
                        <h3 className="text-lg font-semibold mb-1 flex items-center gap-2">
                            <span className="text-blue-400">💥</span>
                            High Impact Messages
                            <Tooltip content="Message types that trigger the most downstream activity. These are the messages that, when published, cause the largest cascade of subsequent messages through the system." />
                        </h3>
                        <p className="text-gray-400 text-sm mb-4">
                            Message types that trigger the most downstream activity
                        </p>

                        <div className="space-y-2">
                            {summary.highImpactMessageTypes.map((msgType, index) => (
                                <div
                                    key={msgType}
                                    className="flex items-center gap-3 bg-gray-800 rounded p-2"
                                >
                                    <span className="text-gray-500 text-sm w-6">{index + 1}.</span>
                                    <span className="text-blue-300">{msgType}</span>
                                </div>
                            ))}
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
}

interface StatCardProps {
    label: string;
    value: string | number;
    tooltip: string;
}

function StatCard({ label, value, tooltip }: StatCardProps) {
    return (
        <div className="bg-gray-900 rounded-lg p-4">
            <div className="flex items-center justify-between mb-1">
                <div className="text-2xl font-bold text-blue-400">{value}</div>
                <Tooltip content={tooltip} />
            </div>
            <div className="text-gray-400 text-sm">{label}</div>
        </div>
    );
}

interface MultiplierCardProps {
    endpoint: MultiplierEndpoint;
}

function MultiplierCard({ endpoint }: MultiplierCardProps) {
    const ratioColor = endpoint.multiplierRatio >= 2
        ? 'text-orange-400'
        : endpoint.multiplierRatio >= 1
            ? 'text-yellow-400'
            : 'text-gray-400';

    return (
        <div className="bg-gray-800 rounded p-3">
            <div className="flex items-center justify-between mb-2">
                <span className="font-medium text-green-400">{endpoint.endpointName}</span>
                <div className="flex items-center gap-2">
                    <span className={`font-bold ${ratioColor}`}>
                        {endpoint.multiplierRatio.toFixed(2)}x
                    </span>
                    <Tooltip content={`This endpoint publishes ${endpoint.multiplierRatio.toFixed(2)} messages for every message it receives. A ratio above 1.0 means the endpoint amplifies message volume.`} />
                </div>
            </div>
            <div className="flex gap-4 text-sm text-gray-400">
                <span>↓ {endpoint.totalReceived} received</span>
                <span>↑ {endpoint.totalPublished} published</span>
            </div>
            {endpoint.commonOutputMessages.length > 0 && (
                <div className="mt-2 text-xs text-gray-500">
                    Publishes: {endpoint.commonOutputMessages.join(', ')}
                </div>
            )}
        </div>
    );
}