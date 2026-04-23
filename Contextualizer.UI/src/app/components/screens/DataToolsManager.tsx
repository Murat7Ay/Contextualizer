import { useEffect, useMemo, useState } from 'react';
import { toast } from 'sonner';
import {
  Check,
  ChevronsUpDown,
  Plus,
  RefreshCcw,
  Save,
  Search,
  Trash2,
  FileJson,
  Wrench,
} from 'lucide-react';
import { ScrollArea } from '../ui/scroll-area';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
import { Input } from '../ui/input';
import { Label } from '../ui/label';
import { Button } from '../ui/button';
import { Textarea } from '../ui/textarea';
import { Badge } from '../ui/badge';
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
} from '../ui/alert-dialog';
import { cn } from '../ui/utils';
import { Popover, PopoverContent, PopoverTrigger } from '../ui/popover';
import { Command, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList } from '../ui/command';
import { useDataToolsStore, type DataToolDefinitionDto, type DataToolParameterDefinitionDto } from '../../stores/dataToolsStore';
import { useHostStore } from '../../stores/hostStore';
import { useActivityLogStore } from '../../stores/activityLogStore';
import {
  addWebView2MessageListener,
  createDataTool,
  deleteDataTool,
  openExternalUrl,
  reloadDataTools,
  requestConfigConnections,
  requestDataToolsList,
  updateDataTool,
} from '../../host/webview2Bridge';

type ParameterDraft = {
  rowId: string;
  name: string;
  db_parameter_name: string;
  type: string;
  description: string;
  required: boolean;
  default_value_text: string;
  enum_text: string;
  array_item_type: string;
  direction: string;
  db_type: string;
  serialize_as_json: boolean;
};

type DataToolEditorDraft = {
  id: string;
  name: string;
  tool_name: string;
  description: string;
  provider: string;
  operation: string;
  connection: string;
  statement: string;
  procedure_name: string;
  enabled: boolean;
  expose_as_tool: boolean;
  command_timeout_seconds: string;
  connection_timeout_seconds: string;
  max_pool_size: string;
  min_pool_size: string;
  disable_pooling: boolean;
  tags_text: string;
  result_mode: string;
  result_max_rows: string;
  include_execution_metadata: boolean;
  include_output_parameters: boolean;
  output_scalar_parameter: string;
  input_schema_text: string;
  provider_options_text: string;
  parameters: ParameterDraft[];
};

type HostDataToolOpMessage =
  | { type: 'data_tool_create_result'; ok: true; id: string }
  | { type: 'data_tool_create_result'; ok: false; error: string }
  | { type: 'data_tool_update_result'; ok: true; id: string; originalId: string }
  | { type: 'data_tool_update_result'; ok: false; error: string }
  | { type: 'data_tool_delete_result'; ok: true; id: string }
  | { type: 'data_tool_delete_result'; ok: false; error: string }
  | { type: 'data_tools_reload_result'; ok: true }
  | { type: 'data_tools_reload_result'; ok: false; error: string };

const providerPresets = ['mssql', 'plsql', 'neo4j', 'redis', 'elasticsearch'];
const operationOptions = ['select', 'scalar', 'execute', 'procedure'];
const parameterTypeOptions = ['string', 'integer', 'number', 'boolean', 'array', 'object'];
const directionOptions = ['input', 'output', 'input_output'];
const dbTypeOptions = [
  '',
  'string',
  'ansi_string',
  'int16',
  'int32',
  'int64',
  'decimal',
  'double',
  'single',
  'boolean',
  'datetime',
  'date',
  'time',
  'guid',
  'binary',
];
const supportedProviders = new Set(['mssql', 'plsql']);

function makeRowId(): string {
  return `row-${Date.now()}-${Math.random().toString(36).slice(2)}`;
}

function createEmptyParameterDraft(): ParameterDraft {
  return {
    rowId: makeRowId(),
    name: '',
    db_parameter_name: '',
    type: 'string',
    description: '',
    required: false,
    default_value_text: '',
    enum_text: '',
    array_item_type: '',
    direction: 'input',
    db_type: '',
    serialize_as_json: false,
  };
}

function createEmptyDraft(): DataToolEditorDraft {
  return {
    id: '',
    name: '',
    tool_name: '',
    description: '',
    provider: 'mssql',
    operation: 'select',
    connection: '',
    statement: '',
    procedure_name: '',
    enabled: true,
    expose_as_tool: true,
    command_timeout_seconds: '',
    connection_timeout_seconds: '',
    max_pool_size: '',
    min_pool_size: '',
    disable_pooling: false,
    tags_text: '',
    result_mode: '',
    result_max_rows: '200',
    include_execution_metadata: true,
    include_output_parameters: true,
    output_scalar_parameter: '',
    input_schema_text: '',
    provider_options_text: '',
    parameters: [],
  };
}

function slugify(value: string): string {
  const chars: string[] = [];
  for (const ch of value.trim()) {
    if (/^[a-z0-9]$/i.test(ch)) {
      chars.push(ch.toLowerCase());
      continue;
    }

    if ([' ', '-', '_', '.'].includes(ch)) {
      if (chars.length === 0 || chars[chars.length - 1] === '_') continue;
      chars.push('_');
    }
  }

  return chars.join('').replace(/^_+|_+$/g, '') || 'tool';
}

function stringifyJsonBlock(value: unknown): string {
  if (value === undefined || value === null) return '';
  try {
    return JSON.stringify(value, null, 2);
  } catch {
    return '';
  }
}

function parameterToDraft(parameter: DataToolParameterDefinitionDto): ParameterDraft {
  return {
    rowId: makeRowId(),
    name: parameter.name ?? '',
    db_parameter_name: parameter.db_parameter_name ?? '',
    type: parameter.type ?? 'string',
    description: parameter.description ?? '',
    required: !!parameter.required,
    default_value_text: stringifyJsonBlock(parameter.default_value),
    enum_text: Array.isArray(parameter.enum) ? parameter.enum.join(', ') : '',
    array_item_type: parameter.array_item_type ?? '',
    direction: parameter.direction ?? 'input',
    db_type: parameter.db_type ?? '',
    serialize_as_json: !!parameter.serialize_as_json,
  };
}

function definitionToDraft(definition: DataToolDefinitionDto): DataToolEditorDraft {
  return {
    id: definition.id ?? '',
    name: definition.name ?? '',
    tool_name: definition.tool_name ?? '',
    description: definition.description ?? '',
    provider: definition.provider ?? '',
    operation: definition.operation ?? 'select',
    connection: definition.connection ?? '',
    statement: definition.statement ?? '',
    procedure_name: definition.procedure_name ?? '',
    enabled: !!definition.enabled,
    expose_as_tool: !!definition.expose_as_tool,
    command_timeout_seconds: definition.command_timeout_seconds == null ? '' : String(definition.command_timeout_seconds),
    connection_timeout_seconds: definition.connection_timeout_seconds == null ? '' : String(definition.connection_timeout_seconds),
    max_pool_size: definition.max_pool_size == null ? '' : String(definition.max_pool_size),
    min_pool_size: definition.min_pool_size == null ? '' : String(definition.min_pool_size),
    disable_pooling: !!definition.disable_pooling,
    tags_text: Array.isArray(definition.tags) ? definition.tags.join(', ') : '',
    result_mode: definition.result?.mode ?? '',
    result_max_rows: String(definition.result?.max_rows ?? 200),
    include_execution_metadata: definition.result?.include_execution_metadata ?? true,
    include_output_parameters: definition.result?.include_output_parameters ?? true,
    output_scalar_parameter: definition.result?.output_scalar_parameter ?? '',
    input_schema_text: stringifyJsonBlock(definition.input_schema),
    provider_options_text: stringifyJsonBlock(definition.provider_options),
    parameters: Array.isArray(definition.parameters) ? definition.parameters.map(parameterToDraft) : [],
  };
}

function parseOptionalInteger(value: string, label: string): { value?: number; error?: string } {
  const trimmed = value.trim();
  if (!trimmed) return {};
  const parsed = Number(trimmed);
  if (!Number.isInteger(parsed)) {
    return { error: `${label} must be an integer.` };
  }
  return { value: parsed };
}

function parseJsonObjectText(value: string, label: string): { value?: unknown; error?: string } {
  const trimmed = value.trim();
  if (!trimmed) return {};
  try {
    const parsed = JSON.parse(trimmed);
    if (typeof parsed !== 'object' || parsed === null || Array.isArray(parsed)) {
      return { error: `${label} must be a JSON object.` };
    }
    return { value: parsed };
  } catch (error) {
    return { error: `${label} is not valid JSON: ${error instanceof Error ? error.message : 'Unknown error'}` };
  }
}

function parseFlexibleJsonValue(value: string): { value?: unknown; defined: boolean; error?: string } {
  const trimmed = value.trim();
  if (!trimmed) return { defined: false };

  try {
    return { defined: true, value: JSON.parse(trimmed) };
  } catch {
    return { defined: true, value: trimmed };
  }
}

function buildDefinitionFromDraft(draft: DataToolEditorDraft): { definition?: DataToolDefinitionDto; error?: string } {
  const id = draft.id.trim();
  if (!id) return { error: 'Id is required.' };

  const provider = draft.provider.trim();
  if (!provider) return { error: 'Provider is required.' };

  const operation = draft.operation.trim().toLowerCase();
  if (!operationOptions.includes(operation)) {
    return { error: `Operation must be one of: ${operationOptions.join(', ')}.` };
  }

  const connection = draft.connection.trim();
  if (!connection) return { error: 'Connection is required.' };

  const statement = draft.statement.trim();
  const procedureName = draft.procedure_name.trim();
  if (operation === 'procedure') {
    if (!procedureName) return { error: 'Procedure name is required for procedure operations.' };
  } else if (!statement) {
    return { error: 'Statement is required for non-procedure operations.' };
  }

  const commandTimeout = parseOptionalInteger(draft.command_timeout_seconds, 'Command timeout');
  if (commandTimeout.error) return { error: commandTimeout.error };
  const connectionTimeout = parseOptionalInteger(draft.connection_timeout_seconds, 'Connection timeout');
  if (connectionTimeout.error) return { error: connectionTimeout.error };
  const maxPool = parseOptionalInteger(draft.max_pool_size, 'Max pool size');
  if (maxPool.error) return { error: maxPool.error };
  const minPool = parseOptionalInteger(draft.min_pool_size, 'Min pool size');
  if (minPool.error) return { error: minPool.error };
  const maxRows = parseOptionalInteger(draft.result_max_rows, 'Result max rows');
  if (maxRows.error) return { error: maxRows.error };

  const inputSchema = parseJsonObjectText(draft.input_schema_text, 'Input schema');
  if (inputSchema.error) return { error: inputSchema.error };
  const providerOptions = parseJsonObjectText(draft.provider_options_text, 'Provider options');
  if (providerOptions.error) return { error: providerOptions.error };

  const parameters: DataToolParameterDefinitionDto[] = [];
  for (const parameter of draft.parameters) {
    const name = parameter.name.trim();
    if (!name) continue;

    const defaultValue = parseFlexibleJsonValue(parameter.default_value_text);
    if (defaultValue.error) return { error: `Default value for parameter '${name}' is invalid.` };

    parameters.push({
      name,
      db_parameter_name: parameter.db_parameter_name.trim() || undefined,
      type: parameter.type.trim() || 'string',
      description: parameter.description.trim() || undefined,
      required: parameter.required,
      default_value: defaultValue.defined ? defaultValue.value : undefined,
      enum: parameter.enum_text
        .split(/[\n,]/)
        .map((entry) => entry.trim())
        .filter(Boolean),
      array_item_type: parameter.array_item_type.trim() || undefined,
      direction: parameter.direction.trim() || 'input',
      db_type: parameter.db_type.trim() || undefined,
      serialize_as_json: parameter.serialize_as_json,
    });
  }

  return {
    definition: {
      id,
      name: draft.name.trim() || undefined,
      tool_name: draft.tool_name.trim() || undefined,
      description: draft.description.trim() || undefined,
      provider,
      operation,
      connection,
      statement: operation === 'procedure' ? undefined : statement,
      procedure_name: operation === 'procedure' ? procedureName : undefined,
      enabled: draft.enabled,
      expose_as_tool: draft.expose_as_tool,
      command_timeout_seconds: commandTimeout.value,
      connection_timeout_seconds: connectionTimeout.value,
      max_pool_size: maxPool.value,
      min_pool_size: minPool.value,
      disable_pooling: draft.disable_pooling,
      parameters,
      input_schema: inputSchema.value,
      tags: draft.tags_text
        .split(/[\n,]/)
        .map((entry) => entry.trim())
        .filter(Boolean),
      result: {
        mode: draft.result_mode.trim() || undefined,
        max_rows: maxRows.value ?? 200,
        include_execution_metadata: draft.include_execution_metadata,
        include_output_parameters: draft.include_output_parameters,
        output_scalar_parameter: draft.output_scalar_parameter.trim() || undefined,
      },
      provider_options: providerOptions.value,
    },
  };
}

function ConnectionCombobox({
  value,
  onChange,
  connectionKeys,
}: {
  value: string;
  onChange: (value: string) => void;
  connectionKeys: string[];
}) {
  const [open, setOpen] = useState(false);
  const [search, setSearch] = useState('');

  useEffect(() => {
    if (open) setSearch(value);
  }, [open]);

  const filtered = connectionKeys.filter((k) => k.toLowerCase().includes(search.toLowerCase()));

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <button
          type="button"
          role="combobox"
          aria-expanded={open}
          className={cn(
            'flex h-9 w-full items-center justify-between rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background',
            'focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2',
          )}
        >
          <span className={cn('truncate font-mono', !value && 'font-sans text-muted-foreground')}>{value || '$config:connections.main_mssql'}</span>
          <ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 text-muted-foreground" />
        </button>
      </PopoverTrigger>
      <PopoverContent className="p-0 w-[--radix-popover-trigger-width]" align="start">
        <Command>
          <CommandInput
            placeholder="Type or search connection…"
            value={search}
            onValueChange={(v) => {
              setSearch(v);
              onChange(v);
            }}
          />
          <CommandList>
            {connectionKeys.length > 0 && filtered.length === 0 && <CommandEmpty>No matches.</CommandEmpty>}
            {filtered.length > 0 && (
              <CommandGroup>
                {filtered.map((key) => (
                  <CommandItem
                    key={key}
                    value={key}
                    onSelect={(selected) => {
                      onChange(selected);
                      setSearch('');
                      setOpen(false);
                    }}
                  >
                    <Check className={cn('mr-2 h-4 w-4 shrink-0', value === key ? 'opacity-100' : 'opacity-0')} />
                    <span className="font-mono text-xs">{key}</span>
                  </CommandItem>
                ))}
              </CommandGroup>
            )}
          </CommandList>
        </Command>
      </PopoverContent>
    </Popover>
  );
}

export function DataToolsManager() {
  const webView2Available = useHostStore((state) => state.webView2Available);
  const hostConnected = useHostStore((state) => state.hostConnected);
  const loaded = useDataToolsStore((state) => state.loaded);
  const definitions = useDataToolsStore((state) => state.definitions);
  const registryPath = useDataToolsStore((state) => state.registryPath);
  const storeError = useDataToolsStore((state) => state.error);
  const connectionKeys = useDataToolsStore((state) => state.connectionKeys);
  const addLog = useActivityLogStore((state) => state.addLog);

  const [searchQuery, setSearchQuery] = useState('');
  const [providerFilter, setProviderFilter] = useState('all');
  const [operationFilter, setOperationFilter] = useState('all');
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [originalId, setOriginalId] = useState<string | null>(null);
  const [draft, setDraft] = useState<DataToolEditorDraft>(() => createEmptyDraft());
  const [baselineSnapshot, setBaselineSnapshot] = useState<string>(JSON.stringify(createEmptyDraft()));
  const [pendingSyncId, setPendingSyncId] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [discardOpen, setDiscardOpen] = useState(false);
  const [pendingDiscardCallback, setPendingDiscardCallback] = useState<(() => void) | null>(null);

  const disabled = !webView2Available || !hostConnected;
  const currentSnapshot = useMemo(() => JSON.stringify(draft), [draft]);
  const isDirty = currentSnapshot !== baselineSnapshot;

  useEffect(() => {
    if (!webView2Available || !hostConnected) return;
    requestDataToolsList();
    requestConfigConnections();
  }, [hostConnected, webView2Available]);

  useEffect(() => {
    const unsubscribe = addWebView2MessageListener((payload) => {
      if (!payload || typeof payload !== 'object') return;
      const msg = payload as Record<string, unknown>;
      const type = msg.type;

      if (
        type === 'data_tool_create_result' ||
        type === 'data_tool_update_result' ||
        type === 'data_tool_delete_result' ||
        type === 'data_tools_reload_result'
      ) {
        const result = payload as HostDataToolOpMessage;
        setLoading(false);

        if (!result.ok) {
          const nextError = result.error ?? 'Operation failed';
          setError(nextError);
          addLog('error', 'Data tool operation failed', nextError);
          toast.error(nextError);
          return;
        }

        setError(null);

        if (type === 'data_tool_delete_result') {
          setSelectedId(null);
          setOriginalId(null);
          const empty = createEmptyDraft();
          setDraft(empty);
          setBaselineSnapshot(JSON.stringify(empty));
          addLog('success', `Data tool '${result.id}' deleted`);
          toast.success(`Deleted '${result.id}'`);
          return;
        }

        if (type === 'data_tool_create_result' || type === 'data_tool_update_result') {
          setPendingSyncId(result.id);
          setSelectedId(result.id);
          setOriginalId(result.id);
          addLog('success', `Data tool '${result.id}' saved`);
          toast.success(`Saved '${result.id}'`);
          return;
        }

        addLog('success', 'Data tools reloaded');
        toast.success('Data tools reloaded');
      }
    });

    return () => {
      try {
        unsubscribe();
      } catch {
        // ignore
      }
    };
  }, [addLog]);

  useEffect(() => {
    if (!pendingSyncId) return;
    const definition = definitions.find((entry) => entry.id === pendingSyncId);
    if (!definition) return;

    const nextDraft = definitionToDraft(definition);
    setDraft(nextDraft);
    setBaselineSnapshot(JSON.stringify(nextDraft));
    setSelectedId(definition.id);
    setOriginalId(definition.id);
    setPendingSyncId(null);
  }, [definitions, pendingSyncId]);

  const totals = useMemo(() => {
    const total = definitions.length;
    const enabled = definitions.filter((definition) => definition.enabled).length;
    const exposed = definitions.filter((definition) => definition.expose_as_tool).length;
    const supported = definitions.filter((definition) => definition.is_supported === true).length;
    return { total, enabled, exposed, supported };
  }, [definitions]);

  const providerOptions = useMemo(() => {
    return Array.from(new Set([...providerPresets, ...definitions.map((definition) => definition.provider).filter(Boolean)])).sort((left, right) =>
      left.localeCompare(right),
    );
  }, [definitions]);

  const filteredDefinitions = useMemo(() => {
    const normalizedQuery = searchQuery.trim().toLowerCase();
    return definitions.filter((definition) => {
      const matchesQuery =
        normalizedQuery.length === 0 ||
        definition.id.toLowerCase().includes(normalizedQuery) ||
        (definition.name ?? '').toLowerCase().includes(normalizedQuery) ||
        (definition.description ?? '').toLowerCase().includes(normalizedQuery) ||
        (definition.provider ?? '').toLowerCase().includes(normalizedQuery) ||
        (definition.resolved_tool_name ?? '').toLowerCase().includes(normalizedQuery);

      const matchesProvider = providerFilter === 'all' || definition.provider === providerFilter;
      const matchesOperation = operationFilter === 'all' || definition.operation === operationFilter;
      return matchesQuery && matchesProvider && matchesOperation;
    });
  }, [definitions, operationFilter, providerFilter, searchQuery]);

  const resolvedToolName = useMemo(() => {
    return slugify(draft.tool_name.trim() || draft.name.trim() || draft.id.trim() || 'tool');
  }, [draft.id, draft.name, draft.tool_name]);

  const executionSupported = supportedProviders.has(draft.provider.trim().toLowerCase());

  function patchDraft(patch: Partial<DataToolEditorDraft>) {
    setDraft((current) => ({ ...current, ...patch }));
  }

  function loadDraft(nextDraft: DataToolEditorDraft, id: string | null) {
    setDraft(nextDraft);
    setBaselineSnapshot(JSON.stringify(nextDraft));
    setSelectedId(id);
    setOriginalId(id);
    setError(null);
  }

  function requestDiscard(callback: () => void) {
    if (!isDirty) {
      callback();
      return;
    }
    setPendingDiscardCallback(() => callback);
    setDiscardOpen(true);
  }

  function handleDiscardConfirmed() {
    setDiscardOpen(false);
    pendingDiscardCallback?.();
    setPendingDiscardCallback(null);
  }

  function handleSelectDefinition(definition: DataToolDefinitionDto) {
    requestDiscard(() => loadDraft(definitionToDraft(definition), definition.id));
  }

  function handleNewDefinition() {
    requestDiscard(() => loadDraft(createEmptyDraft(), null));
  }

  function handleResetEditor() {
    if (!originalId) {
      loadDraft(createEmptyDraft(), null);
      return;
    }

    const current = definitions.find((definition) => definition.id === originalId);
    if (!current) {
      loadDraft(createEmptyDraft(), null);
      return;
    }

    loadDraft(definitionToDraft(current), current.id);
  }

  function handleOpenRegistry() {
    if (!registryPath) {
      toast('Registry path is not available.');
      return;
    }

    const ok = openExternalUrl(registryPath);
    if (!ok) {
      toast('Host bridge is not available.');
    }
  }

  function handleReload() {
    if (disabled) return;
    setLoading(true);
    setError(null);
    if (!reloadDataTools()) {
      setLoading(false);
      setError('Host bridge is not available.');
    }
  }

  function handleSave() {
    if (disabled) return;

    const built = buildDefinitionFromDraft(draft);
    if (!built.definition) {
      setError(built.error ?? 'Definition is not valid.');
      return;
    }

    setLoading(true);
    setError(null);

    if (originalId) {
      if (!updateDataTool(originalId, built.definition)) {
        setLoading(false);
        setError('Host bridge is not available.');
      }
      return;
    }

    if (!createDataTool(built.definition)) {
      setLoading(false);
      setError('Host bridge is not available.');
    }
  }

  function handleDelete() {
    if (!originalId || disabled) return;
    setDeleteOpen(false);
    setLoading(true);
    setError(null);
    if (!deleteDataTool(originalId)) {
      setLoading(false);
      setError('Host bridge is not available.');
    }
  }

  function updateParameter(rowId: string, patch: Partial<ParameterDraft>) {
    setDraft((current) => ({
      ...current,
      parameters: current.parameters.map((parameter) => (parameter.rowId === rowId ? { ...parameter, ...patch } : parameter)),
    }));
  }

  function removeParameter(rowId: string) {
    setDraft((current) => ({
      ...current,
      parameters: current.parameters.filter((parameter) => parameter.rowId !== rowId),
    }));
  }

  function addParameter() {
    setDraft((current) => ({
      ...current,
      parameters: [...current.parameters, createEmptyParameterDraft()],
    }));
  }

  return (
    <ScrollArea className="h-full">
      <div className="p-6 max-w-[1800px] mx-auto space-y-6">
        <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
          <div>
            <h1 className="text-[28px] font-semibold mb-2">Data Tools</h1>
            <p className="text-sm text-muted-foreground">
              Full CRUD management for registry-backed MCP data tools. Providers can be relational today or future custom providers later.
            </p>
          </div>

          <div className="flex flex-wrap gap-2">
            <Button variant="outline" onClick={handleNewDefinition}>
              <Plus className="h-4 w-4 mr-2" />
              New
            </Button>
            <Button variant="outline" onClick={handleReload} disabled={disabled || loading}>
              <RefreshCcw className="h-4 w-4 mr-2" />
              Reload
            </Button>
            <Button variant="outline" onClick={handleOpenRegistry} disabled={!registryPath}>
              <FileJson className="h-4 w-4 mr-2" />
              Open JSON
            </Button>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
          <StatCard title="Total Definitions" value={totals.total} description="Persisted in the registry" />
          <StatCard title="Enabled" value={totals.enabled} description="Available for execution/discovery" accent="text-green-600 dark:text-green-400" />
          <StatCard title="Exposed As Tool" value={totals.exposed} description="Published as direct first-class tools" accent="text-blue-600 dark:text-blue-400" />
          <StatCard title="Execution Supported" value={totals.supported} description="Currently supported by built-in runtime" accent="text-amber-600 dark:text-amber-400" />
        </div>

        <div className="grid grid-cols-1 xl:grid-cols-[420px_minmax(0,1fr)] gap-6">
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-base">Definitions</CardTitle>
              <CardDescription>
                Registry: <span className="font-mono text-xs">{registryPath ?? 'not available'}</span>
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-3">
                <div className="relative">
                  <Search className="absolute left-2 top-2.5 h-4 w-4 text-muted-foreground" />
                  <Input
                    value={searchQuery}
                    onChange={(event) => setSearchQuery(event.target.value)}
                    placeholder="Search by id, tool name, provider..."
                    className="pl-8 h-9"
                  />
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                  <Select value={providerFilter} onValueChange={setProviderFilter}>
                    <SelectTrigger className="h-9">
                      <SelectValue placeholder="Provider" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="all">All providers</SelectItem>
                      {providerOptions.map((provider) => (
                        <SelectItem key={provider} value={provider}>
                          {provider}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>

                  <Select value={operationFilter} onValueChange={setOperationFilter}>
                    <SelectTrigger className="h-9">
                      <SelectValue placeholder="Operation" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="all">All operations</SelectItem>
                      {operationOptions.map((operation) => (
                        <SelectItem key={operation} value={operation}>
                          {operation}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </div>

              {!loaded ? (
                <div className="text-sm text-muted-foreground py-8 text-center">Waiting for host…</div>
              ) : filteredDefinitions.length === 0 ? (
                <div className="text-sm text-muted-foreground py-8 text-center">No data tools match the current filters.</div>
              ) : (
                <div className="space-y-3">
                  {filteredDefinitions.map((definition) => {
                    const active = selectedId === definition.id;
                    const resolvedName = definition.resolved_tool_name ?? slugify(definition.tool_name ?? definition.name ?? definition.id);

                    return (
                      <button
                        key={definition.id}
                        type="button"
                        onClick={() => handleSelectDefinition(definition)}
                        className={cn(
                          'w-full text-left rounded-lg border p-4 transition-colors hover:bg-accent/50',
                          active && 'border-primary bg-accent/60',
                          !definition.enabled && 'opacity-60',
                        )}
                      >
                        <div className="space-y-1.5">
                          <div className="flex flex-wrap items-center gap-2">
                            <div className="font-medium">{definition.name || definition.id}</div>
                            <Badge variant="outline" className="text-xs">{definition.provider}</Badge>
                            <Badge variant="outline" className="text-xs">{definition.operation}</Badge>
                            {!definition.enabled && <Badge variant="outline" className="text-xs text-muted-foreground">Disabled</Badge>}
                          </div>
                          <div className="text-xs font-mono text-muted-foreground truncate">{resolvedName}</div>
                          {definition.description ? <div className="text-xs text-muted-foreground line-clamp-1">{definition.description}</div> : null}
                          <div className="flex gap-3 text-xs text-muted-foreground">
                            {definition.expose_as_tool && <span className="text-blue-600 dark:text-blue-400">Direct Tool</span>}
                            {definition.is_supported === false && <span className="text-amber-600 dark:text-amber-400">Future Provider</span>}
                          </div>
                        </div>
                      </button>
                    );
                  })}
                </div>
              )}

              {storeError ? <div className="text-sm text-red-600 dark:text-red-400">{storeError}</div> : null}
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="text-base">{originalId ? `Edit: ${originalId}` : 'New Data Tool'}</CardTitle>
              <CardDescription>
                Parametric or parameterless definitions are both supported. Other providers can be stored even if runtime support is not implemented yet.
              </CardDescription>
            </CardHeader>

            <CardContent className="space-y-6">
              {/* Action toolbar */}
              <div className="flex items-center justify-between gap-3 rounded-lg border bg-muted/30 px-3 py-2">
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => setDeleteOpen(true)}
                  disabled={!originalId || loading}
                  className="text-destructive hover:text-destructive hover:bg-destructive/10"
                >
                  <Trash2 className="h-4 w-4 mr-1.5" />
                  Delete
                </Button>

                <div className="flex items-center gap-2">
                  {isDirty && (
                    <span className="text-xs text-muted-foreground select-none">Unsaved changes</span>
                  )}
                  <Button variant="ghost" size="sm" onClick={handleResetEditor} disabled={loading}>
                    {originalId ? 'Revert' : 'Clear'}
                  </Button>
                  <Button size="sm" onClick={handleSave} disabled={disabled || loading} className={cn(isDirty && 'ring-2 ring-primary/40')}>
                    <Save className="h-4 w-4 mr-1.5" />
                    Save
                  </Button>
                </div>
              </div>
              {error ? (
                <div className="rounded-md border border-red-300 bg-red-50 px-3 py-2 text-sm text-red-700 dark:border-red-900 dark:bg-red-950/20 dark:text-red-300">
                  {error}
                </div>
              ) : null}

              <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-4">
                <div className="space-y-2">
                  <Label>Id</Label>
                  <Input value={draft.id} onChange={(event) => patchDraft({ id: event.target.value })} placeholder="customer_by_code" />
                </div>
                <div className="space-y-2">
                  <Label>Name</Label>
                  <Input value={draft.name} onChange={(event) => patchDraft({ name: event.target.value })} placeholder="Customer By Code" />
                </div>
                <div className="space-y-2">
                  <Label>Tool Name</Label>
                  <Input value={draft.tool_name} onChange={(event) => patchDraft({ tool_name: event.target.value })} placeholder="get_customer_by_code" />
                </div>
                <div className="space-y-2">
                  <Label>Resolved Tool Name</Label>
                  <div className="h-10 rounded-md border bg-muted px-3 py-2 text-sm font-mono break-all">{resolvedToolName}</div>
                </div>
              </div>

              <div className="space-y-2">
                <Label>Description</Label>
                <Textarea value={draft.description} onChange={(event) => patchDraft({ description: event.target.value })} placeholder="Explain when this tool should be used" rows={3} />
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-4">
                <div className="space-y-2 xl:col-span-2">
                  <Label>Provider</Label>
                  <Input value={draft.provider} onChange={(event) => patchDraft({ provider: event.target.value })} placeholder="mssql, plsql, neo4j, redis, elasticsearch..." />
                  <div className="flex flex-wrap gap-1.5">
                    {providerPresets.map((provider) => (
                      <button
                        key={provider}
                        type="button"
                        onClick={() => patchDraft({ provider })}
                        className={cn(
                          'inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-medium transition-colors',
                          draft.provider === provider
                            ? 'border-primary bg-primary text-primary-foreground'
                            : 'border-border bg-muted text-muted-foreground hover:bg-accent hover:text-accent-foreground',
                        )}
                      >
                        {provider}
                      </button>
                    ))}
                  </div>
                </div>

                <div className="space-y-2">
                  <Label>Operation</Label>
                  <Select value={draft.operation} onValueChange={(value) => patchDraft({ operation: value })}>
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {operationOptions.map((operation) => (
                        <SelectItem key={operation} value={operation}>
                          {operation}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>

                <div className="space-y-2">
                  <Label>Runtime Status</Label>
                  <div className="h-10 rounded-md border bg-muted px-3 py-2 text-sm flex items-center gap-2">
                    <Wrench className="h-4 w-4 text-muted-foreground" />
                    <span>{executionSupported ? 'Supported today' : 'Stored for future provider/runtime support'}</span>
                  </div>
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label>Connection</Label>
                  <ConnectionCombobox value={draft.connection} onChange={(v) => patchDraft({ connection: v })} connectionKeys={connectionKeys} />
                </div>
                <div className="space-y-2">
                  <Label>Tags</Label>
                  <Input value={draft.tags_text} onChange={(event) => patchDraft({ tags_text: event.target.value })} placeholder="customer, mssql, finance" />
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <ToggleField label="Enabled" checked={draft.enabled} onCheckedChange={(checked) => patchDraft({ enabled: checked })} description="Makes this definition discoverable and executable" />
                <ToggleField label="Expose As Tool" checked={draft.expose_as_tool} onCheckedChange={(checked) => patchDraft({ expose_as_tool: checked })} description="Publishes as a first-class named MCP tool" />
              </div>

              {draft.operation === 'procedure' ? (
                <div className="space-y-2">
                  <Label>Procedure Name</Label>
                  <Input value={draft.procedure_name} onChange={(event) => patchDraft({ procedure_name: event.target.value })} placeholder="pkg_customer.approve_customer" />
                </div>
              ) : (
                <div className="space-y-2">
                  <Label>Statement</Label>
                  <Textarea value={draft.statement} onChange={(event) => patchDraft({ statement: event.target.value })} placeholder="SELECT * FROM dbo.customers WHERE customer_code = @institution_code" rows={6} className="font-mono" />
                </div>
              )}



              <div className="space-y-4">
                <div>
                  <h2 className="text-base font-semibold">Result Behavior</h2>
                  <p className="text-sm text-muted-foreground">Control row limits, metadata, and procedure result handling.</p>
                </div>

                <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-4">
                  <div className="space-y-2">
                    <Label>Result Mode</Label>
                    <Select value={draft.result_mode || '__empty__'} onValueChange={(value) => patchDraft({ result_mode: value === '__empty__' ? '' : value })}>
                      <SelectTrigger>
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="__empty__">Default</SelectItem>
                        {operationOptions.map((operation) => (
                          <SelectItem key={operation} value={operation}>
                            {operation}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>
                  <NumberField label="Max Rows" value={draft.result_max_rows} onChange={(value) => patchDraft({ result_max_rows: value })} />
                  {draft.operation === 'procedure' && (
                    <div className="space-y-2">
                      <Label>Output Scalar Parameter</Label>
                      <Input value={draft.output_scalar_parameter} onChange={(event) => patchDraft({ output_scalar_parameter: event.target.value })} placeholder="p_status" />
                    </div>
                  )}
                  <div className="grid grid-cols-1 gap-3">
                    <ToggleField label="Include Execution Metadata" checked={draft.include_execution_metadata} onCheckedChange={(checked) => patchDraft({ include_execution_metadata: checked })} />
                    {draft.operation === 'procedure' && (
                      <ToggleField label="Include Output Parameters" checked={draft.include_output_parameters} onCheckedChange={(checked) => patchDraft({ include_output_parameters: checked })} />
                    )}
                  </div>
                </div>
              </div>

              <div className="space-y-4">
                <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
                  <div>
                    <h2 className="text-base font-semibold">Parameters</h2>
                    <p className="text-sm text-muted-foreground">Leave this list empty for parameterless tools.</p>
                  </div>
                  <Button variant="outline" type="button" onClick={addParameter}>
                    <Plus className="h-4 w-4 mr-2" />
                    Add Parameter
                  </Button>
                </div>

                {draft.parameters.length === 0 ? (
                  <div className="rounded-md border border-dashed px-4 py-6 text-sm text-muted-foreground text-center">
                    No parameters. This definition will behave as a parameterless tool.
                  </div>
                ) : (
                  <div className="space-y-4">
                    {draft.parameters.map((parameter, index) => (
                      <Card key={parameter.rowId}>
                        <CardHeader className="pb-3">
                          <div className="flex items-center justify-between gap-3">
                            <div>
                              <CardTitle className="text-sm">Parameter {index + 1}</CardTitle>
                              <CardDescription>{parameter.name || 'Unnamed parameter'}</CardDescription>
                            </div>
                            <Button variant="destructive" size="sm" onClick={() => removeParameter(parameter.rowId)}>
                              Remove
                            </Button>
                          </div>
                        </CardHeader>
                        <CardContent className="space-y-4">
                          <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-4">
                            <div className="space-y-2">
                              <Label>Name</Label>
                              <Input value={parameter.name} onChange={(event) => updateParameter(parameter.rowId, { name: event.target.value })} placeholder="institution_code" />
                            </div>
                            <div className="space-y-2">
                              <Label>DB Parameter Name</Label>
                              <Input value={parameter.db_parameter_name} onChange={(event) => updateParameter(parameter.rowId, { db_parameter_name: event.target.value })} placeholder="institution_code" />
                            </div>
                            <div className="space-y-2">
                              <Label>Type</Label>
                              <Select value={parameter.type} onValueChange={(value) => updateParameter(parameter.rowId, { type: value })}>
                                <SelectTrigger>
                                  <SelectValue />
                                </SelectTrigger>
                                <SelectContent>
                                  {parameterTypeOptions.map((type) => (
                                    <SelectItem key={type} value={type}>
                                      {type}
                                    </SelectItem>
                                  ))}
                                </SelectContent>
                              </Select>
                            </div>
                            <div className="space-y-2">
                              <Label>Direction</Label>
                              <Select value={parameter.direction} onValueChange={(value) => updateParameter(parameter.rowId, { direction: value })}>
                                <SelectTrigger>
                                  <SelectValue />
                                </SelectTrigger>
                                <SelectContent>
                                  {directionOptions.map((direction) => (
                                    <SelectItem key={direction} value={direction}>
                                      {direction}
                                    </SelectItem>
                                  ))}
                                </SelectContent>
                              </Select>
                            </div>
                          </div>

                          <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-4">
                            <div className="space-y-2">
                              <Label>DB Type</Label>
                              <Select value={parameter.db_type || '__empty__'} onValueChange={(value) => updateParameter(parameter.rowId, { db_type: value === '__empty__' ? '' : value })}>
                                <SelectTrigger>
                                  <SelectValue />
                                </SelectTrigger>
                                <SelectContent>
                                  <SelectItem value="__empty__">Default</SelectItem>
                                  {dbTypeOptions.filter(Boolean).map((dbType) => (
                                    <SelectItem key={dbType} value={dbType}>
                                      {dbType}
                                    </SelectItem>
                                  ))}
                                </SelectContent>
                              </Select>
                            </div>
                            {parameter.type === 'array' && (
                              <div className="space-y-2">
                                <Label>Array Item Type</Label>
                                <Input value={parameter.array_item_type} onChange={(event) => updateParameter(parameter.rowId, { array_item_type: event.target.value })} placeholder="string" />
                              </div>
                            )}
                            <div className="space-y-2">
                              <Label>Enum Values</Label>
                              <Input value={parameter.enum_text} onChange={(event) => updateParameter(parameter.rowId, { enum_text: event.target.value })} placeholder="one, two, three" />
                            </div>
                            <div className="grid grid-cols-1 gap-3">
                              <ToggleField label="Required" checked={parameter.required} onCheckedChange={(checked) => updateParameter(parameter.rowId, { required: checked })} />
                              <ToggleField label="Serialize As JSON" checked={parameter.serialize_as_json} onCheckedChange={(checked) => updateParameter(parameter.rowId, { serialize_as_json: checked })} />
                            </div>
                          </div>

                          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            <div className="space-y-2">
                              <Label>Description</Label>
                              <Textarea value={parameter.description} onChange={(event) => updateParameter(parameter.rowId, { description: event.target.value })} rows={3} placeholder="What this parameter means" />
                            </div>
                            <div className="space-y-2">
                              <Label>Default Value (JSON or plain text)</Label>
                              <Textarea
                                value={parameter.default_value_text}
                                onChange={(event) => updateParameter(parameter.rowId, { default_value_text: event.target.value })}
                                rows={3}
                                placeholder={'123, true, {"mode":"fast"}, or plain text'}
                                className="font-mono"
                              />
                            </div>
                          </div>
                        </CardContent>
                      </Card>
                    ))}
                  </div>
                )}
              </div>

              <div className="space-y-4">
                <div className="border-t pt-4">
                  <h2 className="text-base font-semibold">Connection &amp; Pool Options</h2>
                  <p className="text-sm text-muted-foreground">Leave blank to use defaults.</p>
                </div>
                <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-4">
                  <NumberField label="Command Timeout (sec)" value={draft.command_timeout_seconds} onChange={(value) => patchDraft({ command_timeout_seconds: value })} />
                  <NumberField label="Connection Timeout (sec)" value={draft.connection_timeout_seconds} onChange={(value) => patchDraft({ connection_timeout_seconds: value })} />
                  <NumberField label="Max Pool Size" value={draft.max_pool_size} onChange={(value) => patchDraft({ max_pool_size: value })} />
                  <NumberField label="Min Pool Size" value={draft.min_pool_size} onChange={(value) => patchDraft({ min_pool_size: value })} />
                </div>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <ToggleField label="Disable Pooling" checked={draft.disable_pooling} onCheckedChange={(checked) => patchDraft({ disable_pooling: checked })} description="Disable connection pooling for this definition" />
                </div>
              </div>

              <div className="space-y-4">
                <div className="border-t pt-4">
                  <h2 className="text-base font-semibold">Advanced JSON</h2>
                  <p className="text-sm text-muted-foreground">Optional explicit schema and provider-specific options.</p>
                </div>

                <div className="grid grid-cols-1 xl:grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <Label>Input Schema (JSON object)</Label>
                    <Textarea
                      value={draft.input_schema_text}
                      onChange={(event) => patchDraft({ input_schema_text: event.target.value })}
                      rows={10}
                      className="font-mono"
                      placeholder={'{\n  "type": "object",\n  "properties": { ... }\n}'}
                    />
                  </div>
                  <div className="space-y-2">
                    <Label>Provider Options (JSON object)</Label>
                    <Textarea
                      value={draft.provider_options_text}
                      onChange={(event) => patchDraft({ provider_options_text: event.target.value })}
                      rows={10}
                      className="font-mono"
                      placeholder={'{\n  "future_provider_flag": true\n}'}
                    />
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>

      <AlertDialog open={discardOpen} onOpenChange={setDiscardOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Unsaved Changes</AlertDialogTitle>
            <AlertDialogDescription>
              You have unsaved changes. If you continue, they will be lost.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Keep Editing</AlertDialogCancel>
            <AlertDialogAction onClick={handleDiscardConfirmed}>
              Discard Changes
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      <AlertDialog open={deleteOpen} onOpenChange={setDeleteOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete Data Tool</AlertDialogTitle>
            <AlertDialogDescription>
              This removes <span className="font-mono">{(originalId && originalId.trim()) || (draft.id && draft.id.trim()) || 'the selected definition'}</span> from the registry file.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancel</AlertDialogCancel>
            <AlertDialogAction className="bg-destructive text-destructive-foreground hover:bg-destructive/90" onClick={handleDelete}>
              Delete
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </ScrollArea>
  );
}

function StatCard({
  title,
  value,
  description,
  accent,
}: {
  title: string;
  value: number;
  description: string;
  accent?: string;
}) {
  return (
    <Card>
      <CardHeader className="pb-2">
        <CardTitle className="text-sm font-medium">{title}</CardTitle>
      </CardHeader>
      <CardContent>
        <div className={cn('text-2xl font-bold', accent)}>{value}</div>
        <p className="text-xs text-muted-foreground">{description}</p>
      </CardContent>
    </Card>
  );
}

function ToggleField({
  label,
  checked,
  onCheckedChange,
  description,
}: {
  label: string;
  checked: boolean;
  onCheckedChange: (checked: boolean) => void;
  description?: string;
}) {
  return (
    <div className="flex items-center justify-between rounded-md border px-3 py-2 gap-3">
      <div>
        <Label>{label}</Label>
        {description && <p className="text-xs text-muted-foreground">{description}</p>}
      </div>
      <Switch checked={checked} onCheckedChange={onCheckedChange} />
    </div>
  );
}

function NumberField({
  label,
  value,
  onChange,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
}) {
  return (
    <div className="space-y-2">
      <Label>{label}</Label>
      <Input type="number" value={value} onChange={(event) => onChange(event.target.value)} />
    </div>
  );
}