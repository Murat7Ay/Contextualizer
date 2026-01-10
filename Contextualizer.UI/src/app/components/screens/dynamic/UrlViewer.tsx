import { useMemo, useState } from 'react';
import { Alert, AlertDescription, AlertTitle } from '../../ui/alert';
import { Button } from '../../ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '../../ui/card';
import { Input } from '../../ui/input';
import { useActivityLogStore } from '../../../stores/activityLogStore';
import { openExternalUrl } from '../../../host/webview2Bridge';
import { ExternalLink, Copy } from 'lucide-react';

function isHttpUrl(url: string): boolean {
  return /^https?:\/\//i.test(url);
}

export function UrlViewer({ url }: { url: string }) {
  const addLog = useActivityLogStore((s) => s.addLog);
  const [embed, setEmbed] = useState<boolean>(true);

  const cleaned = useMemo(() => url.trim(), [url]);

  const copy = async () => {
    try {
      await navigator.clipboard.writeText(cleaned);
      addLog('success', 'Copied URL to clipboard');
    } catch (e) {
      addLog('error', 'Failed to copy URL', e instanceof Error ? e.message : String(e));
    }
  };

  const openExternal = () => {
    if (!cleaned) return;
    const ok = openExternalUrl(cleaned);
    if (!ok) {
      // Browser/dev mode fallback
      window.open(cleaned, '_blank', 'noopener,noreferrer');
    }
    addLog('info', 'Opened URL externally');
  };

  return (
    <Card>
      <CardHeader className="pb-3">
        <div className="flex flex-col gap-3">
          <div className="flex items-center justify-between gap-3">
            <CardTitle className="text-base">URL Viewer</CardTitle>
            <div className="flex gap-2">
              <Button variant="outline" size="sm" onClick={copy} title="Copy URL">
                <Copy className="h-4 w-4 mr-2" />
                Copy
              </Button>
              <Button variant="outline" size="sm" onClick={openExternal} title="Open in default browser">
                <ExternalLink className="h-4 w-4 mr-2" />
                Open
              </Button>
            </div>
          </div>
          <Input value={cleaned} readOnly />
        </div>
      </CardHeader>

      <CardContent className="space-y-3">
        {!cleaned && (
          <Alert variant="destructive">
            <AlertTitle>No URL provided</AlertTitle>
            <AlertDescription>Context did not include a URL.</AlertDescription>
          </Alert>
        )}

        {cleaned && !isHttpUrl(cleaned) && (
          <Alert>
            <AlertTitle>Embedded preview may be limited</AlertTitle>
            <AlertDescription>
              This URL is not an http(s) URL. Use “Open” to view it in the default browser.
            </AlertDescription>
          </Alert>
        )}

        {cleaned && isHttpUrl(cleaned) && (
          <>
            <div className="flex items-center justify-between">
              <div className="text-xs text-muted-foreground">Preview</div>
              <Button variant="outline" size="sm" onClick={() => setEmbed((v) => !v)}>
                {embed ? 'Hide preview' : 'Show preview'}
              </Button>
            </div>
            {embed && (
              <div className="h-[70vh] rounded-md border overflow-hidden bg-background">
                <iframe
                  src={cleaned}
                  className="w-full h-full"
                  referrerPolicy="no-referrer"
                />
              </div>
            )}
          </>
        )}
      </CardContent>
    </Card>
  );
}


