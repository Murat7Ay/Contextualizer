import { vi } from 'vitest';

type EventCallback = (event: { payload: unknown }) => void;

const listeners: Map<string, Set<EventCallback>> = new Map();

export const listen = vi.fn(async (eventName: string, handler: EventCallback) => {
  if (!listeners.has(eventName)) {
    listeners.set(eventName, new Set());
  }
  listeners.get(eventName)!.add(handler);
  return () => {
    listeners.get(eventName)?.delete(handler);
  };
});

export const emit = vi.fn(async (_eventName: string, _payload?: unknown) => {
  // no-op in tests
});

export function __emitTestEvent(eventName: string, payload: unknown) {
  listeners.get(eventName)?.forEach((cb) => cb({ payload }));
}

export function __clearListeners() {
  listeners.clear();
  listen.mockClear();
  emit.mockClear();
}
