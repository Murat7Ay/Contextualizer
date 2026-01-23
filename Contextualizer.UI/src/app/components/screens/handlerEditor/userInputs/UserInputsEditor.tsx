import { Button } from '../../../ui/button';
import { UserInputEditor } from './UserInputEditor';
import type { UserInputDraft } from '../types';
import { createEmptyUserInput } from '../helpers';

export function UserInputsEditor({
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
          key={`user-input-${index}`}
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
