import { useEffect, useMemo, useState } from 'react';
import { Package, Calendar, ShoppingBag, Settings, Activity, Zap, RefreshCw } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
import { Button } from '../ui/button';
import { useNavigate } from 'react-router-dom';
import { useTabStore } from '../../stores/tabStore';
import { useHostStore } from '../../stores/hostStore';
import { useHandlersStore } from '../../stores/handlersStore';
import { useCronStore } from '../../stores/cronStore';
import { useActivityLogStore } from '../../stores/activityLogStore';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../ui/select';
import { executeManualHandler, reloadHandlers, requestCronList, requestHandlersList } from '../../host/webview2Bridge';

export function Dashboard() {
  const navigate = useNavigate();
  const openTab = useTabStore((state) => state.openTab);
  const webView2Available = useHostStore((s) => s.webView2Available);
  const hostConnected = useHostStore((s) => s.hostConnected);

  const handlers = useHandlersStore((s) => s.handlers);
  const handlersLoaded = useHandlersStore((s) => s.loaded);
  const cronJobs = useCronStore((s) => s.jobs);
  const cronLoaded = useCronStore((s) => s.loaded);
  const cronRunning = useCronStore((s) => s.isRunning);
  const logs = useActivityLogStore((s) => s.logs);

  const manualHandlers = useMemo(() => handlers.filter((h) => h.isManual), [handlers]);
  const [selectedManual, setSelectedManual] = useState<string>('');

  useEffect(() => {
    if (!webView2Available || !hostConnected) return;
    if (!handlersLoaded) requestHandlersList();
    if (!cronLoaded) requestCronList();
  }, [cronLoaded, handlersLoaded, hostConnected, webView2Available]);

  useEffect(() => {
    if (selectedManual) return;
    if (manualHandlers.length > 0) setSelectedManual(manualHandlers[0].name);
  }, [manualHandlers, selectedManual]);

  const handlerStats = useMemo(() => {
    const total = handlers.length;
    const enabled = handlers.filter((h) => h.enabled).length;
    const disabled = total - enabled;
    const manual = manualHandlers.length;
    return { total, enabled, disabled, manual };
  }, [handlers, manualHandlers.length]);

  const cronStats = useMemo(() => {
    const total = cronJobs.length;
    const enabled = cronJobs.filter((j) => j.enabled).length;
    return { total, enabled };
  }, [cronJobs]);

  const recentLogs = useMemo(() => logs.slice(0, 5), [logs]);

  const handleOpenSettings = () => {
    openTab('settings', 'Settings');
    navigate('/settings');
  };

  const handleOpenHandlers = () => {
    openTab('handlers', 'Handler Management');
    navigate('/handlers');
  };

  const handleOpenMarketplace = () => {
    openTab('marketplace', 'Handler Exchange');
    navigate('/marketplace');
  };

  const handleOpenCron = () => {
    openTab('cron', 'Cron Manager');
    navigate('/cron');
  };

  const canUseHost = webView2Available && hostConnected;

  const runSelectedManual = () => {
    if (!selectedManual) return;
    const ok = executeManualHandler(selectedManual);
    if (!ok) {
      // Host will also show a toast via WebView2 bridge if available
      // but we keep a small UX hint in dev mode.
      // eslint-disable-next-line no-console
      console.warn('Host not available: cannot execute manual handler');
    }
  };

  return (
    <div className="p-6 max-w-7xl mx-auto">
      {/* Welcome Section */}
      <div className="mb-8">
        <h1 className="text-3xl font-bold mb-2">Welcome to Contextualizer</h1>
        <p className="text-muted-foreground">
          Clipboard automation and context processing desktop application
        </p>
      </div>

      {/* Statistics Cards */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">Active Handlers</CardTitle>
            <Package className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{canUseHost ? handlerStats.enabled : '—'}</div>
            <p className="text-xs text-muted-foreground">
              {canUseHost ? `${handlerStats.disabled} inactive • ${handlerStats.manual} manual` : 'Connect to host to load stats'}
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">Cron Jobs</CardTitle>
            <Calendar className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{canUseHost ? cronStats.total : '—'}</div>
            <p className="text-xs text-muted-foreground">
              {canUseHost ? `${cronStats.enabled} enabled • ${cronRunning ? 'scheduler running' : 'scheduler stopped'}` : 'Connect to host to load stats'}
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">Recent Activity</CardTitle>
            <Activity className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">{logs.length}</div>
            <p className="text-xs text-muted-foreground">In this session</p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between pb-2">
            <CardTitle className="text-sm font-medium">Available Packages</CardTitle>
            <ShoppingBag className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            <div className="text-2xl font-bold">—</div>
            <p className="text-xs text-muted-foreground">Marketplace is not wired yet</p>
          </CardContent>
        </Card>
      </div>

      {/* Manual Handlers */}
      <Card className="mb-8">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Zap className="h-4 w-4" />
            Manual Handlers
          </CardTitle>
          <CardDescription>Run triggerable handlers on-demand</CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col md:flex-row md:items-end gap-3">
          <div className="flex-1 space-y-2">
            <div className="text-sm font-medium">Select handler</div>
            <Select
              value={selectedManual}
              onValueChange={setSelectedManual}
              disabled={!canUseHost || manualHandlers.length === 0}
            >
              <SelectTrigger disabled={!canUseHost || manualHandlers.length === 0}>
                <SelectValue placeholder={canUseHost ? 'Choose a manual handler…' : 'Host not connected'} />
              </SelectTrigger>
              <SelectContent>
                {manualHandlers.map((h) => (
                  <SelectItem key={h.name} value={h.name}>
                    {h.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            {!canUseHost ? (
              <p className="text-xs text-muted-foreground">Connect to the WPF host to load and run manual handlers.</p>
            ) : manualHandlers.length === 0 ? (
              <p className="text-xs text-muted-foreground">No manual handlers found in your `handlers.json`.</p>
            ) : null}
          </div>

          <div className="flex gap-2">
            <Button onClick={runSelectedManual} disabled={!canUseHost || !selectedManual}>
              Run
            </Button>
            <Button
              variant="outline"
              onClick={() => reloadHandlers(true)}
              disabled={!canUseHost}
              title="Reload handlers & plugins"
            >
              <RefreshCw className="h-4 w-4 mr-2" />
              Reload
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Quick Actions */}
      <Card className="mb-8">
        <CardHeader>
          <CardTitle>Quick Actions</CardTitle>
          <CardDescription>Get started with common tasks</CardDescription>
        </CardHeader>
        <CardContent className="grid grid-cols-1 md:grid-cols-2 gap-3">
          <Button variant="outline" className="justify-start h-auto py-4" onClick={handleOpenHandlers}>
            <Package className="h-5 w-5 mr-3" />
            <div className="text-left">
              <div className="font-medium">Manage Handlers</div>
              <div className="text-xs text-muted-foreground">View and configure your handlers</div>
            </div>
          </Button>

          <Button variant="outline" className="justify-start h-auto py-4" onClick={handleOpenCron}>
            <Calendar className="h-5 w-5 mr-3" />
            <div className="text-left">
              <div className="font-medium">Cron Jobs</div>
              <div className="text-xs text-muted-foreground">Schedule automated tasks</div>
            </div>
          </Button>

          <Button variant="outline" className="justify-start h-auto py-4" onClick={handleOpenMarketplace}>
            <ShoppingBag className="h-5 w-5 mr-3" />
            <div className="text-left">
              <div className="font-medium">Browse Marketplace</div>
              <div className="text-xs text-muted-foreground">Discover new handler packages</div>
            </div>
          </Button>

          <Button variant="outline" className="justify-start h-auto py-4" onClick={handleOpenSettings}>
            <Settings className="h-5 w-5 mr-3" />
            <div className="text-left">
              <div className="font-medium">Settings</div>
              <div className="text-xs text-muted-foreground">Configure application preferences</div>
            </div>
          </Button>
        </CardContent>
      </Card>

      {/* Recent Activity Preview */}
      <Card>
        <CardHeader>
          <CardTitle>Recent Activity</CardTitle>
          <CardDescription>Last 5 log entries</CardDescription>
        </CardHeader>
        <CardContent>
          {recentLogs.length === 0 ? (
            <div className="text-sm text-muted-foreground">No activity yet.</div>
          ) : (
            <div className="space-y-2 text-sm">
              {recentLogs.map((l) => (
                <div key={l.id} className="flex items-center gap-3 p-2 rounded border">
                  <div
                    className={
                      l.level === 'success'
                        ? 'h-2 w-2 rounded-full bg-green-500'
                        : l.level === 'error' || l.level === 'critical'
                          ? 'h-2 w-2 rounded-full bg-red-500'
                          : l.level === 'warning'
                            ? 'h-2 w-2 rounded-full bg-yellow-500'
                            : l.level === 'debug'
                              ? 'h-2 w-2 rounded-full bg-purple-500'
                              : 'h-2 w-2 rounded-full bg-blue-500'
                    }
                  />
                  <div className="flex-1 truncate">{l.message}</div>
                  <div className="text-xs text-muted-foreground">{l.timestamp.toLocaleTimeString()}</div>
                </div>
              ))}
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
