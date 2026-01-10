import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { toast } from 'sonner';
import { useTabStore } from '../stores/tabStore';
import { useHandlersStore, type HandlerDto } from '../stores/handlersStore';
import { useCronStore, type CronJobDto } from '../stores/cronStore';
import { useActivityLogStore } from '../stores/activityLogStore';
import { useAppSettingsStore, type AppSettingsDto, type LogClearResult } from '../stores/appSettingsStore';
import { useAppStore } from '../stores/appStore';
import { addWebView2MessageListener, executeToastAction, notifyToastClosed } from './webview2Bridge';
import { CountdownToastView, type CountdownToastAction } from '../components/ui/countdown-toast';

type TabAction = {
  id: string;
  label: string;
};

type OpenTabMessage = {
  type: 'open_tab';
  screenId: string;
  title: string;
  context?: unknown;
  actions?: TabAction[];
  autoFocus?: boolean;
  bringToFront?: boolean;
};

type ToastMessage = {
  type: 'toast';
  toastId?: string;
  level?: 'success' | 'error' | 'warning' | 'info' | 'debug' | 'critical';
  title?: string;
  message: string;
  details?: string;
  durationSeconds?: number;
  actions?: CountdownToastAction[];
};

type HandlersListMessage = {
  type: 'handlers_list';
  handlers: HandlerDto[];
};

type CronListMessage = {
  type: 'cron_list';
  isRunning: boolean;
  jobs: CronJobDto[];
};

type AppSettingsMessage = {
  type: 'app_settings';
  settings: AppSettingsDto;
  error?: string;
};

type AppSettingsSavedMessage = {
  type: 'app_settings_saved';
  ok: boolean;
  error?: string;
};

type LogClearResultMessage = {
  type: 'log_clear_result';
  deletedCount: number;
};

export function HostBridgeListener() {
  const navigate = useNavigate();

  const openTab = useTabStore((state) => state.openTab);
  const setHandlers = useHandlersStore((s) => s.setHandlers);
  const setCron = useCronStore((s) => s.setCron);
  const addLog = useActivityLogStore((s) => s.addLog);
  const setFromHost = useAppSettingsStore((s) => s.setFromHost);
  const setSaving = useAppSettingsStore((s) => s.setSaving);
  const setSaveError = useAppSettingsStore((s) => s.setSaveError);
  const markSaved = useAppSettingsStore((s) => s.markSaved);
  const setLogClearResult = useAppSettingsStore((s) => s.setLogClearResult);
  const setTheme = useAppStore((s) => s.setTheme);

  useEffect(() => {
    // IMPORTANT: do not rely on a single "lastMessage" snapshot for host events.
    // WebView2 can deliver multiple messages quickly; snapshots may drop intermediate payloads.
    const unsubscribe = addWebView2MessageListener((payload) => {
      if (!payload || typeof payload !== 'object') return;

      const msg = payload as Record<string, unknown>;
      const type = msg.type;

      if (type === 'handlers_list') {
        const m = payload as HandlersListMessage;
        if (Array.isArray(m.handlers)) setHandlers(m.handlers);
        return;
      }

      if (type === 'cron_list') {
        const m = payload as CronListMessage;
        if (Array.isArray(m.jobs)) setCron({ isRunning: !!m.isRunning, jobs: m.jobs });
        return;
      }

      if (type === 'app_settings') {
        const m = payload as AppSettingsMessage;
        if (m.error) {
          addLog('error', 'Failed to load settings', m.error);
          setSaveError(m.error);
          return;
        }
        if (m.settings && typeof m.settings === 'object') {
          setFromHost(m.settings);
          // Theme'i appStore'a da aktar
          if (m.settings.uiSettings?.theme) {
            setTheme(m.settings.uiSettings.theme);
          }
          return;
        }
      }

      if (type === 'app_settings_saved') {
        const m = payload as AppSettingsSavedMessage;
        if (!m.ok) {
          const err = m.error ?? 'Unknown error';
          addLog('error', 'Failed to save settings', err);
          setSaving(false);
          setSaveError(err);
          return;
        }
        markSaved();
        addLog('success', 'Settings saved');
        return;
      }

      if (type === 'log_clear_result') {
        const m = payload as LogClearResultMessage;
        const r: LogClearResult = { deletedCount: typeof m.deletedCount === 'number' ? m.deletedCount : 0 };
        setLogClearResult(r);
        return;
      }

      if (type === 'open_tab') {
        const m = payload as OpenTabMessage;
        if (!m.screenId || !m.title) return;

        if (m.context == null) {
          addLog('warning', `open_tab received without context: ${m.screenId} / ${m.title}`);
        } else if (typeof m.context === 'object') {
          try {
            const keys = Object.keys(m.context as Record<string, unknown>);
            addLog('debug', `open_tab context received (${keys.length} keys)`, keys.slice(0, 30).join(', '));
          } catch {
            // ignore
          }
        }

        // Respect explicit autoFocus value from host. Default to false if not specified.
        const shouldAutoFocus = m.autoFocus === true;
        openTab(m.screenId, m.title, m.context, shouldAutoFocus, m.actions);

        if (shouldAutoFocus) {
          const state = useTabStore.getState();
          const activeId = state.activeTabId;
          const tab = state.tabs.find((t) => t.id === activeId);
          if (tab) navigate(tab.route);
        }
        return;
      }

      if (type === 'toast') {
        const m = payload as ToastMessage;
        if (!m.message) return;

        const durationMs =
          typeof m.durationSeconds === 'number'
            ? Math.max(1, Math.min(600, m.durationSeconds)) * 1000
            : 5000;

        const actions = Array.isArray(m.actions) ? m.actions : [];
        const hasActions = actions.length > 0;
        const hasHostToastId = typeof m.toastId === 'string' && m.toastId.length > 0;
        const isActionableHostToast = hasActions && hasHostToastId;

        if (isActionableHostToast) {
          const toastId = m.toastId as string;
          const defaultAction = actions.find((a) => a && a.isDefaultAction) ?? null;
          let dismissedByAction = false;
          let defaultExecuted = false;

          toast.custom(
            () => (
              <CountdownToastView
                level={m.level}
                title={m.title}
                message={m.message}
                details={m.details}
                durationMs={durationMs}
                actions={actions}
                onClose={() => {
                  if (defaultAction && !defaultExecuted) {
                    defaultExecuted = true;
                    executeToastAction(toastId, defaultAction.id);
                  }
                  toast.dismiss(toastId);
                }}
                onExpire={() => {
                  if (defaultAction && !defaultExecuted) {
                    defaultExecuted = true;
                    executeToastAction(toastId, defaultAction.id);
                  }
                  toast.dismiss(toastId);
                }}
                onAction={(actionId, closeOnClick) => {
                  executeToastAction(toastId, actionId);
                  if (closeOnClick) {
                    dismissedByAction = true;
                    toast.dismiss(toastId);
                  }
                }}
              />
            ),
            {
              id: toastId,
              duration: Infinity,
              onDismiss: () => {
                // If dismissed without an action click, behave like old WPF close button: execute default (if any).
                if (!dismissedByAction && defaultAction && !defaultExecuted) {
                  defaultExecuted = true;
                  executeToastAction(toastId, defaultAction.id);
                }
                notifyToastClosed(toastId);
              },
            },
          );
          return;
        }

        toast.custom(
          (id) => (
            <CountdownToastView
              level={m.level}
              title={m.title}
              message={m.message}
              details={m.details}
              durationMs={durationMs}
              onClose={() => toast.dismiss(id)}
              onExpire={() => toast.dismiss(id)}
            />
          ),
          { duration: Infinity },
        );
      }
    });

    return () => {
      try {
        unsubscribe();
      } catch {
        // ignore
      }
    };
  }, [addLog, markSaved, navigate, openTab, setCron, setFromHost, setHandlers, setLogClearResult, setSaveError, setSaving, setTheme]);

  return null;
}


