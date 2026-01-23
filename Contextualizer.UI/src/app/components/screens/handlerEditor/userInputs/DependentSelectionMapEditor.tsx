import { useEffect, useRef, useState } from 'react';
import { Input } from '../../../ui/input';
import { Button } from '../../../ui/button';
import { LabelWithHelp } from '../shared/LabelWithHelp';
import { SelectionItemsEditor } from './SelectionItemsEditor';

export function DependentSelectionMapEditor({
  value,
  onChange,
}: {
  value: Record<string, { selection_items: Array<{ value: string; display: string }>; default_value?: string }>;
  onChange: (next: Record<string, { selection_items: Array<{ value: string; display: string }>; default_value?: string }>) => void;
}) {
  const [rows, setRows] = useState<
    Array<{
      id: string;
      key: string;
      selection_items: Array<{ value: string; display: string }>;
      default_value?: string;
    }>
  >([]);
  const lastEmittedRef = useRef<string>('');

  useEffect(() => {
    const serialized = JSON.stringify(value ?? {});
    if (serialized === lastEmittedRef.current) return;
    lastEmittedRef.current = serialized;
    const next = Object.entries(value ?? {}).map(([k, v]) => ({
      id: `${k}-${Math.random().toString(36).slice(2)}`,
      key: k,
      selection_items: v.selection_items ?? [],
      default_value: v.default_value ?? '',
    }));
    setRows(next);
  }, [value]);

  const emitChange = (nextRows: typeof rows) => {
    const next = nextRows.reduce<
      Record<string, { selection_items: Array<{ value: string; display: string }>; default_value?: string }>
    >((acc, row) => {
      const k = row.key.trim();
      if (k.length === 0) return acc;
      acc[k] = {
        selection_items: row.selection_items ?? [],
        default_value: row.default_value ?? '',
      };
      return acc;
    }, {});
    lastEmittedRef.current = JSON.stringify(next);
    onChange(next);
  };

  const updateRow = (id: string, patch: Partial<(typeof rows)[number]>) => {
    const nextRows = rows.map((row) => (row.id === id ? { ...row, ...patch } : row));
    setRows(nextRows);
    emitChange(nextRows);
  };

  const removeRow = (id: string) => {
    const nextRows = rows.filter((row) => row.id !== id);
    setRows(nextRows);
    emitChange(nextRows);
  };

  const addRow = () => {
    const nextRows = [
      ...rows,
      {
        id: `dep-${Date.now()}-${Math.random().toString(36).slice(2)}`,
        key: '',
        selection_items: [],
        default_value: '',
      },
    ];
    setRows(nextRows);
  };

  return (
    <div className="space-y-2">
      <LabelWithHelp
        label="Dependent Selection Item Map"
        help="Maps dependent_key value → selection_items + default_value."
      />
      {rows.length === 0 && <div className="text-xs text-muted-foreground">No dependent mappings</div>}
      {rows.map((row) => (
        <div key={row.id} className="p-3 border rounded-md space-y-2">
          <div className="grid grid-cols-1 md:grid-cols-5 gap-2 items-center">
            <Input
              className="md:col-span-2"
              placeholder="dependent value"
              value={row.key}
              onChange={(e) => updateRow(row.id, { key: e.target.value })}
            />
            <Input
              className="md:col-span-2"
              placeholder="default value"
              value={row.default_value ?? ''}
              onChange={(e) => updateRow(row.id, { default_value: e.target.value })}
            />
            <Button variant="ghost" size="sm" onClick={() => removeRow(row.id)}>
              Remove
            </Button>
          </div>
          <SelectionItemsEditor
            items={row.selection_items}
            onChange={(items) => updateRow(row.id, { selection_items: items })}
          />
        </div>
      ))}
      <Button variant="outline" size="sm" onClick={addRow}>
        Add Mapping
      </Button>
    </div>
  );
}
