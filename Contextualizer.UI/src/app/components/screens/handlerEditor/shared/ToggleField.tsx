import { Label } from '../../../ui/label';
import { Switch } from '../../../ui/switch';

export function ToggleField({ label, checked, onChange }: { label: string; checked: boolean; onChange: (checked: boolean) => void }) {
  return (
    <div className="flex items-center justify-between p-3 border rounded-md">
      <Label className="font-semibold">{label}</Label>
      <Switch checked={checked} onCheckedChange={onChange} />
    </div>
  );
}
