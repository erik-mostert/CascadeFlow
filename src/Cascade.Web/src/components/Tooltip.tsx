import { useState, useRef, useEffect } from 'react';
import { createPortal } from 'react-dom';

interface TooltipProps {
  content: string;
  children?: React.ReactNode;
}

export function Tooltip({ content, children }: TooltipProps) {
  const [isVisible, setIsVisible] = useState(false);
  const [coords, setCoords] = useState({ top: 0, left: 0 });
  const [position, setPosition] = useState<'top' | 'bottom'>('bottom');
  const [align, setAlign] = useState<'center' | 'left' | 'right'>('center');
  const triggerRef = useRef<HTMLDivElement>(null);

  const TOOLTIP_WIDTH = 256; // w-64 = 16rem = 256px

  useEffect(() => {
    if (isVisible && triggerRef.current) {
      const rect = triggerRef.current.getBoundingClientRect();
      const viewportWidth = window.innerWidth;
      
      // Vertical position
      const showBelow = rect.top < 150;
      setPosition(showBelow ? 'bottom' : 'top');

      // Horizontal position
      const centerX = rect.left + rect.width / 2;
      const tooltipLeft = centerX - TOOLTIP_WIDTH / 2;
      const tooltipRight = centerX + TOOLTIP_WIDTH / 2;

      let left = centerX;
      let newAlign: 'center' | 'left' | 'right' = 'center';

      if (tooltipLeft < 8) {
        // Too close to left edge
        left = 8 + TOOLTIP_WIDTH / 2;
        newAlign = 'left';
      } else if (tooltipRight > viewportWidth - 8) {
        // Too close to right edge
        left = viewportWidth - 8 - TOOLTIP_WIDTH / 2;
        newAlign = 'right';
      }

      setAlign(newAlign);
      setCoords({
        left,
        top: showBelow ? rect.bottom + 8 : rect.top - 8,
      });
    }
  }, [isVisible]);

  return (
    <div className="relative inline-flex items-center">
      <div
        ref={triggerRef}
        onMouseEnter={() => setIsVisible(true)}
        onMouseLeave={() => setIsVisible(false)}
        className="cursor-help"
      >
        {children ?? <InfoIcon />}
      </div>
      
      {isVisible && createPortal(
        <div
          style={{
            position: 'fixed',
            left: coords.left,
            top: coords.top,
            transform: `translateX(-50%) ${position === 'top' ? 'translateY(-100%)' : ''}`,
          }}
          className="z-[100] w-64 px-3 py-2 text-sm bg-gray-900 border border-gray-600 rounded-lg shadow-lg text-gray-200"
        >
          {content}
          {/* Arrow */}
          <div 
            style={{
              left: align === 'center' ? '50%' : 
                    align === 'left' ? '24px' : 
                    'calc(100% - 24px)',
            }}
            className={`absolute -translate-x-1/2 w-2 h-2 bg-gray-900 border-gray-600 transform rotate-45 ${
              position === 'top'
                ? 'bottom-0 translate-y-1/2 border-r border-b'
                : 'top-0 -translate-y-1/2 border-l border-t'
            }`}
          />
        </div>,
        document.body
      )}
    </div>
  );
}

function InfoIcon() {
  return (
    <svg 
      className="w-4 h-4 text-gray-500 hover:text-gray-300 transition-colors" 
      fill="none" 
      stroke="currentColor" 
      viewBox="0 0 24 24"
    >
      <circle cx="12" cy="12" r="10" strokeWidth="2" />
      <path strokeWidth="2" strokeLinecap="round" d="M12 16v-4m0-4h.01" />
    </svg>
  );
}