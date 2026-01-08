import { useState, useEffect } from 'react';
import { getApiKeys, createApiKey, revokeApiKey, deleteApiKey, AdminKeyRequiredError, setStoredAdminKey, getStoredAdminKey } from '../services/api';
import type { ApiKey, CreateApiKeyResponse } from '../types';

export function ApiKeysView() {
    const [keys, setKeys] = useState<ApiKey[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
    const [newKey, setNewKey] = useState<CreateApiKeyResponse | null>(null);
    const [deleteConfirmId, setDeleteConfirmId] = useState<number | null>(null);
    const [showAdminKeyPrompt, setShowAdminKeyPrompt] = useState(false);
    const [adminKeyRequired, setAdminKeyRequired] = useState(false);

    useEffect(() => {
        loadKeys();
    }, []);

    const loadKeys = async () => {
        setIsLoading(true);
        setError(null);
        try {
            const data = await getApiKeys();
            setKeys(data);
            setAdminKeyRequired(false);
        } catch (err) {
            if (err instanceof AdminKeyRequiredError) {
                setAdminKeyRequired(true);
                setShowAdminKeyPrompt(true);
            } else {
                setError(err instanceof Error ? err.message : 'Failed to load API keys');
            }
        } finally {
            setIsLoading(false);
        }
    };

    const handleAdminKeySubmit = (key: string) => {
        setStoredAdminKey(key);
        setShowAdminKeyPrompt(false);
        loadKeys();
    };

    const handleRevoke = async (id: number) => {
        try {
            await revokeApiKey(id);
            await loadKeys();
        } catch (err) {
            if (err instanceof AdminKeyRequiredError) {
                setAdminKeyRequired(true);
                setShowAdminKeyPrompt(true);
            } else {
                setError(err instanceof Error ? err.message : 'Failed to revoke key');
            }
        }
    };

    const handleDelete = async (id: number) => {
        try {
            await deleteApiKey(id);
            setDeleteConfirmId(null);
            await loadKeys();
        } catch (err) {
            if (err instanceof AdminKeyRequiredError) {
                setAdminKeyRequired(true);
                setShowAdminKeyPrompt(true);
            } else {
                setError(err instanceof Error ? err.message : 'Failed to delete key');
            }
        }
    };

    const handleCreateSuccess = (key: CreateApiKeyResponse) => {
        setNewKey(key);
        loadKeys();
    };

    if (isLoading && !adminKeyRequired) {
        return (
            <div className="bg-gray-800 rounded-lg p-4 h-full flex items-center justify-center">
                <div className="flex flex-col items-center gap-3">
                    <div className="w-8 h-8 border-2 border-blue-500 border-t-transparent rounded-full animate-spin"></div>
                    <p className="text-gray-400">Loading API keys...</p>
                </div>
            </div>
        );
    }

    // Show admin key required view
    if (adminKeyRequired && !getStoredAdminKey()) {
        return (
            <div className="bg-gray-800 rounded-lg p-4 h-full overflow-auto">
                <div className="flex items-center justify-between mb-4">
                    <div>
                        <h2 className="text-xl font-semibold">API Keys</h2>
                        <p className="text-gray-400 text-sm">
                            Manage authentication keys for telemetry ingestion
                        </p>
                    </div>
                </div>

                <div className="bg-yellow-900/30 border border-yellow-600 rounded-lg p-6 text-center">
                    <svg className="w-12 h-12 mx-auto mb-4 text-yellow-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
                    </svg>
                    <h3 className="text-lg font-semibold text-yellow-400 mb-2">Admin Key Required</h3>
                    <p className="text-gray-300 mb-4">
                        API key management is protected. Enter the admin key to access this section.
                    </p>
                    <button
                        onClick={() => setShowAdminKeyPrompt(true)}
                        className="bg-yellow-600 hover:bg-yellow-700 px-4 py-2 rounded transition-colors"
                    >
                        Enter Admin Key
                    </button>
                </div>

                {showAdminKeyPrompt && (
                    <AdminKeyPromptModal
                        onSubmit={handleAdminKeySubmit}
                        onClose={() => setShowAdminKeyPrompt(false)}
                    />
                )}
            </div>
        );
    }

    return (
        <div className="bg-gray-800 rounded-lg p-4 h-full overflow-auto">
            <div className="flex items-center justify-between mb-4">
                <div>
                    <h2 className="text-xl font-semibold">API Keys</h2>
                    <p className="text-gray-400 text-sm">
                        Manage authentication keys for telemetry ingestion
                    </p>
                </div>
                <div className="flex gap-2">
                    <button
                        onClick={loadKeys}
                        className="bg-gray-700 hover:bg-gray-600 px-3 py-1.5 rounded text-sm transition-colors"
                    >
                        Refresh
                    </button>
                    <button
                        onClick={() => setIsCreateModalOpen(true)}
                        className="bg-blue-600 hover:bg-blue-700 px-3 py-1.5 rounded text-sm transition-colors"
                    >
                        Create Key
                    </button>
                </div>
            </div>

            {error && (
                <div className="bg-red-900/50 border border-red-500 rounded p-4 mb-4">
                    <p className="text-red-300">{error}</p>
                    <button
                        onClick={() => setError(null)}
                        className="mt-2 text-red-400 hover:text-red-300 text-sm"
                    >
                        Dismiss
                    </button>
                </div>
            )}

            {/* New Key Display */}
            {newKey && (
                <NewKeyAlert keyData={newKey} onDismiss={() => setNewKey(null)} />
            )}

            {/* Keys Table */}
            {keys.length === 0 ? (
                <div className="bg-gray-900 rounded-lg p-8 text-center">
                    <p className="text-gray-400 mb-4">No API keys have been created yet.</p>
                    <button
                        onClick={() => setIsCreateModalOpen(true)}
                        className="bg-blue-600 hover:bg-blue-700 px-4 py-2 rounded transition-colors"
                    >
                        Create Your First Key
                    </button>
                </div>
            ) : (
                <div className="bg-gray-900 rounded-lg overflow-hidden">
                    <table className="w-full">
                        <thead>
                            <tr className="bg-gray-800 text-left text-sm text-gray-400">
                                <th className="px-4 py-3">Status</th>
                                <th className="px-4 py-3">Name</th>
                                <th className="px-4 py-3">Key Prefix</th>
                                <th className="px-4 py-3">Endpoint Restriction</th>
                                <th className="px-4 py-3">Created</th>
                                <th className="px-4 py-3">Last Used</th>
                                <th className="px-4 py-3">Actions</th>
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-gray-800">
                            {keys.map((key) => (
                                <tr key={key.id} className="hover:bg-gray-800/50">
                                    <td className="px-4 py-3">
                                        <StatusBadge isActive={key.isActive} />
                                    </td>
                                    <td className="px-4 py-3 font-medium">{key.name}</td>
                                    <td className="px-4 py-3">
                                        <code className="bg-gray-800 px-2 py-1 rounded text-sm text-gray-300">
                                            {key.keyPrefix}...
                                        </code>
                                    </td>
                                    <td className="px-4 py-3 text-gray-400">
                                        {key.endpointName || <span className="text-gray-500">All endpoints</span>}
                                    </td>
                                    <td className="px-4 py-3 text-gray-400 text-sm">
                                        {formatDate(key.createdAt)}
                                    </td>
                                    <td className="px-4 py-3 text-gray-400 text-sm">
                                        {key.lastUsedAt ? formatDate(key.lastUsedAt) : <span className="text-gray-500">Never</span>}
                                    </td>
                                    <td className="px-4 py-3">
                                        <div className="flex gap-2">
                                            {key.isActive && (
                                                <button
                                                    onClick={() => handleRevoke(key.id)}
                                                    className="text-yellow-400 hover:text-yellow-300 text-sm"
                                                >
                                                    Revoke
                                                </button>
                                            )}
                                            {deleteConfirmId === key.id ? (
                                                <div className="flex gap-2 items-center">
                                                    <span className="text-gray-400 text-sm">Delete?</span>
                                                    <button
                                                        onClick={() => handleDelete(key.id)}
                                                        className="text-red-400 hover:text-red-300 text-sm"
                                                    >
                                                        Yes
                                                    </button>
                                                    <button
                                                        onClick={() => setDeleteConfirmId(null)}
                                                        className="text-gray-400 hover:text-gray-300 text-sm"
                                                    >
                                                        No
                                                    </button>
                                                </div>
                                            ) : (
                                                <button
                                                    onClick={() => setDeleteConfirmId(key.id)}
                                                    className="text-red-400 hover:text-red-300 text-sm"
                                                >
                                                    Delete
                                                </button>
                                            )}
                                        </div>
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            )}

            {/* Create Modal */}
            {isCreateModalOpen && (
                <CreateKeyModal
                    onClose={() => setIsCreateModalOpen(false)}
                    onSuccess={handleCreateSuccess}
                />
            )}
        </div>
    );
}

interface StatusBadgeProps {
    isActive: boolean;
}

function StatusBadge({ isActive }: StatusBadgeProps) {
    return (
        <span
            className={`inline-flex items-center gap-1.5 px-2 py-1 rounded text-xs font-medium ${
                isActive
                    ? 'bg-green-900/50 text-green-400'
                    : 'bg-red-900/50 text-red-400'
            }`}
        >
            <span className={`w-1.5 h-1.5 rounded-full ${isActive ? 'bg-green-400' : 'bg-red-400'}`}></span>
            {isActive ? 'Active' : 'Revoked'}
        </span>
    );
}

interface NewKeyAlertProps {
    keyData: CreateApiKeyResponse;
    onDismiss: () => void;
}

function NewKeyAlert({ keyData, onDismiss }: NewKeyAlertProps) {
    const [copied, setCopied] = useState(false);

    const copyToClipboard = async () => {
        try {
            await navigator.clipboard.writeText(keyData.key);
            setCopied(true);
            setTimeout(() => setCopied(false), 2000);
        } catch {
            // Fallback for older browsers
            const textArea = document.createElement('textarea');
            textArea.value = keyData.key;
            document.body.appendChild(textArea);
            textArea.select();
            document.execCommand('copy');
            document.body.removeChild(textArea);
            setCopied(true);
            setTimeout(() => setCopied(false), 2000);
        }
    };

    return (
        <div className="bg-green-900/30 border border-green-500 rounded-lg p-4 mb-4">
            <div className="flex items-start justify-between">
                <div>
                    <h3 className="text-green-400 font-semibold mb-1">API Key Created Successfully</h3>
                    <p className="text-gray-300 text-sm mb-3">
                        Make sure to copy your API key now. You won't be able to see it again!
                    </p>
                </div>
                <button
                    onClick={onDismiss}
                    className="text-gray-400 hover:text-white"
                >
                    <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                    </svg>
                </button>
            </div>
            <div className="flex items-center gap-2">
                <code className="flex-1 bg-gray-900 px-3 py-2 rounded text-green-300 font-mono text-sm break-all">
                    {keyData.key}
                </code>
                <button
                    onClick={copyToClipboard}
                    className={`px-3 py-2 rounded transition-colors ${
                        copied
                            ? 'bg-green-600 text-white'
                            : 'bg-gray-700 hover:bg-gray-600 text-gray-300'
                    }`}
                >
                    {copied ? 'Copied!' : 'Copy'}
                </button>
            </div>
            <p className="text-yellow-400 text-xs mt-2">
                Store this key securely. Set it as the CASCADE_API_KEY environment variable in your NServiceBus endpoints.
            </p>
        </div>
    );
}

interface CreateKeyModalProps {
    onClose: () => void;
    onSuccess: (key: CreateApiKeyResponse) => void;
}

function CreateKeyModal({ onClose, onSuccess }: CreateKeyModalProps) {
    const [name, setName] = useState('');
    const [endpointName, setEndpointName] = useState('');
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!name.trim()) {
            setError('Name is required');
            return;
        }

        setIsSubmitting(true);
        setError(null);

        try {
            const result = await createApiKey(name.trim(), endpointName.trim() || undefined);
            onSuccess(result);
            onClose();
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Failed to create API key');
        } finally {
            setIsSubmitting(false);
        }
    };

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center">
            {/* Backdrop */}
            <div className="absolute inset-0 bg-black/70" onClick={onClose} />

            {/* Modal content */}
            <div className="relative bg-gray-800 rounded-lg w-full max-w-md shadow-2xl">
                {/* Header */}
                <div className="flex items-center justify-between px-4 py-3 border-b border-gray-700">
                    <h2 className="text-lg font-semibold">Create API Key</h2>
                    <button
                        onClick={onClose}
                        className="text-gray-400 hover:text-white p-1 rounded hover:bg-gray-700 transition-colors"
                    >
                        <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                        </svg>
                    </button>
                </div>

                {/* Body */}
                <form onSubmit={handleSubmit} className="p-4">
                    {error && (
                        <div className="bg-red-900/50 border border-red-500 rounded p-3 mb-4">
                            <p className="text-red-300 text-sm">{error}</p>
                        </div>
                    )}

                    <div className="mb-4">
                        <label className="block text-sm text-gray-400 mb-1">
                            Name <span className="text-red-400">*</span>
                        </label>
                        <input
                            type="text"
                            value={name}
                            onChange={(e) => setName(e.target.value)}
                            placeholder="e.g., OrderService Production"
                            className="w-full bg-gray-900 border border-gray-700 rounded px-3 py-2 text-white focus:outline-none focus:border-blue-500"
                            autoFocus
                        />
                        <p className="text-gray-500 text-xs mt-1">
                            A descriptive name to identify this key
                        </p>
                    </div>

                    <div className="mb-6">
                        <label className="block text-sm text-gray-400 mb-1">
                            Endpoint Restriction <span className="text-gray-500">(optional)</span>
                        </label>
                        <input
                            type="text"
                            value={endpointName}
                            onChange={(e) => setEndpointName(e.target.value)}
                            placeholder="e.g., Orders.Endpoint"
                            className="w-full bg-gray-900 border border-gray-700 rounded px-3 py-2 text-white focus:outline-none focus:border-blue-500"
                        />
                        <p className="text-gray-500 text-xs mt-1">
                            Leave empty to allow any endpoint, or specify an endpoint name to restrict this key
                        </p>
                    </div>

                    <div className="flex gap-3 justify-end">
                        <button
                            type="button"
                            onClick={onClose}
                            className="px-4 py-2 rounded bg-gray-700 hover:bg-gray-600 transition-colors"
                        >
                            Cancel
                        </button>
                        <button
                            type="submit"
                            disabled={isSubmitting}
                            className="px-4 py-2 rounded bg-blue-600 hover:bg-blue-700 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                        >
                            {isSubmitting ? 'Creating...' : 'Create Key'}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
}

function formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString(undefined, {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
    });
}

interface AdminKeyPromptModalProps {
    onSubmit: (key: string) => void;
    onClose: () => void;
}

function AdminKeyPromptModal({ onSubmit, onClose }: AdminKeyPromptModalProps) {
    const [adminKey, setAdminKey] = useState('');
    const [error, setError] = useState<string | null>(null);

    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        if (!adminKey.trim()) {
            setError('Admin key is required');
            return;
        }
        onSubmit(adminKey.trim());
    };

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center">
            {/* Backdrop */}
            <div className="absolute inset-0 bg-black/70" onClick={onClose} />

            {/* Modal content */}
            <div className="relative bg-gray-800 rounded-lg w-full max-w-md shadow-2xl">
                {/* Header */}
                <div className="flex items-center justify-between px-4 py-3 border-b border-gray-700">
                    <h2 className="text-lg font-semibold">Enter Admin Key</h2>
                    <button
                        onClick={onClose}
                        className="text-gray-400 hover:text-white p-1 rounded hover:bg-gray-700 transition-colors"
                    >
                        <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                        </svg>
                    </button>
                </div>

                {/* Body */}
                <form onSubmit={handleSubmit} className="p-4">
                    <p className="text-gray-400 text-sm mb-4">
                        API key management requires administrator access. Enter the admin key configured on the Cascade Collector.
                    </p>

                    {error && (
                        <div className="bg-red-900/50 border border-red-500 rounded p-3 mb-4">
                            <p className="text-red-300 text-sm">{error}</p>
                        </div>
                    )}

                    <div className="mb-4">
                        <label className="block text-sm text-gray-400 mb-1">
                            Admin Key <span className="text-red-400">*</span>
                        </label>
                        <input
                            type="password"
                            value={adminKey}
                            onChange={(e) => setAdminKey(e.target.value)}
                            placeholder="Enter admin key"
                            className="w-full bg-gray-900 border border-gray-700 rounded px-3 py-2 text-white focus:outline-none focus:border-blue-500"
                            autoFocus
                        />
                        <p className="text-gray-500 text-xs mt-1">
                            Set via Cascade__AdminKey environment variable on the Collector
                        </p>
                    </div>

                    <div className="flex gap-3 justify-end">
                        <button
                            type="button"
                            onClick={onClose}
                            className="px-4 py-2 rounded bg-gray-700 hover:bg-gray-600 transition-colors"
                        >
                            Cancel
                        </button>
                        <button
                            type="submit"
                            className="px-4 py-2 rounded bg-blue-600 hover:bg-blue-700 transition-colors"
                        >
                            Submit
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
}
