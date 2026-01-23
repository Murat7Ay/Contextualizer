import { useEffect, useMemo, useState } from 'react';
import { Badge } from '../ui/badge';
import { Button } from '../ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
import { Input } from '../ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../ui/select';
import { cn } from '../ui/utils';
import { useActivityLogStore } from '../../stores/activityLogStore';
import { useHandlerExchangeStore, type HandlerPackageDto } from '../../stores/handlerExchangeStore';
import { useHostStore } from '../../stores/hostStore';
import { Search, RefreshCcw, FileText, Download, Trash2, Loader2 } from 'lucide-react';
import { ScrollArea } from '../ui/scroll-area';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from '../ui/dialog';
import {
  requestExchangePackages,
  requestExchangePackageDetails,
  installExchangePackage,
  updateExchangePackage,
  removeExchangePackage,
} from '../../host/webview2Bridge';

type SortMode = 'name_asc' | 'name_desc' | 'newest' | 'most_downloaded';

const tagBadgeClass = 'bg-muted text-muted-foreground hover:bg-muted';

export function HandlerExchange() {
  const addLog = useActivityLogStore((state) => state.addLog);

  const webView2Available = useHostStore((s) => s.webView2Available);
  const hostConnected = useHostStore((s) => s.hostConnected);
  const canUseHost = webView2Available && hostConnected;

  // Store state
  const packages = useHandlerExchangeStore((s) => s.packages);
  const loaded = useHandlerExchangeStore((s) => s.loaded);
  const loading = useHandlerExchangeStore((s) => s.loading);
  const error = useHandlerExchangeStore((s) => s.error);
  const installingIds = useHandlerExchangeStore((s) => s.installingIds);
  const updatingIds = useHandlerExchangeStore((s) => s.updatingIds);
  const removingIds = useHandlerExchangeStore((s) => s.removingIds);
  const setLoading = useHandlerExchangeStore((s) => s.setLoading);
  const setInstalling = useHandlerExchangeStore((s) => s.setInstalling);
  const setUpdating = useHandlerExchangeStore((s) => s.setUpdating);
  const setRemoving = useHandlerExchangeStore((s) => s.setRemoving);
  const detailsOpen = useHandlerExchangeStore((s) => s.detailsOpen);
  const detailsLoading = useHandlerExchangeStore((s) => s.detailsLoading);
  const detailsError = useHandlerExchangeStore((s) => s.detailsError);
  const detailsPackage = useHandlerExchangeStore((s) => s.detailsPackage);
  const detailsHandlerId = useHandlerExchangeStore((s) => s.detailsHandlerId);
  const openDetails = useHandlerExchangeStore((s) => s.openDetails);
  const closeDetails = useHandlerExchangeStore((s) => s.closeDetails);

  // Local UI state
  const [searchQuery, setSearchQuery] = useState('');
  const [tagFilter, setTagFilter] = useState<string>('all');
  const [sortMode, setSortMode] = useState<SortMode>('name_asc');

  // Load packages on mount
  useEffect(() => {
    if (!canUseHost) return;
    if (!loaded && !loading) {
      setLoading(true);
      requestExchangePackages();
    }
  }, [canUseHost, loaded, loading, setLoading]);

  // Extract all tags from packages
  const allTags = useMemo(() => {
    const tags = new Set<string>();
    packages.forEach((p) => p.tags?.forEach((t) => tags.add(t)));
    return Array.from(tags).sort((a, b) => a.localeCompare(b));
  }, [packages]);

  // Filter and sort packages
  const filtered = useMemo(() => {
    const q = searchQuery.trim().toLowerCase();
    const base = packages.filter((p) => {
      const matchesSearch =
        q.length === 0 ||
        p.name.toLowerCase().includes(q) ||
        (p.description ?? '').toLowerCase().includes(q) ||
        (p.author ?? '').toLowerCase().includes(q);

      const matchesTag = tagFilter === 'all' || (p.tags ?? []).includes(tagFilter);
      return matchesSearch && matchesTag;
    });

    switch (sortMode) {
      case 'name_desc':
        return base.slice().sort((a, b) => b.name.localeCompare(a.name));
      case 'newest':
        return base.slice().sort((a, b) => (b.version ?? '').localeCompare(a.version ?? ''));
      case 'most_downloaded':
        return base.slice().sort((a, b) => (b.downloadCount ?? 0) - (a.downloadCount ?? 0));
      case 'name_asc':
      default:
        return base.slice().sort((a, b) => a.name.localeCompare(b.name));
    }
  }, [packages, searchQuery, tagFilter, sortMode]);

  const refresh = () => {
    if (!canUseHost) {
      addLog('warning', 'Host not connected');
      return;
    }
    setLoading(true);
    requestExchangePackages();
  };

  const showDetails = (pkg: HandlerPackageDto) => {
    if (!canUseHost) return;
    openDetails(pkg.id);
    requestExchangePackageDetails(pkg.id);
  };

  const install = (id: string) => {
    if (!canUseHost) return;
    setInstalling(id, true);
    installExchangePackage(id);
  };

  const update = (id: string) => {
    if (!canUseHost) return;
    setUpdating(id, true);
    updateExchangePackage(id);
  };

  const remove = (id: string) => {
    if (!canUseHost) return;
    setRemoving(id, true);
    removeExchangePackage(id);
  };

  const isOperating = (id: string) =>
    installingIds.has(id) || updatingIds.has(id) || removingIds.has(id);

  return (
    <ScrollArea className="h-full">
      <div className="p-6 max-w-7xl mx-auto space-y-6">
      <div className="flex items-start justify-between gap-4">
        <div>
          <h1 className="text-[28px] font-semibold mb-2">Handler Exchange</h1>
          <p className="text-sm text-muted-foreground">
            Browse community packages and manage installed templates.
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={refresh} disabled={loading || !canUseHost}>
            {loading ? (
              <Loader2 className="h-4 w-4 mr-2 animate-spin" />
            ) : (
              <RefreshCcw className="h-4 w-4 mr-2" />
            )}
            Refresh
          </Button>
        </div>
      </div>

      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base">Search & Filters</CardTitle>
          <CardDescription>Find handlers by name, author, tags, or description</CardDescription>
        </CardHeader>
        <CardContent className="flex flex-col md:flex-row gap-3">
          <div className="relative flex-1">
            <Search className="absolute left-2 top-2.5 h-4 w-4 text-muted-foreground" />
            <Input
              placeholder="Search packages..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="pl-8 h-9"
            />
          </div>

          <Select value={tagFilter} onValueChange={setTagFilter}>
            <SelectTrigger className="h-9 w-full md:w-[200px]">
              <SelectValue placeholder="Filter by tag" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Tags</SelectItem>
              {allTags.map((t) => (
                <SelectItem key={t} value={t}>
                  {t}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>

          <Select value={sortMode} onValueChange={(v) => setSortMode(v as SortMode)}>
            <SelectTrigger className="h-9 w-full md:w-[200px]">
              <SelectValue placeholder="Sort" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="name_asc">Name (A–Z)</SelectItem>
              <SelectItem value="name_desc">Name (Z–A)</SelectItem>
              <SelectItem value="newest">Newest</SelectItem>
              <SelectItem value="most_downloaded">Most Downloaded</SelectItem>
            </SelectContent>
          </Select>
        </CardContent>
      </Card>

      {/* Status info */}
      {!canUseHost && (
        <div className="text-center text-sm text-muted-foreground py-8">
          Connect to the WPF host to load exchange packages.
        </div>
      )}

      {canUseHost && error && (
        <div className="text-center text-sm text-red-500 py-8">
          Error loading packages: {error}
        </div>
      )}

      {canUseHost && !loaded && loading && (
        <div className="text-center text-sm text-muted-foreground py-12 flex items-center justify-center gap-2">
          <Loader2 className="h-4 w-4 animate-spin" />
          Loading packages...
        </div>
      )}

      {canUseHost && loaded && filtered.length === 0 && (
        <div className="text-center text-sm text-muted-foreground py-12">
          No packages match the current filters.
        </div>
      )}

      {canUseHost && loaded && filtered.length > 0 && (
        <div className="space-y-4">
          {filtered.map((pkg) => {
            const operating = isOperating(pkg.id);
            return (
              <Card key={pkg.id} className="hover:bg-accent/30 transition-colors">
                <CardHeader className="pb-3">
                  <div className="flex flex-col md:flex-row md:items-start md:justify-between gap-3">
                    <div className="min-w-0">
                      <div className="flex flex-wrap items-center gap-2">
                        <CardTitle className="text-base">{pkg.name}</CardTitle>
                        {pkg.isInstalled && (
                          <Badge className="text-xs" variant="secondary">
                            Installed
                          </Badge>
                        )}
                        {pkg.hasUpdate && (
                          <Badge
                            className="text-xs bg-yellow-500/10 text-yellow-700 dark:text-yellow-400"
                            variant="secondary"
                          >
                            Update available
                          </Badge>
                        )}
                      </div>
                      <p className="text-sm text-muted-foreground mt-1">{pkg.description}</p>
                      <div className="flex flex-wrap items-center gap-x-4 gap-y-1 mt-2 text-xs text-muted-foreground">
                        <span>
                          v<span className="font-mono">{pkg.version}</span>
                        </span>
                        {pkg.author && <span>by {pkg.author}</span>}
                        {typeof pkg.downloadCount === 'number' && pkg.downloadCount > 0 && (
                          <span>{pkg.downloadCount.toLocaleString()} downloads</span>
                        )}
                      </div>
                    </div>

                    <div className="flex gap-2 md:justify-end">
                      <Button variant="outline" size="sm" onClick={() => showDetails(pkg)}>
                        <FileText className="h-4 w-4 mr-2" />
                        Details
                      </Button>

                      {!pkg.isInstalled ? (
                        <Button
                          size="sm"
                          onClick={() => install(pkg.id)}
                          disabled={operating}
                        >
                          {installingIds.has(pkg.id) ? (
                            <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                          ) : (
                            <Download className="h-4 w-4 mr-2" />
                          )}
                          Install
                        </Button>
                      ) : (
                        <>
                          {pkg.hasUpdate && (
                            <Button
                              size="sm"
                              className="bg-yellow-600 hover:bg-yellow-600/90 text-white"
                              onClick={() => update(pkg.id)}
                              disabled={operating}
                            >
                              {updatingIds.has(pkg.id) ? (
                                <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                              ) : (
                                <RefreshCcw className="h-4 w-4 mr-2" />
                              )}
                              Update
                            </Button>
                          )}
                          <Button
                            variant="destructive"
                            size="sm"
                            onClick={() => remove(pkg.id)}
                            disabled={operating}
                          >
                            {removingIds.has(pkg.id) ? (
                              <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                            ) : (
                              <Trash2 className="h-4 w-4 mr-2" />
                            )}
                            Remove
                          </Button>
                        </>
                      )}
                    </div>
                  </div>
                </CardHeader>

                <CardContent className="space-y-3">
                  {(pkg.tags ?? []).length > 0 && (
                    <div className="flex flex-wrap gap-2">
                      {pkg.tags.map((t) => (
                        <Badge key={t} variant="secondary" className={cn('text-xs', tagBadgeClass)}>
                          {t}
                        </Badge>
                      ))}
                    </div>
                  )}

                  {(pkg.dependencies ?? []).length > 0 && (
                    <div className="text-xs text-muted-foreground">
                      <span className="font-medium text-foreground">Dependencies:</span>{' '}
                      {pkg.dependencies.join(', ')}
                    </div>
                  )}
                </CardContent>
              </Card>
            );
          })}
        </div>
      )}

      <Dialog open={detailsOpen} onOpenChange={(o) => (!o ? closeDetails() : null)}>
        <DialogContent className="sm:max-w-2xl">
          <DialogHeader>
            <DialogTitle>Package details</DialogTitle>
            <DialogDescription>
              {detailsHandlerId ? `ID: ${detailsHandlerId}` : ''}
            </DialogDescription>
          </DialogHeader>

          {detailsLoading ? (
            <div className="flex items-center gap-2 text-sm text-muted-foreground py-6">
              <Loader2 className="h-4 w-4 animate-spin" />
              Loading…
            </div>
          ) : detailsError ? (
            <div className="text-sm text-red-500 py-2">{detailsError}</div>
          ) : !detailsPackage ? (
            <div className="text-sm text-muted-foreground py-2">No details available.</div>
          ) : (
            <div className="space-y-4">
              <div className="space-y-1">
                <div className="text-lg font-semibold">{detailsPackage.name}</div>
                <div className="text-sm text-muted-foreground">{detailsPackage.description}</div>
                <div className="flex flex-wrap gap-2 pt-2 text-xs text-muted-foreground">
                  <span>
                    v<span className="font-mono">{detailsPackage.version}</span>
                  </span>
                  {detailsPackage.author && <span>by {detailsPackage.author}</span>}
                  {typeof detailsPackage.downloadCount === 'number' && detailsPackage.downloadCount > 0 && (
                    <span>{detailsPackage.downloadCount.toLocaleString()} downloads</span>
                  )}
                  {detailsPackage.isInstalled && <Badge variant="secondary">Installed</Badge>}
                  {detailsPackage.hasUpdate && (
                    <Badge variant="secondary" className="bg-yellow-500/10 text-yellow-700 dark:text-yellow-400">
                      Update available
                    </Badge>
                  )}
                </div>
              </div>

              {(detailsPackage.tags ?? []).length > 0 && (
                <div className="space-y-2">
                  <div className="text-sm font-medium">Tags</div>
                  <div className="flex flex-wrap gap-2">
                    {detailsPackage.tags.map((t) => (
                      <Badge key={t} variant="secondary" className={cn('text-xs', tagBadgeClass)}>
                        {t}
                      </Badge>
                    ))}
                  </div>
                </div>
              )}

              {(detailsPackage.dependencies ?? []).length > 0 && (
                <div className="space-y-2">
                  <div className="text-sm font-medium">Dependencies</div>
                  <div className="text-sm text-muted-foreground">
                    {detailsPackage.dependencies.join(', ')}
                  </div>
                </div>
              )}

              {detailsPackage.metadata && Object.keys(detailsPackage.metadata).length > 0 && (
                <div className="space-y-2">
                  <div className="text-sm font-medium">Metadata</div>
                  <pre className="text-xs p-3 bg-muted rounded border overflow-x-auto">
                    {JSON.stringify(detailsPackage.metadata, null, 2)}
                  </pre>
                </div>
              )}
            </div>
          )}
        </DialogContent>
      </Dialog>
      </div>
    </ScrollArea>
  );
}
