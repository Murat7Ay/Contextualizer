import { Label } from '../../../ui/label';
import { Input } from '../../../ui/input';
import { Textarea } from '../../../ui/textarea';
import { Button } from '../../../ui/button';
import { ToggleField } from '../shared/ToggleField';
import { LabelWithHelp } from '../shared/LabelWithHelp';
import { SelectionItemsEditor } from './SelectionItemsEditor';
import { DependentSelectionMapEditor } from './DependentSelectionMapEditor';
import type { UserInputDraft } from '../types';
import { toLines, fromLines } from '../helpers';

export function UserInputEditor({
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
        <ToggleField label="Folder Picker" checked={value.is_folder_picker === true} onChange={(checked) => onChange({ ...value, is_folder_picker: checked })} />
        <ToggleField label="Multi Line" checked={value.is_multi_line === true} onChange={(checked) => onChange({ ...value, is_multi_line: checked })} />
      </div>
      <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
        <ToggleField label="Password" checked={value.is_password === true} onChange={(checked) => onChange({ ...value, is_password: checked })} />
        <ToggleField label="Date" checked={value.is_date === true} onChange={(checked) => onChange({ ...value, is_date: checked })} />
        <ToggleField label="Date Picker" checked={value.is_date_picker === true} onChange={(checked) => onChange({ ...value, is_date_picker: checked })} />
      </div>
      <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
        <ToggleField label="Time" checked={value.is_time === true} onChange={(checked) => onChange({ ...value, is_time: checked })} />
        <ToggleField label="Time Picker" checked={value.is_time_picker === true} onChange={(checked) => onChange({ ...value, is_time_picker: checked })} />
        <ToggleField label="Date Time" checked={value.is_date_time === true} onChange={(checked) => onChange({ ...value, is_date_time: checked })} />
      </div>
      <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
        <ToggleField label="DateTime Picker" checked={value.is_datetime_picker === true} onChange={(checked) => onChange({ ...value, is_datetime_picker: checked })} />
      </div>
      <div className="text-xs text-muted-foreground">
        Only one input type should be enabled at a time (list, file/folder, date/time/date-time, password, multi-line).
      </div>
      {value.is_file_picker && (
        <div className="space-y-2">
          <Label>File Extensions</Label>
          <Textarea
            value={toLines(value.file_extensions)}
            onChange={(e) => onChange({ ...value, file_extensions: fromLines(e.target.value) })}
            className="min-h-[80px]"
            placeholder=".txt\n.json"
          />
          <div className="text-xs text-muted-foreground">
            Optional. One extension per line (e.g. .txt). If empty, all files allowed.
          </div>
        </div>
      )}
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
