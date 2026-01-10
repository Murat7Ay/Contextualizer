import { useEffect, useMemo, useState } from 'react';
import { Search, RefreshCcw } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '../ui/card';
import { Button } from '../ui/button';
import { Input } from '../ui/input';
import { Badge } from '../ui/badge';
import { cn } from '../ui/utils';
import { useActivityLogStore } from '../../stores/activityLogStore';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../ui/select';
import { useHandlersStore } from '../../stores/handlersStore';
import { reloadHandlers, requestHandlersList, setHandlerEnabled, setHandlerMcpEnabled } from '../../host/webview2Bridge';

const statusBadgeClasses = {
  enabled: 'bg-green-500/10 text-green-700 dark:text-green-400',
  disabled: 'bg-red-500/10 text-red-700 dark:text-red-400',
  mcpOn: 'bg-blue-500/10 text-blue-700 dark:text-blue-400',
  mcpOff: 'bg-muted text-muted-foreground',
} as const;

export function HandlerManagement() {
  const addLog = useActivityLogStore((state) => state.addLog);

  const handlers = useHandlersStore((s) => s.handlers);
  const loaded = useHandlersStore((s) => s.loaded);

  const [searchQuery, setSearchQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState<'all' | 'enabled' | 'disabled'>('all');
  const [mcpFilter, setMcpFilter] = useState<'all' | 'on' | 'off'>('all');

  useEffect(() => {
    requestHandlersList();
  }, []);

  const totals = useMemo(() => {
    const total = handlers.length;
    const enabled = handlers.filter((h) => h.enabled).length;
    const disabled = total - enabled;
    return { total, enabled, disabled };
  }, [handlers]);

  const filtered = useMemo(() => {
    const q = searchQuery.trim().toLowerCase();
    return handlers.filter((h) => {
      const matchesSearch =
        q.length === 0 ||
        h.name.toLowerCase().includes(q) ||
        (h.description ?? '').toLowerCase().includes(q) ||
        (h.type ?? '').toLowerCase().includes(q);

      const matchesStatus =
        statusFilter === 'all' ||
        (statusFilter === 'enabled' && h.enabled) ||
        (statusFilter === 'disabled' && !h.enabled);

      const matchesMcp =
        mcpFilter === 'all' ||
        (mcpFilter === 'on' && h.mcpEnabled) ||
        (mcpFilter === 'off' && !h.mcpEnabled);

      return matchesSearch && matchesStatus && matchesMcp;
    });
  }, [handlers, searchQuery, statusFilter, mcpFilter]);

  const toggleEnabled = (name: string, currentEnabled: boolean) => {
    setHandlerEnabled(name, !currentEnabled);
    addLog('info', `Handler '${name}' ${currentEnabled ? 'disabled' : 'enabled'}`);
  };

  const toggleMcp = (name: string, currentMcpEnabled: boolean) => {
    setHandlerMcpEnabled(name, !currentMcpEnabled);
    addLog('info', `Handler '${name}' MCP ${currentMcpEnabled ? 'disabled' : 'enabled'}`);
  };

  const handleReload = () => {
    reloadHandlers(true);
    addLog('info', 'Reload requested');
  };

  return (
    <div className="p-6 max-w-7xl mx-auto space-y-6">
      <div className="flex items-start justify-between gap-4">
        <div>
          <h1 className="text-[28px] font-semibold mb-2">Handler Management</h1>
          <p className="text-sm text-muted-foreground">
            Enable/disable handlers and control MCP exposure.
          </p>
        </div>
        <Button variant="outline" onClick={handleReload}>
          <RefreshCcw className="h-4 w-4 mr-2" />
          Reload
        </Button>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">Total Handlers</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{totals.total}</div>
            <p className="text-xs text-muted-foreground">Loaded in UI</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">Enabled</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-green-600 dark:text-green-400">{totals.enabled}</div>
            <p className="text-xs text-muted-foreground">Active for processing</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">Disabled</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold text-red-600 dark:text-red-400">{totals.disabled}</div>
            <p className="text-xs text-muted-foreground">Skipped when processing</p>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base">Handlers</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex flex-col md:flex-row gap-3">
            <div className="relative flex-1">
              <Search className="absolute left-2 top-2.5 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder="Search by name, type, or description..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="pl-8 h-9"
              />
            </div>

            <Select value={statusFilter} onValueChange={(v) => setStatusFilter(v as typeof statusFilter)}>
              <SelectTrigger className="h-9 w-full md:w-[160px]">
                <SelectValue placeholder="Status" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All Status</SelectItem>
                <SelectItem value="enabled">Enabled</SelectItem>
                <SelectItem value="disabled">Disabled</SelectItem>
              </SelectContent>
            </Select>

            <Select value={mcpFilter} onValueChange={(v) => setMcpFilter(v as typeof mcpFilter)}>
              <SelectTrigger className="h-9 w-full md:w-[160px]">
                <SelectValue placeholder="MCP" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">All MCP</SelectItem>
                <SelectItem value="on">MCP On</SelectItem>
                <SelectItem value="off">MCP Off</SelectItem>
              </SelectContent>
            </Select>
          </div>

          {!loaded && handlers.length === 0 ? (
            <div className="text-center text-sm text-muted-foreground py-8">Waiting for host…</div>
          ) : filtered.length === 0 ? (
            <div className="text-center text-sm text-muted-foreground py-8">
              No handlers match the current filters.
            </div>
          ) : (
            <div className="space-y-3">
              {filtered.map((h) => (
                <div
                  key={h.name}
                  className="p-4 rounded-lg border bg-card hover:bg-accent/50 transition-colors"
                >
                  <div className="flex flex-col md:flex-row md:items-center gap-3">
                    <div className="flex items-start gap-3 flex-1 min-w-0">
                      <div
                        className={cn(
                          'mt-1.5 h-2 w-2 rounded-full shrink-0',
                          h.enabled ? 'bg-green-500' : 'bg-red-500',
                        )}
                      />
                      <div className="min-w-0">
                        <div className="flex flex-wrap items-center gap-2">
                          <h3 className="font-medium truncate">{h.name}</h3>
                          <Badge variant="outline" className="text-xs">
                            {h.type ?? 'Unknown'}
                          </Badge>
                          <Badge
                            variant="secondary"
                            className={cn(
                              'text-xs',
                              h.enabled ? statusBadgeClasses.enabled : statusBadgeClasses.disabled,
                            )}
                          >
                            {h.enabled ? 'Enabled' : 'Disabled'}
                          </Badge>
                          <Badge
                            variant="secondary"
                            className={cn(
                              'text-xs',
                              h.mcpEnabled ? statusBadgeClasses.mcpOn : statusBadgeClasses.mcpOff,
                            )}
                          >
                            {h.mcpEnabled ? 'MCP On' : 'MCP Off'}
                          </Badge>
                        </div>
                        <p className="text-sm text-muted-foreground mt-1">{h.description ?? ''}</p>
                      </div>
                    </div>

                    <div className="flex gap-2 md:justify-end">
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => toggleEnabled(h.name, h.enabled)}
                      >
                        {h.enabled ? 'Disable' : 'Enable'}
                      </Button>
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => toggleMcp(h.name, h.mcpEnabled)}
                      >
                        {h.mcpEnabled ? 'MCP Disable' : 'MCP Enable'}
                      </Button>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}

          <p className="text-xs text-muted-foreground">
            Tip: Disabled handlers will be skipped when processing clipboard content.
          </p>
        </CardContent>
      </Card>
    </div>
  );
}
