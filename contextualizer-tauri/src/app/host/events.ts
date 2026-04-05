import { listen, type UnlistenFn } from "@tauri-apps/api/event";

export interface HandlerProcessedPayload {
  handlerName: string;
  handlerType: string;
  status: string;
  output: string | null;
  timestamp: number;
}

export interface ClipboardCapturedPayload {
  contentType: string;
  contentLength: number;
  preview: string;
  timestamp: number;
}

export interface HandlersReloadedPayload {
  count: number;
  timestamp: number;
}

export interface McpRequestPayload {
  method: string;
  toolName: string | null;
  timestamp: number;
}

export interface SettingsChangedPayload {
  changedKeys: string[];
  timestamp: number;
}

export interface CronTriggeredPayload {
  jobId: string;
  handlerName: string;
  timestamp: number;
}

export interface ThemeChangedPayload {
  theme: string;
}

export interface LogPayload {
  level: "success" | "error" | "warning" | "info" | "debug" | "critical";
  message: string;
  details?: string;
}

export interface ToastPayload {
  toastId?: string;
  level: string;
  title?: string;
  message: string;
  details?: string;
  durationSeconds: number;
  actions?: { id: string; label: string }[];
}

export interface OpenTabPayload {
  screenId: string;
  title: string;
  context: Record<string, unknown>;
  actions?: { id: string; label: string }[];
  autoFocus: boolean;
  bringToFront: boolean;
}

export interface HostReadyPayload {
  appVersion: string;
  theme: string;
  mcpUrl?: string;
  protocolVersion: number;
}

export interface UiConfirmRequestPayload {
  requestId: string;
  title?: string;
  message: string;
  details?: unknown;
}

export interface UiUserInputRequestPayload {
  requestId: string;
  request: unknown;
  context?: unknown;
}

export interface UiUserInputNavRequestPayload {
  requestId: string;
  request: unknown;
  context: unknown;
  canGoBack: boolean;
  currentStep: number;
  totalSteps: number;
}

export const TauriEvents = {
  HANDLER_PROCESSED: "handler-processed",
  CLIPBOARD_CAPTURED: "clipboard-captured",
  HANDLERS_RELOADED: "handlers-reloaded",
  MCP_REQUEST: "mcp-request",
  SETTINGS_CHANGED: "settings-changed",
  CRON_TRIGGERED: "cron-triggered",
  THEME_CHANGED: "theme_changed",
  LOG: "log",
  TOAST: "toast",
  OPEN_TAB: "open_tab",
  HOST_READY: "host_ready",
  UI_CONFIRM_REQUEST: "ui_confirm_request",
  UI_USER_INPUT_REQUEST: "ui_user_input_request",
  UI_USER_INPUT_NAV_REQUEST: "ui_user_input_navigation_request",
} as const;

export function onHandlerProcessed(
  callback: (payload: HandlerProcessedPayload) => void,
): Promise<UnlistenFn> {
  return listen<HandlerProcessedPayload>(TauriEvents.HANDLER_PROCESSED, (event) => {
    callback(event.payload);
  });
}

export function onClipboardCaptured(
  callback: (payload: ClipboardCapturedPayload) => void,
): Promise<UnlistenFn> {
  return listen<ClipboardCapturedPayload>(TauriEvents.CLIPBOARD_CAPTURED, (event) => {
    callback(event.payload);
  });
}

export function onHandlersReloaded(
  callback: (payload: HandlersReloadedPayload) => void,
): Promise<UnlistenFn> {
  return listen<HandlersReloadedPayload>(TauriEvents.HANDLERS_RELOADED, (event) => {
    callback(event.payload);
  });
}

export function onMcpRequest(
  callback: (payload: McpRequestPayload) => void,
): Promise<UnlistenFn> {
  return listen<McpRequestPayload>(TauriEvents.MCP_REQUEST, (event) => {
    callback(event.payload);
  });
}

export function onSettingsChanged(
  callback: (payload: SettingsChangedPayload) => void,
): Promise<UnlistenFn> {
  return listen<SettingsChangedPayload>(TauriEvents.SETTINGS_CHANGED, (event) => {
    callback(event.payload);
  });
}

export function onCronTriggered(
  callback: (payload: CronTriggeredPayload) => void,
): Promise<UnlistenFn> {
  return listen<CronTriggeredPayload>(TauriEvents.CRON_TRIGGERED, (event) => {
    callback(event.payload);
  });
}

export function onThemeChanged(
  callback: (payload: ThemeChangedPayload) => void,
): Promise<UnlistenFn> {
  return listen<ThemeChangedPayload>(TauriEvents.THEME_CHANGED, (event) => {
    callback(event.payload);
  });
}

export function onLog(
  callback: (payload: LogPayload) => void,
): Promise<UnlistenFn> {
  return listen<LogPayload>(TauriEvents.LOG, (event) => {
    callback(event.payload);
  });
}

export function onToast(
  callback: (payload: ToastPayload) => void,
): Promise<UnlistenFn> {
  return listen<ToastPayload>(TauriEvents.TOAST, (event) => {
    callback(event.payload);
  });
}

export function onOpenTab(
  callback: (payload: OpenTabPayload) => void,
): Promise<UnlistenFn> {
  return listen<OpenTabPayload>(TauriEvents.OPEN_TAB, (event) => {
    callback(event.payload);
  });
}

export function onHostReady(
  callback: (payload: HostReadyPayload) => void,
): Promise<UnlistenFn> {
  return listen<HostReadyPayload>(TauriEvents.HOST_READY, (event) => {
    callback(event.payload);
  });
}

export function onUiConfirmRequest(
  callback: (payload: UiConfirmRequestPayload) => void,
): Promise<UnlistenFn> {
  return listen<UiConfirmRequestPayload>(TauriEvents.UI_CONFIRM_REQUEST, (event) => {
    callback(event.payload);
  });
}

export function onUiUserInputRequest(
  callback: (payload: UiUserInputRequestPayload) => void,
): Promise<UnlistenFn> {
  return listen<UiUserInputRequestPayload>(TauriEvents.UI_USER_INPUT_REQUEST, (event) => {
    callback(event.payload);
  });
}

export function onUiUserInputNavRequest(
  callback: (payload: UiUserInputNavRequestPayload) => void,
): Promise<UnlistenFn> {
  return listen<UiUserInputNavRequestPayload>(TauriEvents.UI_USER_INPUT_NAV_REQUEST, (event) => {
    callback(event.payload);
  });
}
