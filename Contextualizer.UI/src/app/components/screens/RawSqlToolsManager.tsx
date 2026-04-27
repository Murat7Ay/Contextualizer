import { useEffect, useMemo, useState } from 'react';
import { toast } from 'sonner';
import {
  Check,
  ChevronsUpDown,
  Database,
  FileCog,
  FileKey2,
  FilePlus2,
  RefreshCcw,
  Save,
  Search,
  Trash2,
} from 'lucide-react';
import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle } from '../ui/alert-dialog';
import { Badge } from '../ui/badge';
import { Button } from '../ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
import { Checkbox } from '../ui/checkbox';
import { Command, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList } from '../ui/command';
import { Input } from '../ui/input';
import { Label } from '../ui/label';
import { Popover, PopoverContent, PopoverTrigger } from '../ui/popover';
import { ScrollArea } from '../ui/scroll-area';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../ui/select';
import { Textarea } from '../ui/textarea';
import { cn } from '../ui/utils';
import { useActivityLogStore } from '../../stores/activityLogStore';
import { useDataToolsStore } from '../../stores/dataToolsStore';
import { useHostStore } from '../../stores/hostStore';
import {
  addWebView2MessageListener,
  deleteRawSqlTool,
  openExternalUrl,
  requestConfigConnections,
  requestRawSqlToolsList,
  saveRawSqlTool,
} from '../../host/webview2Bridge';

type RawSqlToolDto = {
  tool_name: string;
  provider: string;
  connection_template: string;
  description?: string | null;
  allowed_modes: string[];
  source_file_type: 'config' | 'secrets';
  effective_description: string;
};

type RawSqlToolDraft = {
  originalToolName: string | null;
  originalFileType: 'config' | 'secrets';
  toolName: string;
  description: string;
  provider: string;
  connectionTemplate: string;
  allowedModes: string[];
  fileType: 'config' | 'secrets';
};

type RawSqlToolsListMessage = {
  type: 'raw_sql_tools_list';
  definitions: RawSqlToolDto[];
  configFilePath?: string | null;
  secretsFilePath?: string | null;
};

type RawSqlToolSaveResultMessage = {
  type: 'raw_sql_tool_save_result';
  ok: boolean;
  toolName?: string;
  fileType?: string;
  error?: string;
};

type RawSqlToolDeleteResultMessage = {
  type: 'raw_sql_tool_delete_result';
  ok: boolean;
  toolName?: string;
  fileType?: string;
  error?: string;
};

const providerOptions = ['mssql', 'plsql'];
const modeOptions = ['select', 'scalar', 'execute'] as const;

function createEmptyDraft(): RawSqlToolDraft {
  return {
    originalToolName: null,
    originalFileType: 'config',
    toolName: '',
    description: '',
    provider: 'mssql',
    connectionTemplate: '',
    allowedModes: ['select', 'scalar'],
    fileType: 'config',
  };
}

function draftFromTool(definition: RawSqlToolDto): RawSqlToolDraft {
  return {
    originalToolName: definition.tool_name,
    originalFileType: definition.source_file_type,
    toolName: definition.tool_name,
    description: definition.description ?? '',
    provider: definition.provider,
    connectionTemplate: definition.connection_template,
    allowedModes: definition.allowed_modes.length > 0 ? definition.allowed_modes : ['select', 'scalar'],
    fileType: definition.source_file_type,
  };
}

function serializeDraft(draft: RawSqlToolDraft): string {
  return JSON.stringify({
    ...draft,
    toolName: draft.toolName.trim(),
    description: draft.description.trim(),
    provider: draft.provider.trim().toLowerCase(),
    connectionTemplate: draft.connectionTemplate.trim(),
    allowedModes: [...draft.allowedModes].sort(),
  });
}

function buildEffectiveDescription(draft: RawSqlToolDraft): string {
  const baseDescription = draft.description.trim() || `Execute raw SQL against a fixed ${draft.provider || 'mssql'} connection.`;
  if (draft.allowedModes.length === 1)
    return `${baseDescription} Mode is fixed to ${draft.allowedModes[0]}.`;

  return `${baseDescription} Allowed modes: ${draft.allowedModes.join(', ')}.`;
}

function ConnectionCombobox({
  value,
  onChange,
  connectionKeys,
}: {
  value: string;
  onChange: (value: string) => void;
  connectionKeys: string[];
}) {
  const [open, setOpen] = useState(false);
  const [search, setSearch] = useState('');

  useEffect(() => {
    if (open) setSearch(value);
  }, [open, value]);

  const filtered = connectionKeys.filter((key) => key.toLowerCase().includes(search.toLowerCase()));

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <button
          type="button"
          role="combobox"
          aria-expanded={open}
          className={cn(
            'flex h-9 w-full items-center justify-between rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background',
            'focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2',
          )}
        >
          <span className={cn('truncate font-mono', !value && 'font-sans text-muted-foreground')}>
            {value || '$config:connections.main_mssql'}
          </span>
          <ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 text-muted-foreground" />
        </button>
      </PopoverTrigger>
      <PopoverContent className="w-[--radix-popover-trigger-width] p-0" align="start">
        <Command>
          <CommandInput
            placeholder="Type or search connection…"
            value={search}
            onValueChange={(nextValue) => {
              setSearch(nextValue);
              onChange(nextValue);
            }}
          />
          <CommandList>
            {connectionKeys.length > 0 && filtered.length === 0 && <CommandEmpty>No matches.</CommandEmpty>}
            {filtered.length > 0 && (
              <CommandGroup>
                {filtered.map((key) => (
                  <CommandItem
                    key={key}
                    value={key}
                    onSelect={(selected) => {
                      onChange(selected);
                      setSearch('');
                      setOpen(false);
                    }}
                  >
                    <Check className={cn('mr-2 h-4 w-4 shrink-0', value === key ? 'opacity-100' : 'opacity-0')} />
                    <span className="font-mono text-xs">{key}</span>
                  </CommandItem>
                ))}
              </CommandGroup>
            )}
          </CommandList>
        </Command>
      </PopoverContent>
    </Popover>
  );
}

export function RawSqlToolsManager() {
  const webView2Available = useHostStore((state) => state.webView2Available);
  const hostConnected = useHostStore((state) => state.hostConnected);
  const addLog = useActivityLogStore((state) => state.addLog);
  const connectionKeys = useDataToolsStore((state) => state.connectionKeys);

  const [definitions, setDefinitions] = useState<RawSqlToolDto[]>([]);
  const [configFilePath, setConfigFilePath] = useState('');
  const [secretsFilePath, setSecretsFilePath] = useState('');
  const [draft, setDraft] = useState<RawSqlToolDraft>(() => createEmptyDraft());
  const [savedSnapshot, setSavedSnapshot] = useState(() => serializeDraft(createEmptyDraft()));
  const [selectedToolName, setSelectedToolName] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isBusy, setIsBusy] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [discardDialogOpen, setDiscardDialogOpen] = useState(false);
  const [pendingDraft, setPendingDraft] = useState<RawSqlToolDraft | null>(null);

  const disabled = !webView2Available || !hostConnected || isBusy;
  const isDirty = serializeDraft(draft) !== savedSnapshot;

  useEffect(() => {
    if (!webView2Available || !hostConnected) return;

    requestRawSqlToolsList();
    requestConfigConnections();
  }, [hostConnected, webView2Available]);

  useEffect(() => {
    const unsubscribe = addWebView2MessageListener((payload) => {
      if (!payload || typeof payload !== 'object') return;

      const message = payload as { type?: string };

      if (message.type === 'raw_sql_tools_list') {
        const listMessage = payload as RawSqlToolsListMessage;
        const nextDefinitions = Array.isArray(listMessage.definitions) ? listMessage.definitions : [];

        setDefinitions(nextDefinitions);
        setConfigFilePath(listMessage.configFilePath ?? '');
        setSecretsFilePath(listMessage.secretsFilePath ?? '');
        setError(null);

        if (selectedToolName) {
          const selected = nextDefinitions.find((definition) => definition.tool_name === selectedToolName);
          if (!selected) {
            const emptyDraft = createEmptyDraft();
            setDraft(emptyDraft);
            setSavedSnapshot(serializeDraft(emptyDraft));
            setSelectedToolName(null);
          }
        }

        return;
      }

      if (message.type === 'raw_sql_tool_save_result') {
        const saveMessage = payload as RawSqlToolSaveResultMessage;
        setIsBusy(false);

        if (!saveMessage.ok) {
          const nextError = saveMessage.error ?? 'Failed to save raw SQL tool.';
          setError(nextError);
          addLog('error', 'Raw SQL tool save failed', nextError);
          toast.error(nextError);
          return;
        }

        const normalizedDraft: RawSqlToolDraft = {
          ...draft,
          originalToolName: draft.toolName.trim(),
          originalFileType: draft.fileType,
          toolName: draft.toolName.trim(),
          description: draft.description.trim(),
          connectionTemplate: draft.connectionTemplate.trim(),
        };
        setDraft(normalizedDraft);
        setSavedSnapshot(serializeDraft(normalizedDraft));
        setSelectedToolName(normalizedDraft.toolName);
        setError(null);
        addLog('success', `Raw SQL tool '${normalizedDraft.toolName}' saved`);
        toast.success(`Saved '${normalizedDraft.toolName}'`);
        return;
      }

      if (message.type === 'raw_sql_tool_delete_result') {
        const deleteMessage = payload as RawSqlToolDeleteResultMessage;
        setIsBusy(false);

        if (!deleteMessage.ok) {
          const nextError = deleteMessage.error ?? 'Failed to delete raw SQL tool.';
          setError(nextError);
          addLog('error', 'Raw SQL tool delete failed', nextError);
          toast.error(nextError);
          return;
        }

        const emptyDraft = createEmptyDraft();
        setDraft(emptyDraft);
        setSavedSnapshot(serializeDraft(emptyDraft));
        setSelectedToolName(null);
        setDeleteDialogOpen(false);
        setError(null);
        addLog('success', `Raw SQL tool '${deleteMessage.toolName ?? ''}' deleted`);
        toast.success(`Deleted '${deleteMessage.toolName ?? ''}'`);
      }
    });

    return () => {
      try {
        unsubscribe();
      } catch {
        // noop
      }
    };
  }, [addLog, draft, selectedToolName]);

  const filteredDefinitions = useMemo(() => {
    const query = search.trim().toLowerCase();
    if (!query) return definitions;

    return definitions.filter((definition) => {
      return (
        definition.tool_name.toLowerCase().includes(query) ||
        definition.provider.toLowerCase().includes(query) ||
        definition.connection_template.toLowerCase().includes(query) ||
        (definition.description ?? '').toLowerCase().includes(query)
      );
    });
  }, [definitions, search]);

  const descriptionPreview = useMemo(() => buildEffectiveDescription(draft), [draft]);

  function resetDraft(nextDraft: RawSqlToolDraft) {
    setDraft(nextDraft);
    setSavedSnapshot(serializeDraft(nextDraft));
  }

  function requestSelect(nextDraft: RawSqlToolDraft) {
    if (isDirty) {
      setPendingDraft(nextDraft);
      setDiscardDialogOpen(true);
      return;
    }

    resetDraft(nextDraft);
    setSelectedToolName(nextDraft.originalToolName ?? null);
    setError(null);
  }

  function handleNewTool() {
    requestSelect(createEmptyDraft());
  }

  function handleSelectTool(definition: RawSqlToolDto) {
    requestSelect(draftFromTool(definition));
  }

  function toggleMode(mode: (typeof modeOptions)[number]) {
    setDraft((currentDraft) => {
      const hasMode = currentDraft.allowedModes.includes(mode);
      const nextModes = hasMode
        ? currentDraft.allowedModes.filter((entry) => entry !== mode)
        : [...currentDraft.allowedModes, mode];

      return {
        ...currentDraft,
        allowedModes: nextModes,
      };
    });
  }

  function handleOpenPath(path: string) {
    if (!path.trim()) {
      toast.error('Path is empty.');
      return;
    }

    const ok = openExternalUrl(path);
    if (!ok) toast.error('Host bridge is not available.');
  }

  function handleSave() {
    const toolName = draft.toolName.trim();
    const connectionTemplate = draft.connectionTemplate.trim();

    if (!toolName) {
      const nextError = 'Tool name is required.';
      setError(nextError);
      toast.error(nextError);
      return;
    }

    if (!connectionTemplate) {
      const nextError = 'Connection template is required.';
      setError(nextError);
      toast.error(nextError);
      return;
    }

    if (draft.allowedModes.length === 0) {
      const nextError = 'Select at least one mode.';
      setError(nextError);
      toast.error(nextError);
      return;
    }

    setIsBusy(true);
    setError(null);

    const ok = saveRawSqlTool({
      originalToolName: draft.originalToolName,
      originalFileType: draft.originalFileType,
      toolName,
      description: draft.description.trim(),
      provider: draft.provider,
      connectionTemplate,
      allowedModes: draft.allowedModes,
      fileType: draft.fileType,
    });

    if (!ok) {
      setIsBusy(false);
      setError('Host bridge is not available.');
      toast.error('Host bridge is not available.');
    }
  }

  function handleDelete() {
    if (!draft.originalToolName) return;

    setIsBusy(true);
    const ok = deleteRawSqlTool(draft.originalToolName, draft.originalFileType);
    if (!ok) {
      setIsBusy(false);
      setError('Host bridge is not available.');
      toast.error('Host bridge is not available.');
    }
  }

  const selectedDefinition = definitions.find((definition) => definition.tool_name === selectedToolName) ?? null;

  return (
    <div className="p-6 max-w-[1800px] mx-auto space-y-6">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
        <div>
          <h2 className="text-[28px] font-semibold mb-2">Raw SQL Tools</h2>
          <p className="text-sm text-muted-foreground">
            Fixed-connection MCP tools for one-off investigation. You choose the modes; the LLM-facing description comes from the field below.
          </p>
        </div>

        <div className="flex flex-wrap gap-2">
          <Button variant="outline" onClick={handleNewTool} disabled={disabled}>
            <FilePlus2 className="h-4 w-4 mr-2" />
            New
          </Button>
          <Button variant="outline" onClick={() => requestRawSqlToolsList()} disabled={!webView2Available || !hostConnected || isBusy}>
            <RefreshCcw className="h-4 w-4 mr-2" />
            Reload
          </Button>
          <Button variant="outline" onClick={() => handleOpenPath(configFilePath)} disabled={!configFilePath.trim()}>
            <FileCog className="h-4 w-4 mr-2" />
            Open Config
          </Button>
          <Button variant="outline" onClick={() => handleOpenPath(secretsFilePath)} disabled={!secretsFilePath.trim()}>
            <FileKey2 className="h-4 w-4 mr-2" />
            Open Secrets
          </Button>
        </div>
      </div>

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-[360px_minmax(0,1fr)]">
        <Card className="h-[calc(100vh-260px)] min-h-[640px]">
          <CardHeader className="space-y-3">
            <div className="flex items-center justify-between gap-3">
              <div>
                <CardTitle>Defined Tools</CardTitle>
                <CardDescription>{definitions.length} effective raw SQL tools</CardDescription>
              </div>
              <Badge variant="secondary">MCP</Badge>
            </div>

            <div className="relative">
              <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <Input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search tools..." className="pl-9" />
            </div>
          </CardHeader>

          <CardContent className="h-[calc(100%-128px)]">
            <ScrollArea className="h-full pr-3">
              {filteredDefinitions.length === 0 ? (
                <div className="rounded-lg border border-dashed p-6 text-sm text-muted-foreground">
                  No raw SQL tools match the current filter.
                </div>
              ) : (
                <div className="space-y-3">
                  {filteredDefinitions.map((definition) => {
                    const isSelected = definition.tool_name === selectedToolName;

                    return (
                      <button
                        key={`${definition.source_file_type}:${definition.tool_name}`}
                        type="button"
                        className={cn(
                          'w-full rounded-xl border p-4 text-left transition-colors hover:bg-accent/40',
                          isSelected && 'border-primary bg-accent/60',
                        )}
                        onClick={() => handleSelectTool(definition)}
                      >
                        <div className="space-y-2">
                          <div className="flex flex-wrap items-center gap-2">
                            <div className="font-medium">{definition.tool_name}</div>
                            <Badge variant={definition.source_file_type === 'secrets' ? 'destructive' : 'secondary'}>
                              {definition.source_file_type}
                            </Badge>
                            <Badge variant="outline">{definition.provider}</Badge>
                          </div>

                          <div className="text-xs font-mono text-muted-foreground truncate">{definition.connection_template}</div>

                          <div className="flex flex-wrap gap-1">
                            {definition.allowed_modes.map((mode) => (
                              <Badge key={mode} variant="outline">
                                {mode}
                              </Badge>
                            ))}
                          </div>

                          <p className="text-xs text-muted-foreground line-clamp-3">{definition.effective_description}</p>
                        </div>
                      </button>
                    );
                  })}
                </div>
              )}
            </ScrollArea>
          </CardContent>
        </Card>

        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>Editor</CardTitle>
              <CardDescription>
                Save raw SQL MCP tools into config or secrets. If only one mode is selected, the MCP schema hides the `mode` field from the LLM.
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              {error && <div className="rounded-lg border border-destructive/40 bg-destructive/5 px-4 py-3 text-sm text-destructive">{error}</div>}

              <div className="grid gap-4 md:grid-cols-2">
                <div className="space-y-2">
                  <Label htmlFor="raw-sql-tool-name">Tool Name</Label>
                  <Input
                    id="raw-sql-tool-name"
                    value={draft.toolName}
                    onChange={(event) => setDraft((currentDraft) => ({ ...currentDraft, toolName: event.target.value }))}
                    placeholder="db_raw_sql_core_test"
                    disabled={disabled}
                  />
                </div>

                <div className="space-y-2">
                  <Label>Target File</Label>
                  <Select value={draft.fileType} onValueChange={(value: 'config' | 'secrets') => setDraft((currentDraft) => ({ ...currentDraft, fileType: value }))} disabled={disabled}>
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="config">config.ini</SelectItem>
                      <SelectItem value="secrets">secrets.ini</SelectItem>
                    </SelectContent>
                  </Select>
                </div>

                <div className="space-y-2">
                  <Label>Provider</Label>
                  <Select value={draft.provider} onValueChange={(value) => setDraft((currentDraft) => ({ ...currentDraft, provider: value }))} disabled={disabled}>
                    <SelectTrigger>
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {providerOptions.map((provider) => (
                        <SelectItem key={provider} value={provider}>
                          {provider}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>

                <div className="space-y-2">
                  <Label>Connection Template</Label>
                  <ConnectionCombobox
                    value={draft.connectionTemplate}
                    onChange={(value) => setDraft((currentDraft) => ({ ...currentDraft, connectionTemplate: value }))}
                    connectionKeys={connectionKeys}
                  />
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="raw-sql-description">LLM Description</Label>
                <Textarea
                  id="raw-sql-description"
                  value={draft.description}
                  onChange={(event) => setDraft((currentDraft) => ({ ...currentDraft, description: event.target.value }))}
                  placeholder="Explain when the MCP client should use this tool and what data it is meant to inspect."
                  className="min-h-[120px]"
                  disabled={disabled}
                />
                <p className="text-xs text-muted-foreground">
                  Keep this short and task-oriented. It becomes the MCP tool description the LLM sees.
                </p>
              </div>

              <div className="space-y-3">
                <Label>Allowed Modes</Label>
                <div className="grid gap-3 md:grid-cols-3">
                  {modeOptions.map((mode) => {
                    const checked = draft.allowedModes.includes(mode);
                    return (
                      <label
                        key={mode}
                        className={cn(
                          'flex cursor-pointer items-start gap-3 rounded-xl border p-4 transition-colors',
                          checked ? 'border-primary bg-primary/5' : 'hover:bg-accent/40',
                          disabled && 'cursor-not-allowed opacity-60',
                        )}
                      >
                        <Checkbox checked={checked} onCheckedChange={() => toggleMode(mode)} disabled={disabled} />
                        <div className="space-y-1">
                          <div className="font-medium capitalize">{mode}</div>
                          <p className="text-xs text-muted-foreground">
                            {mode === 'select'
                              ? 'Returns rows. Good for code investigation and quick reads.'
                              : mode === 'scalar'
                                ? 'Returns a single value such as a count or status.'
                                : 'Runs DML commands. Keep this narrow and intentional.'}
                          </p>
                        </div>
                      </label>
                    );
                  })}
                </div>
              </div>

              <div className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_320px]">
                <Card className="border-dashed">
                  <CardHeader>
                    <CardTitle className="text-base">What MCP Sees</CardTitle>
                    <CardDescription>
                      {draft.allowedModes.length === 1
                        ? 'Only one mode is selected, so the MCP schema can omit the mode field.'
                        : 'Multiple modes are enabled, so the MCP schema keeps an explicit mode field.'}
                    </CardDescription>
                  </CardHeader>
                  <CardContent className="space-y-3 text-sm">
                    <div className="rounded-lg bg-muted/60 p-4 text-sm leading-6">{descriptionPreview}</div>
                    <div className="flex flex-wrap gap-2">
                      {draft.allowedModes.map((mode) => (
                        <Badge key={mode} variant="outline">
                          {mode}
                        </Badge>
                      ))}
                    </div>
                  </CardContent>
                </Card>

                <Card>
                  <CardHeader>
                    <CardTitle className="text-base">Actions</CardTitle>
                    <CardDescription>
                      {selectedDefinition
                        ? `Editing ${selectedDefinition.tool_name} from ${selectedDefinition.source_file_type}.`
                        : 'Create a new raw SQL MCP tool.'}
                    </CardDescription>
                  </CardHeader>
                  <CardContent className="space-y-3">
                    <Button className="w-full" onClick={handleSave} disabled={disabled}>
                      <Save className="h-4 w-4 mr-2" />
                      {draft.originalToolName ? 'Save Changes' : 'Create Tool'}
                    </Button>
                    <Button variant="outline" className="w-full" onClick={() => resetDraft(draft.originalToolName ? draftFromTool(selectedDefinition ?? {
                      tool_name: draft.originalToolName,
                      provider: draft.provider,
                      connection_template: draft.connectionTemplate,
                      description: draft.description,
                      allowed_modes: draft.allowedModes,
                      source_file_type: draft.originalFileType,
                      effective_description: buildEffectiveDescription(draft),
                    }) : createEmptyDraft())} disabled={disabled || !isDirty}>
                      Revert
                    </Button>
                    <Button variant="outline" className="w-full" onClick={handleNewTool} disabled={disabled}>
                      Clear
                    </Button>
                    <Button variant="destructive" className="w-full" onClick={() => setDeleteDialogOpen(true)} disabled={disabled || !draft.originalToolName}>
                      <Trash2 className="h-4 w-4 mr-2" />
                      Delete Tool
                    </Button>
                  </CardContent>
                </Card>
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle className="text-base">Storage</CardTitle>
              <CardDescription>Raw SQL definitions are stored under the [mcp_raw_sql_tools] section.</CardDescription>
            </CardHeader>
            <CardContent className="grid gap-4 lg:grid-cols-2">
              <div className="rounded-xl border bg-muted/30 p-4">
                <div className="mb-1 flex items-center gap-2 font-medium">
                  <Database className="h-4 w-4" />
                  config.ini
                </div>
                <div className="text-xs text-muted-foreground break-all">{configFilePath || 'Not configured'}</div>
              </div>
              <div className="rounded-xl border bg-muted/30 p-4">
                <div className="mb-1 flex items-center gap-2 font-medium">
                  <FileKey2 className="h-4 w-4" />
                  secrets.ini
                </div>
                <div className="text-xs text-muted-foreground break-all">{secretsFilePath || 'Not configured'}</div>
              </div>
            </CardContent>
          </Card>
        </div>
      </div>

      <AlertDialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Delete raw SQL tool?</AlertDialogTitle>
            <AlertDialogDescription>
              This removes the selected entry from {draft.originalFileType}. If another file still defines the same tool name, that version may become visible again.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel disabled={isBusy}>Cancel</AlertDialogCancel>
            <AlertDialogAction onClick={handleDelete} disabled={isBusy}>
              Delete
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      <AlertDialog open={discardDialogOpen} onOpenChange={setDiscardDialogOpen}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Discard unsaved changes?</AlertDialogTitle>
            <AlertDialogDescription>
              You have unsaved raw SQL tool edits. Continue and lose those changes?
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel onClick={() => setPendingDraft(null)}>Cancel</AlertDialogCancel>
            <AlertDialogAction
              onClick={() => {
                if (!pendingDraft) return;
                resetDraft(pendingDraft);
                setSelectedToolName(pendingDraft.originalToolName ?? null);
                setPendingDraft(null);
                setDiscardDialogOpen(false);
              }}
            >
              Discard Changes
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}