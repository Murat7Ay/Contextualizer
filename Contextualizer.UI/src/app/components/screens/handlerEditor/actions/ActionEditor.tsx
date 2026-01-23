import { useMemo } from 'react';
import { Label } from '../../../ui/label';
import { Input } from '../../../ui/input';
import { Button } from '../../../ui/button';
import { ToggleField } from '../shared/ToggleField';
import { KeyValueEditor } from '../shared/KeyValueEditor';
import { Section } from '../shared/Section';
import { ConditionEditor } from './ConditionEditor';
import { UserInputsEditor } from '../userInputs';
import { ActionsEditor } from './ActionsEditor';
import type { ConfigActionDraft } from '../types';

export function ActionEditor({
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
