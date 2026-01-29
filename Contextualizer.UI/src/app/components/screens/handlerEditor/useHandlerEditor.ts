import { useEffect, useMemo, useState } from 'react';
import { useActivityLogStore } from '../../../stores/activityLogStore';
import {
  addWebView2MessageListener,
  requestHandlerConfig,
  requestHandlersList,
  requestPluginsList,
} from '../../../host/webview2Bridge';
import {
  buildDraftForType,
  coerceToHandlerType,
  handlerTypeOptions,
  type HandlerConfigDraft,
  type HandlerType,
  validateDraft,
} from '../../../lib/handlerSchemas';
import type {
  HostHandlerGetMessage,
  HostHandlerOpMessage,
  HostHandlersListMessage,
  HostPluginsListMessage,
  HttpConfigDraft,
} from './types';

export function useHandlerEditor(
  open: boolean,
  mode: 'new' | 'edit',
  handlerName?: string,
  initialHandlerType: HandlerType = 'Regex',
  onSaved?: () => void,
  onDeleted?: () => void,
  onCancel?: () => void
) {
  const addLog = useActivityLogStore((s) => s.addLog);

  const [activeTab, setActiveTab] = useState<'wizard' | 'json'>('wizard');
  const [loading, setLoading] = useState(false);
  const [handlerType, setHandlerType] = useState<HandlerType>(initialHandlerType);
  const [draft, setDraft] = useState<HandlerConfigDraft>(() => buildDraftForType(initialHandlerType));
  const [jsonText, setJsonText] = useState<string>('{}');
  const [error, setError] = useState<string | null>(null);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [handlerNames, setHandlerNames] = useState<string[]>([]);
  const [actionNames, setActionNames] = useState<string[]>([]);
  const [validatorNames, setValidatorNames] = useState<string[]>([]);
  const [contextProviderNames, setContextProviderNames] = useState<string[]>([]);
  const [registeredHandlerTypes, setRegisteredHandlerTypes] = useState<string[]>([]);
  const [isSaved, setIsSaved] = useState(false);
  const [publishDialogOpen, setPublishDialogOpen] = useState(false);

  // Subscribe to host responses
  useEffect(() => {
    const unsubscribe = addWebView2MessageListener((payload) => {
      if (!payload || typeof payload !== 'object') return;
      const msg = payload as any;
      const t = msg.type;

      if (t === 'handler_get') {
        const m = payload as HostHandlerGetMessage;
        if (!open) return;
        setLoading(false);
        if (!m.ok) {
          setError(m.error || 'Failed to load handler config');
          return;
        }
        try {
          const cfg = m.handlerConfig as any;
          const type = (cfg?.type ?? '').toString();
          const coercedType = coerceToHandlerType(type);
          if (coercedType) {
            setHandlerType(coercedType);
          } else {
            // Fallback: try to map manually
            const mappedType = (type.charAt(0).toUpperCase() + type.slice(1)) as HandlerType;
            const nextType = (handlerTypeOptions.find((o) => o.value.toLowerCase() === mappedType.toLowerCase()) ??
              handlerTypeOptions[0]?.value ?? 'Regex') as HandlerType;
            setHandlerType(nextType);
          }
          setDraft(cfg as HandlerConfigDraft);
          setJsonText(JSON.stringify(cfg, null, 2));
          setError(null);
        } catch (ex: any) {
          setError(ex?.message ?? 'Failed to parse handler config');
        }
        return;
      }

      if (t === 'handlers_list') {
        const m = payload as HostHandlersListMessage;
        const names = Array.isArray(m.handlers) ? m.handlers.map((h) => h.name).filter(Boolean) : [];
        setHandlerNames(names.sort((a, b) => a.localeCompare(b)));
        return;
      }

      if (t === 'plugins_list') {
        const m = payload as HostPluginsListMessage;
        setActionNames((m.actions ?? []).slice().sort((a, b) => a.localeCompare(b)));
        setValidatorNames((m.validators ?? []).slice().sort((a, b) => a.localeCompare(b)));
        setContextProviderNames((m.contextProviders ?? []).slice().sort((a, b) => a.localeCompare(b)));
        setRegisteredHandlerTypes((m.handlerTypes ?? []).slice().sort((a, b) => a.localeCompare(b)));
        return;
      }

      if (t === 'handler_create_result' || t === 'handler_update_result' || t === 'handler_delete_result') {
        const m = payload as HostHandlerOpMessage;
        setLoading(false);
        if ((m as any).ok !== true) {
          const e = (m as any).error ?? 'Operation failed';
          setError(e);
          addLog('error', 'Handler operation failed', e);
          return;
        }
        setError(null);
        addLog('success', 'Handler saved');
        if (t === 'handler_delete_result') {
          onDeleted?.();
          onCancel?.();
        } else {
          setIsSaved(true);
          onSaved?.();
        }
      }
    });

    return () => {
      try {
        unsubscribe();
      } catch {
        // ignore
      }
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open]);

  useEffect(() => {
    if (!open) return;
    setError(null);
    setActiveTab('wizard');

    if (mode === 'new') {
      const d = buildDraftForType(handlerType);
      setDraft(d);
      setJsonText(JSON.stringify(d, null, 2));
    }

    if (mode === 'edit' && handlerName) {
      setLoading(true);
      requestHandlerConfig(handlerName);
      setIsSaved(true); // If editing, handler already exists (is saved)
    } else {
      setIsSaved(false); // New handler is not saved yet
    }

    requestHandlersList();
    requestPluginsList();
  }, [open, mode, handlerName, handlerType]);

  const wizardValidation = useMemo(() => validateDraft(draft), [draft]);
  const http = (draft.http ?? {}) as HttpConfigDraft;

  // Get current handler type from draft if available, otherwise use handlerType state
  const currentHandlerType = useMemo(() => {
    if (draft.type) {
      const coerced = coerceToHandlerType(draft.type);
      if (coerced) return coerced;
    }
    return handlerType;
  }, [draft.type, handlerType]);

  const updateHttpSection = (section: keyof HttpConfigDraft, patch: Record<string, unknown>) => {
    setDraft((d) => {
      const current = (d.http ?? {}) as HttpConfigDraft;
      const existing = (current[section] as Record<string, unknown>) ?? {};
      return { ...d, http: { ...current, [section]: { ...existing, ...patch } } };
    });
  };

  const onChangeType = (t: HandlerType) => {
    setHandlerType(t);
    const d = buildDraftForType(t);
    // keep name/description when switching during new flow
    d.name = draft.name ?? '';
    d.description = draft.description;
    setDraft(d);
    setJsonText(JSON.stringify(d, null, 2));
  };

  const applyJsonToWizard = () => {
    try {
      const obj = JSON.parse(jsonText) as HandlerConfigDraft;
      const coerced = coerceToHandlerType(String(obj?.type ?? '')) ?? handlerType;
      setHandlerType(coerced);
      setDraft(obj);
      setError(null);
    } catch (ex: any) {
      setError(ex?.message ?? 'Invalid JSON');
    }
  };

  const updateJsonFromWizard = () => {
    setJsonText(JSON.stringify(draft, null, 2));
    setError(null);
  };

  return {
    // State
    activeTab,
    setActiveTab,
    loading,
    setLoading,
    handlerType,
    setHandlerType,
    draft,
    setDraft,
    jsonText,
    setJsonText,
    error,
    setError,
    deleteOpen,
    setDeleteOpen,
    handlerNames,
    actionNames,
    validatorNames,
    contextProviderNames,
    registeredHandlerTypes,
    isSaved,
    setIsSaved,
    publishDialogOpen,
    setPublishDialogOpen,
    // Computed
    wizardValidation,
    http,
    currentHandlerType,
    // Methods
    updateHttpSection,
    onChangeType,
    applyJsonToWizard,
    updateJsonFromWizard,
  };
}
