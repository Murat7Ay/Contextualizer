import { Alert, AlertDescription, AlertTitle } from '../../ui/alert';
import { Button } from '../../ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '../../ui/card';
import { useActivityLogStore } from '../../../stores/activityLogStore';

export function JsonViewer({ jsonText, title }: { jsonText: string; title?: string }) {
  const addLog = useActivityLogStore((s) => s.addLog);

  let pretty = jsonText;
  let error: string | null = null;
  try {
    const parsed = JSON.parse(jsonText);
    pretty = JSON.stringify(parsed, null, 2);
  } catch (e) {
    error = e instanceof Error ? e.message : 'Invalid JSON';
  }

  const copy = async () => {
    try {
      await navigator.clipboard.writeText(pretty);
      addLog('success', 'Copied JSON to clipboard');
    } catch (e) {
      addLog('error', 'Failed to copy JSON', e instanceof Error ? e.message : String(e));
    }
  };

  return (
    <Card>
      <CardHeader className="pb-3 flex flex-row items-center justify-between">
        <CardTitle className="text-base">{title ?? 'JSON'}</CardTitle>
        <Button variant="outline" size="sm" onClick={copy}>
          Copy
        </Button>
      </CardHeader>
      <CardContent className="space-y-3">
        {error && (
          <Alert variant="destructive">
            <AlertTitle>Invalid JSON</AlertTitle>
            <AlertDescription>{error}</AlertDescription>
          </Alert>
        )}
        <pre className="text-xs p-3 bg-muted rounded border overflow-x-auto whitespace-pre">
          {pretty}
        </pre>
      </CardContent>
    </Card>
  );
}


