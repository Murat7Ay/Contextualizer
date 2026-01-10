import { useMemo, useState } from 'react';
import { Badge } from '../ui/badge';
import { Button } from '../ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
import { Input } from '../ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../ui/select';
import { cn } from '../ui/utils';
import { useActivityLogStore } from '../../stores/activityLogStore';
import { Search, RefreshCcw, Plus, FileText, Download, Trash2 } from 'lucide-react';

type HandlerPackage = {
  id: string;
  name: string;
  description: string;
  version: string;
  author: string;
  downloadCount: number;
  tags: string[];
  dependencies: string[];
  isInstalled: boolean;
  hasUpdate: boolean;
};

const initialPackages: HandlerPackage[] = [
  {
    id: 'pkg_pretty_json',
    name: 'Pretty JSON Toolkit',
    description: 'Format, validate, and extract keys from JSON clipboard content.',
    version: '1.4.0',
    author: 'Contextualizer',
    downloadCount: 1420,
    tags: ['json', 'formatting', 'devtools'],
    dependencies: [],
    isInstalled: true,
    hasUpdate: true,
  },
  {
    id: 'pkg_xml_tools',
    name: 'XML Tools Pack',
    description: 'Pretty print and validate XML with helpful error output.',
    version: '1.1.2',
    author: 'Community',
    downloadCount: 640,
    tags: ['xml', 'validation'],
    dependencies: ['Markdig'],
    isInstalled: true,
    hasUpdate: false,
  },
  {
    id: 'pkg_open_file',
    name: 'Open File Action',
    description: 'Open a file path from context via the OS shell.',
    version: '0.9.3',
    author: 'Community',
    downloadCount: 310,
    tags: ['actions', 'productivity'],
    dependencies: [],
    isInstalled: false,
    hasUpdate: false,
  },
];

type SortMode = 'name_asc' | 'name_desc' | 'newest' | 'most_downloaded';

const tagBadgeClass = 'bg-muted text-muted-foreground hover:bg-muted';

export function HandlerExchange() {
  const addLog = useActivityLogStore((state) => state.addLog);

  const [packages, setPackages] = useState<HandlerPackage[]>(initialPackages);
  const [searchQuery, setSearchQuery] = useState('');
  const [tagFilter, setTagFilter] = useState<string>('all');
  const [sortMode, setSortMode] = useState<SortMode>('name_asc');

  const allTags = useMemo(() => {
    const tags = new Set<string>();
    packages.forEach((p) => p.tags.forEach((t) => tags.add(t)));
    return Array.from(tags).sort((a, b) => a.localeCompare(b));
  }, [packages]);

  const filtered = useMemo(() => {
    const q = searchQuery.trim().toLowerCase();
    const base = packages.filter((p) => {
      const matchesSearch =
        q.length === 0 ||
        p.name.toLowerCase().includes(q) ||
        p.description.toLowerCase().includes(q) ||
        p.author.toLowerCase().includes(q);

      const matchesTag = tagFilter === 'all' || p.tags.includes(tagFilter);
      return matchesSearch && matchesTag;
    });

    switch (sortMode) {
      case 'name_desc':
        return base.slice().sort((a, b) => b.name.localeCompare(a.name));
      case 'newest':
        // UI mock: treat version as recency proxy
        return base.slice().sort((a, b) => b.version.localeCompare(a.version));
      case 'most_downloaded':
        return base.slice().sort((a, b) => b.downloadCount - a.downloadCount);
      case 'name_asc':
      default:
        return base.slice().sort((a, b) => a.name.localeCompare(b.name));
    }
  }, [packages, searchQuery, tagFilter, sortMode]);

  const refresh = () => {
    addLog('info', 'Marketplace refresh requested (UI mock)');
  };

  const addNewHandler = () => {
    addLog('info', 'Add new handler requested (UI mock)');
  };

  const showDetails = (pkg: HandlerPackage) => {
    addLog('info', `Viewing details for '${pkg.name}' (UI mock)`);
  };

  const install = (id: string) => {
    const pkg = packages.find((p) => p.id === id);
    if (!pkg) return;
    setPackages((prev) => prev.map((p) => (p.id === id ? { ...p, isInstalled: true } : p)));
    addLog('success', `Installed '${pkg.name}' (UI mock)`);
  };

  const update = (id: string) => {
    const pkg = packages.find((p) => p.id === id);
    if (!pkg) return;
    setPackages((prev) =>
      prev.map((p) => (p.id === id ? { ...p, hasUpdate: false } : p)),
    );
    addLog('success', `Updated '${pkg.name}' (UI mock)`);
  };

  const remove = (id: string) => {
    const pkg = packages.find((p) => p.id === id);
    if (!pkg) return;
    setPackages((prev) =>
      prev.map((p) => (p.id === id ? { ...p, isInstalled: false, hasUpdate: false } : p)),
    );
    addLog('warning', `Removed '${pkg.name}' (UI mock)`);
  };

  return (
    <div className="p-6 max-w-7xl mx-auto space-y-6">
      <div className="flex items-start justify-between gap-4">
        <div>
          <h1 className="text-[28px] font-semibold mb-2">Handler Exchange</h1>
          <p className="text-sm text-muted-foreground">
            Browse community packages and manage installed templates.
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={refresh}>
            <RefreshCcw className="h-4 w-4 mr-2" />
            Refresh
          </Button>
          <Button variant="outline" onClick={addNewHandler}>
            <Plus className="h-4 w-4 mr-2" />
            Add New
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

      {filtered.length === 0 ? (
        <div className="text-center text-sm text-muted-foreground py-12">
          No packages match the current filters.
        </div>
      ) : (
        <div className="space-y-4">
          {filtered.map((pkg) => (
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
                        <Badge className="text-xs bg-yellow-500/10 text-yellow-700 dark:text-yellow-400" variant="secondary">
                          Update available
                        </Badge>
                      )}
                    </div>
                    <p className="text-sm text-muted-foreground mt-1">{pkg.description}</p>
                    <div className="flex flex-wrap items-center gap-x-4 gap-y-1 mt-2 text-xs text-muted-foreground">
                      <span>
                        v<span className="font-mono">{pkg.version}</span>
                      </span>
                      <span>by {pkg.author}</span>
                      <span>{pkg.downloadCount.toLocaleString()} downloads</span>
                    </div>
                  </div>

                  <div className="flex gap-2 md:justify-end">
                    <Button variant="outline" size="sm" onClick={() => showDetails(pkg)}>
                      <FileText className="h-4 w-4 mr-2" />
                      Details
                    </Button>

                    {!pkg.isInstalled ? (
                      <Button size="sm" onClick={() => install(pkg.id)}>
                        <Download className="h-4 w-4 mr-2" />
                        Install
                      </Button>
                    ) : (
                      <>
                        {pkg.hasUpdate && (
                          <Button
                            size="sm"
                            className="bg-yellow-600 hover:bg-yellow-600/90 text-white"
                            onClick={() => update(pkg.id)}
                          >
                            <RefreshCcw className="h-4 w-4 mr-2" />
                            Update
                          </Button>
                        )}
                        <Button variant="destructive" size="sm" onClick={() => remove(pkg.id)}>
                          <Trash2 className="h-4 w-4 mr-2" />
                          Remove
                        </Button>
                      </>
                    )}
                  </div>
                </div>
              </CardHeader>

              <CardContent className="space-y-3">
                <div className="flex flex-wrap gap-2">
                  {pkg.tags.map((t) => (
                    <Badge key={t} variant="secondary" className={cn('text-xs', tagBadgeClass)}>
                      {t}
                    </Badge>
                  ))}
                </div>

                {pkg.dependencies.length > 0 && (
                  <div className="text-xs text-muted-foreground">
                    <span className="font-medium text-foreground">Dependencies:</span>{' '}
                    {pkg.dependencies.join(', ')}
                  </div>
                )}
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
