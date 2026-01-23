import { useMemo } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { ScrollArea } from '../ui/scroll-area';
import { Button } from '../ui/button';
import { HandlerEditorBody } from './HandlerEditorDialog';
import { requestHandlersList } from '../../host/webview2Bridge';

type Props = {
  mode: 'new' | 'edit';
};

export function HandlerEditorPage({ mode }: Props) {
  const navigate = useNavigate();
  const params = useParams();

  const handlerName = useMemo(() => {
    if (mode !== 'edit') return undefined;
    const raw = params.name ?? '';
    return raw ? decodeURIComponent(raw) : undefined;
  }, [mode, params.name]);

  const onDone = () => {
    requestHandlersList();
  };

  const title = mode === 'new' ? 'New Handler' : `Edit Handler: ${handlerName ?? ''}`;

  if (mode === 'edit' && !handlerName) {
    return (
      <ScrollArea className="h-full">
        <div className="p-6 max-w-7xl mx-auto space-y-4">
          <h1 className="text-[28px] font-semibold">Edit Handler</h1>
          <p className="text-sm text-muted-foreground">No handler name provided.</p>
          <Button variant="outline" onClick={() => navigate('/handlers')}>
            Back to handlers
          </Button>
        </div>
      </ScrollArea>
    );
  }

  return (
    <ScrollArea className="h-full">
      <div className="p-6 max-w-7xl mx-auto space-y-6">
        <div className="space-y-1">
          <div className="text-[28px] font-semibold">{title}</div>
          <p className="text-sm text-muted-foreground">
            Wizard for guided editing, or Advanced JSON for full control. Validation happens on save (host-side).
          </p>
        </div>
        <HandlerEditorBody
          open
          mode={mode}
          handlerName={handlerName}
          onCancel={() => navigate('/handlers')}
          onSaved={onDone}
          onDeleted={onDone}
        />
      </div>
    </ScrollArea>
  );
}

