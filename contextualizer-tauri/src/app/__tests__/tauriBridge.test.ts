import { describe, test, expect, beforeEach } from 'vitest';
import { invoke, __mockInvokeHandler, __clearInvokeHandlers } from '../../__mocks__/tauri-api-core';
import { listen, __emitTestEvent, __clearListeners } from '../../__mocks__/tauri-api-event';
import {
  requestHandlersList,
  getAppSettings,
  saveAppSettings,
  ping,
  greet,
  onHostMessage,
} from '../host/tauriBridge';

beforeEach(() => {
  __clearInvokeHandlers();
  __clearListeners();
});

describe('tauriBridge', () => {
  test('requestHandlersList invokes handlers_list command', async () => {
    __mockInvokeHandler('handlers_list', () => [{ name: 'test' }]);
    const result = await requestHandlersList();
    expect(invoke).toHaveBeenCalledWith('handlers_list');
    expect(result).toEqual([{ name: 'test' }]);
  });

  test('getAppSettings invokes get_app_settings command', async () => {
    __mockInvokeHandler('get_app_settings', () => ({ theme: 'dark' }));
    const result = await getAppSettings();
    expect(invoke).toHaveBeenCalledWith('get_app_settings');
    expect(result).toEqual({ theme: 'dark' });
  });

  test('saveAppSettings invokes save_app_settings with settings', async () => {
    __mockInvokeHandler('save_app_settings', () => true);
    const result = await saveAppSettings({ theme: 'light' });
    expect(invoke).toHaveBeenCalledWith('save_app_settings', { settings: { theme: 'light' } });
    expect(result).toBe(true);
  });

  test('ping invokes ping command and returns pong', async () => {
    __mockInvokeHandler('ping', () => ({ pong: true, timestamp: 1234567890 }));
    const result = await ping();
    expect(invoke).toHaveBeenCalledWith('ping');
    expect(result.pong).toBe(true);
    expect(result.timestamp).toBe(1234567890);
  });

  test('greet invokes greet command with name', async () => {
    __mockInvokeHandler('greet', (args: Record<string, unknown>) => `Hello, ${args.name}!`);
    const result = await greet('World');
    expect(invoke).toHaveBeenCalledWith('greet', { name: 'World' });
    expect(result).toBe('Hello, World!');
  });

  test('onHostMessage registers event listener', async () => {
    const messages: unknown[] = [];
    const unlisten = await onHostMessage((msg) => messages.push(msg));

    expect(listen).toHaveBeenCalledWith('host-message', expect.any(Function));

    __emitTestEvent('host-message', { type: 'test', data: 42 });
    expect(messages).toHaveLength(1);
    expect(messages[0]).toEqual({ type: 'test', data: 42 });

    __emitTestEvent('host-message', { type: 'another' });
    expect(messages).toHaveLength(2);

    unlisten();
    __emitTestEvent('host-message', { type: 'after-unlisten' });
    expect(messages).toHaveLength(2);
  });
});
