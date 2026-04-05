import { describe, test, expect, beforeEach, afterEach, vi } from 'vitest';
import { createBridge, detectBridgeType } from '../host/bridgeAdapter';

describe('detectBridgeType', () => {
  const originalWindow = { ...window };

  afterEach(() => {
    delete (window as Record<string, unknown>).__TAURI__;
    delete (window as Record<string, unknown>).chrome;
  });

  test('returns tauri when __TAURI__ is present', () => {
    (window as Record<string, unknown>).__TAURI__ = {};
    expect(detectBridgeType()).toBe('tauri');
  });

  test('returns webview2 when chrome.webview is present', () => {
    delete (window as Record<string, unknown>).__TAURI__;
    (window as Record<string, unknown>).chrome = {
      webview: {
        postMessage: () => {},
        addEventListener: () => {},
        removeEventListener: () => {},
      },
    };
    expect(detectBridgeType()).toBe('webview2');
  });

  test('returns none when no bridge environment is found', () => {
    delete (window as Record<string, unknown>).__TAURI__;
    delete (window as Record<string, unknown>).chrome;
    expect(detectBridgeType()).toBe('none');
  });

  test('tauri takes priority over webview2', () => {
    (window as Record<string, unknown>).__TAURI__ = {};
    (window as Record<string, unknown>).chrome = {
      webview: {
        postMessage: () => {},
        addEventListener: () => {},
        removeEventListener: () => {},
      },
    };
    expect(detectBridgeType()).toBe('tauri');
  });
});

describe('Tauri bridge methods', () => {
  beforeEach(() => {
    (window as Record<string, unknown>).__TAURI__ = {};
  });

  afterEach(() => {
    delete (window as Record<string, unknown>).__TAURI__;
  });

  test('requestHandlersList calls tauriBridge and returns result', async () => {
    const { invoke } = await import('@tauri-apps/api/core');
    vi.mocked(invoke).mockResolvedValue([]);
    const bridge = createBridge();
    const result = await bridge.requestHandlersList();
    expect(Array.isArray(result)).toBe(true);
  });

  test('getAppSettings calls tauriBridge and returns result', async () => {
    const { invoke } = await import('@tauri-apps/api/core');
    vi.mocked(invoke).mockResolvedValue({ shortcut: 'Ctrl+C' });
    const bridge = createBridge();
    const result = await bridge.getAppSettings();
    expect(result).toEqual({ shortcut: 'Ctrl+C' });
  });

  test('saveAppSettings calls tauriBridge and returns boolean', async () => {
    const { invoke } = await import('@tauri-apps/api/core');
    vi.mocked(invoke).mockResolvedValue(true);
    const bridge = createBridge();
    const result = await bridge.saveAppSettings({ shortcut: 'Ctrl+C' });
    expect(result).toBe(true);
  });

  test('ping calls tauriBridge and returns pong', async () => {
    const { invoke } = await import('@tauri-apps/api/core');
    vi.mocked(invoke).mockResolvedValue({ pong: true, timestamp: 123 });
    const bridge = createBridge();
    const result = await bridge.ping();
    expect(result).toHaveProperty('pong');
  });

  test('onHostMessage delegates to tauriBridge module', async () => {
    const { listen } = await import('@tauri-apps/api/event');
    vi.mocked(listen).mockResolvedValue(vi.fn());
    const bridge = createBridge();
    const callback = vi.fn();
    const unlisten = await bridge.onHostMessage(callback);
    expect(unlisten).toBeDefined();
  });
});

describe('createBridge', () => {
  afterEach(() => {
    delete (window as Record<string, unknown>).__TAURI__;
    delete (window as Record<string, unknown>).chrome;
  });

  test('Tauri environment returns bridge with type tauri', () => {
    (window as Record<string, unknown>).__TAURI__ = {};
    const bridge = createBridge();
    expect(bridge.type).toBe('tauri');
  });

  test('WebView2 environment returns bridge with type webview2', () => {
    delete (window as Record<string, unknown>).__TAURI__;
    (window as Record<string, unknown>).chrome = {
      webview: {
        postMessage: () => {},
        addEventListener: () => {},
        removeEventListener: () => {},
      },
    };
    const bridge = createBridge();
    expect(bridge.type).toBe('webview2');
  });

  test('No environment throws error', () => {
    delete (window as Record<string, unknown>).__TAURI__;
    delete (window as Record<string, unknown>).chrome;
    expect(() => createBridge()).toThrow('No bridge available');
  });
});

describe('WebView2 bridge methods', () => {
  let postMessageSpy: ReturnType<typeof vi.fn>;
  let addEventSpy: ReturnType<typeof vi.fn>;
  let removeEventSpy: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    delete (window as Record<string, unknown>).__TAURI__;
    postMessageSpy = vi.fn();
    addEventSpy = vi.fn();
    removeEventSpy = vi.fn();
    (window as Record<string, unknown>).chrome = {
      webview: {
        postMessage: postMessageSpy,
        addEventListener: addEventSpy,
        removeEventListener: removeEventSpy,
      },
    };
  });

  afterEach(() => {
    delete (window as Record<string, unknown>).chrome;
  });

  test('requestHandlersList sends message and returns empty array', async () => {
    const bridge = createBridge();
    const result = await bridge.requestHandlersList();
    expect(postMessageSpy).toHaveBeenCalledWith(
      JSON.stringify({ type: 'handlers_list_request' }),
    );
    expect(result).toEqual([]);
  });

  test('getAppSettings sends message and returns empty object', async () => {
    const bridge = createBridge();
    const result = await bridge.getAppSettings();
    expect(postMessageSpy).toHaveBeenCalledWith(
      JSON.stringify({ type: 'get_app_settings' }),
    );
    expect(result).toEqual({});
  });

  test('saveAppSettings sends message and returns true', async () => {
    const bridge = createBridge();
    const result = await bridge.saveAppSettings({ shortcut: 'Ctrl+C' });
    expect(postMessageSpy).toHaveBeenCalledWith(
      JSON.stringify({ type: 'save_app_settings', settings: { shortcut: 'Ctrl+C' } }),
    );
    expect(result).toBe(true);
  });

  test('ping sends message and returns pong', async () => {
    const bridge = createBridge();
    const result = await bridge.ping();
    expect(postMessageSpy).toHaveBeenCalledWith(
      JSON.stringify({ type: 'ping' }),
    );
    expect(result.pong).toBe(true);
    expect(result.timestamp).toBeGreaterThan(0);
  });

  test('onHostMessage registers and unregisters listener', () => {
    const bridge = createBridge();
    const callback = vi.fn();
    const unlisten = bridge.onHostMessage(callback);
    expect(addEventSpy).toHaveBeenCalledWith('message', expect.any(Function));

    if (typeof unlisten === 'function') {
      unlisten();
      expect(removeEventSpy).toHaveBeenCalled();
    }
  });

  test('onHostMessage listener parses JSON string data', () => {
    const bridge = createBridge();
    const callback = vi.fn();
    bridge.onHostMessage(callback);

    const listener = addEventSpy.mock.calls[0][1];
    listener({ data: '{"type":"test"}' });
    expect(callback).toHaveBeenCalledWith({ type: 'test' });
  });

  test('onHostMessage listener passes non-JSON data as-is', () => {
    const bridge = createBridge();
    const callback = vi.fn();
    bridge.onHostMessage(callback);

    const listener = addEventSpy.mock.calls[0][1];
    listener({ data: 'plain string' });
    expect(callback).toHaveBeenCalledWith('plain string');
  });

  test('onHostMessage listener passes object data directly', () => {
    const bridge = createBridge();
    const callback = vi.fn();
    bridge.onHostMessage(callback);

    const listener = addEventSpy.mock.calls[0][1];
    const objData = { type: 'object_data' };
    listener({ data: objData });
    expect(callback).toHaveBeenCalledWith(objData);
  });
});
