import type { ConfigActionDraft, ConditionDraft, HandlerConfigDraft, UserInputDraft } from '../../../lib/handlerSchemas';

export type Props = {
  open: boolean;
  mode: 'new' | 'edit';
  handlerName?: string;
  onOpenChange: (open: boolean) => void;
  onSaved?: () => void;
  onDeleted?: () => void;
};

export type EditorProps = {
  open: boolean;
  mode: 'new' | 'edit';
  handlerName?: string;
  onCancel: () => void;
  onSaved?: () => void;
  onDeleted?: () => void;
};

export type HostHandlerGetMessage =
  | { type: 'handler_get'; name: string; ok: true; handlerConfig: unknown }
  | { type: 'handler_get'; name: string; ok: false; error: string };

export type HostHandlerOpMessage =
  | { type: 'handler_create_result'; ok: true; name: string }
  | { type: 'handler_create_result'; ok: false; error: string }
  | { type: 'handler_update_result'; ok: true; name: string; updatedFields?: string[] }
  | { type: 'handler_update_result'; ok: false; error: string }
  | { type: 'handler_delete_result'; ok: true; name: string }
  | { type: 'handler_delete_result'; ok: false; error: string };

export type HostHandlersListMessage = {
  type: 'handlers_list';
  handlers: Array<{ name: string }>;
};

export type HostPluginsListMessage = {
  type: 'plugins_list';
  handlerTypes: string[];
  actions: string[];
  validators: string[];
  contextProviders: string[];
};

export type HttpConfigDraft = {
  request?: {
    url?: string;
    method?: string;
    headers?: Record<string, string>;
    query?: Record<string, string>;
    body?: unknown;
    body_text?: string;
    content_type?: string;
    charset?: string;
    allow_body_for_get?: boolean;
    allow_body_for_delete?: boolean;
  };
  auth?: {
    type?: string;
    token?: string;
    username?: string;
    password?: string;
    header_name?: string;
    query_name?: string;
    token_prefix?: string;
  };
  proxy?: {
    url?: string;
    username?: string;
    password?: string;
    bypass?: string[];
    use_system_proxy?: boolean;
    use_default_credentials?: boolean;
  };
  tls?: {
    allow_invalid_cert?: boolean;
    min_tls?: string;
    client_cert_path?: string;
    client_cert_password?: string;
  };
  timeouts?: {
    connect_seconds?: number;
    read_seconds?: number;
    overall_seconds?: number;
  };
  retry?: {
    enabled?: boolean;
    max_attempts?: number;
    base_delay_ms?: number;
    max_delay_ms?: number;
    jitter?: boolean;
    retry_on_status?: number[];
    retry_on_exceptions?: string[];
  };
  pagination?: {
    type?: string;
    cursor_path?: string;
    next_param?: string;
    limit_param?: string;
    offset_param?: string;
    page_param?: string;
    page_size?: number;
    max_pages?: number;
    start_page?: number;
    start_offset?: number;
  };
  response?: {
    expect?: string;
    flatten_json?: boolean;
    flatten_prefix?: string;
    max_bytes?: number;
    include_headers?: boolean;
    header_prefix?: string;
  };
  output?: {
    mappings?: Record<string, string>;
    header_mappings?: Record<string, string>;
    include_raw_body?: boolean;
    raw_body_key?: string;
  };
};

export type { ConfigActionDraft, ConditionDraft, HandlerConfigDraft, UserInputDraft };

export type PublishPackageDraft = {
  version: string;
  author: string;
  tags: string[];
  description?: string;
  metadata?: Record<string, string>;
  template_user_inputs?: UserInputDraft[];
};

export type PublishDialogProps = {
  open: boolean;
  handlerName: string;
  handlerJson: unknown;
  handlerDescription?: string;
  onOpenChange: (open: boolean) => void;
  onPublished?: () => void;
};
