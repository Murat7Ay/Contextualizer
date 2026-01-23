import { Button } from '../../../ui/button';
import { Input } from '../../../ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../../../ui/select';
import { operatorOptions } from '../constants';
import { createEmptyCondition } from '../helpers';
import type { ConditionDraft } from '../types';

export function ConditionEditor({
  value,
  onChange,
}: {
  value?: ConditionDraft;
  onChange: (value?: ConditionDraft) => void;
}) {
  if (!value) {
    return (
      <Button variant="outline" size="sm" onClick={() => onChange(createEmptyCondition())}>
        Add Condition
      </Button>
    );
  }

  const isGroup = value.operator === 'and' || value.operator === 'or';
  const isUnary = value.operator === 'is_empty' || value.operator === 'is_not_empty';
  const conditions = value.conditions ?? [];

  return (
    <div className="space-y-2">
      <div className="grid grid-cols-1 md:grid-cols-3 gap-2 items-center">
        <Select
          value={value.operator}
          onValueChange={(op) =>
            onChange(
              op === 'and' || op === 'or'
                ? { operator: op, conditions: conditions.length ? conditions : [createEmptyCondition()] }
                : { operator: op, field: '', value: '' }
            )
          }
        >
          <SelectTrigger>
            <SelectValue placeholder="Operator" />
          </SelectTrigger>
          <SelectContent>
            {operatorOptions.map((op) => (
              <SelectItem key={op.value} value={op.value}>
                {op.label}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        {!isGroup && (
          <Input
            placeholder="Field"
            value={value.field ?? ''}
            onChange={(e) => onChange({ ...value, field: e.target.value })}
          />
        )}
        {!isGroup && !isUnary && (
          <Input
            placeholder="Value"
            value={value.value ?? ''}
            onChange={(e) => onChange({ ...value, value: e.target.value })}
          />
        )}
      </div>
      {isGroup && (
        <div className="space-y-2 pl-3 border-l">
          {conditions.map((cond, index) => (
            <div key={`cond-${index}`} className="space-y-2">
              <ConditionEditor
                value={cond}
                onChange={(next) => {
                  const clone = conditions.slice();
                  if (next) clone[index] = next;
                  onChange({ ...value, conditions: clone });
                }}
              />
              <Button
                variant="ghost"
                size="sm"
                onClick={() => {
                  const clone = conditions.slice();
                  clone.splice(index, 1);
                  onChange({ ...value, conditions: clone });
                }}
              >
                Remove Condition
              </Button>
            </div>
          ))}
          <Button
            variant="outline"
            size="sm"
            onClick={() => onChange({ ...value, conditions: [...conditions, createEmptyCondition()] })}
          >
            Add Subcondition
          </Button>
        </div>
      )}
      <Button variant="ghost" size="sm" onClick={() => onChange(undefined)}>
        Clear Condition
      </Button>
    </div>
  );
}
