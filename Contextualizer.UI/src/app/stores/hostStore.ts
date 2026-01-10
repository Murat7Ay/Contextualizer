import { create } from 'zustand';
import type { Theme } from './appStore';

export interface HostInfo {
  appVersion?: string;
  theme?: Theme;
  apiBaseUrl?: string;
  mcpSseUrl?: string;
}

interface HostStore {
  webView2Available: boolean;
  hostConnected: boolean;
  hostInfo: HostInfo | null;
  lastPongAt: Date | null;
  lastMessage: unknown;

  setWebView2Available: (available: boolean) => void;
  setHostConnected: (connected: boolean) => void;
  setHostInfo: (info: HostInfo | null) => void;
  setLastPongAt: (date: Date | null) => void;
  setLastMessage: (msg: unknown) => void;
}

export const useHostStore = create<HostStore>((set) => ({
  webView2Available: false,
  hostConnected: false,
  hostInfo: null,
  lastPongAt: null,
  lastMessage: null,

  setWebView2Available: (available) => set({ webView2Available: available }),
  setHostConnected: (connected) => set({ hostConnected: connected }),
  setHostInfo: (hostInfo) => set({ hostInfo }),
  setLastPongAt: (lastPongAt) => set({ lastPongAt }),
  setLastMessage: (lastMessage) => set({ lastMessage }),
}));


