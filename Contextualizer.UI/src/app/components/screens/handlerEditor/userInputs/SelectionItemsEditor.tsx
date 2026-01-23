import { Input } from '../../../ui/input';
import { Button } from '../../../ui/button';

export function SelectionItemsEditor({
  items,
  onChange,
}: {
  items: Array<{ value: string; display: string }>;
  onChange: (items: Array<{ value: string; display: string }>) => void;
}) {
  const updateItem = (index: number, next: { value: string; display: string }) => {
    const clone = items.slice();
    clone[index] = next;
    onChange(clone);
  };

  const removeItem = (index: number) => {
    const clone = items.slice();
    clone.splice(index, 1);
    onChange(clone);
  };

  return (
    <div className="space-y-2">
      {items.map((item, index) => (
        <div key={`selection-item-${index}`} className="grid grid-cols-1 md:grid-cols-5 gap-2 items-center">
          <Input
            className="md:col-span-2"
            placeholder="value"
            value={item.value ?? ''}
            onChange={(e) => updateItem(index, { ...item, value: e.target.value })}
          />
          <Input
            className="md:col-span-2"
            placeholder="display"
            value={item.display ?? ''}
            onChange={(e) => updateItem(index, { ...item, display: e.target.value })}
          />
          <Button variant="ghost" size="sm" onClick={() => removeItem(index)}>
            Remove
          </Button>
        </div>
      ))}
      <Button variant="outline" size="sm" onClick={() => onChange([...items, { value: '', display: '' }])}>
        Add Selection Item
      </Button>
    </div>
  );
}
