import React, { useEffect, useMemo, useState } from 'react';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../ui/tabs';
import { Input } from '../ui/input';
import { Textarea } from '../ui/textarea';
import { Label } from '../ui/label';
import { Button } from '../ui/button';
import { Switch } from '../ui/switch';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../ui/select';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from '../ui/alert-dialog';
import { useActivityLogStore } from '../../stores/activityLogStore';
import {
  addWebView2MessageListener,
  createHandler,
  deleteHandler,
  requestHandlerConfig,
  requestHandlersList,
  requestPluginsList,
  updateHandler,
} from '../../host/webview2Bridge';
import {
  buildDraftForType,
  coerceToHandlerType,
  handlerTypeOptions,
  type HandlerConfigDraft,
  type HandlerType,
  validateDraft,
} from '../../lib/handlerSchemas';
import type { Props, EditorProps, HostHandlerGetMessage, HostHandlerOpMessage, HostHandlersListMessage, HostPluginsListMessage, HttpConfigDraft } from './handlerEditor/types';
import { getHandlerEditorTitle } from './handlerEditor/helpers';
import { toLines, fromLines } from './handlerEditor/helpers';
import { Section, LabelWithHelp, KeyValueEditor, JsonEditorField } from './handlerEditor/shared';
import { UserInputsEditor } from './handlerEditor/userInputs';
import { ActionsEditor } from './handlerEditor/actions';
import { HttpConfigForm } from './handlerEditor/http';
import { TypeFieldsRenderer } from './handlerEditor/typeFields';
import { PublishSection, PublishDialog } from './handlerEditor/publish';

export function HandlerEditorDialog({ open, mode, handlerName, onOpenChange, onSaved, onDeleted }: Props) {
  if (!open) return null;
  const title = getHandlerEditorTitle(mode, handlerName);
  return (
    <div className="max-w-5xl p-4 space-y-4">
      <div className="space-y-1">
        <div className="text-lg font-semibold">{title}</div>
        <p className="text-sm text-muted-foreground">
          Wizard for guided editing, or Advanced JSON for full control. Validation happens on save (host-side).
        </p>
      </div>
      <HandlerEditorBody
        open={open}
        mode={mode}
        handlerName={handlerName}
        onCancel={() => onOpenChange(false)}
        onSaved={onSaved}
        onDeleted={onDeleted}
      />
    </div>
  );
}

export function HandlerEditorBody({ open, mode, handlerName, onCancel, onSaved, onDeleted }: EditorProps) {
  const addLog = useActivityLogStore((s) => s.addLog);

  const [activeTab, setActiveTab] = useState<'wizard' | 'json'>('wizard');
  const [loading, setLoading] = useState(false);

  const [handlerType, setHandlerType] = useState<HandlerType>('Regex');
  const [draft, setDraft] = useState<HandlerConfigDraft>(() => buildDraftForType('Regex'));

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
          onCancel();
        } else {
          setIsSaved(true);
          onSaved?.();
        }
        return;
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
  }, [open, onCancel]);

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

  const saveFromWizard = () => {
    const v = validateDraft(draft);
    if (!v.ok) {
      setError(v.errors.join('\n'));
      return;
    }
    setLoading(true);
    if (mode === 'new') {
      const ok = createHandler(draft, true);
      if (!ok) {
        setLoading(false);
        setError('Unable to send create request to host');
      }
    } else {
      // updates as full object (host will apply as partial update; easiest here is replace via update with all fields)
      const ok = updateHandler(handlerName ?? draft.name, draft, true);
      if (!ok) {
        setLoading(false);
        setError('Unable to send update request to host');
      }
    }
  };

  const saveFromJson = () => {
    try {
      const obj = JSON.parse(jsonText);
      setLoading(true);
      if (mode === 'new') {
        const ok = createHandler(obj, true);
        if (!ok) {
          setLoading(false);
          setError('Unable to send create request to host');
        }
      } else {
        const ok = updateHandler(handlerName ?? '', obj, true);
        if (!ok) {
          setLoading(false);
          setError('Unable to send update request to host');
        }
      }
    } catch (ex: any) {
      setError(ex?.message ?? 'Invalid JSON');
    }
  };

  const onDelete = () => {
    if (!handlerName) return;
    setLoading(true);
    const ok = deleteHandler(handlerName, true);
    if (!ok) {
      setLoading(false);
      setError('Unable to send delete request to host');
    }
  };

  return (
    <div className="space-y-6">
      {error && (
        <div className="p-3 border rounded-md text-sm text-red-600 dark:text-red-400 bg-red-50 dark:bg-red-950/20 border-red-200 dark:border-red-900 whitespace-pre-wrap">
          {error}
        </div>
      )}

      <Tabs value={activeTab} onValueChange={(v) => setActiveTab(v as any)}>
        <TabsList>
          <TabsTrigger value="wizard">Wizard</TabsTrigger>
          <TabsTrigger value="json">Advanced JSON</TabsTrigger>
        </TabsList>

        <TabsContent value="wizard" className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <div className="space-y-2">
              <Label>Type</Label>
              <Select 
                value={currentHandlerType} 
                onValueChange={(v) => onChangeType(v as HandlerType)} 
                disabled={mode === 'edit'}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Select type" />
                </SelectTrigger>
                <SelectContent>
                  {handlerTypeOptions.map((o) => (
                    <SelectItem key={o.value} value={o.value}>
                      {o.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {mode === 'edit' && (
                <p className="text-xs text-muted-foreground">Type cannot be changed from the editor.</p>
              )}
            </div>

            <div className="space-y-2">
              <Label>Name</Label>
              <Input
                value={draft.name ?? ''}
                onChange={(e) => setDraft((d) => ({ ...d, name: e.target.value }))}
                disabled={mode === 'edit'}
              />
              {mode === 'edit' && (
                <p className="text-xs text-muted-foreground">Name cannot be changed (identifier).</p>
              )}
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <div className="space-y-2">
              <Label>Description</Label>
              <Input value={draft.description ?? ''} onChange={(e) => setDraft((d) => ({ ...d, description: e.target.value }))} />
            </div>
            <div className="space-y-2">
              <LabelWithHelp label="Screen Id" help="Used by show_window actions to route to a tab (optional)." />
              <Input value={draft.screen_id ?? ''} onChange={(e) => setDraft((d) => ({ ...d, screen_id: e.target.value }))} />
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <div className="space-y-2">
              <Label>Title</Label>
              <Input value={draft.title ?? ''} onChange={(e) => setDraft((d) => ({ ...d, title: e.target.value }))} />
            </div>
            <div className="space-y-2">
              <LabelWithHelp
                label="Output Format"
                help="Template for formatted output. Supports $(key), $config:, $file:, $func:."
              />
              <Textarea
                value={draft.output_format ?? ''}
                onChange={(e) => setDraft((d) => ({ ...d, output_format: e.target.value }))}
                className="min-h-[80px]"
              />
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <div className="flex items-center justify-between p-3 border rounded-md">
              <div>
                <Label className="font-semibold">Enabled</Label>
                <p className="text-xs text-muted-foreground">Affects clipboard processing</p>
              </div>
              <Switch
                checked={draft.enabled !== false}
                onCheckedChange={(checked) => setDraft((d) => ({ ...d, enabled: checked }))}
              />
            </div>
            <div className="flex items-center justify-between p-3 border rounded-md">
              <div>
                <Label className="font-semibold">Requires Confirmation</Label>
                <p className="text-xs text-muted-foreground">Blocks MCP headless</p>
              </div>
              <Switch
                checked={draft.requires_confirmation === true}
                onCheckedChange={(checked) => setDraft((d) => ({ ...d, requires_confirmation: checked }))}
              />
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <div className="flex items-center justify-between p-3 border rounded-md">
              <div>
                <Label className="font-semibold">Auto Focus Tab</Label>
                <p className="text-xs text-muted-foreground">Focus UI tab after execution</p>
              </div>
              <Switch
                checked={draft.auto_focus_tab === true}
                onCheckedChange={(checked) => setDraft((d) => ({ ...d, auto_focus_tab: checked }))}
              />
            </div>
            <div className="flex items-center justify-between p-3 border rounded-md">
              <div>
                <Label className="font-semibold">Bring Window to Front</Label>
                <p className="text-xs text-muted-foreground">Show UI window after execution</p>
              </div>
              <Switch
                checked={draft.bring_window_to_front === true}
                onCheckedChange={(checked) => setDraft((d) => ({ ...d, bring_window_to_front: checked }))}
              />
            </div>
          </div>

          <Section title="Seeder / Constant Seeder">
            <KeyValueEditor
              label="Seeder (templated)"
              help="Dictionary<string,string> merged into context; values resolve placeholders."
              value={draft.seeder ?? {}}
              onChange={(value) => setDraft((d) => ({ ...d, seeder: value }))}
            />
            <KeyValueEditor
              label="Constant Seeder"
              help="Dictionary<string,string> merged as-is before templating."
              value={draft.constant_seeder ?? {}}
              onChange={(value) => setDraft((d) => ({ ...d, constant_seeder: value }))}
            />
          </Section>

          <Section title="Handler User Inputs">
            <UserInputsEditor
              inputs={draft.user_inputs ?? []}
              onChange={(inputs) => setDraft((d) => ({ ...d, user_inputs: inputs }))}
            />
          </Section>

          <Section title="Actions">
            <ActionsEditor
              actions={draft.actions ?? []}
              actionNames={actionNames}
              onChange={(actions) => setDraft((d) => ({ ...d, actions }))}
            />
          </Section>

          <Section title="MCP">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              <div className="flex items-center justify-between p-3 border rounded-md">
                <div>
                  <Label className="font-semibold">MCP Enabled</Label>
                  <p className="text-xs text-muted-foreground">Expose as MCP tool</p>
                </div>
                <Switch
                  checked={draft.mcp_enabled === true}
                  onCheckedChange={(checked) => setDraft((d) => ({ ...d, mcp_enabled: checked }))}
                />
              </div>
              <div className="flex items-center justify-between p-3 border rounded-md">
                <div>
                  <Label className="font-semibold">MCP Headless</Label>
                  <p className="text-xs text-muted-foreground">No dialogs (user_inputs/confirm)</p>
                </div>
                <Switch
                  checked={draft.mcp_headless === true}
                  onCheckedChange={(checked) => setDraft((d) => ({ ...d, mcp_headless: checked }))}
                />
              </div>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              <div className="space-y-2">
                <LabelWithHelp label="MCP Tool Name" help="Optional tool name override for MCP server." />
                <Input value={draft.mcp_tool_name ?? ''} onChange={(e) => setDraft((d) => ({ ...d, mcp_tool_name: e.target.value }))} />
              </div>
              <div className="space-y-2">
                <LabelWithHelp label="MCP Description" help="Optional description shown to MCP clients." />
                <Input value={draft.mcp_description ?? ''} onChange={(e) => setDraft((d) => ({ ...d, mcp_description: e.target.value }))} />
              </div>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              <div className="space-y-2">
                <LabelWithHelp label="MCP Input Template" help="Builds ClipboardContent.Text from MCP args." />
                <Textarea
                  value={draft.mcp_input_template ?? ''}
                  onChange={(e) => setDraft((d) => ({ ...d, mcp_input_template: e.target.value }))}
                  className="min-h-[80px]"
                />
              </div>
              <div className="space-y-2">
                <LabelWithHelp label="MCP Return Keys" help="If set, only these context keys are returned." />
                <Textarea
                  value={toLines(draft.mcp_return_keys)}
                  onChange={(e) => setDraft((d) => ({ ...d, mcp_return_keys: fromLines(e.target.value) }))}
                  className="min-h-[80px]"
                />
              </div>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              <div className="flex items-center justify-between p-3 border rounded-md">
                <div>
                  <Label className="font-semibold">MCP Seed Overwrite</Label>
                  <p className="text-xs text-muted-foreground">Allow MCP args to overwrite context</p>
                </div>
                <Switch
                  checked={draft.mcp_seed_overwrite === true}
                  onCheckedChange={(checked) => setDraft((d) => ({ ...d, mcp_seed_overwrite: checked }))}
                />
              </div>
              <JsonEditorField
                label="MCP Input Schema"
                value={draft.mcp_input_schema ?? {}}
                onChange={(value) => setDraft((d) => ({ ...d, mcp_input_schema: value }))}
                placeholder='{"type":"object","properties":{"text":{"type":"string"}}}'
              />
            </div>
          </Section>

          <HttpConfigForm http={http} updateHttpSection={updateHttpSection} />

          <Section title="Core Template Preview">
            <div className="text-xs text-muted-foreground space-y-2">
              <div>
                <code>$(key)</code> placeholder: <code>$(id)</code> or <code>$(group_1)</code>
              </div>
              <div>
                <code>$config:section.key</code> or <code>$config:secrets.section.key</code> for config lookup
              </div>
              <div>
                <code>$file:C:\\path\\file.txt</code> loads file content before placeholder processing
              </div>
              <div>
                <code>$func:today().format("yyyy-MM-dd")</code>
              </div>
              <div>
                <code>{'$func:{{ $(id) | string.upper() }}'}</code>
              </div>
            </div>
          </Section>

          <Section title="Type Configuration">
            <TypeFieldsRenderer
              handlerType={handlerType}
              draft={draft}
              setDraft={setDraft}
              handlerNames={handlerNames}
              registeredHandlerTypes={registeredHandlerTypes}
              validatorNames={validatorNames}
              contextProviderNames={contextProviderNames}
            />
          </Section>

          {!wizardValidation.ok && (
            <div className="text-xs text-muted-foreground whitespace-pre-wrap">
              {wizardValidation.errors.map((e) => `- ${e}`).join('\n')}
            </div>
          )}

          <PublishSection
            handlerName={draft.name ?? ''}
            isSaved={isSaved}
            onPublish={() => setPublishDialogOpen(true)}
          />

          <div className="flex justify-between gap-2">
            <div className="flex gap-2">
              {mode === 'edit' && (
                <AlertDialog open={deleteOpen} onOpenChange={setDeleteOpen}>
                  <AlertDialogTrigger asChild>
                    <Button variant="destructive" disabled={loading}>
                      Delete
                    </Button>
                  </AlertDialogTrigger>
                  <AlertDialogContent>
                    <AlertDialogHeader>
                      <AlertDialogTitle>Delete handler</AlertDialogTitle>
                      <AlertDialogDescription>
                        Delete handler '{handlerName}'? This cannot be undone.
                      </AlertDialogDescription>
                    </AlertDialogHeader>
                    <AlertDialogFooter>
                      <AlertDialogCancel>Cancel</AlertDialogCancel>
                      <AlertDialogAction onClick={onDelete}>Delete</AlertDialogAction>
                    </AlertDialogFooter>
                  </AlertDialogContent>
                </AlertDialog>
              )}
            </div>
            <div className="flex gap-2">
              <Button variant="outline" onClick={onCancel} disabled={loading}>
                Cancel
              </Button>
              <Button onClick={saveFromWizard} disabled={loading}>
                {loading ? 'Saving…' : 'Save'}
              </Button>
            </div>
          </div>
        </TabsContent>

        <TabsContent value="json" className="space-y-3">
          <Textarea value={jsonText} onChange={(e) => setJsonText(e.target.value)} className="min-h-[320px] font-mono text-xs" />
          <div className="flex justify-between gap-2">
            <div className="flex gap-2">
              <Button variant="outline" onClick={applyJsonToWizard} disabled={loading}>
                Apply JSON to Wizard
              </Button>
              <Button variant="outline" onClick={updateJsonFromWizard} disabled={loading}>
                Update JSON from Wizard
              </Button>
            </div>
            <div className="flex gap-2">
              <Button variant="outline" onClick={onCancel} disabled={loading}>
                Cancel
              </Button>
              <Button onClick={saveFromJson} disabled={loading}>
                {loading ? 'Saving…' : 'Save JSON'}
              </Button>
            </div>
          </div>
        </TabsContent>
      </Tabs>

      <PublishDialog
        open={publishDialogOpen}
        handlerName={draft.name ?? ''}
        handlerJson={draft}
        handlerDescription={draft.description}
        onOpenChange={setPublishDialogOpen}
        onPublished={() => {
          // Optionally refresh or show success message
        }}
      />
    </div>
  );
}
