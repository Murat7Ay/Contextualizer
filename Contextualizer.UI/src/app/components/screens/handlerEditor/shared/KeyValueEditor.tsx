import { useEffect, useRef, useState } from 'react';
import { Input } from '../../../ui/input';
import { Button } from '../../../ui/button';
import { LabelWithHelp } from './LabelWithHelp';

export function KeyValueEditor({
  label,
  help,
  value,
  onChange,
}: {
  label: string;
  help: string;
  value: Record<string, string>;
  onChange: (next: Record<string, string>) => void;
}) {
  const [rows, setRows] = useState<Array<{ id: string; key: string; value: string }>>([]);
  const lastEmittedRef = useRef<string>('');

  useEffect(() => {
    const serialized = JSON.stringify(value ?? {});
    if (serialized === lastEmittedRef.current) {
      return;
    }
    lastEmittedRef.current = serialized;
    const next = Object.entries(value ?? {}).map(([k, v]) => ({
      id: `${k}-${Math.random().toString(36).slice(2)}`,
      key: k,
      value: v,
    }));
    setRows(next);
  }, [value]);

  const emitChange = (nextRows: Array<{ id: string; key: string; value: string }>) => {
    const next = nextRows.reduce<Record<string, string>>((acc, row) => {
      const k = row.key.trim();
      if (k.length === 0) return acc;
      acc[k] = row.value;
      return acc;
    }, {});
    lastEmittedRef.current = JSON.stringify(next);
    onChange(next);
  };

  const updateRow = (id: string, patch: Partial<{ key: string; value: string }>) => {
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
      { id: `row-${Date.now()}-${Math.random().toString(36).slice(2)}`, key: '', value: '' },
    ];
    setRows(nextRows);
  };

  return (
    <div className="space-y-2">
      <LabelWithHelp label={label} help={help} />
      {rows.length === 0 && <div className="text-xs text-muted-foreground">No entries</div>}
      {rows.map((row) => (
        <div key={row.id} className="grid grid-cols-1 md:grid-cols-5 gap-2 items-center">
          <Input
            className="md:col-span-2"
            placeholder="key"
            value={row.key}
            onChange={(e) => updateRow(row.id, { key: e.target.value })}
          />
          <Input
            className="md:col-span-2"
            placeholder="value"
            value={row.value}
            onChange={(e) => updateRow(row.id, { value: e.target.value })}
          />
          <Button variant="ghost" size="sm" onClick={() => removeRow(row.id)}>
            Remove
          </Button>
        </div>
      ))}
      <Button variant="outline" size="sm" onClick={addRow}>
        Add Pair
      </Button>
    </div>
  );
}
