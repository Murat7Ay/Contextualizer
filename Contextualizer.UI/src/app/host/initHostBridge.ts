import { useActivityLogStore } from '../stores/activityLogStore';
import { useAppStore } from '../stores/appStore';
import { useHostStore } from '../stores/hostStore';
import { addWebView2MessageListener, isWebView2Available, postWebView2Message } from './webview2Bridge';

type HostMessage =
  | { type: 'host_ready'; appVersion?: string; theme?: 'light' | 'dark'; apiBaseUrl?: string; mcpSseUrl?: string }
  | { type: 'theme_changed'; theme: 'light' | 'dark' }
  | { type: 'pong'; id?: string }
  | { type: 'log'; level?: 'success' | 'error' | 'warning' | 'info' | 'debug' | 'critical'; message?: string; details?: string }
  | { type: string; [k: string]: unknown };

let initialized = false;

export function initHostBridge(): () => void {
  if (initialized) return () => {};
  initialized = true;

  const setWebView2Available = useHostStore.getState().setWebView2Available;
  const setHostConnected = useHostStore.getState().setHostConnected;
  const setHostInfo = useHostStore.getState().setHostInfo;
  const setLastMessage = useHostStore.getState().setLastMessage;
  const setLastPongAt = useHostStore.getState().setLastPongAt;

  const available = isWebView2Available();
  setWebView2Available(available);

  if (!available) {
    setHostConnected(false);
    setHostInfo(null);
    return () => {
      initialized = false;
    };
  }

  const addLog = useActivityLogStore.getState().addLog;

  const unsubscribe = addWebView2MessageListener((payload) => {
    setLastMessage(payload);

    if (!payload || typeof payload !== 'object') return;
    const msg = payload as HostMessage;

    if (msg.type === 'host_ready') {
      setHostConnected(true);
      setHostInfo({
        appVersion: msg.appVersion,
        theme: msg.theme,
        apiBaseUrl: msg.apiBaseUrl,
        mcpSseUrl: msg.mcpSseUrl,
      });

      if (msg.theme) {
        useAppStore.getState().setTheme(msg.theme);
      }

      addLog('success', 'Connected to WPF host');
      return;
    }

    if (msg.type === 'theme_changed') {
      useAppStore.getState().setTheme(msg.theme);
      return;
    }

    if (msg.type === 'pong') {
      setLastPongAt(new Date());
      addLog('debug', 'Pong received from host');
      return;
    }

    if (msg.type === 'log' && msg.message) {
      addLog(msg.level ?? 'info', msg.message, msg.details);
      return;
    }
  });

  // Basic handshake
  postWebView2Message({
    type: 'ui_ready',
    protocolVersion: 1,
    ts: Date.now(),
  });

  return () => {
    try {
      unsubscribe();
    } finally {
      initialized = false;
    }
  };
}

export function sendPing(): void {
  postWebView2Message({ type: 'ping', id: `${Date.now()}`, ts: Date.now() });
}


