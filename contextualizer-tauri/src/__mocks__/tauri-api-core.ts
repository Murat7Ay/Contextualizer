import { vi } from 'vitest';

const invokeHandlers: Record<string, (...args: unknown[]) => unknown> = {};

export const invoke = vi.fn(async (cmd: string, args?: Record<string, unknown>) => {
  if (invokeHandlers[cmd]) {
    return invokeHandlers[cmd](args);
  }
  return undefined;
});

export function __mockInvokeHandler(cmd: string, handler: (...args: unknown[]) => unknown) {
  invokeHandlers[cmd] = handler;
}

export function __clearInvokeHandlers() {
  Object.keys(invokeHandlers).forEach((k) => delete invokeHandlers[k]);
  invoke.mockClear();
}
