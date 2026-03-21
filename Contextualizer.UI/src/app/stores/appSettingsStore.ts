import { create } from 'zustand';

export type UiTheme = 'light' | 'dark';
export type LogLevel = 'Debug' | 'Info' | 'Warning' | 'Error' | 'Critical';

export interface AppSettingsDto {
  handlersFilePath: string;
  pluginsDirectory: string;
  exchangeDirectory: string;

  keyboardShortcut: {
    modifierKeys: string[];
    key: string;
  };

  clipboardWaitTimeout: number;
  windowActivationDelay: number;
  clipboardClearDelay: number;

  configSystem: {
    enabled: boolean;
    configFilePath: string;
    secretsFilePath: string;
    autoCreateFiles: boolean;
    fileFormat: string;
  };

  uiSettings: {
    toastPositionX: number;
    toastPositionY: number;
    theme: UiTheme;
    skippedUpdateVersion?: string | null;
    lastUpdateCheck?: string | null;
    networkUpdateSettings: {
      enableNetworkUpdates: boolean;
      networkUpdatePath: string;
      updateScriptPath: string;
      checkIntervalHours: number;
      autoInstallNonMandatory: boolean;
      autoInstallMandatory: boolean;
    };
    initialDeploymentSettings: {
      enabled: boolean;
      sourcePath: string;
      isCompleted: boolean;
      copyExchangeHandlers: boolean;
      copyInstalledHandlers: boolean;
      copyPlugins: boolean;
    };
  };

  loggingSettings: {
    enableLocalLogging: boolean;
    enableUsageTracking: boolean;
    localLogPath: string;
    usageEndpointUrl?: string | null;
    minimumLogLevel: LogLevel;
    maxLogFileSizeMB: number;
    maxLogFileCount: number;
    enableDebugMode: boolean;
  };

  mcpSettings: {
    enabled: boolean;
    port: number;
    useNativeUi: boolean;
    managementToolsEnabled: boolean;
  };

  /** Omitted in older hosts until first save; merged in setFromHost. */
  aiSkillsHub?: {
    sources: { id: string; path: string; label?: string | null }[];
    cursorSkillsPath?: string | null;
    copilotSkillsPath?: string | null;
  };
}

export interface LogClearResult {
  deletedCount: number;
}

interface AppSettingsStore {
  loaded: boolean;
  settings: AppSettingsDto | null;
  draft: AppSettingsDto | null;
  isSaving: boolean;
  lastSaveError: string | null;
  lastSavedAt: Date | null;
  lastLogClearResult: LogClearResult | null;

  setFromHost: (settings: AppSettingsDto) => void;
  setDraft: (next: AppSettingsDto) => void;
  resetDraft: () => void;
  setSaving: (saving: boolean) => void;
  setSaveError: (error: string | null) => void;
  markSaved: () => void;
  setLogClearResult: (r: LogClearResult | null) => void;
}

export const useAppSettingsStore = create<AppSettingsStore>((set, get) => ({
  loaded: false,
  settings: null,
  draft: null,
  isSaving: false,
  lastSaveError: null,
  lastSavedAt: null,
  lastLogClearResult: null,

  setFromHost: (settings) => {
    const merged: AppSettingsDto = {
      ...settings,
      aiSkillsHub: settings.aiSkillsHub ?? {
        sources: [],
        cursorSkillsPath: null,
        copilotSkillsPath: null,
      },
    };
    return set({
      loaded: true,
      settings: merged,
      draft: merged,
      isSaving: false,
      lastSaveError: null,
    });
  },
  setDraft: (draft) => set({ draft }),
  resetDraft: () => set({ draft: get().settings }),
  setSaving: (isSaving) => set({ isSaving }),
  setSaveError: (lastSaveError) => set({ lastSaveError }),
  markSaved: () => set({ lastSavedAt: new Date(), isSaving: false, lastSaveError: null }),
  setLogClearResult: (lastLogClearResult) => set({ lastLogClearResult }),
}));


