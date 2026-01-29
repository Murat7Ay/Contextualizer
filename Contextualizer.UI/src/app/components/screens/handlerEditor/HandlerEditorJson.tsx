import { Button } from '../../ui/button';
import { Textarea } from '../../ui/textarea';
import type { HandlerConfigDraft, HandlerType } from '../../../lib/handlerSchemas';

type Props = {
  jsonText: string;
  setJsonText: (text: string) => void;
  loading: boolean;
  onApplyJsonToWizard: () => void;
  onUpdateJsonFromWizard: () => void;
  onSave: () => void;
  onCancel: () => void;
};

export function HandlerEditorJson({ jsonText, setJsonText, loading, onApplyJsonToWizard, onUpdateJsonFromWizard, onSave, onCancel }: Props) {
  return (
    <div className="space-y-3">
      <Textarea value={jsonText} onChange={(e) => setJsonText(e.target.value)} className="min-h-[320px] font-mono text-xs" />
      <div className="flex justify-between gap-2">
        <div className="flex gap-2">
          <Button variant="outline" onClick={onApplyJsonToWizard} disabled={loading}>
            Apply JSON to Wizard
          </Button>
          <Button variant="outline" onClick={onUpdateJsonFromWizard} disabled={loading}>
            Update JSON from Wizard
          </Button>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={onCancel} disabled={loading}>
            Cancel
          </Button>
          <Button onClick={onSave} disabled={loading}>
            {loading ? 'Saving…' : 'Save JSON'}
          </Button>
        </div>
      </div>
    </div>
  );
}
