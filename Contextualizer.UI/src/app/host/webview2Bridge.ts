type WebView2MessageEvent = { data: unknown };

type WebView2Bridge = {
  postMessage: (message: unknown) => void;
  addEventListener: (type: 'message', listener: (event: WebView2MessageEvent) => void) => void;
  removeEventListener: (type: 'message', listener: (event: WebView2MessageEvent) => void) => void;
};

declare global {
  interface Window {
    chrome?: {
      webview?: WebView2Bridge;
    };
  }
}

export function isWebView2Available(): boolean {
  return typeof window !== 'undefined' && !!window.chrome?.webview;
}

export function postWebView2Message(payload: unknown): boolean {
  if (!isWebView2Available()) return false;

  // Align with existing WPF screens (e.g., `PlSqlEditor`) that post stringified JSON.
  window.chrome!.webview!.postMessage(JSON.stringify(payload));
  return true;
}

export function openExternalUrl(url: string): boolean {
  if (!url) return false;
  return postWebView2Message({ type: 'open_external', url });
}

export function requestHandlersList(): boolean {
  return postWebView2Message({ type: 'handlers_list_request' });
}

export function setHandlerEnabled(name: string, enabled: boolean): boolean {
  return postWebView2Message({ type: 'handler_set_enabled', name, enabled });
}

export function setHandlerMcpEnabled(name: string, mcpEnabled: boolean): boolean {
  return postWebView2Message({ type: 'handler_set_mcp', name, mcpEnabled });
}

export function reloadHandlers(reloadPlugins = true): boolean {
  return postWebView2Message({ type: 'handlers_reload', reloadPlugins });
}

export function executeManualHandler(name: string): boolean {
  if (!name) return false;
  return postWebView2Message({ type: 'manual_handler_execute', name });
}

export function requestCronList(): boolean {
  return postWebView2Message({ type: 'cron_list_request' });
}

export function setCronJobEnabled(jobId: string, enabled: boolean): boolean {
  return postWebView2Message({ type: 'cron_set_enabled', jobId, enabled });
}

export function triggerCronJob(jobId: string): boolean {
  return postWebView2Message({ type: 'cron_trigger', jobId });
}

export function requestAppSettings(): boolean {
  return postWebView2Message({ type: 'app_settings_request' });
}

export function saveAppSettings(settings: unknown): boolean {
  return postWebView2Message({ type: 'app_settings_save', settings });
}

type OpenFileDialogRequest = {
  title?: string;
  filter?: string;
  multiSelect?: boolean;
};

type OpenFileDialogResult =
  | { cancelled: true; error?: string }
  | { cancelled: false; path?: string; paths?: string[]; error?: string };

type OpenFolderDialogRequest = {
  title?: string;
  initialPath?: string;
};

type OpenFolderDialogResult =
  | { cancelled: true; error?: string }
  | { cancelled: false; path?: string; error?: string };

const pendingFileDialogs = new Map<string, (r: OpenFileDialogResult) => void>();
const pendingFolderDialogs = new Map<string, (r: OpenFolderDialogResult) => void>();
let dialogsListenerAttached = false;

function ensureDialogsListener(): void {
  if (dialogsListenerAttached) return;
  dialogsListenerAttached = true;

  addWebView2MessageListener((payload) => {
    if (!payload || typeof payload !== 'object') return;
    const msg = payload as Record<string, unknown>;
    const type = msg.type;

    if (type === 'ui_open_file_dialog_response') {
      const requestId = typeof msg.requestId === 'string' ? msg.requestId : '';
      const resolve = pendingFileDialogs.get(requestId);
      if (!resolve) return;
      pendingFileDialogs.delete(requestId);

      const cancelled = msg.cancelled === true;
      if (cancelled) {
        resolve({ cancelled: true, error: typeof msg.error === 'string' ? msg.error : undefined });
        return;
      }

      const path = typeof msg.path === 'string' ? msg.path : undefined;
      const paths = Array.isArray(msg.paths) ? (msg.paths as unknown[]).filter((p) => typeof p === 'string') as string[] : undefined;
      resolve({ cancelled: false, path, paths, error: typeof msg.error === 'string' ? msg.error : undefined });
      return;
    }

    if (type === 'ui_open_folder_dialog_response') {
      const requestId = typeof msg.requestId === 'string' ? msg.requestId : '';
      const resolve = pendingFolderDialogs.get(requestId);
      if (!resolve) return;
      pendingFolderDialogs.delete(requestId);

      const cancelled = msg.cancelled === true;
      if (cancelled) {
        resolve({ cancelled: true, error: typeof msg.error === 'string' ? msg.error : undefined });
        return;
      }

      const path = typeof msg.path === 'string' ? msg.path : undefined;
      resolve({ cancelled: false, path, error: typeof msg.error === 'string' ? msg.error : undefined });
    }
  });
}

export function openFileDialog(req: OpenFileDialogRequest): Promise<OpenFileDialogResult> {
  ensureDialogsListener();
  const requestId = `${Date.now()}_${Math.random()}`;
  return new Promise((resolve) => {
    pendingFileDialogs.set(requestId, resolve);
    postWebView2Message({
      type: 'ui_open_file_dialog_request',
      requestId,
      title: req.title,
      filter: req.filter,
      multiSelect: !!req.multiSelect,
    });
  });
}

export function openFolderDialog(req: OpenFolderDialogRequest): Promise<OpenFolderDialogResult> {
  ensureDialogsListener();
  const requestId = `${Date.now()}_${Math.random()}`;
  return new Promise((resolve) => {
    pendingFolderDialogs.set(requestId, resolve);
    postWebView2Message({
      type: 'ui_open_folder_dialog_request',
      requestId,
      title: req.title,
      initialPath: req.initialPath,
    });
  });
}

export function requestLoggingTest(): boolean {
  return postWebView2Message({ type: 'logging_test_request' });
}

export function requestUsageTest(): boolean {
  return postWebView2Message({ type: 'usage_test_request' });
}

export function requestClearLogs(path?: string): boolean {
  return postWebView2Message({ type: 'log_clear_request', path });
}

export function executeTabAction(tabId: string, actionId: string, context?: Record<string, unknown>): boolean {
  return postWebView2Message({ type: 'tab_action_execute', tabId, actionId, context });
}

export function notifyTabClosed(tabId: string): boolean {
  if (!tabId) return false;
  return postWebView2Message({ type: 'tab_closed', tabId });
}

export function executeToastAction(toastId: string, actionId: string): boolean {
  if (!toastId || !actionId) return false;
  return postWebView2Message({ type: 'toast_action_execute', toastId, actionId });
}

export function notifyToastClosed(toastId: string): boolean {
  if (!toastId) return false;
  return postWebView2Message({ type: 'toast_closed', toastId });
}

// ─────────────────────────────────────────────────────────────────────────────
// Handler Exchange / Marketplace
// ─────────────────────────────────────────────────────────────────────────────

export function requestExchangePackages(searchTerm?: string, tags?: string[]): boolean {
  return postWebView2Message({ type: 'exchange_list_request', searchTerm, tags });
}

export function requestExchangeTags(): boolean {
  return postWebView2Message({ type: 'exchange_tags_request' });
}

export function requestExchangePackageDetails(handlerId: string): boolean {
  if (!handlerId) return false;
  return postWebView2Message({ type: 'exchange_details_request', handlerId });
}

export function installExchangePackage(handlerId: string): boolean {
  if (!handlerId) return false;
  return postWebView2Message({ type: 'exchange_install', handlerId });
}

export function updateExchangePackage(handlerId: string): boolean {
  if (!handlerId) return false;
  return postWebView2Message({ type: 'exchange_update', handlerId });
}

export function removeExchangePackage(handlerId: string): boolean {
  if (!handlerId) return false;
  return postWebView2Message({ type: 'exchange_remove', handlerId });
}

export function addWebView2MessageListener(handler: (payload: unknown) => void): () => void {
  if (!isWebView2Available()) return () => {};

  const listener = (event: WebView2MessageEvent) => {
    const { data } = event;
    if (typeof data === 'string') {
      try {
        handler(JSON.parse(data));
        return;
      } catch {
        // Not JSON - fallthrough
      }
    }
    handler(data);
  };

  window.chrome!.webview!.addEventListener('message', listener);
  return () => window.chrome!.webview!.removeEventListener('message', listener);
}


