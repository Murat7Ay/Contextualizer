import { useEffect, useState } from 'react';
import { Label } from '../../../ui/label';
import { Textarea } from '../../../ui/textarea';
import { Button } from '../../../ui/button';

export function JsonEditorField({
  label,
  value,
  onChange,
  placeholder,
}: {
  label: string;
  value: unknown;
  onChange: (value: unknown) => void;
  placeholder?: string;
}) {
  const [text, setText] = useState<string>(() => (value ? JSON.stringify(value, null, 2) : ''));
  const [localError, setLocalError] = useState<string | null>(null);

  useEffect(() => {
    setText(value ? JSON.stringify(value, null, 2) : '');
  }, [value]);

  const apply = () => {
    try {
      const parsed = text.trim().length === 0 ? {} : JSON.parse(text);
      onChange(parsed);
      setLocalError(null);
    } catch (ex: any) {
      setLocalError(ex?.message ?? 'Invalid JSON');
    }
  };

  return (
    <div className="space-y-2">
      <Label>{label}</Label>
      <Textarea
        value={text}
        onChange={(e) => setText(e.target.value)}
        className="min-h-[100px] font-mono text-xs"
        placeholder={placeholder}
      />
      <div className="flex items-center justify-between text-xs text-muted-foreground">
        <Button variant="outline" size="sm" onClick={apply}>
          Apply
        </Button>
        {localError && <span className="text-red-600 dark:text-red-400">{localError}</span>}
      </div>
    </div>
  );
}
