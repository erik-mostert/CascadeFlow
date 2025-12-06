import { useState } from 'react';
import type { MessageImpact } from '../types';

interface ImpactTreeProps {
  tree: MessageImpact[];
}

export function ImpactTree({ tree }: ImpactTreeProps) {
  if (tree.length === 0) {
    return <p className="text-gray-500 text-sm">No message tree data</p>;
  }

  return (
    <div className="space-y-1">
      {tree.map((node) => (
        <TreeNode key={node.messageId} node={node} />
      ))}
    </div>
  );
}

interface TreeNodeProps {
  node: MessageImpact;
  depth?: number;
}

function TreeNode({ node, depth = 0 }: TreeNodeProps) {
  const [isExpanded, setIsExpanded] = useState(depth < 2);
  const hasChildren = node.children.length > 0;

  return (
    <div className="select-none">
      <div
        className={`flex items-center gap-2 py-1.5 px-2 rounded hover:bg-gray-700 cursor-pointer ${
          depth === 0 ? 'bg-gray-700' : ''
        }`}
        style={{ paddingLeft: `${depth * 20 + 8}px` }}
        onClick={() => hasChildren && setIsExpanded(!isExpanded)}
      >
        {/* Expand/Collapse icon */}
        {hasChildren ? (
          <span className="text-gray-400 w-4 text-center">
            {isExpanded ? '▼' : '▶'}
          </span>
        ) : (
          <span className="w-4" />
        )}

        {/* Message type */}
        <span className="font-medium text-blue-400">{node.messageType}</span>

        {/* Published by */}
        <span className="text-gray-500 text-sm">from</span>
        <span className="text-green-400 text-sm">{node.publishedBy}</span>

        {/* Handled by */}
        {node.handledBy.length > 0 && (
          <>
            <span className="text-gray-500 text-sm">→</span>
            <span className="text-cyan-400 text-sm">
              {node.handledBy.join(', ')}
            </span>
          </>
        )}

        {/* Impact badge */}
        {node.downstreamMessageCount > 0 && (
          <span className="ml-auto bg-orange-900 text-orange-300 text-xs px-2 py-0.5 rounded">
            +{node.downstreamMessageCount} msgs
          </span>
        )}
      </div>

      {/* Children */}
      {hasChildren && isExpanded && (
        <div>
          {node.children.map((child) => (
            <TreeNode key={child.messageId} node={child} depth={depth + 1} />
          ))}
        </div>
      )}
    </div>
  );
}