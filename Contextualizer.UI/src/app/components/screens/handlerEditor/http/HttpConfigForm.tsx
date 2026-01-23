import React, { useState } from 'react';
import { Label } from '../../../ui/label';
import { Input } from '../../../ui/input';
import { Textarea } from '../../../ui/textarea';
import { Switch } from '../../../ui/switch';
import { Section } from '../shared/Section';
import { ToggleField } from '../shared/ToggleField';
import { KeyValueEditor } from '../shared/KeyValueEditor';
import { JsonEditorField } from '../shared/JsonEditorField';
import type { HttpConfigDraft } from '../types';
import { toLines, fromLines, toNumberLines, fromNumberLines } from '../helpers';

type HttpConfigFormProps = {
  http: HttpConfigDraft;
  updateHttpSection: (section: keyof HttpConfigDraft, patch: Record<string, unknown>) => void;
};

export function HttpConfigForm({ http, updateHttpSection }: HttpConfigFormProps) {
  // Only show HTTP config if there's actual content, not just an empty object
  const hasHttpContent = React.useMemo(() => {
    if (!http || Object.keys(http).length === 0) return false;
    // Check if any section has actual non-empty values
    return Object.values(http).some((v) => {
      if (v === undefined || v === null) return false;
      if (Array.isArray(v)) return v.length > 0;
      if (typeof v === 'object') {
        const keys = Object.keys(v);
        if (keys.length === 0) return false;
        // Check if any nested value is not empty
        return Object.values(v).some((nv) => {
          if (nv === undefined || nv === null || nv === '') return false;
          if (Array.isArray(nv)) return nv.length > 0;
          if (typeof nv === 'object') return Object.keys(nv).length > 0;
          return true;
        });
      }
      return true;
    });
  }, [http]);
  const [showHttpConfig, setShowHttpConfig] = useState(hasHttpContent);

  return (
    <Section title="HTTP (Advanced)">
      <div className="flex items-center justify-between p-3 border rounded-md">
        <div>
          <Label className="font-semibold">HTTP Config</Label>
          <p className="text-xs text-muted-foreground">Optional advanced HTTP settings (http)</p>
        </div>
        <Switch checked={showHttpConfig} onCheckedChange={setShowHttpConfig} />
      </div>
      {showHttpConfig && (
        <div className="space-y-4">
          <Section title="Request">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              <div className="space-y-2">
                <Label>URL</Label>
                <Input
                  value={http.request?.url ?? ''}
                  onChange={(e) => updateHttpSection('request', { url: e.target.value })}
                />
              </div>
              <div className="space-y-2">
                <Label>Method</Label>
                <Input
                  value={http.request?.method ?? ''}
                  onChange={(e) => updateHttpSection('request', { method: e.target.value })}
                  placeholder="GET/POST/PUT/PATCH/DELETE"
                />
              </div>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              <div className="space-y-2">
                <Label>Content Type</Label>
                <Input
                  value={http.request?.content_type ?? ''}
                  onChange={(e) => updateHttpSection('request', { content_type: e.target.value })}
                  placeholder="application/json"
                />
              </div>
              <div className="space-y-2">
                <Label>Charset</Label>
                <Input
                  value={http.request?.charset ?? ''}
                  onChange={(e) => updateHttpSection('request', { charset: e.target.value })}
                  placeholder="utf-8"
                />
              </div>
            </div>
            <KeyValueEditor
              label="Headers"
              help="HTTP headers dictionary"
              value={http.request?.headers ?? {}}
              onChange={(value) => updateHttpSection('request', { headers: value })}
            />
            <KeyValueEditor
              label="Query Params"
              help="Querystring key/value pairs"
              value={http.request?.query ?? {}}
              onChange={(value) => updateHttpSection('request', { query: value })}
            />
            <JsonEditorField
              label="Body (JSON)"
              value={http.request?.body ?? {}}
              onChange={(value) => updateHttpSection('request', { body: value })}
              placeholder='{"id":"$(id)"}'
            />
            <div className="space-y-2">
              <Label>Body (Text)</Label>
              <Textarea
                value={http.request?.body_text ?? ''}
                onChange={(e) => updateHttpSection('request', { body_text: e.target.value })}
                className="min-h-[100px] font-mono text-xs"
              />
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              <ToggleField
                label="Allow Body for GET"
                checked={http.request?.allow_body_for_get === true}
                onChange={(checked) => updateHttpSection('request', { allow_body_for_get: checked })}
              />
              <ToggleField
                label="Allow Body for DELETE"
                checked={http.request?.allow_body_for_delete === true}
                onChange={(checked) => updateHttpSection('request', { allow_body_for_delete: checked })}
              />
            </div>
          </Section>

          <Section title="Auth">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              <div className="space-y-2">
                <Label>Type</Label>
                <Input
                  value={http.auth?.type ?? ''}
                  onChange={(e) => updateHttpSection('auth', { type: e.target.value })}
                  placeholder="basic|bearer|oauth2|api_key|custom"
                />
              </div>
              <div className="space-y-2">
                <Label>Token</Label>
                <Input value={http.auth?.token ?? ''} onChange={(e) => updateHttpSection('auth', { token: e.target.value })} />
              </div>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              <div className="space-y-2">
                <Label>Username</Label>
                <Input value={http.auth?.username ?? ''} onChange={(e) => updateHttpSection('auth', { username: e.target.value })} />
              </div>
              <div className="space-y-2">
                <Label>Password</Label>
                <Input
                  type="password"
                  value={http.auth?.password ?? ''}
                  onChange={(e) => updateHttpSection('auth', { password: e.target.value })}
                />
              </div>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
              <div className="space-y-2">
                <Label>Header Name</Label>
                <Input
                  value={http.auth?.header_name ?? ''}
                  onChange={(e) => updateHttpSection('auth', { header_name: e.target.value })}
                />
              </div>
              <div className="space-y-2">
                <Label>Query Name</Label>
                <Input
                  value={http.auth?.query_name ?? ''}
                  onChange={(e) => updateHttpSection('auth', { query_name: e.target.value })}
                />
              </div>
              <div className="space-y-2">
                <Label>Token Prefix</Label>
                <Input
                  value={http.auth?.token_prefix ?? ''}
                  onChange={(e) => updateHttpSection('auth', { token_prefix: e.target.value })}
                  placeholder="Bearer"
                />
              </div>
            </div>
          </Section>

          <Section title="Proxy">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              <div className="space-y-2">
                <Label>Proxy URL</Label>
                <Input value={http.proxy?.url ?? ''} onChange={(e) => updateHttpSection('proxy', { url: e.target.value })} />
              </div>
              <div className="space-y-2">
                <Label>Bypass List</Label>
                <Textarea
                  value={toLines(http.proxy?.bypass)}
                  onChange={(e) => updateHttpSection('proxy', { bypass: fromLines(e.target.value) })}
                  className="min-h-[80px]"
                  placeholder="*.local&#10;10.0.0.0/8"
                />
              </div>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              <div className="space-y-2">
                <Label>Username</Label>
                <Input
                  value={http.proxy?.username ?? ''}
                  onChange={(e) => updateHttpSection('proxy', { username: e.target.value })}
                />
              </div>
              <div className="space-y-2">
                <Label>Password</Label>
                <Input
                  type="password"
                  value={http.proxy?.password ?? ''}
                  onChange={(e) => updateHttpSection('proxy', { password: e.target.value })}
                />
              </div>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              <ToggleField
                label="Use System Proxy"
                checked={http.proxy?.use_system_proxy === true}
                onChange={(checked) => updateHttpSection('proxy', { use_system_proxy: checked })}
              />
              <ToggleField
                label="Use Default Credentials"
                checked={http.proxy?.use_default_credentials === true}
                onChange={(checked) => updateHttpSection('proxy', { use_default_credentials: checked })}
              />
            </div>
          </Section>

          <Section title="TLS">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              <ToggleField
                label="Allow Invalid Certs"
                checked={http.tls?.allow_invalid_cert === true}
                onChange={(checked) => updateHttpSection('tls', { allow_invalid_cert: checked })}
              />
              <div className="space-y-2">
                <Label>Min TLS</Label>
                <Input value={http.tls?.min_tls ?? ''} onChange={(e) => updateHttpSection('tls', { min_tls: e.target.value })} placeholder="1.2" />
              </div>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              <div className="space-y-2">
                <Label>Client Cert Path</Label>
                <Input
                  value={http.tls?.client_cert_path ?? ''}
                  onChange={(e) => updateHttpSection('tls', { client_cert_path: e.target.value })}
                />
              </div>
              <div className="space-y-2">
                <Label>Client Cert Password</Label>
                <Input
                  type="password"
                  value={http.tls?.client_cert_password ?? ''}
                  onChange={(e) => updateHttpSection('tls', { client_cert_password: e.target.value })}
                />
              </div>
            </div>
          </Section>

          <Section title="Timeouts">
            <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
              <div className="space-y-2">
                <Label>Connect (seconds)</Label>
                <Input
                  type="number"
                  value={http.timeouts?.connect_seconds ?? ''}
                  onChange={(e) =>
                    updateHttpSection('timeouts', {
                      connect_seconds: e.target.value === '' ? undefined : Number(e.target.value),
                    })
                  }
                />
              </div>
              <div className="space-y-2">
                <Label>Read (seconds)</Label>
                <Input
                  type="number"
                  value={http.timeouts?.read_seconds ?? ''}
                  onChange={(e) =>
                    updateHttpSection('timeouts', {
                      read_seconds: e.target.value === '' ? undefined : Number(e.target.value),
                    })
                  }
                />
              </div>
              <div className="space-y-2">
                <Label>Overall (seconds)</Label>
                <Input
                  type="number"
                  value={http.timeouts?.overall_seconds ?? ''}
                  onChange={(e) =>
                    updateHttpSection('timeouts', {
                      overall_seconds: e.target.value === '' ? undefined : Number(e.target.value),
                    })
                  }
                />
              </div>
            </div>
          </Section>

          <Section title="Retry">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              <ToggleField
                label="Retry Enabled"
                checked={http.retry?.enabled === true}
                onChange={(checked) => updateHttpSection('retry', { enabled: checked })}
              />
              <ToggleField
                label="Jitter"
                checked={http.retry?.jitter !== false}
                onChange={(checked) => updateHttpSection('retry', { jitter: checked })}
              />
            </div>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
              <div className="space-y-2">
                <Label>Max Attempts</Label>
                <Input
                  type="number"
                  value={http.retry?.max_attempts ?? ''}
                  onChange={(e) =>
                    updateHttpSection('retry', {
                      max_attempts: e.target.value === '' ? undefined : Number(e.target.value),
                    })
                  }
                />
              </div>
              <div className="space-y-2">
                <Label>Base Delay (ms)</Label>
                <Input
                  type="number"
                  value={http.retry?.base_delay_ms ?? ''}
                  onChange={(e) =>
                    updateHttpSection('retry', {
                      base_delay_ms: e.target.value === '' ? undefined : Number(e.target.value),
                    })
                  }
                />
              </div>
              <div className="space-y-2">
                <Label>Max Delay (ms)</Label>
                <Input
                  type="number"
                  value={http.retry?.max_delay_ms ?? ''}
                  onChange={(e) =>
                    updateHttpSection('retry', {
                      max_delay_ms: e.target.value === '' ? undefined : Number(e.target.value),
                    })
                  }
                />
              </div>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              <div className="space-y-2">
                <Label>Retry on Status</Label>
                <Textarea
                  value={toNumberLines(http.retry?.retry_on_status)}
                  onChange={(e) => updateHttpSection('retry', { retry_on_status: fromNumberLines(e.target.value) })}
                  className="min-h-[80px]"
                  placeholder="408&#10;429&#10;500"
                />
              </div>
              <div className="space-y-2">
                <Label>Retry on Exceptions</Label>
                <Textarea
                  value={toLines(http.retry?.retry_on_exceptions)}
                  onChange={(e) => updateHttpSection('retry', { retry_on_exceptions: fromLines(e.target.value) })}
                  className="min-h-[80px]"
                  placeholder="HttpRequestException"
                />
              </div>
            </div>
          </Section>

          <Section title="Pagination">
            <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
              <div className="space-y-2">
                <Label>Type</Label>
                <Input
                  value={http.pagination?.type ?? ''}
                  onChange={(e) => updateHttpSection('pagination', { type: e.target.value })}
                  placeholder="cursor|offset|page"
                />
              </div>
              <div className="space-y-2">
                <Label>Cursor Path</Label>
                <Input
                  value={http.pagination?.cursor_path ?? ''}
                  onChange={(e) => updateHttpSection('pagination', { cursor_path: e.target.value })}
                />
              </div>
              <div className="space-y-2">
                <Label>Next Param</Label>
                <Input
                  value={http.pagination?.next_param ?? ''}
                  onChange={(e) => updateHttpSection('pagination', { next_param: e.target.value })}
                />
              </div>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
              <div className="space-y-2">
                <Label>Limit Param</Label>
                <Input
                  value={http.pagination?.limit_param ?? ''}
                  onChange={(e) => updateHttpSection('pagination', { limit_param: e.target.value })}
                />
              </div>
              <div className="space-y-2">
                <Label>Offset Param</Label>
                <Input
                  value={http.pagination?.offset_param ?? ''}
                  onChange={(e) => updateHttpSection('pagination', { offset_param: e.target.value })}
                />
              </div>
              <div className="space-y-2">
                <Label>Page Param</Label>
                <Input
                  value={http.pagination?.page_param ?? ''}
                  onChange={(e) => updateHttpSection('pagination', { page_param: e.target.value })}
                />
              </div>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-4 gap-3">
              <div className="space-y-2">
                <Label>Page Size</Label>
                <Input
                  type="number"
                  value={http.pagination?.page_size ?? ''}
                  onChange={(e) =>
                    updateHttpSection('pagination', {
                      page_size: e.target.value === '' ? undefined : Number(e.target.value),
                    })
                  }
                />
              </div>
              <div className="space-y-2">
                <Label>Max Pages</Label>
                <Input
                  type="number"
                  value={http.pagination?.max_pages ?? ''}
                  onChange={(e) =>
                    updateHttpSection('pagination', {
                      max_pages: e.target.value === '' ? undefined : Number(e.target.value),
                    })
                  }
                />
              </div>
              <div className="space-y-2">
                <Label>Start Page</Label>
                <Input
                  type="number"
                  value={http.pagination?.start_page ?? ''}
                  onChange={(e) =>
                    updateHttpSection('pagination', {
                      start_page: e.target.value === '' ? undefined : Number(e.target.value),
                    })
                  }
                />
              </div>
              <div className="space-y-2">
                <Label>Start Offset</Label>
                <Input
                  type="number"
                  value={http.pagination?.start_offset ?? ''}
                  onChange={(e) =>
                    updateHttpSection('pagination', {
                      start_offset: e.target.value === '' ? undefined : Number(e.target.value),
                    })
                  }
                />
              </div>
            </div>
          </Section>

          <Section title="Response">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              <div className="space-y-2">
                <Label>Expect</Label>
                <Input
                  value={http.response?.expect ?? ''}
                  onChange={(e) => updateHttpSection('response', { expect: e.target.value })}
                  placeholder="json|text|binary"
                />
              </div>
              <div className="space-y-2">
                <Label>Max Bytes</Label>
                <Input
                  type="number"
                  value={http.response?.max_bytes ?? ''}
                  onChange={(e) =>
                    updateHttpSection('response', {
                      max_bytes: e.target.value === '' ? undefined : Number(e.target.value),
                    })
                  }
                />
              </div>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              <ToggleField
                label="Flatten JSON"
                checked={http.response?.flatten_json !== false}
                onChange={(checked) => updateHttpSection('response', { flatten_json: checked })}
              />
              <ToggleField
                label="Include Headers"
                checked={http.response?.include_headers === true}
                onChange={(checked) => updateHttpSection('response', { include_headers: checked })}
              />
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              <div className="space-y-2">
                <Label>Flatten Prefix</Label>
                <Input
                  value={http.response?.flatten_prefix ?? ''}
                  onChange={(e) => updateHttpSection('response', { flatten_prefix: e.target.value })}
                />
              </div>
              <div className="space-y-2">
                <Label>Header Prefix</Label>
                <Input
                  value={http.response?.header_prefix ?? ''}
                  onChange={(e) => updateHttpSection('response', { header_prefix: e.target.value })}
                  placeholder="Header."
                />
              </div>
            </div>
          </Section>

          <Section title="Output">
            <KeyValueEditor
              label="Mappings"
              help="context_key -> json_path"
              value={http.output?.mappings ?? {}}
              onChange={(value) => updateHttpSection('output', { mappings: value })}
            />
            <KeyValueEditor
              label="Header Mappings"
              help="context_key -> header_name"
              value={http.output?.header_mappings ?? {}}
              onChange={(value) => updateHttpSection('output', { header_mappings: value })}
            />
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              <ToggleField
                label="Include Raw Body"
                checked={http.output?.include_raw_body !== false}
                onChange={(checked) => updateHttpSection('output', { include_raw_body: checked })}
              />
              <div className="space-y-2">
                <Label>Raw Body Key</Label>
                <Input
                  value={http.output?.raw_body_key ?? ''}
                  onChange={(e) => updateHttpSection('output', { raw_body_key: e.target.value })}
                  placeholder="RawResponse"
                />
              </div>
            </div>
          </Section>
        </div>
      )}
    </Section>
  );
}
