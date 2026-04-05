import { invoke } from "@tauri-apps/api/core";

// ── Error types ─────────────────────────────────────────────────────

export interface AppError {
  kind:
    | "io"
    | "json"
    | "handlerNotFound"
    | "handlerValidation"
    | "config"
    | "plugin"
    | "mcp"
    | "general";
  message: string;
}

// ── Response types ──────────────────────────────────────────────────

export interface HandlerSummary {
  name: string;
  handlerType: string;
  enabled: boolean;
  mcpEnabled: boolean;
  description: string | null;
}

export interface HandlerConfig {
  name: string;
  type: string;
  description?: string;
  enabled?: boolean;
  regex?: string;
  mcpEnabled?: boolean;
  mcpToolName?: string;
  mcpDescription?: string;
  [key: string]: unknown;
}

export interface AppSettings {
  handlersFilePath?: string | null;
  pluginsDirectory?: string | null;
  exchangeDirectory?: string | null;
  keyboardShortcut?: KeyboardShortcut | null;
  clipboardWaitTimeout?: number | null;
  windowActivationDelay?: number | null;
  clipboardClearDelay?: number | null;
  configSystem?: ConfigSystemSettings | null;
  uiSettings?: UiSettings | null;
  loggingSettings?: LoggingSettings | null;
  mcpSettings?: McpSettings | null;
  aiSkillsHub?: AiSkillsHubSettings | null;
}

export interface KeyboardShortcut {
  modifierKeys: string[];
  key: string;
}

export interface ConfigSystemSettings {
  enabled: boolean;
  configFilePath?: string | null;
  secretsFilePath?: string | null;
  autoCreateFiles: boolean;
  fileFormat?: string | null;
}

export interface UiSettings {
  toastPositionX?: number | null;
  toastPositionY?: number | null;
  theme?: string | null;
  skippedUpdateVersion?: string | null;
  lastUpdateCheck?: string | null;
  networkUpdateSettings?: NetworkUpdateSettings | null;
  initialDeploymentSettings?: InitialDeploymentSettings | null;
}

export interface NetworkUpdateSettings {
  enableNetworkUpdates: boolean;
  networkUpdatePath?: string | null;
  updateScriptPath?: string | null;
  checkIntervalHours?: number | null;
  autoInstallNonMandatory: boolean;
  autoInstallMandatory: boolean;
}

export interface InitialDeploymentSettings {
  enabled: boolean;
  sourcePath?: string | null;
  isCompleted: boolean;
  copyExchangeHandlers: boolean;
  copyInstalledHandlers: boolean;
  copyPlugins: boolean;
}

export interface LoggingSettings {
  enableLocalLogging: boolean;
  enableUsageTracking: boolean;
  localLogPath?: string | null;
  usageEndpointUrl?: string | null;
  minimumLogLevel?: string | null;
  maxLogFileSizeMB?: number | null;
  maxLogFileCount?: number | null;
  enableDebugMode: boolean;
}

export interface McpSettings {
  enabled: boolean;
  port?: number | null;
  useNativeUi: boolean;
  managementToolsEnabled: boolean;
}

export interface AiSkillsHubSettings {
  sources: AiSkillsSource[];
  cursorSkillsPath?: string | null;
  copilotSkillsPath?: string | null;
}

export interface AiSkillsSource {
  id: string;
  path: string;
  label?: string | null;
}

export interface PongResponse {
  pong: boolean;
  timestamp: number;
}

export interface DispatchResult {
  handlerName: string;
  status: string;
  output: string | null;
}

export interface ToolSummary {
  name: string;
  description: string;
}

export interface PluginSummary {
  name: string;
  source: string;
  typeName: string;
}

// ── Commands ────────────────────────────────────────────────────────

export function greet(name: string): Promise<string> {
  return invoke<string>("greet", { name });
}

export function ping(): Promise<PongResponse> {
  return invoke<PongResponse>("ping");
}

export function handlersList(): Promise<HandlerSummary[]> {
  return invoke<HandlerSummary[]>("handlers_list");
}

export function handlerGet(name: string): Promise<HandlerConfig> {
  return invoke<HandlerConfig>("handler_get", { name });
}

export function handlerCreate(config: HandlerConfig): Promise<HandlerSummary> {
  return invoke<HandlerSummary>("handler_create", { config });
}

export function handlerDelete(name: string): Promise<HandlerSummary> {
  return invoke<HandlerSummary>("handler_delete", { name });
}

export function handlerToggle(name: string, enabled: boolean): Promise<boolean> {
  return invoke<boolean>("handler_toggle", { name, enabled });
}

export function handlerReload(): Promise<number> {
  return invoke<number>("handler_reload");
}

export function getAppSettings(): Promise<AppSettings> {
  return invoke<AppSettings>("get_app_settings");
}

export function saveAppSettings(settings: Partial<AppSettings>): Promise<boolean> {
  return invoke<boolean>("save_app_settings", { settings });
}

export function dispatchClipboard(input: string): Promise<DispatchResult> {
  return invoke<DispatchResult>("dispatch_clipboard", { input });
}

export function mcpToolsList(): Promise<ToolSummary[]> {
  return invoke<ToolSummary[]>("mcp_tools_list");
}

export function pluginList(): Promise<PluginSummary[]> {
  return invoke<PluginSummary[]>("plugin_list");
}

export function configGet(key: string): Promise<string | null> {
  return invoke<string | null>("config_get", { key });
}

export function configSet(key: string, value: string): Promise<boolean> {
  return invoke<boolean>("config_set", { key, value });
}

// ── Cron ──────────────────────────────────────────────────────────

export interface CronJobSummary {
  jobId: string;
  handlerName: string;
  cronExpression: string;
  enabled: boolean;
}

export interface CronListResponse {
  isRunning: boolean;
  jobs: CronJobSummary[];
}

export function cronList(): Promise<CronListResponse> {
  return invoke<CronListResponse>("cron_list");
}

export function cronSetEnabled(jobId: string, enabled: boolean): Promise<boolean> {
  return invoke<boolean>("cron_set_enabled", { jobId, enabled });
}

export function cronTrigger(jobId: string): Promise<string> {
  return invoke<string>("cron_trigger", { jobId });
}

export function cronUpdate(jobId: string, cronExpression: string): Promise<boolean> {
  return invoke<boolean>("cron_update", { jobId, cronExpression });
}

// ── Handler update / MCP / manual ─────────────────────────────────

export function handlerUpdate(
  handlerName: string,
  updates: Record<string, unknown>,
): Promise<boolean> {
  return invoke<boolean>("handler_update", { handlerName, updates });
}

export function handlerSetMcp(name: string, mcpEnabled: boolean): Promise<boolean> {
  return invoke<boolean>("handler_set_mcp", { name, mcpEnabled });
}

export function manualHandlerExecute(name: string): Promise<DispatchResult> {
  return invoke<DispatchResult>("manual_handler_execute", { name });
}

// ── UI dialogs ────────────────────────────────────────────────────

export interface ConfirmResult {
  confirmed: boolean;
}

export function uiConfirm(title: string, message: string): Promise<ConfirmResult> {
  return invoke<ConfirmResult>("ui_confirm", { title, message });
}

export function uiNotify(message: string, title?: string): Promise<boolean> {
  return invoke<boolean>("ui_notify", { message, title });
}

export function openExternal(url: string): Promise<boolean> {
  return invoke<boolean>("open_external", { url });
}

// ── Theme ─────────────────────────────────────────────────────────

export function setTheme(theme: string): Promise<boolean> {
  return invoke<boolean>("set_theme", { theme });
}

// ── Exchange ──────────────────────────────────────────────────────

export interface ExchangePackage {
  handlerId: string;
  name: string;
  description: string | null;
  version: string | null;
  tags: string[];
}

export function exchangeList(
  searchTerm?: string,
  tags?: string[],
): Promise<ExchangePackage[]> {
  return invoke<ExchangePackage[]>("exchange_list", { searchTerm, tags });
}

export function exchangeInstall(handlerId: string): Promise<boolean> {
  return invoke<boolean>("exchange_install", { handlerId });
}

export function exchangeRemove(handlerId: string): Promise<boolean> {
  return invoke<boolean>("exchange_remove", { handlerId });
}

// ── Log / Toast / Tab ─────────────────────────────────────────────

export function emitLog(
  level: string,
  message: string,
  details?: string,
): Promise<boolean> {
  return invoke<boolean>("emit_log", { level, message, details });
}

export function emitToast(
  level: string,
  message: string,
  title?: string,
  durationSeconds?: number,
): Promise<boolean> {
  return invoke<boolean>("emit_toast", { level, message, title, durationSeconds });
}

export function emitOpenTab(
  screenId: string,
  title: string,
  context: Record<string, unknown>,
  autoFocus?: boolean,
  bringToFront?: boolean,
): Promise<boolean> {
  return invoke<boolean>("emit_open_tab", { screenId, title, context, autoFocus, bringToFront });
}

// ── File / Folder dialogs ─────────────────────────────────────────

export interface FileDialogResult {
  cancelled: boolean;
  path: string | null;
  paths: string[] | null;
}

export function openFileDialog(
  title?: string,
  filters?: string[],
  multiple?: boolean,
): Promise<FileDialogResult> {
  return invoke<FileDialogResult>("open_file_dialog", { title, filters, multiple });
}

export function openFolderDialog(title?: string): Promise<FileDialogResult> {
  return invoke<FileDialogResult>("open_folder_dialog", { title });
}

// ── Prompt system ─────────────────────────────────────────────────

export interface UserInputResponse {
  requestId: string;
  cancelled: boolean;
  value: string | null;
  selectedValues: string[] | null;
}

export interface NavigationInputResponse {
  requestId: string;
  action: string;
  value: string | null;
  selectedValues: string[] | null;
}

export function requestConfirm(
  requestId: string,
  title: string | undefined,
  message: string,
  details?: unknown,
): Promise<boolean> {
  return invoke<boolean>("request_confirm", { requestId, title, message, details });
}

export function submitConfirmResponse(requestId: string, confirmed: boolean): Promise<boolean> {
  return invoke<boolean>("submit_confirm_response", { requestId, confirmed });
}

export function requestUserInput(
  requestId: string,
  request: unknown,
  context?: unknown,
): Promise<UserInputResponse> {
  return invoke<UserInputResponse>("request_user_input", { requestId, request, context });
}

export function submitUserInputResponse(response: UserInputResponse): Promise<boolean> {
  return invoke<boolean>("submit_user_input_response", { response });
}

export function requestUserInputNavigation(
  requestId: string,
  request: unknown,
  context: unknown,
  canGoBack: boolean,
  currentStep: number,
  totalSteps: number,
): Promise<NavigationInputResponse> {
  return invoke<NavigationInputResponse>("request_user_input_navigation", {
    requestId, request, context, canGoBack, currentStep, totalSteps,
  });
}

export function submitNavigationResponse(response: NavigationInputResponse): Promise<boolean> {
  return invoke<boolean>("submit_navigation_response", { response });
}

// ── Tab/Toast lifecycle ───────────────────────────────────────────

export function tabActionExecute(
  tabId: string,
  actionId: string,
  context?: Record<string, unknown>,
): Promise<boolean> {
  return invoke<boolean>("tab_action_execute", { tabId, actionId, context });
}

export function tabClosed(tabId: string): Promise<boolean> {
  return invoke<boolean>("tab_closed", { tabId });
}

export function toastActionExecute(toastId: string, actionId: string): Promise<boolean> {
  return invoke<boolean>("toast_action_execute", { toastId, actionId });
}

export function toastClosed(toastId: string): Promise<boolean> {
  return invoke<boolean>("toast_closed", { toastId });
}

// ── Logging / Usage ───────────────────────────────────────────────

export function loggingTest(): Promise<boolean> {
  return invoke<boolean>("logging_test");
}

export function usageTest(): Promise<boolean> {
  return invoke<boolean>("usage_test");
}

export interface LogClearResult {
  deletedCount: number;
}

export function logClear(path?: string): Promise<LogClearResult> {
  return invoke<LogClearResult>("log_clear", { path });
}

// ── Exchange full ─────────────────────────────────────────────────

export function exchangeTags(): Promise<string[]> {
  return invoke<string[]>("exchange_tags");
}

export function exchangeDetails(handlerId: string): Promise<ExchangePackage | null> {
  return invoke<ExchangePackage | null>("exchange_details", { handlerId });
}

export function exchangeUpdate(handlerId: string): Promise<boolean> {
  return invoke<boolean>("exchange_update", { handlerId });
}

export function exchangePublish(pkg: ExchangePackage): Promise<boolean> {
  return invoke<boolean>("exchange_publish", { package: pkg });
}

// ── AI Skills Hub ─────────────────────────────────────────────────

export interface AiSkillsHubListResult {
  cursorSkillsRoot: string | null;
  copilotSkillsRoot: string | null;
  sources: AiSkillsSource[];
  skills: AiSkillRow[];
  globalOnlySkills: GlobalOnlySkill[];
}

export interface AiSkillRow {
  name: string;
  sourceId: string;
  sourcePath: string;
  cursorState: string;
  copilotState: string;
}

export interface GlobalOnlySkill {
  name: string;
  target: string;
  path: string;
}

export interface AiSkillDeployment {
  skillName: string;
  sourceId: string;
  targets: string[];
}

export interface AiSkillOpResult {
  ok: boolean;
  results: { skillName: string; ok: boolean; error?: string }[];
}

export function aiSkillsHubList(): Promise<AiSkillsHubListResult> {
  return invoke<AiSkillsHubListResult>("ai_skills_hub_list");
}

export function aiSkillsHubDeploy(
  deployments: AiSkillDeployment[],
  customDestinationRoot?: string,
): Promise<AiSkillOpResult> {
  return invoke<AiSkillOpResult>("ai_skills_hub_deploy", { deployments, customDestinationRoot });
}

export function aiSkillsHubRemove(
  skillNames: string[],
  targets: string[],
): Promise<AiSkillOpResult> {
  return invoke<AiSkillOpResult>("ai_skills_hub_remove", { skillNames, targets });
}

export function aiSkillsHubPull(
  skillNames: string[],
  fromTarget: string,
  toSourceId: string,
): Promise<AiSkillOpResult> {
  return invoke<AiSkillOpResult>("ai_skills_hub_pull", { skillNames, fromTarget, toSourceId });
}

// ── Plugin list (WPF shape) ───────────────────────────────────────

export interface PluginsListResponse {
  handlerTypes: string[];
  actions: string[];
  validators: string[];
  contextProviders: string[];
}

export function pluginsListFull(): Promise<PluginsListResponse> {
  return invoke<PluginsListResponse>("plugins_list_full");
}
