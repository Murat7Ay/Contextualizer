export type HandlerType =
  | 'Regex'
  | 'File'
  | 'Lookup'
  | 'Database'
  | 'Api'
  | 'Custom'
  | 'Manual'
  | 'Synthetic'
  | 'Cron';

export type SelectionItemDraft = {
  value: string;
  display: string;
};

export type DependentSelectionItemMapDraft = {
  selection_items: SelectionItemDraft[];
  default_value?: string;
};

export type UserInputDraft = {
  key: string;
  title: string;
  message: string;
  validation_regex?: string;
  is_required?: boolean;
  is_selection_list?: boolean;
  is_password?: boolean;
  selection_items?: SelectionItemDraft[];
  is_multi_select?: boolean;
  is_file_picker?: boolean;
  is_multi_line?: boolean;
  default_value?: string;
  dependent_key?: string;
  dependent_selection_item_map?: Record<string, DependentSelectionItemMapDraft>;
  config_target?: string;
};

export type ConditionDraft = {
  operator: string;
  field?: string;
  value?: string;
  conditions?: ConditionDraft[];
};

export type ConfigActionDraft = {
  name: string;
  requires_confirmation?: boolean;
  key?: string;
  conditions?: ConditionDraft;
  user_inputs?: UserInputDraft[];
  seeder?: Record<string, string>;
  constant_seeder?: Record<string, string>;
  inner_actions?: ConfigActionDraft[];
};

export type HandlerConfigDraft = {
  name: string;
  description?: string;
  type: string;
  screen_id?: string;
  title?: string;

  // common
  enabled?: boolean;
  requires_confirmation?: boolean;
  output_format?: string;
  seeder?: Record<string, string>;
  constant_seeder?: Record<string, string>;
  actions?: ConfigActionDraft[];
  user_inputs?: UserInputDraft[];
  auto_focus_tab?: boolean;
  bring_window_to_front?: boolean;

  // regex/file/lookup/db/api/custom/synthetic/cron fields (subset used by wizard)
  regex?: string;
  groups?: string[];

  file_extensions?: string[];

  path?: string;
  delimiter?: string;
  key_names?: string[];
  value_names?: string[];

  connectionString?: string;
  query?: string;
  connector?: string;
  command_timeout_seconds?: number;
  connection_timeout_seconds?: number;
  max_pool_size?: number;
  min_pool_size?: number;
  disable_pooling?: boolean;

  url?: string;
  method?: string;
  headers?: Record<string, string>;
  request_body?: unknown;
  content_type?: string;
  timeout_seconds?: number;

  validator?: string;
  context_provider?: string;

  reference_handler?: string;
  actual_type?: string;
  synthetic_input?: UserInputDraft;

  cron_job_id?: string;
  cron_expression?: string;
  cron_timezone?: string;
  cron_enabled?: boolean;

  // MCP basics (optional in editor)
  mcp_enabled?: boolean;
  mcp_tool_name?: string;
  mcp_description?: string;
  mcp_input_schema?: unknown;
  mcp_input_template?: string;
  mcp_return_keys?: string[];
  mcp_headless?: boolean;
  mcp_seed_overwrite?: boolean;
};

export type HandlerFieldKey = keyof HandlerConfigDraft;

export type HandlerSchemaField = {
  key: HandlerFieldKey;
  label: string;
  help?: string;
  required?: boolean;
  placeholder?: string;
};

export type HandlerSchema = {
  type: HandlerType;
  typeLabel: string;
  description: string;
  fields: HandlerSchemaField[];
  defaults: Partial<HandlerConfigDraft>;
};

const commonFields: HandlerSchemaField[] = [
  { key: 'name', label: 'Name', required: true, placeholder: 'My Handler' },
  { key: 'description', label: 'Description', placeholder: 'What does this handler do?' },
  { key: 'screen_id', label: 'Screen Id', help: 'UI tab/screen id for show_window actions, optional.' },
  { key: 'title', label: 'Title', help: 'Optional UI title for window/tab content.' },
  { key: 'enabled', label: 'Enabled', help: 'If false, handler is skipped by clipboard processing.' },
  {
    key: 'requires_confirmation',
    label: 'Requires Confirmation',
    help: 'If true, asks the user before executing (cannot run in MCP headless).',
  },
];

export const handlerSchemas: Record<HandlerType, HandlerSchema> = {
  Regex: {
    type: 'Regex',
    typeLabel: 'Regex',
    description: 'Matches clipboard text using a regex and exposes groups as context keys.',
    defaults: { type: 'Regex', enabled: true, requires_confirmation: false, groups: [] },
    fields: [
      ...commonFields,
      { key: 'regex', label: 'Regex', required: true, placeholder: '^ABC(?<id>\\d+)$' },
      { key: 'groups', label: 'Groups', help: 'Optional list of group names to extract (named or positional).' },
    ],
  },
  File: {
    type: 'File',
    typeLabel: 'File',
    description: 'Triggers on file clipboard content and exposes file metadata as context keys.',
    defaults: { type: 'File', enabled: true, file_extensions: ['.txt'] },
    fields: [
      ...commonFields,
      {
        key: 'file_extensions',
        label: 'File Extensions',
        required: true,
        help: "Array of allowed extensions (e.g. ['.json', '.txt']).",
      },
    ],
  },
  Lookup: {
    type: 'Lookup',
    typeLabel: 'Lookup',
    description: 'Looks up clipboard text in a local mapping file (delimiter-separated).',
    defaults: { type: 'Lookup', enabled: true, delimiter: '\t', key_names: [], value_names: [] },
    fields: [
      ...commonFields,
      { key: 'path', label: 'Path', required: true, help: 'Lookup file path. Supports $config:/ $file: resolution.' },
      { key: 'delimiter', label: 'Delimiter', required: true, placeholder: '\\t' },
      { key: 'key_names', label: 'Key Names', required: true, help: 'Which value_names are considered keys.' },
      { key: 'value_names', label: 'Value Names', required: true, help: 'Column names for each value in a row.' },
    ],
  },
  Database: {
    type: 'Database',
    typeLabel: 'Database',
    description: 'Runs a safe SELECT query using parameters derived from input/regex or MCP args.',
    defaults: { type: 'Database', enabled: true, connector: 'mssql' },
    fields: [
      ...commonFields,
      { key: 'connector', label: 'Connector', required: true, help: "Supported: 'mssql' | 'plsql'." },
      { key: 'connectionString', label: 'Connection String', required: true },
      { key: 'query', label: 'Query', required: true, help: 'Must start with SELECT. No DML/DDL allowed.' },
      { key: 'command_timeout_seconds', label: 'Command Timeout (seconds)', help: 'Query execution timeout.' },
      { key: 'connection_timeout_seconds', label: 'Connection Timeout (seconds)' },
      { key: 'max_pool_size', label: 'Max Pool Size' },
      { key: 'min_pool_size', label: 'Min Pool Size' },
      { key: 'disable_pooling', label: 'Disable Pooling' },
      { key: 'regex', label: 'Optional Regex', help: 'Optional pre-filter + group extraction for parameters.' },
      { key: 'groups', label: 'Groups', help: 'Optional group names to map into SQL params.' },
    ],
  },
  Api: {
    type: 'Api',
    typeLabel: 'API',
    description: 'Calls an HTTP endpoint, expands $(key) placeholders from context/args, and flattens JSON.',
    defaults: { type: 'Api', enabled: true, method: 'GET', content_type: 'application/json', timeout_seconds: 30 },
    fields: [
      ...commonFields,
      { key: 'url', label: 'URL', required: true, placeholder: 'https://api.example.com/items/$(id)' },
      { key: 'method', label: 'Method', placeholder: 'GET' },
      { key: 'headers', label: 'Headers', help: 'JSON object map: { \"Authorization\": \"Bearer ...\" }' },
      { key: 'request_body', label: 'Request Body', help: 'JSON object/array, optional.' },
      { key: 'content_type', label: 'Content Type', placeholder: 'application/json' },
      { key: 'timeout_seconds', label: 'Timeout (seconds)' },
      { key: 'regex', label: 'Optional Regex', help: 'Optional pre-filter + group extraction.' },
      { key: 'groups', label: 'Groups', help: 'Optional group names to add to context.' },
    ],
  },
  Custom: {
    type: 'Custom',
    typeLabel: 'Custom',
    description: 'Delegates validation/context creation to plugins (validator/context_provider).',
    defaults: { type: 'Custom', enabled: true },
    fields: [
      ...commonFields,
      { key: 'validator', label: 'Validator', help: 'Plugin validator name (IContextValidator.Name).' },
      { key: 'context_provider', label: 'Context Provider', help: 'Plugin provider name (IContextProvider.Name).' },
    ],
  },
  Manual: {
    type: 'Manual',
    typeLabel: 'Manual',
    description: 'Runs only when manually triggered (does not depend on clipboard content).',
    defaults: { type: 'Manual', enabled: true },
    fields: [...commonFields],
  },
  Synthetic: {
    type: 'Synthetic',
    typeLabel: 'Synthetic',
    description: 'Wraps another handler by reference (reference_handler) or embeds one by actual_type.',
    defaults: { type: 'Synthetic', enabled: true },
    fields: [
      ...commonFields,
      { key: 'reference_handler', label: 'Reference Handler', help: 'Name of an existing handler to execute.' },
      { key: 'actual_type', label: 'Actual Type', help: 'Type of embedded handler (e.g. Database/Api/Regex).' },
    ],
  },
  Cron: {
    type: 'Cron',
    typeLabel: 'Cron',
    description: 'Schedules execution using cron_expression and runs an embedded handler (actual_type).',
    defaults: { type: 'Cron', enabled: true, cron_enabled: true, cron_timezone: 'Europe/Istanbul' },
    fields: [
      ...commonFields,
      { key: 'cron_job_id', label: 'Cron Job Id', required: true },
      { key: 'cron_expression', label: 'Cron Expression', required: true },
      { key: 'cron_timezone', label: 'Cron Timezone', placeholder: 'Europe/Istanbul' },
      { key: 'cron_enabled', label: 'Cron Enabled' },
      { key: 'actual_type', label: 'Actual Type', required: true, help: 'Type of the underlying handler.' },
    ],
  },
};

export const handlerTypeOptions: { value: HandlerType; label: string; description: string }[] = (
  Object.values(handlerSchemas) as HandlerSchema[]
).map((s) => ({ value: s.type, label: s.typeLabel, description: s.description }));

export function coerceToHandlerType(type: string): HandlerType | null {
  const t = (type ?? '').trim().toLowerCase();
  switch (t) {
    case 'regex':
      return 'Regex';
    case 'file':
      return 'File';
    case 'lookup':
      return 'Lookup';
    case 'database':
      return 'Database';
    case 'api':
      return 'Api';
    case 'custom':
      return 'Custom';
    case 'manual':
      return 'Manual';
    case 'synthetic':
      return 'Synthetic';
    case 'cron':
      return 'Cron';
    default:
      return null;
  }
}

export function buildDraftForType(type: HandlerType): HandlerConfigDraft {
  const schema = handlerSchemas[type];
  return {
    name: '',
    type: schema.defaults.type ?? type,
    ...schema.defaults,
  } as HandlerConfigDraft;
}

export function validateDraft(draft: HandlerConfigDraft): { ok: boolean; errors: string[] } {
  const errors: string[] = [];

  if (!draft.name || draft.name.trim().length === 0) errors.push('name is required');
  if (!draft.type || String(draft.type).trim().length === 0) errors.push('type is required');

  const ht = coerceToHandlerType(draft.type);
  if (!ht) {
    errors.push(`unsupported type: ${draft.type}`);
    return { ok: false, errors };
  }

  const schema = handlerSchemas[ht];
  for (const f of schema.fields) {
    if (!f.required) continue;
    const v = (draft as any)[f.key];
    const isEmptyArray = Array.isArray(v) && v.length === 0;
    const isEmptyString = typeof v === 'string' && v.trim().length === 0;
    const isMissing = v == null || isEmptyString || isEmptyArray;
    if (isMissing) errors.push(`${String(f.key)} is required for type ${schema.typeLabel}`);
  }

  // extra semantic rules (lightweight; heavy validation lives in Core)
  if (ht === 'Custom') {
    if (!draft.validator && !draft.context_provider) {
      errors.push('custom handler requires validator or context_provider');
    }
  }
  if (ht === 'Synthetic') {
    if (!draft.reference_handler && !draft.actual_type) {
      errors.push('synthetic handler requires reference_handler or actual_type');
    }
  }

  return { ok: errors.length === 0, errors };
}


