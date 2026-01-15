import React, { useEffect, useMemo, useRef, useState } from 'react';
import type { Dispatch, ReactNode, SetStateAction } from 'react';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '../ui/tabs';
import { Input } from '../ui/input';
import { Textarea } from '../ui/textarea';
import { Label } from '../ui/label';
import { Button } from '../ui/button';
import { Switch } from '../ui/switch';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../ui/select';
import { Tooltip, TooltipContent, TooltipTrigger } from '../ui/tooltip';
import { Info } from 'lucide-react';
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
  type ConfigActionDraft,
  type ConditionDraft,
  type HandlerConfigDraft,
  type HandlerType,
  type UserInputDraft,
  validateDraft,
} from '../../lib/handlerSchemas';

type Props = {
  open: boolean;
  mode: 'new' | 'edit';
  handlerName?: string;
  onOpenChange: (open: boolean) => void;
  onSaved?: () => void;
  onDeleted?: () => void;
};

type EditorProps = {
  open: boolean;
  mode: 'new' | 'edit';
  handlerName?: string;
  onCancel: () => void;
  onSaved?: () => void;
  onDeleted?: () => void;
};

type HostHandlerGetMessage =
  | { type: 'handler_get'; name: string; ok: true; handlerConfig: unknown }
  | { type: 'handler_get'; name: string; ok: false; error: string };

type HostHandlerOpMessage =
  | { type: 'handler_create_result'; ok: true; name: string }
  | { type: 'handler_create_result'; ok: false; error: string }
  | { type: 'handler_update_result'; ok: true; name: string; updatedFields?: string[] }
  | { type: 'handler_update_result'; ok: false; error: string }
  | { type: 'handler_delete_result'; ok: true; name: string }
  | { type: 'handler_delete_result'; ok: false; error: string };

type HostHandlersListMessage = {
  type: 'handlers_list';
  handlers: Array<{ name: string }>;
};

type HostPluginsListMessage = {
  type: 'plugins_list';
  handlerTypes: string[];
  actions: string[];
  validators: string[];
  contextProviders: string[];
};

const operatorOptions = [
  { value: 'and', label: 'AND (group)' },
  { value: 'or', label: 'OR (group)' },
  { value: 'equals', label: 'Equals' },
  { value: 'not_equals', label: 'Not Equals' },
  { value: 'greater_than', label: 'Greater Than' },
  { value: 'less_than', label: 'Less Than' },
  { value: 'contains', label: 'Contains' },
  { value: 'starts_with', label: 'Starts With' },
  { value: 'ends_with', label: 'Ends With' },
  { value: 'matches_regex', label: 'Matches Regex' },
  { value: 'is_empty', label: 'Is Empty' },
  { value: 'is_not_empty', label: 'Is Not Empty' },
];

const httpMethods = ['GET', 'POST', 'PUT', 'PATCH', 'DELETE'];
const dbConnectors = ['mssql', 'plsql'];

const toLines = (values?: string[]) => (values ?? []).join('\n');
const fromLines = (text: string) =>
  text
    .split(/\r?\n/g)
    .map((v) => v.trim())
    .filter((v) => v.length > 0);

const createEmptyCondition = (): ConditionDraft => ({
  operator: 'equals',
  field: '',
  value: '',
});

const createEmptyUserInput = (): UserInputDraft => ({
  key: '',
  title: '',
  message: '',
  is_required: true,
  default_value: '',
});

const createEmptyAction = (): ConfigActionDraft => ({
  name: '',
  requires_confirmation: false,
  user_inputs: [],
  seeder: {},
  constant_seeder: {},
});

const getHandlerEditorTitle = (mode: 'new' | 'edit', handlerName?: string) =>
  mode === 'new' ? 'New Handler' : `Edit Handler: ${handlerName ?? ''}`;

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
          const mappedType = (type.charAt(0).toUpperCase() + type.slice(1)) as HandlerType;
          const nextType = (handlerTypeOptions.find((o) => o.value.toLowerCase() === mappedType.toLowerCase())?.value ??
            'Regex') as HandlerType;
          setHandlerType(nextType);
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
        if (t === 'handler_delete_result') onDeleted?.();
        else onSaved?.();
        onCancel();
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
    }

    requestHandlersList();
    requestPluginsList();
  }, [open, mode, handlerName, handlerType]);

  const wizardValidation = useMemo(() => validateDraft(draft), [draft]);

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
      createHandler(draft, true);
    } else {
      // updates as full object (host will apply as partial update; easiest here is replace via update with all fields)
      updateHandler(handlerName ?? draft.name, draft, true);
    }
  };

  const saveFromJson = () => {
    try {
      const obj = JSON.parse(jsonText);
      setLoading(true);
      if (mode === 'new') createHandler(obj, true);
      else updateHandler(handlerName ?? '', obj, true);
    } catch (ex: any) {
      setError(ex?.message ?? 'Invalid JSON');
    }
  };

  const onDelete = () => {
    if (!handlerName) return;
    setLoading(true);
    deleteHandler(handlerName, true);
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
              <Select value={handlerType} onValueChange={(v) => onChangeType(v as HandlerType)} disabled={mode === 'edit'}>
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
            {renderTypeFields({
              handlerType,
              draft,
              setDraft,
              handlerNames,
              registeredHandlerTypes,
              validatorNames,
              contextProviderNames,
            })}
          </Section>

          {!wizardValidation.ok && (
            <div className="text-xs text-muted-foreground whitespace-pre-wrap">
              {wizardValidation.errors.map((e) => `- ${e}`).join('\n')}
            </div>
          )}

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
    </div>
  );
}

function Section({ title, children }: { title: string; children: ReactNode }) {
  return (
    <div className="space-y-3">
      <div className="text-sm font-semibold">{title}</div>
      <div className="space-y-3">{children}</div>
    </div>
  );
}

function LabelWithHelp({ label, help }: { label: string; help: string }) {
  return (
    <div className="flex items-center gap-2">
      <Label>{label}</Label>
      <Tooltip>
        <TooltipTrigger asChild>
          <button type="button" className="inline-flex items-center text-muted-foreground hover:text-foreground">
            <Info className="h-3.5 w-3.5" />
          </button>
        </TooltipTrigger>
        <TooltipContent className="max-w-xs text-xs">{help}</TooltipContent>
      </Tooltip>
    </div>
  );
}

function KeyValueEditor({
  label,
  help,
  value,
  onChange,
}: {
  label: string;
  help: string;
  value: Record<string, string>;
  onChange: (next: Record<string, string>) => void;
}) {
  const [rows, setRows] = useState<Array<{ id: string; key: string; value: string }>>([]);
  const lastEmittedRef = useRef<string>('');

  useEffect(() => {
    const serialized = JSON.stringify(value ?? {});
    if (serialized === lastEmittedRef.current) {
      return;
    }
    lastEmittedRef.current = serialized;
    const next = Object.entries(value ?? {}).map(([k, v]) => ({
      id: `${k}-${Math.random().toString(36).slice(2)}`,
      key: k,
      value: v,
    }));
    setRows(next);
  }, [value]);

  const emitChange = (nextRows: Array<{ id: string; key: string; value: string }>) => {
    const next = nextRows.reduce<Record<string, string>>((acc, row) => {
      const k = row.key.trim();
      if (k.length === 0) return acc;
      acc[k] = row.value;
      return acc;
    }, {});
    lastEmittedRef.current = JSON.stringify(next);
    onChange(next);
  };

  const updateRow = (id: string, patch: Partial<{ key: string; value: string }>) => {
    const nextRows = rows.map((row) => (row.id === id ? { ...row, ...patch } : row));
    setRows(nextRows);
    emitChange(nextRows);
  };

  const removeRow = (id: string) => {
    const nextRows = rows.filter((row) => row.id !== id);
    setRows(nextRows);
    emitChange(nextRows);
  };

  const addRow = () => {
    const nextRows = [
      ...rows,
      { id: `row-${Date.now()}-${Math.random().toString(36).slice(2)}`, key: '', value: '' },
    ];
    setRows(nextRows);
  };

  return (
    <div className="space-y-2">
      <LabelWithHelp label={label} help={help} />
      {rows.length === 0 && <div className="text-xs text-muted-foreground">No entries</div>}
      {rows.map((row) => (
        <div key={row.id} className="grid grid-cols-1 md:grid-cols-5 gap-2 items-center">
          <Input
            className="md:col-span-2"
            placeholder="key"
            value={row.key}
            onChange={(e) => updateRow(row.id, { key: e.target.value })}
          />
          <Input
            className="md:col-span-2"
            placeholder="value"
            value={row.value}
            onChange={(e) => updateRow(row.id, { value: e.target.value })}
          />
          <Button variant="ghost" size="sm" onClick={() => removeRow(row.id)}>
            Remove
          </Button>
        </div>
      ))}
      <Button variant="outline" size="sm" onClick={addRow}>
        Add Pair
      </Button>
    </div>
  );
}

function JsonEditorField({
  label,
  value,
  onChange,
  placeholder,
}: {
  label: string;
  value: unknown;
  onChange: (value: unknown) => void;
  placeholder?: string;
}) {
  const [text, setText] = useState<string>(() => (value ? JSON.stringify(value, null, 2) : ''));
  const [localError, setLocalError] = useState<string | null>(null);

  useEffect(() => {
    setText(value ? JSON.stringify(value, null, 2) : '');
  }, [value]);

  const apply = () => {
    try {
      const parsed = text.trim().length === 0 ? {} : JSON.parse(text);
      onChange(parsed);
      setLocalError(null);
    } catch (ex: any) {
      setLocalError(ex?.message ?? 'Invalid JSON');
    }
  };

  return (
    <div className="space-y-2">
      <Label>{label}</Label>
      <Textarea
        value={text}
        onChange={(e) => setText(e.target.value)}
        className="min-h-[100px] font-mono text-xs"
        placeholder={placeholder}
      />
      <div className="flex items-center justify-between text-xs text-muted-foreground">
        <Button variant="outline" size="sm" onClick={apply}>
          Apply
        </Button>
        {localError && <span className="text-red-600 dark:text-red-400">{localError}</span>}
      </div>
    </div>
  );
}

function UserInputsEditor({
  inputs,
  onChange,
}: {
  inputs: UserInputDraft[];
  onChange: (inputs: UserInputDraft[]) => void;
}) {
  const updateItem = (index: number, next: UserInputDraft) => {
    const clone = inputs.slice();
    clone[index] = next;
    onChange(clone);
  };

  const removeItem = (index: number) => {
    const clone = inputs.slice();
    clone.splice(index, 1);
    onChange(clone);
  };

  return (
    <div className="space-y-3">
      {inputs.length === 0 && <div className="text-xs text-muted-foreground">No user inputs configured.</div>}
      {inputs.map((input, index) => (
        <UserInputEditor
          key={`${input.key}-${index}`}
          value={input}
          onChange={(next) => updateItem(index, next)}
          onRemove={() => removeItem(index)}
        />
      ))}
      <Button variant="outline" onClick={() => onChange([...inputs, createEmptyUserInput()])}>
        Add User Input
      </Button>
    </div>
  );
}

function UserInputEditor({
  value,
  onChange,
  onRemove,
}: {
  value: UserInputDraft;
  onChange: (value: UserInputDraft) => void;
  onRemove: () => void;
}) {
  const selectionItems = value.selection_items ?? [];

  return (
    <div className="p-3 border rounded-md space-y-3">
      <div className="flex justify-between items-center">
        <div className="text-sm font-semibold">User Input</div>
        <Button variant="ghost" size="sm" onClick={onRemove}>
          Remove
        </Button>
      </div>
      <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
        <div className="space-y-2">
          <Label>Key</Label>
          <Input value={value.key ?? ''} onChange={(e) => onChange({ ...value, key: e.target.value })} />
        </div>
        <div className="space-y-2">
          <Label>Title</Label>
          <Input value={value.title ?? ''} onChange={(e) => onChange({ ...value, title: e.target.value })} />
        </div>
      </div>
      <div className="space-y-2">
        <Label>Message</Label>
        <Input value={value.message ?? ''} onChange={(e) => onChange({ ...value, message: e.target.value })} />
      </div>
      <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
        <div className="space-y-2">
          <Label>Default Value</Label>
          <Input value={value.default_value ?? ''} onChange={(e) => onChange({ ...value, default_value: e.target.value })} />
        </div>
        <div className="space-y-2">
          <Label>Validation Regex</Label>
          <Input value={value.validation_regex ?? ''} onChange={(e) => onChange({ ...value, validation_regex: e.target.value })} />
        </div>
      </div>
      <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
        <div className="space-y-2">
          <LabelWithHelp label="Config Target" help="Format: secrets.section.key or config.section.key." />
          <Input
            value={value.config_target ?? ''}
            onChange={(e) => onChange({ ...value, config_target: e.target.value })}
            placeholder="secrets.section.key"
          />
        </div>
        <div className="space-y-2">
          <Label>Dependent Key</Label>
          <Input value={value.dependent_key ?? ''} onChange={(e) => onChange({ ...value, dependent_key: e.target.value })} />
        </div>
      </div>
      <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
        <ToggleField label="Required" checked={value.is_required !== false} onChange={(checked) => onChange({ ...value, is_required: checked })} />
        <ToggleField label="Selection List" checked={value.is_selection_list === true} onChange={(checked) => onChange({ ...value, is_selection_list: checked })} />
        <ToggleField label="Multi Select" checked={value.is_multi_select === true} onChange={(checked) => onChange({ ...value, is_multi_select: checked })} />
      </div>
      <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
        <ToggleField label="File Picker" checked={value.is_file_picker === true} onChange={(checked) => onChange({ ...value, is_file_picker: checked })} />
        <ToggleField label="Multi Line" checked={value.is_multi_line === true} onChange={(checked) => onChange({ ...value, is_multi_line: checked })} />
        <ToggleField label="Password" checked={value.is_password === true} onChange={(checked) => onChange({ ...value, is_password: checked })} />
      </div>
      {value.is_selection_list && (
        <div className="space-y-2">
          <Label>Selection Items</Label>
          <SelectionItemsEditor
            items={selectionItems}
            onChange={(next) => onChange({ ...value, selection_items: next })}
          />
        </div>
      )}
      <DependentSelectionMapEditor
        value={value.dependent_selection_item_map ?? {}}
        onChange={(next) => onChange({ ...value, dependent_selection_item_map: next })}
      />
    </div>
  );
}

function ToggleField({ label, checked, onChange }: { label: string; checked: boolean; onChange: (checked: boolean) => void }) {
  return (
    <div className="flex items-center justify-between p-3 border rounded-md">
      <Label className="font-semibold">{label}</Label>
      <Switch checked={checked} onCheckedChange={onChange} />
    </div>
  );
}

function SelectionItemsEditor({
  items,
  onChange,
}: {
  items: Array<{ value: string; display: string }>;
  onChange: (items: Array<{ value: string; display: string }>) => void;
}) {
  const updateItem = (index: number, next: { value: string; display: string }) => {
    const clone = items.slice();
    clone[index] = next;
    onChange(clone);
  };

  const removeItem = (index: number) => {
    const clone = items.slice();
    clone.splice(index, 1);
    onChange(clone);
  };

  return (
    <div className="space-y-2">
      {items.map((item, index) => (
        <div key={`${item.value}-${index}`} className="grid grid-cols-1 md:grid-cols-5 gap-2 items-center">
          <Input
            className="md:col-span-2"
            placeholder="value"
            value={item.value ?? ''}
            onChange={(e) => updateItem(index, { ...item, value: e.target.value })}
          />
          <Input
            className="md:col-span-2"
            placeholder="display"
            value={item.display ?? ''}
            onChange={(e) => updateItem(index, { ...item, display: e.target.value })}
          />
          <Button variant="ghost" size="sm" onClick={() => removeItem(index)}>
            Remove
          </Button>
        </div>
      ))}
      <Button variant="outline" size="sm" onClick={() => onChange([...items, { value: '', display: '' }])}>
        Add Selection Item
      </Button>
    </div>
  );
}

function DependentSelectionMapEditor({
  value,
  onChange,
}: {
  value: Record<string, { selection_items: Array<{ value: string; display: string }>; default_value?: string }>;
  onChange: (next: Record<string, { selection_items: Array<{ value: string; display: string }>; default_value?: string }>) => void;
}) {
  const [rows, setRows] = useState<
    Array<{
      id: string;
      key: string;
      selection_items: Array<{ value: string; display: string }>;
      default_value?: string;
    }>
  >([]);
  const lastEmittedRef = useRef<string>('');

  useEffect(() => {
    const serialized = JSON.stringify(value ?? {});
    if (serialized === lastEmittedRef.current) return;
    lastEmittedRef.current = serialized;
    const next = Object.entries(value ?? {}).map(([k, v]) => ({
      id: `${k}-${Math.random().toString(36).slice(2)}`,
      key: k,
      selection_items: v.selection_items ?? [],
      default_value: v.default_value ?? '',
    }));
    setRows(next);
  }, [value]);

  const emitChange = (nextRows: typeof rows) => {
    const next = nextRows.reduce<
      Record<string, { selection_items: Array<{ value: string; display: string }>; default_value?: string }>
    >((acc, row) => {
      const k = row.key.trim();
      if (k.length === 0) return acc;
      acc[k] = {
        selection_items: row.selection_items ?? [],
        default_value: row.default_value ?? '',
      };
      return acc;
    }, {});
    lastEmittedRef.current = JSON.stringify(next);
    onChange(next);
  };

  const updateRow = (id: string, patch: Partial<(typeof rows)[number]>) => {
    const nextRows = rows.map((row) => (row.id === id ? { ...row, ...patch } : row));
    setRows(nextRows);
    emitChange(nextRows);
  };

  const removeRow = (id: string) => {
    const nextRows = rows.filter((row) => row.id !== id);
    setRows(nextRows);
    emitChange(nextRows);
  };

  const addRow = () => {
    const nextRows = [
      ...rows,
      {
        id: `dep-${Date.now()}-${Math.random().toString(36).slice(2)}`,
        key: '',
        selection_items: [],
        default_value: '',
      },
    ];
    setRows(nextRows);
  };

  return (
    <div className="space-y-2">
      <LabelWithHelp
        label="Dependent Selection Item Map"
        help="Maps dependent_key value → selection_items + default_value."
      />
      {rows.length === 0 && <div className="text-xs text-muted-foreground">No dependent mappings</div>}
      {rows.map((row) => (
        <div key={row.id} className="p-3 border rounded-md space-y-2">
          <div className="grid grid-cols-1 md:grid-cols-5 gap-2 items-center">
            <Input
              className="md:col-span-2"
              placeholder="dependent value"
              value={row.key}
              onChange={(e) => updateRow(row.id, { key: e.target.value })}
            />
            <Input
              className="md:col-span-2"
              placeholder="default value"
              value={row.default_value ?? ''}
              onChange={(e) => updateRow(row.id, { default_value: e.target.value })}
            />
            <Button variant="ghost" size="sm" onClick={() => removeRow(row.id)}>
              Remove
            </Button>
          </div>
          <SelectionItemsEditor
            items={row.selection_items}
            onChange={(items) => updateRow(row.id, { selection_items: items })}
          />
        </div>
      ))}
      <Button variant="outline" size="sm" onClick={addRow}>
        Add Mapping
      </Button>
    </div>
  );
}

function ActionsEditor({
  actions,
  actionNames,
  onChange,
}: {
  actions: ConfigActionDraft[];
  actionNames: string[];
  onChange: (actions: ConfigActionDraft[]) => void;
}) {
  const updateItem = (index: number, next: ConfigActionDraft) => {
    const clone = actions.slice();
    clone[index] = next;
    onChange(clone);
  };

  const removeItem = (index: number) => {
    const clone = actions.slice();
    clone.splice(index, 1);
    onChange(clone);
  };

  return (
    <div className="space-y-3">
      {actions.length === 0 && <div className="text-xs text-muted-foreground">No actions configured.</div>}
      {actions.map((action, index) => (
        <ActionEditor
          key={`${action.name}-${index}`}
          action={action}
          actionNames={actionNames}
          onChange={(next) => updateItem(index, next)}
          onRemove={() => removeItem(index)}
        />
      ))}
      <Button variant="outline" onClick={() => onChange([...actions, createEmptyAction()])}>
        Add Action
      </Button>
    </div>
  );
}

function ActionEditor({
  action,
  actionNames,
  onChange,
  onRemove,
}: {
  action: ConfigActionDraft;
  actionNames: string[];
  onChange: (action: ConfigActionDraft) => void;
  onRemove: () => void;
}) {
  const listId = useMemo(() => `action-names-${Math.random().toString(36).slice(2)}`, []);

  return (
    <div className="p-3 border rounded-md space-y-3">
      <div className="flex justify-between items-center">
        <div className="text-sm font-semibold">Action</div>
        <Button variant="ghost" size="sm" onClick={onRemove}>
          Remove
        </Button>
      </div>
      <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
        <div className="space-y-2">
          <Label>Action Name</Label>
          <Input list={listId} value={action.name ?? ''} onChange={(e) => onChange({ ...action, name: e.target.value })} />
          <datalist id={listId}>
            {actionNames.map((name) => (
              <option key={name} value={name} />
            ))}
          </datalist>
        </div>
        <div className="space-y-2">
          <Label>Key</Label>
          <Input value={action.key ?? ''} onChange={(e) => onChange({ ...action, key: e.target.value })} />
        </div>
      </div>
      <ToggleField
        label="Requires Confirmation"
        checked={action.requires_confirmation === true}
        onChange={(checked) => onChange({ ...action, requires_confirmation: checked })}
      />
      <KeyValueEditor
        label="Seeder (templated)"
        help="Dictionary<string,string> merged into context; values resolve placeholders."
        value={action.seeder ?? {}}
        onChange={(value) => onChange({ ...action, seeder: value })}
      />
      <KeyValueEditor
        label="Constant Seeder"
        help="Dictionary<string,string> merged as-is before templating."
        value={action.constant_seeder ?? {}}
        onChange={(value) => onChange({ ...action, constant_seeder: value })}
      />
      <Section title="Conditions">
        <ConditionEditor
          value={action.conditions}
          onChange={(next) => onChange({ ...action, conditions: next })}
        />
      </Section>
      <Section title="Action User Inputs">
        <UserInputsEditor
          inputs={action.user_inputs ?? []}
          onChange={(next) => onChange({ ...action, user_inputs: next })}
        />
      </Section>
      <Section title="Inner Actions">
        <ActionsEditor
          actions={action.inner_actions ?? []}
          actionNames={actionNames}
          onChange={(next) => onChange({ ...action, inner_actions: next })}
        />
      </Section>
    </div>
  );
}

function ConditionEditor({
  value,
  onChange,
}: {
  value?: ConditionDraft;
  onChange: (value?: ConditionDraft) => void;
}) {
  if (!value) {
    return (
      <Button variant="outline" size="sm" onClick={() => onChange(createEmptyCondition())}>
        Add Condition
      </Button>
    );
  }

  const isGroup = value.operator === 'and' || value.operator === 'or';
  const isUnary = value.operator === 'is_empty' || value.operator === 'is_not_empty';
  const conditions = value.conditions ?? [];

  return (
    <div className="space-y-2">
      <div className="grid grid-cols-1 md:grid-cols-3 gap-2 items-center">
        <Select
          value={value.operator}
          onValueChange={(op) =>
            onChange(
              op === 'and' || op === 'or'
                ? { operator: op, conditions: conditions.length ? conditions : [createEmptyCondition()] }
                : { operator: op, field: '', value: '' }
            )
          }
        >
          <SelectTrigger>
            <SelectValue placeholder="Operator" />
          </SelectTrigger>
          <SelectContent>
            {operatorOptions.map((op) => (
              <SelectItem key={op.value} value={op.value}>
                {op.label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        {!isGroup && (
          <Input
            placeholder="Field"
            value={value.field ?? ''}
            onChange={(e) => onChange({ ...value, field: e.target.value })}
          />
        )}
        {!isGroup && !isUnary && (
          <Input
            placeholder="Value"
            value={value.value ?? ''}
            onChange={(e) => onChange({ ...value, value: e.target.value })}
          />
        )}
      </div>
      {isGroup && (
        <div className="space-y-2 pl-3 border-l">
          {conditions.map((cond, index) => (
            <div key={`cond-${index}`} className="space-y-2">
              <ConditionEditor
                value={cond}
                onChange={(next) => {
                  const clone = conditions.slice();
                  if (next) clone[index] = next;
                  onChange({ ...value, conditions: clone });
                }}
              />
              <Button
                variant="ghost"
                size="sm"
                onClick={() => {
                  const clone = conditions.slice();
                  clone.splice(index, 1);
                  onChange({ ...value, conditions: clone });
                }}
              >
                Remove Condition
              </Button>
            </div>
          ))}
          <Button
            variant="outline"
            size="sm"
            onClick={() => onChange({ ...value, conditions: [...conditions, createEmptyCondition()] })}
          >
            Add Subcondition
          </Button>
        </div>
      )}
      <Button variant="ghost" size="sm" onClick={() => onChange(undefined)}>
        Clear Condition
      </Button>
    </div>
  );
}

function renderTypeFields({
  handlerType,
  draft,
  setDraft,
  handlerNames,
  registeredHandlerTypes,
  validatorNames,
  contextProviderNames,
  depth = 0,
}: {
  handlerType: HandlerType;
  draft: HandlerConfigDraft;
  setDraft: Dispatch<SetStateAction<HandlerConfigDraft>>;
  handlerNames: string[];
  registeredHandlerTypes: string[];
  validatorNames: string[];
  contextProviderNames: string[];
  depth?: number;
}) {
  const actualTypeOptions =
    registeredHandlerTypes.length > 0 ? registeredHandlerTypes : handlerTypeOptions.map((o) => o.value);

  const renderCommonRegex = () => (
    <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
      <div className="space-y-2">
        <Label>Regex</Label>
        <Input value={draft.regex ?? ''} onChange={(e) => setDraft((d) => ({ ...d, regex: e.target.value }))} />
      </div>
      <div className="space-y-2">
        <Label>Groups</Label>
        <Textarea
          value={toLines(draft.groups)}
          onChange={(e) => setDraft((d) => ({ ...d, groups: fromLines(e.target.value) }))}
          className="min-h-[80px]"
        />
      </div>
    </div>
  );

  if (handlerType === 'Regex') {
    return (
      <div className="space-y-3">
        <Label>Regex</Label>
        <Input value={draft.regex ?? ''} onChange={(e) => setDraft((d) => ({ ...d, regex: e.target.value }))} />
        <div className="space-y-2">
          <Label>Groups</Label>
          <Textarea
            value={toLines(draft.groups)}
            onChange={(e) => setDraft((d) => ({ ...d, groups: fromLines(e.target.value) }))}
            className="min-h-[80px]"
          />
        </div>
      </div>
    );
  }

  if (handlerType === 'File') {
    return (
      <div className="space-y-2">
        <Label>File Extensions</Label>
        <Textarea
          value={toLines(draft.file_extensions)}
          onChange={(e) => setDraft((d) => ({ ...d, file_extensions: fromLines(e.target.value) }))}
          className="min-h-[80px]"
        />
      </div>
    );
  }

  if (handlerType === 'Lookup') {
    return (
      <div className="space-y-3">
        <div className="space-y-2">
          <LabelWithHelp label="Path" help="Supports $config: and $file: resolution." />
          <Input value={draft.path ?? ''} onChange={(e) => setDraft((d) => ({ ...d, path: e.target.value }))} />
        </div>
        <div className="space-y-2">
          <LabelWithHelp label="Delimiter" help="Delimiter used in the lookup file (e.g. tab)." />
          <Input value={draft.delimiter ?? ''} onChange={(e) => setDraft((d) => ({ ...d, delimiter: e.target.value }))} />
        </div>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
          <div className="space-y-2">
            <Label>Key Names</Label>
            <Textarea
              value={toLines(draft.key_names)}
              onChange={(e) => setDraft((d) => ({ ...d, key_names: fromLines(e.target.value) }))}
              className="min-h-[80px]"
            />
          </div>
          <div className="space-y-2">
            <Label>Value Names</Label>
            <Textarea
              value={toLines(draft.value_names)}
              onChange={(e) => setDraft((d) => ({ ...d, value_names: fromLines(e.target.value) }))}
              className="min-h-[80px]"
            />
          </div>
        </div>
      </div>
    );
  }

  if (handlerType === 'Database') {
    return (
      <div className="space-y-3">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
          <div className="space-y-2">
          <LabelWithHelp label="Connector" help="Database connector: mssql or plsql." />
            <Select value={draft.connector ?? ''} onValueChange={(v) => setDraft((d) => ({ ...d, connector: v }))}>
              <SelectTrigger>
                <SelectValue placeholder="Select connector" />
              </SelectTrigger>
              <SelectContent>
                {dbConnectors.map((c) => (
                  <SelectItem key={c} value={c}>
                    {c}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="space-y-2">
          <LabelWithHelp label="Connection String" help="Supports $config: placeholders." />
            <Input
              value={draft.connectionString ?? ''}
              onChange={(e) => setDraft((d) => ({ ...d, connectionString: e.target.value }))}
            />
          </div>
        </div>
        <div className="space-y-2">
        <LabelWithHelp label="Query" help="SELECT-only. Supports $config: and $file:." />
          <Textarea value={draft.query ?? ''} onChange={(e) => setDraft((d) => ({ ...d, query: e.target.value }))} className="min-h-[120px]" />
        </div>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
          <div className="space-y-2">
            <Label>Command Timeout (seconds)</Label>
            <Input
              type="number"
              value={draft.command_timeout_seconds ?? ''}
              onChange={(e) =>
                setDraft((d) => ({
                  ...d,
                  command_timeout_seconds: e.target.value === '' ? undefined : Number(e.target.value),
                }))
              }
            />
          </div>
          <div className="space-y-2">
            <Label>Connection Timeout (seconds)</Label>
            <Input
              type="number"
              value={draft.connection_timeout_seconds ?? ''}
              onChange={(e) =>
                setDraft((d) => ({
                  ...d,
                  connection_timeout_seconds: e.target.value === '' ? undefined : Number(e.target.value),
                }))
              }
            />
          </div>
          <div className="space-y-2">
            <Label>Disable Pooling</Label>
            <Switch
              checked={draft.disable_pooling === true}
              onCheckedChange={(checked) => setDraft((d) => ({ ...d, disable_pooling: checked }))}
            />
          </div>
        </div>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
          <div className="space-y-2">
            <Label>Max Pool Size</Label>
            <Input
              type="number"
              value={draft.max_pool_size ?? ''}
              onChange={(e) =>
                setDraft((d) => ({
                  ...d,
                  max_pool_size: e.target.value === '' ? undefined : Number(e.target.value),
                }))
              }
            />
          </div>
          <div className="space-y-2">
            <Label>Min Pool Size</Label>
            <Input
              type="number"
              value={draft.min_pool_size ?? ''}
              onChange={(e) =>
                setDraft((d) => ({
                  ...d,
                  min_pool_size: e.target.value === '' ? undefined : Number(e.target.value),
                }))
              }
            />
          </div>
        </div>
        {renderCommonRegex()}
      </div>
    );
  }

  if (handlerType === 'Api') {
    return (
      <div className="space-y-3">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
          <div className="space-y-2">
          <LabelWithHelp label="URL" help="Supports $(key), $config:, $file:, $func:." />
            <Input value={draft.url ?? ''} onChange={(e) => setDraft((d) => ({ ...d, url: e.target.value }))} />
          </div>
          <div className="space-y-2">
          <LabelWithHelp label="Method" help="HTTP method for the request." />
            <Select value={draft.method ?? ''} onValueChange={(v) => setDraft((d) => ({ ...d, method: v }))}>
              <SelectTrigger>
                <SelectValue placeholder="Select method" />
              </SelectTrigger>
              <SelectContent>
                {httpMethods.map((m) => (
                  <SelectItem key={m} value={m}>
                    {m}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </div>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
          <div className="space-y-2">
          <LabelWithHelp label="Content Type" help="Content-Type for request body." />
            <Input value={draft.content_type ?? ''} onChange={(e) => setDraft((d) => ({ ...d, content_type: e.target.value }))} />
          </div>
          <div className="space-y-2">
          <LabelWithHelp label="Timeout (seconds)" help="HTTP timeout for the request." />
            <Input
              type="number"
              value={draft.timeout_seconds ?? ''}
              onChange={(e) =>
                setDraft((d) => ({
                  ...d,
                  timeout_seconds: e.target.value === '' ? undefined : Number(e.target.value),
                }))
              }
            />
          </div>
        </div>
        <JsonEditorField
          label="Headers"
          value={draft.headers ?? {}}
          onChange={(value) => setDraft((d) => ({ ...d, headers: value as Record<string, string> }))}
          placeholder='{"Authorization":"Bearer ..."}'
        />
        <JsonEditorField
          label="Request Body"
          value={draft.request_body ?? {}}
          onChange={(value) => setDraft((d) => ({ ...d, request_body: value }))}
          placeholder='{"id":"$(id)"}'
        />
        {renderCommonRegex()}
      </div>
    );
  }

  if (handlerType === 'Custom') {
    const validatorListId = `validator-list-${Math.random().toString(36).slice(2)}`;
    const providerListId = `provider-list-${Math.random().toString(36).slice(2)}`;
    return (
      <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
        <div className="space-y-2">
          <Label>Validator</Label>
          <Input
            list={validatorListId}
            value={draft.validator ?? ''}
            onChange={(e) => setDraft((d) => ({ ...d, validator: e.target.value }))}
          />
          <datalist id={validatorListId}>
            {validatorNames.map((name) => (
              <option key={name} value={name} />
            ))}
          </datalist>
        </div>
        <div className="space-y-2">
          <Label>Context Provider</Label>
          <Input
            list={providerListId}
            value={draft.context_provider ?? ''}
            onChange={(e) => setDraft((d) => ({ ...d, context_provider: e.target.value }))}
          />
          <datalist id={providerListId}>
            {contextProviderNames.map((name) => (
              <option key={name} value={name} />
            ))}
          </datalist>
        </div>
      </div>
    );
  }

  if (handlerType === 'Synthetic') {
    return (
      <div className="space-y-3">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
          <div className="space-y-2">
            <Label>Reference Handler</Label>
            <Select
              value={draft.reference_handler ?? ''}
              onValueChange={(v) => setDraft((d) => ({ ...d, reference_handler: v || undefined }))}
            >
              <SelectTrigger>
                <SelectValue placeholder="Select handler" />
              </SelectTrigger>
              <SelectContent>
                {handlerNames.map((name) => (
                  <SelectItem key={name} value={name}>
                    {name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
          <div className="space-y-2">
            <Label>Actual Type</Label>
            <Select
              value={draft.actual_type ?? ''}
              onValueChange={(v) => setDraft((d) => ({ ...d, actual_type: v || undefined }))}
            >
              <SelectTrigger>
                <SelectValue placeholder="Select type" />
              </SelectTrigger>
              <SelectContent>
                {actualTypeOptions.map((t) => (
                  <SelectItem key={t} value={t}>
                    {t}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </div>
        <Section title="Synthetic Input">
          {draft.synthetic_input ? (
            <UserInputEditor
              value={draft.synthetic_input}
              onChange={(next) => setDraft((d) => ({ ...d, synthetic_input: next }))}
              onRemove={() => setDraft((d) => ({ ...d, synthetic_input: undefined }))}
            />
          ) : (
            <Button variant="outline" size="sm" onClick={() => setDraft((d) => ({ ...d, synthetic_input: createEmptyUserInput() }))}>
              Add Synthetic Input
            </Button>
          )}
        </Section>
        {draft.actual_type && (
          <Section title="Embedded Handler Config">
            {depth > 0 ? (
              <div className="text-xs text-muted-foreground">Nested synthetic embedding is not rendered in the wizard.</div>
            ) : coerceToHandlerType(draft.actual_type) ? (
              renderTypeFields({
                handlerType: coerceToHandlerType(draft.actual_type) as HandlerType,
                draft,
                setDraft,
                handlerNames,
                registeredHandlerTypes,
                validatorNames,
                contextProviderNames,
                depth: depth + 1,
              })
            ) : (
              <div className="text-xs text-muted-foreground">No wizard fields for actual_type: {draft.actual_type}</div>
            )}
          </Section>
        )}
      </div>
    );
  }

  if (handlerType === 'Cron') {
    return (
      <div className="space-y-3">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
          <div className="space-y-2">
            <Label>Cron Job Id</Label>
            <Input value={draft.cron_job_id ?? ''} onChange={(e) => setDraft((d) => ({ ...d, cron_job_id: e.target.value }))} />
          </div>
          <div className="space-y-2">
            <Label>Cron Expression</Label>
            <Input
              value={draft.cron_expression ?? ''}
              onChange={(e) => setDraft((d) => ({ ...d, cron_expression: e.target.value }))}
            />
          </div>
        </div>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
          <div className="space-y-2">
            <Label>Cron Timezone</Label>
            <Input
              value={draft.cron_timezone ?? ''}
              onChange={(e) => setDraft((d) => ({ ...d, cron_timezone: e.target.value }))}
            />
          </div>
          <div className="flex items-center justify-between p-3 border rounded-md">
            <Label className="font-semibold">Cron Enabled</Label>
            <Switch checked={draft.cron_enabled !== false} onCheckedChange={(checked) => setDraft((d) => ({ ...d, cron_enabled: checked }))} />
          </div>
        </div>
        <div className="space-y-2">
          <Label>Actual Type</Label>
          <Select
            value={draft.actual_type ?? ''}
            onValueChange={(v) => setDraft((d) => ({ ...d, actual_type: v || undefined }))}
          >
            <SelectTrigger>
              <SelectValue placeholder="Select type" />
            </SelectTrigger>
            <SelectContent>
              {actualTypeOptions.map((t) => (
                <SelectItem key={t} value={t}>
                  {t}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        {draft.actual_type && (
          <Section title="Embedded Handler Config">
            {depth > 0 ? (
              <div className="text-xs text-muted-foreground">Nested cron embedding is not rendered in the wizard.</div>
            ) : coerceToHandlerType(draft.actual_type) ? (
              renderTypeFields({
                handlerType: coerceToHandlerType(draft.actual_type) as HandlerType,
                draft,
                setDraft,
                handlerNames,
                registeredHandlerTypes,
                validatorNames,
                contextProviderNames,
                depth: depth + 1,
              })
            ) : (
              <div className="text-xs text-muted-foreground">No wizard fields for actual_type: {draft.actual_type}</div>
            )}
          </Section>
        )}
      </div>
    );
  }

  return <div className="text-xs text-muted-foreground">No type-specific fields.</div>;
}


