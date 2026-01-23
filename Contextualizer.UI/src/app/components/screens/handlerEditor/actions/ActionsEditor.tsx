import { Button } from '../../../ui/button';
import { ActionEditor } from './ActionEditor';
import { createEmptyAction } from '../helpers';
import type { ConfigActionDraft } from '../types';

export function ActionsEditor({
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
          key={`action-${index}`}
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
