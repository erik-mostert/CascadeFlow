import { useState, useEffect } from 'react';
import {
  BarChart,
  Bar,
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip as RechartsTooltip,
  ResponsiveContainer,
  Legend,
  AreaChart,
  Area
} from 'recharts';
import { Tooltip } from './Tooltip';
import {
  getDashboardStats,
  getMessagesOverTime,
  getTopEndpoints,
  getSlowestHandlers,
  getFailureRateOverTime
} from '../services/api';
import type {
  DashboardStats,
  MessagesOverTime,
  TopEndpoint,
  SlowestHandler,
  FailureRateOverTime
} from '../types';

export function DashboardView() {
  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [messagesOverTime, setMessagesOverTime] = useState<MessagesOverTime[]>([]);
  const [topEndpoints, setTopEndpoints] = useState<TopEndpoint[]>([]);
  const [slowestHandlers, setSlowestHandlers] = useState<SlowestHandler[]>([]);
  const [failureRate, setFailureRate] = useState<FailureRateOverTime[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadData();
  }, []);

  const loadData = async () => {
    setIsLoading(true);
    setError(null);

    try {
      const [statsData, messagesData, endpointsData, handlersData, failureData] = await Promise.all([
        getDashboardStats(),
        getMessagesOverTime(24),
        getTopEndpoints(10),
        getSlowestHandlers(10),
        getFailureRateOverTime(24)
      ]);

      setStats(statsData);
      setMessagesOverTime(messagesData);
      setTopEndpoints(endpointsData);
      setSlowestHandlers(handlersData);
      setFailureRate(failureData);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load dashboard data');
    } finally {
      setIsLoading(false);
    }
  };

  if (isLoading) {
    return (
      <div className="bg-gray-800 rounded-lg p-4 h-full flex items-center justify-center">
        <div className="flex flex-col items-center gap-3">
          <div className="w-8 h-8 border-2 border-blue-500 border-t-transparent rounded-full animate-spin"></div>
          <p className="text-gray-400">Loading dashboard...</p>
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
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <h2 className="text-xl font-semibold">Dashboard</h2>
          <p className="text-gray-400 text-sm">System health and performance metrics</p>
        </div>
        <button
          onClick={loadData}
          className="bg-gray-700 hover:bg-gray-600 px-3 py-1.5 rounded text-sm transition-colors"
        >
          Refresh
        </button>
      </div>

      {/* Stats Cards */}
      {stats && (
        <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-4 mb-6">
          <StatCard
            label="Total Messages"
            value={stats.totalMessages.toLocaleString()}
            tooltip="Total number of messages ever recorded in the database"
          />
          <StatCard
            label="Last 24h"
            value={stats.messagesLast24h.toLocaleString()}
            tooltip="Number of messages processed in the last 24 hours"
          />
          <StatCard
            label="Last Hour"
            value={stats.messagesLastHour.toLocaleString()}
            tooltip="Number of messages processed in the last hour"
            highlight
          />
          <StatCard
            label="Live Cache"
            value={`${stats.activeFlows}/100`}
            tooltip="Message flows cached in memory for real-time display. Maximum 100 flows are kept in memory; older flows are persisted to the database and searchable via the Flows view."
            color="green"
          />
          <StatCard
            label="Total Failures"
            value={stats.totalFailures.toLocaleString()}
            tooltip="Total number of message handling failures recorded"
            color={stats.totalFailures > 0 ? 'red' : 'default'}
          />
          <StatCard
            label="Failure Rate"
            value={`${stats.failureRate.toFixed(2)}%`}
            tooltip="Percentage of messages that failed processing"
            color={stats.failureRate > 5 ? 'red' : stats.failureRate > 1 ? 'yellow' : 'green'}
          />
        </div>
      )}

      {/* Charts Row 1 */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-6">
        {/* Messages Over Time */}
        <div className="bg-gray-900 rounded-lg p-4">
          <div className="flex items-center gap-2 mb-4">
            <h3 className="text-lg font-semibold">Messages Over Time</h3>
            <Tooltip content="Hourly message volume over the last 24 hours. Shows both successful messages and failures." />
          </div>
          <div className="h-64">
            <ResponsiveContainer width="100%" height="100%">
              <AreaChart data={messagesOverTime}>
                <CartesianGrid strokeDasharray="3 3" stroke="#374151" />
                <XAxis dataKey="hour" stroke="#9ca3af" fontSize={12} />
                <YAxis stroke="#9ca3af" fontSize={12} />
                <RechartsTooltip
                  contentStyle={{ backgroundColor: '#1f2937', border: '1px solid #374151' }}
                  labelStyle={{ color: '#f3f4f6' }}
                />
                <Legend />
                <Area
                  type="monotone"
                  dataKey="count"
                  name="Messages"
                  stroke="#3b82f6"
                  fill="#3b82f6"
                  fillOpacity={0.3}
                />
                <Area
                  type="monotone"
                  dataKey="failures"
                  name="Failures"
                  stroke="#ef4444"
                  fill="#ef4444"
                  fillOpacity={0.3}
                />
              </AreaChart>
            </ResponsiveContainer>
          </div>
        </div>

        {/* Failure Rate Over Time */}
        <div className="bg-gray-900 rounded-lg p-4">
          <div className="flex items-center gap-2 mb-4">
            <h3 className="text-lg font-semibold">Failure Rate Over Time</h3>
            <Tooltip content="Percentage of messages that failed per hour. A rising trend may indicate system issues." />
          </div>
          <div className="h-64">
            <ResponsiveContainer width="100%" height="100%">
              <LineChart data={failureRate}>
                <CartesianGrid strokeDasharray="3 3" stroke="#374151" />
                <XAxis dataKey="hour" stroke="#9ca3af" fontSize={12} />
                <YAxis stroke="#9ca3af" fontSize={12} unit="%" />
                <RechartsTooltip
                  contentStyle={{ backgroundColor: '#1f2937', border: '1px solid #374151' }}
                  labelStyle={{ color: '#f3f4f6' }}
                  formatter={(value: number) => [`${value.toFixed(2)}%`, 'Failure Rate']}
                />
                <Line
                  type="monotone"
                  dataKey="failureRate"
                  name="Failure Rate"
                  stroke="#f59e0b"
                  strokeWidth={2}
                  dot={false}
                />
              </LineChart>
            </ResponsiveContainer>
          </div>
        </div>
      </div>

      {/* Charts Row 2 */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Top Endpoints */}
        <div className="bg-gray-900 rounded-lg p-4">
          <div className="flex items-center gap-2 mb-4">
            <h3 className="text-lg font-semibold">Top Endpoints by Volume</h3>
            <Tooltip content="Busiest endpoints in the last 24 hours ranked by message count." />
          </div>
          <div className="h-64">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={topEndpoints} layout="vertical">
                <CartesianGrid strokeDasharray="3 3" stroke="#374151" />
                <XAxis type="number" stroke="#9ca3af" fontSize={12} />
                <YAxis
                  type="category"
                  dataKey="endpoint"
                  stroke="#9ca3af"
                  fontSize={11}
                  width={120}
                  tickFormatter={(value) => value.length > 15 ? `${value.substring(0, 15)}...` : value}
                />
                <RechartsTooltip
                  contentStyle={{ backgroundColor: '#1f2937', border: '1px solid #374151' }}
                  labelStyle={{ color: '#f3f4f6' }}
                />
                <Legend />
                <Bar dataKey="messageCount" name="Messages" fill="#3b82f6" />
                <Bar dataKey="failures" name="Failures" fill="#ef4444" />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </div>

        {/* Slowest Handlers */}
        <div className="bg-gray-900 rounded-lg p-4">
          <div className="flex items-center gap-2 mb-4">
            <h3 className="text-lg font-semibold">Slowest Handlers</h3>
            <Tooltip content="Message handlers with the highest average processing time in the last 24 hours. These may be candidates for optimization." />
          </div>
          <div className="h-64 overflow-auto">
            {slowestHandlers.length === 0 ? (
              <p className="text-gray-500 text-sm">No handler data available</p>
            ) : (
              <table className="w-full text-sm">
                <thead className="text-gray-400 text-left">
                  <tr>
                    <th className="pb-2">Endpoint</th>
                    <th className="pb-2">Message</th>
                    <th className="pb-2 text-right">Avg</th>
                    <th className="pb-2 text-right">Max</th>
                  </tr>
                </thead>
                <tbody>
                  {slowestHandlers.map((handler, index) => (
                    <tr key={index} className="border-t border-gray-700">
                      <td className="py-2 text-green-400">{handler.endpoint}</td>
                      <td className="py-2 text-blue-300">{handler.messageType}</td>
                      <td className={`py-2 text-right ${handler.avgProcessingMs > 100 ? 'text-orange-400' : 'text-gray-300'}`}>
                        {handler.avgProcessingMs.toFixed(0)}ms
                      </td>
                      <td className={`py-2 text-right ${handler.maxProcessingMs > 500 ? 'text-red-400' : 'text-gray-300'}`}>
                        {handler.maxProcessingMs.toFixed(0)}ms
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}

interface StatCardProps {
  label: string;
  value: string;
  tooltip: string;
  color?: 'default' | 'green' | 'red' | 'yellow';
  highlight?: boolean;
}

function StatCard({ label, value, tooltip, color = 'default', highlight = false }: StatCardProps) {
  const colorClasses = {
    default: 'text-blue-400',
    green: 'text-green-400',
    red: 'text-red-400',
    yellow: 'text-yellow-400'
  };

  return (
    <div className={`bg-gray-900 rounded-lg p-4 ${highlight ? 'ring-1 ring-blue-500' : ''}`}>
      <div className="flex items-center justify-between mb-1">
        <div className={`text-2xl font-bold ${colorClasses[color]}`}>{value}</div>
        <Tooltip content={tooltip} />
      </div>
      <div className="text-gray-400 text-sm">{label}</div>
    </div>
  );
}