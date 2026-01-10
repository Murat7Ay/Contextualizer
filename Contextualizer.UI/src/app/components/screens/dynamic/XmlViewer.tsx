import { Alert, AlertDescription, AlertTitle } from '../../ui/alert';
import { Button } from '../../ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '../../ui/card';
import { useActivityLogStore } from '../../../stores/activityLogStore';

function formatXml(xml: string): string {
  const trimmed = xml.trim();
  // Basic pretty-printer: insert newlines and indent based on tag boundaries.
  const reg = /(>)(<)(\/*)/g;
  const withNewlines = trimmed.replace(reg, '$1\n$2$3');
  const lines = withNewlines.split('\n');

  let pad = 0;
  const indent = (n: number) => '  '.repeat(Math.max(0, n));

  return lines
    .map((line) => {
      const l = line.trim();
      if (!l) return '';

      if (l.startsWith('<?') || l.startsWith('<!')) {
        return indent(pad) + l;
      }

      if (l.startsWith('</')) {
        pad = Math.max(0, pad - 1);
        return indent(pad) + l;
      }

      const isSelfClosing = l.endsWith('/>') || l.startsWith('<!') || l.startsWith('<?');
      const isClosingSameLine = l.includes('</') && l.startsWith('<') && !l.startsWith('</');

      const out = indent(pad) + l;
      if (!isSelfClosing && !isClosingSameLine && l.startsWith('<') && !l.startsWith('</')) {
        pad += 1;
      }
      return out;
    })
    .filter((l) => l.length > 0)
    .join('\n');
}

export function XmlViewer({ xmlText, title }: { xmlText: string; title?: string }) {
  const addLog = useActivityLogStore((s) => s.addLog);

  let pretty = xmlText;
  let error: string | null = null;

  try {
    const parser = new DOMParser();
    const doc = parser.parseFromString(xmlText, 'application/xml');
    const parseError = doc.getElementsByTagName('parsererror')[0];
    if (parseError) {
      error = parseError.textContent?.trim() ?? 'Invalid XML';
    } else {
      const serialized = new XMLSerializer().serializeToString(doc);
      pretty = formatXml(serialized);
    }
  } catch (e) {
    error = e instanceof Error ? e.message : 'Invalid XML';
  }

  const copy = async () => {
    try {
      await navigator.clipboard.writeText(pretty);
      addLog('success', 'Copied XML to clipboard');
    } catch (e) {
      addLog('error', 'Failed to copy XML', e instanceof Error ? e.message : String(e));
    }
  };

  return (
    <Card>
      <CardHeader className="pb-3 flex flex-row items-center justify-between">
        <CardTitle className="text-base">{title ?? 'XML'}</CardTitle>
        <Button variant="outline" size="sm" onClick={copy}>
          Copy
        </Button>
      </CardHeader>
      <CardContent className="space-y-3">
        {error && (
          <Alert variant="destructive">
            <AlertTitle>Invalid XML</AlertTitle>
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


