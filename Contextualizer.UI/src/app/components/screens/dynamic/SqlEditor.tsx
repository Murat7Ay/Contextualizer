import { useMemo, useState } from 'react';
import { Button } from '../../ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '../../ui/card';
import { Textarea } from '../../ui/textarea';
import { useActivityLogStore } from '../../../stores/activityLogStore';
import { Copy } from 'lucide-react';

export function SqlEditor({ initialSql, title }: { initialSql: string; title?: string }) {
  const addLog = useActivityLogStore((s) => s.addLog);
  const [sql, setSql] = useState(initialSql);

  const normalized = useMemo(() => (sql ?? '').replace(/\r\n/g, '\n'), [sql]);

  const copy = async () => {
    try {
      await navigator.clipboard.writeText(normalized);
      addLog('success', 'Copied SQL to clipboard');
    } catch (e) {
      addLog('error', 'Failed to copy SQL', e instanceof Error ? e.message : String(e));
    }
  };

  return (
    <Card>
      <CardHeader className="pb-3 flex flex-row items-center justify-between">
        <CardTitle className="text-base">{title ?? 'SQL Editor'}</CardTitle>
        <Button variant="outline" size="sm" onClick={copy}>
          <Copy className="h-4 w-4 mr-2" />
          Copy
        </Button>
      </CardHeader>
      <CardContent className="space-y-3">
        <Textarea
          value={sql}
          onChange={(e) => setSql(e.target.value)}
          className="min-h-[60vh] font-mono text-sm"
        />
        <p className="text-xs text-muted-foreground">
          Editing is local for now (host sync can be added later via WebView2 messages).
        </p>
      </CardContent>
    </Card>
  );
}


