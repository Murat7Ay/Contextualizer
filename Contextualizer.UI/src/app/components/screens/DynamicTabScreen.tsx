import { useLocation, useParams } from 'react-router-dom';
import { Card, CardContent, CardHeader, CardTitle } from '../ui/card';
import { Button } from '../ui/button';
import { useTabStore } from '../../stores/tabStore';
import { executeTabAction } from '../../host/webview2Bridge';
import { MarkdownViewer } from './dynamic/MarkdownViewer';
import { JsonViewer } from './dynamic/JsonViewer';
import { XmlViewer } from './dynamic/XmlViewer';
import { UrlViewer } from './dynamic/UrlViewer';
import { SqlEditor } from './dynamic/SqlEditor';

export function DynamicTabScreen() {
  const { screenId, title } = useParams();
  const location = useLocation();
  const tabs = useTabStore((state) => state.tabs);
  const activeTabId = useTabStore((state) => state.activeTabId);
  // Prefer matching the actual current pathname (most reliable across encoding differences).
  // Fallback to computed route to support edge cases.
  const route = screenId && title ? `/tab/${screenId}/${encodeURIComponent(title)}` : null;
  const currentTab =
    tabs.find((t) => t.route === location.pathname) ??
    (route ? tabs.find((t) => t.route === route) : undefined) ??
    (activeTabId ? tabs.find((t) => t.id === activeTabId) : undefined) ??
    (screenId ? [...tabs].reverse().find((t) => t.screenId === screenId) : undefined);
  const context = currentTab?.context as Record<string, unknown> | undefined;

  const body =
    context &&
    (typeof context._body === 'string'
      ? (context._body as string)
      : typeof context.body === 'string'
        ? (context.body as string)
        : undefined);
  const decodedTitle = title || 'Dynamic Screen';
  const actions = currentTab?.actions;
  
  const handleActionClick = (actionId: string) => {
    if (!currentTab?.id) return;
    executeTabAction(currentTab.id, actionId, context as Record<string, unknown>);
  };
  
  return (
    <div className="p-6 max-w-5xl mx-auto space-y-4 flex flex-col min-h-0">
      <div>
        <h1 className="text-2xl font-bold mb-1">{decodedTitle}</h1>
        <p className="text-sm text-muted-foreground">Screen ID: {screenId}</p>
      </div>

      <div className="flex-1 space-y-4">
        {screenId === 'markdown2' && body != null ? (
        <Card>
          <CardHeader className="pb-3">
            <CardTitle className="text-base">Markdown</CardTitle>
          </CardHeader>
          <CardContent>
            <MarkdownViewer markdown={body} />
          </CardContent>
        </Card>
      ) : screenId === 'jsonformatter' && body != null ? (
        <JsonViewer jsonText={body} title={decodedTitle} />
      ) : screenId === 'xmlformatter' && body != null ? (
        <XmlViewer xmlText={body} title={decodedTitle} />
      ) : screenId === 'url_viewer' && body != null ? (
        <UrlViewer url={body} />
      ) : screenId === 'plsql_editor' && body != null ? (
        <SqlEditor initialSql={body} title={decodedTitle} />
      ) : body ? (
        <Card>
          <CardHeader className="pb-3">
            <CardTitle className="text-base">Content</CardTitle>
          </CardHeader>
          <CardContent>
            <pre className="text-sm p-3 bg-muted rounded overflow-x-auto whitespace-pre-wrap">
              {body}
            </pre>
          </CardContent>
        </Card>
      ) : (
        <Card>
          <CardHeader className="pb-3">
            <CardTitle className="text-base">Context</CardTitle>
          </CardHeader>
          <CardContent>
            <pre className="text-xs p-3 bg-muted rounded overflow-x-auto">
              {context ? JSON.stringify(context, null, 2) : 'No context provided.'}
            </pre>
          </CardContent>
        </Card>
        )}
      </div>

      {actions && actions.length > 0 && (
        <div className="flex justify-end gap-2 pt-4 border-t">
          {actions.map((action) => (
            <Button
              key={action.id}
              onClick={() => handleActionClick(action.id)}
              variant="default"
            >
              {action.label}
            </Button>
          ))}
        </div>
      )}
    </div>
  );
}
