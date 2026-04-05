import { useEffect, useRef, useState } from 'react';
import { X } from 'lucide-react';
import { Button } from './button';

export type CountdownToastLevel = 'success' | 'error' | 'warning' | 'info' | 'debug' | 'critical';

export type CountdownToastAction = {
  id: string;
  label: string;
  style?: 'primary' | 'secondary' | 'danger';
  closeOnClick?: boolean;
  isDefaultAction?: boolean;
};

export function CountdownToastView(props: {
  level?: CountdownToastLevel;
  title?: string;
  message: string;
  details?: string;
  durationMs: number;
  actions?: CountdownToastAction[];
  onClose: () => void;
  onExpire: () => void;
  onAction?: (actionId: string, closeOnClick: boolean) => void;
}) {
  const { level, title, message, details, durationMs, actions = [], onClose, onExpire, onAction } = props;

  const startedAtRef = useRef<number>(Date.now());
  const pausedAtRef = useRef<number | null>(null);
  const pausedTotalRef = useRef<number>(0);
  const expiredRef = useRef<boolean>(false);

  const [now, setNow] = useState<number>(() => Date.now());
  const [isPaused, setIsPaused] = useState<boolean>(false);

  useEffect(() => {
    startedAtRef.current = Date.now();
  }, []);

  useEffect(() => {
    if (!durationMs || durationMs <= 0) return;
    if (isPaused) return;

    const id = window.setInterval(() => {
      const t = Date.now();
      setNow(t);

      const elapsed = t - startedAtRef.current - pausedTotalRef.current;
      const remaining = Math.max(0, durationMs - elapsed);
      if (remaining <= 0 && !expiredRef.current) {
        expiredRef.current = true;
        onExpire();
      }
    }, 200);

    return () => window.clearInterval(id);
  }, [durationMs, isPaused, onExpire]);

  const remainingMs = (() => {
    if (!durationMs || durationMs <= 0) return 0;
    const elapsed = now - startedAtRef.current - pausedTotalRef.current;
    return Math.max(0, durationMs - elapsed);
  })();

  const remainingSeconds = Math.max(0, Math.ceil(remainingMs / 1000));
  const progress = durationMs && durationMs > 0 ? Math.min(1, Math.max(0, 1 - remainingMs / durationMs)) : 0;

  const accent =
    level === 'success'
      ? 'bg-emerald-500'
      : level === 'warning'
        ? 'bg-amber-500'
        : level === 'error' || level === 'critical'
          ? 'bg-red-500'
          : 'bg-blue-500';

  const handleMouseEnter = () => {
    if (isPaused) return;
    pausedAtRef.current = Date.now();
    setIsPaused(true);
  };

  const handleMouseLeave = () => {
    if (!isPaused) return;
    const pausedAt = pausedAtRef.current;
    if (pausedAt != null) pausedTotalRef.current += Date.now() - pausedAt;
    pausedAtRef.current = null;
    setIsPaused(false);
    setNow(Date.now());
  };

  return (
    <div
      className="w-[360px] rounded-lg border bg-popover text-popover-foreground shadow-lg overflow-hidden"
      onMouseEnter={handleMouseEnter}
      onMouseLeave={handleMouseLeave}
    >
      <div className="flex">
        <div className={`w-1 ${accent}`} />
        <div className="flex-1">
          <div className="flex items-start justify-between gap-3 p-4 pb-3">
            <div className="min-w-0">
              {title ? <div className="text-sm font-semibold truncate">{title}</div> : null}
              <div className="text-sm whitespace-pre-wrap break-words">{message}</div>
              {details ? (
                <div className="mt-1 text-xs text-muted-foreground whitespace-pre-wrap break-words">{details}</div>
              ) : null}
            </div>
            <div className="shrink-0 flex items-center gap-2">
              {durationMs > 0 ? (
                <span className="text-xs tabular-nums text-muted-foreground">{remainingSeconds}s</span>
              ) : null}
              <button
                type="button"
                className="rounded-md p-1 text-muted-foreground hover:bg-accent hover:text-foreground"
                onClick={onClose}
                aria-label="Close"
              >
                <X className="h-4 w-4" />
              </button>
            </div>
          </div>

          {actions.length > 0 && onAction ? (
            <div className="flex justify-end gap-2 px-4 pb-4 pt-1">
              {actions.map((a) => {
                const variant =
                  a.style === 'danger' ? 'destructive' : a.style === 'secondary' ? 'outline' : 'default';
                const closeOnClick = a.closeOnClick !== false;

                return (
                  <Button key={a.id} size="sm" variant={variant} onClick={() => onAction(a.id, closeOnClick)}>
                    {a.label}
                  </Button>
                );
              })}
            </div>
          ) : null}
        </div>
      </div>

      {durationMs > 0 ? (
        <div className="h-1 w-full bg-muted/50">
          <div className={`h-1 ${accent}`} style={{ width: `${Math.round(progress * 100)}%`, transition: 'width 200ms linear' }} />
        </div>
      ) : null}
    </div>
  );
}


