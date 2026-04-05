import type { HostMessageCallback } from './tauriBridge';

declare global {
  interface Window {
    __TAURI__?: Record<string, unknown>;
    chrome?: {
      webview?: {
        postMessage: (message: unknown) => void;
        addEventListener: (type: 'message', listener: (event: { data: unknown }) => void) => void;
        removeEventListener: (type: 'message', listener: (event: { data: unknown }) => void) => void;
      };
    };
  }
}

export type BridgeType = 'tauri' | 'webview2' | 'none';

export interface Bridge {
  type: BridgeType;
  requestHandlersList(): Promise<unknown[]>;
  getAppSettings(): Promise<unknown>;
  saveAppSettings(settings: unknown): Promise<boolean>;
  ping(): Promise<{ pong: boolean; timestamp: number }>;
  onHostMessage(callback: HostMessageCallback): Promise<() => void> | (() => void);
}

function createTauriBridge(): Bridge {
  return {
    type: 'tauri',
    async requestHandlersList() {
      const mod = await import('./tauriBridge');
      return mod.requestHandlersList();
    },
    async getAppSettings() {
      const mod = await import('./tauriBridge');
      return mod.getAppSettings();
    },
    async saveAppSettings(settings) {
      const mod = await import('./tauriBridge');
      return mod.saveAppSettings(settings);
    },
    async ping() {
      const mod = await import('./tauriBridge');
      return mod.ping();
    },
    async onHostMessage(callback) {
      const mod = await import('./tauriBridge');
      return mod.onHostMessage(callback);
    },
  };
}

function createWebView2Bridge(): Bridge {
  const wv = window.chrome!.webview!;
  return {
    type: 'webview2',
    async requestHandlersList() {
      wv.postMessage(JSON.stringify({ type: 'handlers_list_request' }));
      return [];
    },
    async getAppSettings() {
      wv.postMessage(JSON.stringify({ type: 'get_app_settings' }));
      return {};
    },
    async saveAppSettings(settings) {
      wv.postMessage(JSON.stringify({ type: 'save_app_settings', settings }));
      return true;
    },
    async ping() {
      wv.postMessage(JSON.stringify({ type: 'ping' }));
      return { pong: true, timestamp: Date.now() };
    },
    onHostMessage(callback: HostMessageCallback) {
      const listener = (event: { data: unknown }) => {
        let payload = event.data;
        if (typeof payload === 'string') {
          try { payload = JSON.parse(payload); } catch { /* use raw string */ }
        }
        callback(payload);
      };
      wv.addEventListener('message', listener);
      return () => wv.removeEventListener('message', listener);
    },
  };
}

export function detectBridgeType(): BridgeType {
  if (typeof window !== 'undefined' && '__TAURI__' in window) return 'tauri';
  if (typeof window !== 'undefined' && window.chrome?.webview) return 'webview2';
  return 'none';
}

export function createBridge(): Bridge {
  const bridgeType = detectBridgeType();
  switch (bridgeType) {
    case 'tauri':
      return createTauriBridge();
    case 'webview2':
      return createWebView2Bridge();
    default:
      throw new Error('No bridge available');
  }
}
