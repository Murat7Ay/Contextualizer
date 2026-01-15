import { useMemo, useState } from 'react';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '../ui/dialog';
import { Label } from '../ui/label';
import { Input } from '../ui/input';
import { Textarea } from '../ui/textarea';
import { Button } from '../ui/button';
import { useActivityLogStore } from '../../stores/activityLogStore';
import { publishExchangePackage } from '../../host/webview2Bridge';

type Props = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
};

function toId(name: string): string {
  return (name ?? '')
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .slice(0, 80);
}

export function ExchangePublishDialog({ open, onOpenChange }: Props) {
  const addLog = useActivityLogStore((s) => s.addLog);

  const [name, setName] = useState('My Template');
  const [version, setVersion] = useState('1.0.0');
  const [author, setAuthor] = useState('Me');
  const [description, setDescription] = useState('Template description');
  const [tags, setTags] = useState('template');
  const [handlerJson, setHandlerJson] = useState(
    JSON.stringify(
      {
        name: 'Example Handler',
        type: 'manual',
        description: 'Created from marketplace template',
        actions: [],
      },
      null,
      2,
    ),
  );

  const id = useMemo(() => toId(name) || `pkg-${Date.now()}`, [name]);

  const publish = () => {
    try {
      const hj = JSON.parse(handlerJson);
      const pkg = {
        id,
        name,
        version,
        author,
        description,
        tags: tags
          .split(',')
          .map((t) => t.trim())
          .filter(Boolean),
        dependencies: [],
        handlerJson: hj,
        template_user_inputs: [],
        metadata: {},
      };

      publishExchangePackage(pkg);
      addLog('info', `Publishing package '${id}'…`);
      onOpenChange(false);
    } catch (e: any) {
      addLog('error', 'Invalid JSON', e?.message ?? String(e));
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-3xl">
        <DialogHeader>
          <DialogTitle>Publish to Marketplace (File Exchange)</DialogTitle>
          <DialogDescription>
            Creates a HandlerPackage JSON and writes it into the exchange directory via the WPF host.
          </DialogDescription>
        </DialogHeader>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
          <div className="space-y-2">
            <Label>Name</Label>
            <Input value={name} onChange={(e) => setName(e.target.value)} />
          </div>
          <div className="space-y-2">
            <Label>Id</Label>
            <Input value={id} readOnly />
          </div>
          <div className="space-y-2">
            <Label>Version</Label>
            <Input value={version} onChange={(e) => setVersion(e.target.value)} />
          </div>
          <div className="space-y-2">
            <Label>Author</Label>
            <Input value={author} onChange={(e) => setAuthor(e.target.value)} />
          </div>
        </div>

        <div className="space-y-2">
          <Label>Description</Label>
          <Input value={description} onChange={(e) => setDescription(e.target.value)} />
        </div>

        <div className="space-y-2">
          <Label>Tags (comma separated)</Label>
          <Input value={tags} onChange={(e) => setTags(e.target.value)} />
        </div>

        <div className="space-y-2">
          <Label>handlerJson</Label>
          <Textarea value={handlerJson} onChange={(e) => setHandlerJson(e.target.value)} className="min-h-[260px] font-mono text-xs" />
        </div>

        <div className="flex justify-end gap-2">
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Cancel
          </Button>
          <Button onClick={publish}>Publish</Button>
        </div>
      </DialogContent>
    </Dialog>
  );
}


