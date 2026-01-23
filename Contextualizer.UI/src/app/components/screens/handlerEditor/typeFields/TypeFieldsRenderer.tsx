import type { Dispatch, SetStateAction } from 'react';
import { Label } from '../../../ui/label';
import { Input } from '../../../ui/input';
import { Textarea } from '../../../ui/textarea';
import { Switch } from '../../../ui/switch';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../../../ui/select';
import { Button } from '../../../ui/button';
import { Section } from '../shared/Section';
import { LabelWithHelp } from '../shared/LabelWithHelp';
import { JsonEditorField } from '../shared/JsonEditorField';
import { UserInputEditor } from '../userInputs';
import { handlerTypeOptions, coerceToHandlerType, type HandlerType, type HandlerConfigDraft } from '../../../../lib/handlerSchemas';
import { dbConnectors, httpMethods } from '../constants';
import { toLines, fromLines, createEmptyUserInput } from '../helpers';

type TypeFieldsRendererProps = {
  handlerType: HandlerType;
  draft: HandlerConfigDraft;
  setDraft: Dispatch<SetStateAction<HandlerConfigDraft>>;
  handlerNames: string[];
  registeredHandlerTypes: string[];
  validatorNames: string[];
  contextProviderNames: string[];
  depth?: number;
};

export function TypeFieldsRenderer({
  handlerType,
  draft,
  setDraft,
  handlerNames,
  registeredHandlerTypes,
  validatorNames,
  contextProviderNames,
  depth = 0,
}: TypeFieldsRendererProps) {
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
              <TypeFieldsRenderer
                handlerType={coerceToHandlerType(draft.actual_type) as HandlerType}
                draft={draft}
                setDraft={setDraft}
                handlerNames={handlerNames}
                registeredHandlerTypes={registeredHandlerTypes}
                validatorNames={validatorNames}
                contextProviderNames={contextProviderNames}
                depth={depth + 1}
              />
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
              <TypeFieldsRenderer
                handlerType={coerceToHandlerType(draft.actual_type) as HandlerType}
                draft={draft}
                setDraft={setDraft}
                handlerNames={handlerNames}
                registeredHandlerTypes={registeredHandlerTypes}
                validatorNames={validatorNames}
                contextProviderNames={contextProviderNames}
                depth={depth + 1}
              />
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
