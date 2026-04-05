import { invoke } from '@tauri-apps/api/core';
import { listen, emit, type UnlistenFn } from '@tauri-apps/api/event';

export type HostMessageCallback = (payload: unknown) => void;

export async function requestHandlersList(): Promise<unknown[]> {
  return await invoke('handlers_list');
}

export async function getAppSettings(): Promise<unknown> {
  return await invoke('get_app_settings');
}

export async function saveAppSettings(settings: unknown): Promise<boolean> {
  return await invoke('save_app_settings', { settings });
}

export async function ping(): Promise<{ pong: boolean; timestamp: number }> {
  return await invoke('ping');
}

export async function greet(name: string): Promise<string> {
  return await invoke('greet', { name });
}

export function onHostMessage(callback: HostMessageCallback): Promise<UnlistenFn> {
  return listen('host-message', (event) => callback(event.payload));
}

export async function emitToHost(eventName: string, payload?: unknown): Promise<void> {
  await emit(eventName, payload);
}
