import { useMemo } from 'react';
import { Input } from '../../ui/input';
import { Textarea } from '../../ui/textarea';
import { Label } from '../../ui/label';
import { Button } from '../../ui/button';
import { Switch } from '../../ui/switch';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../../ui/select';
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
} from '../../ui/alert-dialog';
import { handlerTypeOptions, type HandlerConfigDraft, type HandlerType } from '../../../lib/handlerSchemas';
import { toLines, fromLines } from './helpers';
import { Section, LabelWithHelp, KeyValueEditor, JsonEditorField } from './shared';
import { UserInputsEditor } from './userInputs';
import { ActionsEditor } from './actions';
import { HttpConfigForm } from './http';
import { TypeFieldsRenderer } from './typeFields';
import { PublishSection } from './publish';
import type { HttpConfigDraft } from './types';

type Props = {
  mode: 'new' | 'edit';
  handlerName?: string;
  draft: HandlerConfigDraft;
  setDraft: React.Dispatch<React.SetStateAction<HandlerConfigDraft>>;
  currentHandlerType: HandlerType;
  handlerType: HandlerType;
  onChangeType: (t: HandlerType) => void;
  http: HttpConfigDraft;
  updateHttpSection: (section: keyof HttpConfigDraft, patch: Record<string, unknown>) => void;
  handlerNames: string[];
  actionNames: string[];
  validatorNames: string[];
  contextProviderNames: string[];
  registeredHandlerTypes: string[];
  wizardValidation: { ok: boolean; errors: string[] };
  isSaved: boolean;
  loading: boolean;
  deleteOpen: boolean;
  setDeleteOpen: (open: boolean) => void;
  onDelete: () => void;
  onSave: () => void;
  onCancel: () => void;
  onPublish: () => void;
};

export function HandlerEditorWizard({
  mode,
  handlerName,
  draft,
  setDraft,
  currentHandlerType,
  handlerType,
  onChangeType,
  http,
  updateHttpSection,
  handlerNames,
  actionNames,
  validatorNames,
  contextProviderNames,
  registeredHandlerTypes,
  wizardValidation,
  isSaved,
  loading,
  deleteOpen,
  setDeleteOpen,
  onDelete,
  onSave,
  onCancel,
  onPublish,
}: Props) {
  return (
    <div className="space-y-4">
      <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
        <div className="space-y-2">
          <Label>Type</Label>
          <Select value={currentHandlerType} onValueChange={(v) => onChangeType(v as HandlerType)} disabled={mode === 'edit'}>
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
          {mode === 'edit' && <p className="text-xs text-muted-foreground">Type cannot be changed from the editor.</p>}
        </div>

        <div className="space-y-2">
          <Label>Name</Label>
          <Input
            value={draft.name ?? ''}
            onChange={(e) => setDraft((d) => ({ ...d, name: e.target.value }))}
            disabled={mode === 'edit'}
          />
          {mode === 'edit' && <p className="text-xs text-muted-foreground">Name cannot be changed (identifier).</p>}
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
          <Switch checked={draft.enabled !== false} onCheckedChange={(checked) => setDraft((d) => ({ ...d, enabled: checked }))} />
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
        <UserInputsEditor inputs={draft.user_inputs ?? []} onChange={(inputs) => setDraft((d) => ({ ...d, user_inputs: inputs }))} />
      </Section>

      <Section title="Actions">
        <ActionsEditor actions={draft.actions ?? []} actionNames={actionNames} onChange={(actions) => setDraft((d) => ({ ...d, actions }))} />
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

      <PublishSection handlerName={draft.name ?? ''} isSaved={isSaved} onPublish={onPublish} />

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
                  <AlertDialogDescription>Delete handler '{handlerName}'? This cannot be undone.</AlertDialogDescription>
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
          <Button onClick={onSave} disabled={loading}>
            {loading ? 'Saving…' : 'Save'}
          </Button>
        </div>
      </div>
    </div>
  );
}
