import { useCallback, useEffect, useState } from 'react';
import { toast } from 'sonner';
import { FolderOpen, Loader2, Plus, RefreshCcw, Save, Trash2 } from 'lucide-react';
import { ScrollArea } from '../ui/scroll-area';
import { Card, CardContent, CardHeader, CardTitle } from '../ui/card';
import { Button } from '../ui/button';
import { Input } from '../ui/input';
import { Badge } from '../ui/badge';
import { Checkbox } from '../ui/checkbox';
import { Label } from '../ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '../ui/select';
import {
  addWebView2MessageListener,
  openFolderDialog,
  requestAiSkillsHubDeploy,
  requestAiSkillsHubList,
  requestAiSkillsHubPull,
  requestAiSkillsHubRemove,
  requestAppSettings,
  saveAppSettings,
} from '../../host/webview2Bridge';
import { useAppSettingsStore } from '../../stores/appSettingsStore';
import { useActivityLogStore } from '../../stores/activityLogStore';
import { cn } from '../ui/utils';

type SkillRow = {
  skillName: string;
  sourceId: string;
  sourceLabel?: string | null;
  hasSkillMd: boolean;
  nameConflict: boolean;
  cursorSync: string;
  copilotSync: string;
};

type GlobalOnlyRow = {
  skillName: string;
  inCursor: boolean;
  inCopilot: boolean;
  hasSkillMdCursor: boolean;
  hasSkillMdCopilot: boolean;
};

type SourceRow = { id: string; path: string; label: string };

function syncBadge(sync: string) {
  if (sync === 'synced') return <Badge className="bg-green-500/15 text-green-700 dark:text-green-400">synced</Badge>;
  if (sync === 'diverged') return <Badge variant="outline">diverged</Badge>;
  return <Badge variant="secondary">needs deploy</Badge>;
}

function globalPresence(here: boolean) {
  return here ? (
    <Badge className="bg-emerald-500/15 text-emerald-800 dark:text-emerald-300">yes</Badge>
  ) : (
    <Badge variant="secondary">—</Badge>
  );
}

export function AiSkillsHubScreen() {
  const addLog = useActivityLogStore((s) => s.addLog);
  const draft = useAppSettingsStore((s) => s.draft);
  const settingsLoaded = useAppSettingsStore((s) => s.loaded);
  const isSaving = useAppSettingsStore((s) => s.isSaving);
  const setDraft = useAppSettingsStore((s) => s.setDraft);
  const setSaving = useAppSettingsStore((s) => s.setSaving);

  const [sources, setSources] = useState<SourceRow[]>([]);
  const [cursorPath, setCursorPath] = useState('');
  const [copilotPath, setCopilotPath] = useState('');
  const [skills, setSkills] = useState<SkillRow[]>([]);
  const [globalOnlySkills, setGlobalOnlySkills] = useState<GlobalOnlyRow[]>([]);
  const [roots, setRoots] = useState({ cursor: '', copilot: '' });
  const [loading, setLoading] = useState(false);
  const [actionBusy, setActionBusy] = useState<'idle' | 'deploy' | 'remove' | 'pull'>('idle');
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [deployCursor, setDeployCursor] = useState(true);
  const [deployCopilot, setDeployCopilot] = useState(true);
  const [customDest, setCustomDest] = useState('');
  const [pullTarget, setPullTarget] = useState<'cursor' | 'copilot'>('cursor');
  const [pullSourceId, setPullSourceId] = useState('');

  const applyHubFromDraft = useCallback(() => {
    const hub = useAppSettingsStore.getState().draft?.aiSkillsHub;
    const src = hub?.sources ?? [];
    setSources(
      src.map((s) => ({
        id: s.id,
        path: s.path,
        label: s.label ?? '',
      }))
    );
    setCursorPath(hub?.cursorSkillsPath ?? '');
    setCopilotPath(hub?.copilotSkillsPath ?? '');
  }, []);

  useEffect(() => {
    requestAppSettings();
  }, []);

  const aiSkillsHubSignature = JSON.stringify(draft?.aiSkillsHub ?? null);

  // Re-apply when host loads settings or aiSkillsHub payload changes (saved sources must reappear).
  useEffect(() => {
    if (!settingsLoaded) return;
    applyHubFromDraft();
  }, [settingsLoaded, aiSkillsHubSignature, applyHubFromDraft]);

  const refreshList = useCallback(() => {
    setLoading(true);
    requestAiSkillsHubList();
  }, []);

  useEffect(() => {
    refreshList();
  }, [refreshList]);

  useEffect(() => {
    const unsub = addWebView2MessageListener((payload) => {
      if (!payload || typeof payload !== 'object') return;
      const msg = payload as Record<string, unknown>;
      const t = msg.type;
      if (t === 'ai_skills_hub_list') {
        setLoading(false);
        if (typeof msg.error === 'string') {
          toast.error(msg.error);
          addLog('error', 'AI Skills list failed', msg.error);
          return;
        }
        const list = Array.isArray(msg.skills) ? (msg.skills as SkillRow[]) : [];
        setSkills(list);
        const glob = Array.isArray(msg.globalOnlySkills) ? (msg.globalOnlySkills as GlobalOnlyRow[]) : [];
        setGlobalOnlySkills(glob);
        setRoots({
          cursor: typeof msg.cursorSkillsRoot === 'string' ? msg.cursorSkillsRoot : '',
          copilot: typeof msg.copilotSkillsRoot === 'string' ? msg.copilotSkillsRoot : '',
        });
      }
      if (t === 'ai_skills_hub_deploy_result') {
        setActionBusy('idle');
        const ok = msg.ok === true;
        if (!ok) {
          toast.error(String(msg.error ?? 'Deploy failed'));
          addLog('error', 'AI Skills deploy', String(msg.error ?? ''));
          return;
        }
        const results = Array.isArray(msg.results) ? msg.results : [];
        const failed = results.filter((r: { ok?: boolean }) => !r.ok);
        if (failed.length) {
          toast.warning('Some deployments failed');
          failed.forEach((r: { skillName?: string; error?: string }) =>
            addLog('error', `Deploy ${r.skillName}`, r.error ?? '')
          );
        } else {
          toast.success('Deployed');
          addLog('success', 'AI Skills deployed');
        }
        refreshList();
      }
      if (t === 'ai_skills_hub_remove_result') {
        setActionBusy('idle');
        if (msg.ok !== true) {
          toast.error(String(msg.error ?? 'Remove failed'));
          return;
        }
        toast.success('Removed from target(s)');
        refreshList();
      }
      if (t === 'ai_skills_hub_pull_result') {
        setActionBusy('idle');
        if (msg.ok !== true) {
          toast.error(String(msg.error ?? 'Pull failed'));
          return;
        }
        toast.success('Pulled into source folder');
        refreshList();
      }
    });
    return unsub;
  }, [addLog, refreshList]);

  const toggleSelect = (key: string) => {
    setSelected((prev) => {
      const next = new Set(prev);
      if (next.has(key)) next.delete(key);
      else next.add(key);
      return next;
    });
  };

  const saveHubSettings = () => {
    if (!draft) return;
    const next = {
      ...draft,
      aiSkillsHub: {
        sources: sources.map((s) => ({
          id: s.id,
          path: s.path.trim(),
          label: s.label.trim() || null,
        })),
        cursorSkillsPath: cursorPath.trim() || null,
        copilotSkillsPath: copilotPath.trim() || null,
      },
    };
    setDraft(next);
    setSaving(true);
    if (!saveAppSettings(next)) {
      setSaving(false);
      toast.error('Cannot save (host bridge unavailable)');
      return;
    }
    addLog('info', 'Saving AI Skills hub settings…');
  };

  const addSource = () => {
    const id =
      typeof crypto !== 'undefined' && crypto.randomUUID
        ? crypto.randomUUID().replace(/-/g, '')
        : `${Date.now()}`;
    setSources((s) => [...s, { id, path: '', label: '' }]);
  };

  const removeSource = (id: string) => {
    setSources((s) => s.filter((x) => x.id !== id));
  };

  const pickFolder = async (index: number) => {
    const r = await openFolderDialog({ title: 'Select skill source root folder' });
    if (r.cancelled || !r.path) return;
    setSources((prev) => {
      const copy = [...prev];
      if (copy[index]) copy[index] = { ...copy[index], path: r.path! };
      return copy;
    });
  };

  const pickCustomDest = async () => {
    const r = await openFolderDialog({ title: 'Select destination skills root (e.g. .cursor/skills)' });
    if (!r.cancelled && r.path) setCustomDest(r.path);
  };

  const runDeploy = () => {
    const targets: ('cursor' | 'copilot')[] = [];
    if (deployCursor) targets.push('cursor');
    if (deployCopilot) targets.push('copilot');
    const custom = customDest.trim() || undefined;

    const deployments: { skillName: string; sourceId: string; targets: ('cursor' | 'copilot')[] }[] = [];
    for (const key of selected) {
      if (key.startsWith('global:')) continue;
      const row = skills.find((s) => `${s.skillName}\0${s.sourceId}` === key);
      if (!row) continue;
      deployments.push({
        skillName: row.skillName,
        sourceId: row.sourceId,
        targets: targets.length ? targets : [],
      });
    }
    if (deployments.length === 0) {
      toast.error('Select at least one skill from your sources (global-only rows cannot be deployed without a source)');
      return;
    }
    if (targets.length === 0 && !custom) {
      toast.error('Choose Cursor and/or Copilot, or set a one-time custom folder');
      return;
    }
    setActionBusy('deploy');
    requestAiSkillsHubDeploy(deployments, custom);
  };

  const runRemove = () => {
    if (!deployCursor && !deployCopilot) {
      toast.error('Select Cursor and/or Copilot to remove from');
      return;
    }
    const names = new Set<string>();
    for (const key of selected) {
      if (key.startsWith('global:')) {
        names.add(key.slice('global:'.length));
        continue;
      }
      const row = skills.find((s) => `${s.skillName}\0${s.sourceId}` === key);
      if (row) names.add(row.skillName);
    }
    if (names.size === 0) {
      toast.error('Select at least one skill');
      return;
    }
    const targets: ('cursor' | 'copilot')[] = [];
    if (deployCursor) targets.push('cursor');
    if (deployCopilot) targets.push('copilot');
    setActionBusy('remove');
    requestAiSkillsHubRemove([...names], targets);
  };

  const runPull = () => {
    if (!pullSourceId) {
      toast.error('Choose a destination source');
      return;
    }
    const names = new Set<string>();
    for (const key of selected) {
      if (key.startsWith('global:')) {
        names.add(key.slice('global:'.length));
        continue;
      }
      const row = skills.find((s) => `${s.skillName}\0${s.sourceId}` === key);
      if (row) names.add(row.skillName);
    }
    if (names.size === 0) {
      toast.error('Select at least one skill');
      return;
    }
    setActionBusy('pull');
    requestAiSkillsHubPull([...names], pullTarget, pullSourceId);
  };

  return (
    <ScrollArea className="h-full">
      <div className="p-6 max-w-7xl mx-auto space-y-6">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">AI Skills Hub</h1>
          <p className="text-sm text-muted-foreground mt-1">
            Sync skill folders (SKILL.md) between your source roots and global Cursor / Copilot skill directories.
          </p>
        </div>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-base">Source roots (saved in app settings)</CardTitle>
            <div className="flex gap-2">
              <Button type="button" variant="outline" size="sm" onClick={addSource} disabled={isSaving}>
                <Plus className="h-4 w-4 mr-1" />
                Add source
              </Button>
              <Button type="button" size="sm" onClick={saveHubSettings} disabled={isSaving}>
                {isSaving ? <Loader2 className="h-4 w-4 mr-1 animate-spin" /> : <Save className="h-4 w-4 mr-1" />}
                {isSaving ? 'Saving…' : 'Save sources'}
              </Button>
            </div>
          </CardHeader>
          <CardContent className="space-y-4">
            {sources.length === 0 && (
              <p className="text-sm text-muted-foreground">Add at least one folder that contains skill subfolders.</p>
            )}
            {sources.map((src, i) => (
              <div key={src.id} className="flex flex-wrap gap-2 items-end border rounded-md p-3">
                <div className="grid gap-1 flex-1 min-w-[200px]">
                  <Label className="text-xs">Label</Label>
                  <Input
                    value={src.label}
                    onChange={(e) => {
                      const v = e.target.value;
                      setSources((prev) => {
                        const c = [...prev];
                        c[i] = { ...c[i], label: v };
                        return c;
                      });
                    }}
                    placeholder="Optional"
                  />
                </div>
                <div className="grid gap-1 flex-[2] min-w-[240px]">
                  <Label className="text-xs">Path</Label>
                  <Input
                    value={src.path}
                    onChange={(e) => {
                      const v = e.target.value;
                      setSources((prev) => {
                        const c = [...prev];
                        c[i] = { ...c[i], path: v };
                        return c;
                      });
                    }}
                    placeholder="C:\path\to\shared\skills"
                  />
                </div>
                <Button type="button" variant="secondary" size="icon" onClick={() => pickFolder(i)} title="Browse">
                  <FolderOpen className="h-4 w-4" />
                </Button>
                <Button type="button" variant="ghost" size="icon" onClick={() => removeSource(src.id)} title="Remove">
                  <Trash2 className="h-4 w-4 text-destructive" />
                </Button>
              </div>
            ))}

            <div className="grid md:grid-cols-2 gap-4">
              <div className="grid gap-1">
                <Label className="text-xs">Override Cursor skills root (optional)</Label>
                <Input
                  value={cursorPath}
                  onChange={(e) => setCursorPath(e.target.value)}
                  placeholder={`Default: ${roots.cursor || '%USERPROFILE%\\.cursor\\skills'}`}
                />
              </div>
              <div className="grid gap-1">
                <Label className="text-xs">Override Copilot skills root (optional)</Label>
                <Input
                  value={copilotPath}
                  onChange={(e) => setCopilotPath(e.target.value)}
                  placeholder={`Default: ${roots.copilot || '%USERPROFILE%\\.copilot\\skills'}`}
                />
              </div>
            </div>
            <p className="text-xs text-muted-foreground">
              Effective Cursor root: {roots.cursor || '—'} · Effective Copilot root: {roots.copilot || '—'}
            </p>
          </CardContent>
        </Card>

        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <div>
              <CardTitle className="text-base">Skills from sources</CardTitle>
              <p className="text-xs text-muted-foreground font-normal mt-1">
                Compared to your global Cursor/Copilot folders. Deploy pushes from the listed source folder.
              </p>
            </div>
            <Button type="button" variant="outline" size="sm" onClick={refreshList} disabled={loading}>
              <RefreshCcw className={cn('h-4 w-4 mr-1', loading && 'animate-spin')} />
              Refresh
            </Button>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex flex-wrap gap-4 items-center text-sm">
              <div className="flex items-center gap-2">
                <Checkbox
                  id="dc"
                  checked={deployCursor}
                  onCheckedChange={(v) => setDeployCursor(v === true)}
                />
                <Label htmlFor="dc">Cursor</Label>
              </div>
              <div className="flex items-center gap-2">
                <Checkbox
                  id="dp"
                  checked={deployCopilot}
                  onCheckedChange={(v) => setDeployCopilot(v === true)}
                />
                <Label htmlFor="dp">Copilot</Label>
              </div>
            </div>
            <div className="flex flex-wrap gap-2 items-end">
              <div className="grid gap-1 flex-1 min-w-[200px]">
                <Label className="text-xs">One-time custom destination (skill root)</Label>
                <Input
                  value={customDest}
                  onChange={(e) => setCustomDest(e.target.value)}
                  placeholder="Paste path or browse…"
                />
              </div>
              <Button type="button" variant="secondary" onClick={pickCustomDest}>
                Browse
              </Button>
            </div>

            <div className="flex flex-wrap gap-2">
              <Button
                type="button"
                onClick={runDeploy}
                disabled={selected.size === 0 || actionBusy !== 'idle'}
              >
                {actionBusy === 'deploy' ? <Loader2 className="h-4 w-4 mr-1 animate-spin" /> : null}
                Deploy selected
              </Button>
              <Button
                type="button"
                variant="destructive"
                onClick={runRemove}
                disabled={selected.size === 0 || actionBusy !== 'idle'}
              >
                {actionBusy === 'remove' ? <Loader2 className="h-4 w-4 mr-1 animate-spin" /> : null}
                Remove from global
              </Button>
            </div>

            <div className="flex flex-wrap gap-4 items-end border-t pt-4">
              <div className="grid gap-1">
                <Label className="text-xs">Pull from</Label>
                <Select value={pullTarget} onValueChange={(v) => setPullTarget(v as 'cursor' | 'copilot')}>
                  <SelectTrigger className="w-[140px]">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="cursor">Cursor global</SelectItem>
                    <SelectItem value="copilot">Copilot global</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              <div className="grid gap-1 min-w-[200px]">
                <Label className="text-xs">Into source</Label>
                <Select value={pullSourceId} onValueChange={setPullSourceId}>
                  <SelectTrigger>
                    <SelectValue placeholder="Choose source" />
                  </SelectTrigger>
                  <SelectContent>
                    {sources.map((s) => (
                      <SelectItem key={s.id} value={s.id}>
                        {s.label || s.path || s.id}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <Button
                type="button"
                variant="secondary"
                onClick={runPull}
                disabled={selected.size === 0 || !pullSourceId || actionBusy !== 'idle'}
              >
                {actionBusy === 'pull' ? <Loader2 className="h-4 w-4 mr-1 animate-spin" /> : null}
                Pull into source
              </Button>
            </div>

            <div className="border rounded-md overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b bg-muted/40 text-left">
                    <th className="p-2 w-10" />
                    <th className="p-2">Skill</th>
                    <th className="p-2">Source</th>
                    <th className="p-2">Cursor</th>
                    <th className="p-2">Copilot</th>
                  </tr>
                </thead>
                <tbody>
                  {skills.length === 0 && (
                    <tr>
                      <td colSpan={5} className="p-4 text-muted-foreground">
                        {loading ? 'Loading…' : 'No skills under any configured source. Add folders above and refresh.'}
                      </td>
                    </tr>
                  )}
                  {skills.map((row) => {
                    const key = `${row.skillName}\0${row.sourceId}`;
                    return (
                      <tr key={key} className="border-b last:border-0">
                        <td className="p-2">
                          <Checkbox
                            checked={selected.has(key)}
                            onCheckedChange={() => toggleSelect(key)}
                            aria-label={`Select ${row.skillName}`}
                          />
                        </td>
                        <td className="p-2 font-medium">
                          {row.skillName}
                          {row.nameConflict && (
                            <Badge variant="destructive" className="ml-2">
                              name conflict
                            </Badge>
                          )}
                          {!row.hasSkillMd && (
                            <Badge variant="outline" className="ml-2">
                              no SKILL.md
                            </Badge>
                          )}
                        </td>
                        <td className="p-2 text-muted-foreground text-xs">{row.sourceLabel || row.sourceId}</td>
                        <td className="p-2">{syncBadge(row.cursorSync)}</td>
                        <td className="p-2">{syncBadge(row.copilotSync)}</td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>

            <div className="border-t pt-6 space-y-2">
              <div>
                <h3 className="text-sm font-semibold">Global only (not in any source)</h3>
                <p className="text-xs text-muted-foreground">
                  These folders exist under your Cursor and/or Copilot global skill directories but do not appear under
                  any path you added here. You can remove them from global or pull them into a source folder.
                </p>
              </div>
              <div className="border rounded-md overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b bg-muted/40 text-left">
                      <th className="p-2 w-10" />
                      <th className="p-2">Skill</th>
                      <th className="p-2">On Cursor global</th>
                      <th className="p-2">On Copilot global</th>
                      <th className="p-2">SKILL.md</th>
                    </tr>
                  </thead>
                  <tbody>
                    {globalOnlySkills.length === 0 && (
                      <tr>
                        <td colSpan={5} className="p-4 text-muted-foreground">
                          {loading ? 'Loading…' : 'None — every global skill matches a folder under your sources, or globals are empty.'}
                        </td>
                      </tr>
                    )}
                    {globalOnlySkills.map((row) => {
                      const gkey = `global:${row.skillName}`;
                      return (
                        <tr key={gkey} className="border-b last:border-0 bg-muted/20">
                          <td className="p-2">
                            <Checkbox
                              checked={selected.has(gkey)}
                              onCheckedChange={() => toggleSelect(gkey)}
                              aria-label={`Select ${row.skillName}`}
                            />
                          </td>
                          <td className="p-2 font-medium">
                            {row.skillName}
                            <Badge variant="outline" className="ml-2">
                              global only
                            </Badge>
                          </td>
                          <td className="p-2">{globalPresence(row.inCursor)}</td>
                          <td className="p-2">{globalPresence(row.inCopilot)}</td>
                          <td className="p-2 text-xs text-muted-foreground">
                            {[
                              row.inCursor ? (row.hasSkillMdCursor ? 'Cursor ✓' : 'Cursor ✗') : null,
                              row.inCopilot ? (row.hasSkillMdCopilot ? 'Copilot ✓' : 'Copilot ✗') : null,
                            ]
                              .filter(Boolean)
                              .join(' · ') || '—'}
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    </ScrollArea>
  );
}
