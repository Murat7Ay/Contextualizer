import React, { useEffect, useMemo, useState } from 'react';
import { toast } from 'sonner';
import { CountdownToastView } from '../ui/countdown-toast';
import { Button } from '../ui/button';
import { Input } from '../ui/input';
import { Label } from '../ui/label';
import { Switch } from '../ui/switch';
import { RadioGroup, RadioGroupItem } from '../ui/radio-group';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../ui/select';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../ui/card';
import { ScrollArea } from '../ui/scroll-area';
import { Skeleton } from '../ui/skeleton';
import { 
  Settings as SettingsIcon, 
  Folder, 
  Keyboard, 
  Zap, 
  Lock, 
  Cloud, 
  Download, 
  FileText, 
  Code 
} from 'lucide-react';
import { cn } from '../ui/utils';
import { useAppStore, type Theme } from '../../stores/appStore';
import { useAppSettingsStore, type AppSettingsDto, type LogLevel } from '../../stores/appSettingsStore';
import { useHostStore } from '../../stores/hostStore';
import { sendPing } from '../../host/initHostBridge';
import {
  openFileDialog,
  openFolderDialog,
  postWebView2Message,
  requestAppSettings,
  requestClearLogs,
  requestLoggingTest,
  requestUsageTest,
  saveAppSettings,
} from '../../host/webview2Bridge';

type SettingsSection = 
  | 'general' 
  | 'file-paths' 
  | 'keyboard' 
  | 'performance' 
  | 'config' 
  | 'network' 
  | 'deployment' 
  | 'logging' 
  | 'advanced';

const sections = [
  { id: 'general', label: 'General', icon: SettingsIcon },
  { id: 'file-paths', label: 'File Paths', icon: Folder },
  { id: 'keyboard', label: 'Keyboard', icon: Keyboard },
  { id: 'performance', label: 'Performance', icon: Zap },
  { id: 'config', label: 'Config System', icon: Lock },
  { id: 'network', label: 'Network Updates', icon: Cloud },
  { id: 'deployment', label: 'Initial Deployment', icon: Download },
  { id: 'logging', label: 'Logging', icon: FileText },
  { id: 'advanced', label: 'Advanced', icon: Code },
] as const;

export function SettingsScreen() {
  const [activeSection, setActiveSection] = useState<SettingsSection>('general');

  const webView2Available = useHostStore((state) => state.webView2Available);
  const hostConnected = useHostStore((state) => state.hostConnected);

  const loaded = useAppSettingsStore((s) => s.loaded);
  const settings = useAppSettingsStore((s) => s.settings);
  const draft = useAppSettingsStore((s) => s.draft);
  const isSaving = useAppSettingsStore((s) => s.isSaving);
  const lastSaveError = useAppSettingsStore((s) => s.lastSaveError);
  const lastSavedAt = useAppSettingsStore((s) => s.lastSavedAt);
  const resetDraft = useAppSettingsStore((s) => s.resetDraft);
  const setSaving = useAppSettingsStore((s) => s.setSaving);
  const setSaveError = useAppSettingsStore((s) => s.setSaveError);

  const appTheme = useAppStore((s) => s.theme);
  const setAppTheme = useAppStore((s) => s.setTheme);

  useEffect(() => {
    if (!webView2Available) return;
    if (!hostConnected) return;
    if (loaded) return;
    requestAppSettings();
  }, [hostConnected, loaded, webView2Available]);

  const isDirty = useMemo(() => {
    if (!settings || !draft) return false;
    try {
      return JSON.stringify(settings) !== JSON.stringify(draft);
    } catch {
      return true;
    }
  }, [draft, settings]);

  const canSave = webView2Available && hostConnected && !!draft && isDirty && !isSaving;
  const canCancel = !!draft && isDirty && !isSaving;

  function handleCancel() {
    setSaveError(null);
    resetDraft();

    const originalTheme = settings?.uiSettings?.theme;
    if (originalTheme && originalTheme !== appTheme) {
      setAppTheme(originalTheme as Theme);
      postWebView2Message({ type: 'set_theme', theme: originalTheme });
    }
  }

  function handleSave() {
    if (!draft) return;

    setSaveError(null);
    setSaving(true);

    const ok = saveAppSettings(draft);
    if (!ok) {
      const err = 'Host is not available';
      setSaving(false);
      setSaveError(err);
      toast.custom(
        (id) => (
          <CountdownToastView
            level="error"
            title="Settings"
            message="Failed to save settings"
            details={err}
            durationMs={5000}
            onClose={() => toast.dismiss(id)}
            onExpire={() => toast.dismiss(id)}
          />
        ),
        { duration: Infinity },
      );
    }
  }

  return (
    <div className="flex h-full">
      {/* Left Navigation Sidebar */}
      <div className="w-60 border-r bg-card">
        <ScrollArea className="h-full">
          <div className="p-4 space-y-1">
            {sections.map((section) => {
              const Icon = section.icon;
              return (
                <button
                  key={section.id}
                  onClick={() => setActiveSection(section.id as SettingsSection)}
                  className={cn(
                    "w-full flex items-center gap-3 px-3 py-2 rounded-md text-sm transition-colors",
                    "hover:bg-accent hover:text-accent-foreground",
                    activeSection === section.id && "bg-accent text-accent-foreground font-medium"
                  )}
                >
                  <Icon className="h-4 w-4" />
                  {section.label}
                </button>
              );
            })}
          </div>
        </ScrollArea>
      </div>

      {/* Content Area */}
      <ScrollArea className="flex-1">
        <div className="p-6 max-w-4xl">
          {!webView2Available && (
            <div className="mb-6 p-3 rounded-md border bg-muted/30 text-sm">
              <div className="font-semibold mb-1">Host not detected</div>
              <div className="text-muted-foreground">
                Settings are loaded from the WPF host. In browser/dev mode, only UI theme preview works and saving is disabled.
              </div>
            </div>
          )}

          {lastSaveError && (
            <div className="mb-6 p-3 rounded-md border border-destructive/30 bg-destructive/5 text-sm">
              <div className="font-semibold mb-1">Save failed</div>
              <div className="text-muted-foreground">{lastSaveError}</div>
            </div>
          )}

          {webView2Available && hostConnected && !draft ? (
            <SettingsLoading />
          ) : (
            <>
              {activeSection === 'general' && <GeneralSettings />}
              {activeSection === 'file-paths' && <FilePathsSettings />}
              {activeSection === 'keyboard' && <KeyboardSettings />}
              {activeSection === 'performance' && <PerformanceSettings />}
              {activeSection === 'config' && <ConfigSystemSettings />}
              {activeSection === 'network' && <NetworkUpdateSettings />}
              {activeSection === 'deployment' && <DeploymentSettings />}
              {activeSection === 'logging' && <LoggingSettings />}
              {activeSection === 'advanced' && <AdvancedSettings />}
            </>
          )}

          {/* Action Buttons */}
          <div className="sticky bottom-0 bg-background pt-4 mt-8 border-t flex items-center justify-between gap-3">
            <div className="text-xs text-muted-foreground">
              {isSaving
                ? 'Saving…'
                : lastSavedAt
                  ? `Last saved: ${lastSavedAt.toLocaleString()}`
                  : ''}
            </div>
            <div className="flex justify-end gap-3">
              <Button variant="outline" onClick={handleCancel} disabled={!canCancel}>
                Cancel
              </Button>
              <Button onClick={handleSave} disabled={!canSave}>
                Save Changes
              </Button>
            </div>
          </div>
        </div>
      </ScrollArea>
    </div>
  );
}

function SettingsLoading() {
  return (
    <div className="space-y-6">
      <div>
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-4 w-80 mt-2" />
      </div>

      <Card>
        <CardHeader>
          <Skeleton className="h-5 w-40" />
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-2">
            <Skeleton className="h-4 w-32" />
            <Skeleton className="h-9 w-full" />
          </div>
          <div className="space-y-2">
            <Skeleton className="h-4 w-40" />
            <Skeleton className="h-9 w-full" />
          </div>
          <div className="space-y-2">
            <Skeleton className="h-4 w-44" />
            <Skeleton className="h-9 w-full" />
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

function GeneralSettings() {
  const webView2Available = useHostStore((state) => state.webView2Available);
  const theme = useAppStore((state) => state.theme);
  const setTheme = useAppStore((state) => state.setTheme);
  const draft = useAppSettingsStore((s) => s.draft);
  const setDraft = useAppSettingsStore((s) => s.setDraft);

  const selectedTheme = (draft?.uiSettings?.theme ?? theme) as Theme;

  function handleThemeChange(value: string) {
    const next = value as Theme;
    setTheme(next);
    if (draft) {
      setDraft({
        ...draft,
        uiSettings: {
          ...draft.uiSettings,
          theme: next,
        },
      });
    }

    if (webView2Available) {
      postWebView2Message({ type: 'set_theme', theme: next });
    }
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-[28px] font-semibold mb-2">General Settings</h1>
        <p className="text-sm text-muted-foreground">Configure basic application preferences</p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Appearance</CardTitle>
        </CardHeader>
        <CardContent>
          <Label className="text-sm font-semibold mb-3 block">Theme</Label>
          <RadioGroup value={selectedTheme} onValueChange={handleThemeChange} className="space-y-3">
            <div className="flex items-center space-x-2">
              <RadioGroupItem value="light" id="light" />
              <Label htmlFor="light" className="font-normal cursor-pointer">Light</Label>
            </div>
            <div className="flex items-center space-x-2">
              <RadioGroupItem value="dark" id="dark" />
              <Label htmlFor="dark" className="font-normal cursor-pointer">Dark</Label>
            </div>
          </RadioGroup>
          {draft && (
            <p className="text-xs text-muted-foreground mt-3">
              Theme changes apply immediately; click <span className="font-medium">Save Changes</span> to persist.
            </p>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Language & Region</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-2">
            <Label className="text-sm font-semibold">Language</Label>
            <Select defaultValue="en-us" disabled>
              <SelectTrigger disabled>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="en-us">English (US)</SelectItem>
                <SelectItem value="en-gb">English (UK)</SelectItem>
                <SelectItem value="es">Spanish</SelectItem>
                <SelectItem value="fr">French</SelectItem>
              </SelectContent>
            </Select>
          </div>
          <div className="space-y-2">
            <Label className="text-sm font-semibold">Region</Label>
            <Select defaultValue="us" disabled>
              <SelectTrigger disabled>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="us">United States</SelectItem>
                <SelectItem value="uk">United Kingdom</SelectItem>
                <SelectItem value="eu">Europe</SelectItem>
              </SelectContent>
            </Select>
          </div>
          <p className="text-xs text-muted-foreground">
            Language and region settings are not wired yet.
          </p>
        </CardContent>
      </Card>
    </div>
  );
}

function FilePathsSettings() {
  const webView2Available = useHostStore((state) => state.webView2Available);
  const hostConnected = useHostStore((state) => state.hostConnected);
  const draft = useAppSettingsStore((s) => s.draft);
  const setDraft = useAppSettingsStore((s) => s.setDraft);

  const disabled = !webView2Available || !hostConnected;

  function updateDraft(updater: (d: AppSettingsDto) => AppSettingsDto) {
    const current = useAppSettingsStore.getState().draft;
    if (!current) return;
    setDraft(updater(current));
  }

  async function browseHandlersFile() {
    if (disabled) return;
    const r = await openFileDialog({
      title: 'Select Handlers File',
      filter: 'JSON files (*.json)|*.json|All files (*.*)|*.*',
    });
    if (r.cancelled) return;
    const path = r.path ?? r.paths?.[0];
    if (!path) return;
    updateDraft((d) => ({ ...d, handlersFilePath: path }));
  }

  async function browsePluginsDir() {
    if (disabled) return;
    const r = await openFolderDialog({
      title: 'Select Plugins Directory',
      initialPath: draft?.pluginsDirectory,
    });
    if (r.cancelled) return;
    if (!r.path) return;
    updateDraft((d) => ({ ...d, pluginsDirectory: r.path ?? d.pluginsDirectory }));
  }

  async function browseExchangeDir() {
    if (disabled) return;
    const r = await openFolderDialog({
      title: 'Select Exchange Directory',
      initialPath: draft?.exchangeDirectory,
    });
    if (r.cancelled) return;
    if (!r.path) return;
    updateDraft((d) => ({ ...d, exchangeDirectory: r.path ?? d.exchangeDirectory }));
  }

  if (!draft) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-[28px] font-semibold mb-2">File Paths</h1>
          <p className="text-sm text-muted-foreground">Configure file and directory locations</p>
        </div>

        <Card>
          <CardContent className="p-4 text-sm text-muted-foreground">
            Connect to the WPF host to view and edit these settings.
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-[28px] font-semibold mb-2">File Paths</h1>
        <p className="text-sm text-muted-foreground">Configure file and directory locations</p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Configuration Paths</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-2">
            <Label className="text-sm font-semibold">Handlers File</Label>
            <div className="flex gap-2">
              <Input
                value={draft.handlersFilePath ?? ''}
                onChange={(e) => updateDraft((d) => ({ ...d, handlersFilePath: e.target.value }))}
                placeholder="C:\\App\\handlers.json"
                className="flex-1"
                disabled={disabled}
              />
              <Button variant="outline" onClick={() => void browseHandlersFile()} disabled={disabled}>
                Browse
              </Button>
            </div>
            <p className="text-xs text-muted-foreground">JSON configuration file for handler definitions</p>
          </div>

          <div className="space-y-2">
            <Label className="text-sm font-semibold">Plugins Directory</Label>
            <div className="flex gap-2">
              <Input
                value={draft.pluginsDirectory ?? ''}
                onChange={(e) => updateDraft((d) => ({ ...d, pluginsDirectory: e.target.value }))}
                placeholder="C:\\App\\Plugins"
                className="flex-1"
                disabled={disabled}
              />
              <Button variant="outline" onClick={() => void browsePluginsDir()} disabled={disabled}>
                Browse
              </Button>
            </div>
            <p className="text-xs text-muted-foreground">Directory containing plugin DLL files</p>
          </div>

          <div className="space-y-2">
            <Label className="text-sm font-semibold">Exchange Directory</Label>
            <div className="flex gap-2">
              <Input
                value={draft.exchangeDirectory ?? ''}
                onChange={(e) => updateDraft((d) => ({ ...d, exchangeDirectory: e.target.value }))}
                placeholder="\\\\server\\share\\Exchange"
                className="flex-1"
                disabled={disabled}
              />
              <Button variant="outline" onClick={() => void browseExchangeDir()} disabled={disabled}>
                Browse
              </Button>
            </div>
            <p className="text-xs text-muted-foreground">Shared directory for handler templates</p>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

function KeyboardSettings() {
  const webView2Available = useHostStore((state) => state.webView2Available);
  const hostConnected = useHostStore((state) => state.hostConnected);
  const draft = useAppSettingsStore((s) => s.draft);
  const setDraft = useAppSettingsStore((s) => s.setDraft);

  const disabled = !webView2Available || !hostConnected;

  function updateDraft(updater: (d: AppSettingsDto) => AppSettingsDto) {
    const current = useAppSettingsStore.getState().draft;
    if (!current) return;
    setDraft(updater(current));
  }

  function hasModifier(mod: string): boolean {
    const mods = draft?.keyboardShortcut?.modifierKeys ?? [];
    return mods.some((m) => m.toLowerCase() === mod.toLowerCase());
  }

  function setModifier(mod: string, enabled: boolean) {
    updateDraft((d) => {
      const current = d.keyboardShortcut?.modifierKeys ?? [];
      const exists = current.some((m) => m.toLowerCase() === mod.toLowerCase());
      const next = enabled
        ? exists
          ? current
          : [...current, mod]
        : current.filter((m) => m.toLowerCase() !== mod.toLowerCase());

      return {
        ...d,
        keyboardShortcut: {
          ...d.keyboardShortcut,
          modifierKeys: next,
        },
      };
    });
  }

  function setKey(nextKey: string) {
    updateDraft((d) => ({
      ...d,
      keyboardShortcut: {
        ...d.keyboardShortcut,
        key: nextKey,
      },
    }));
  }

  const shortcutPreview = draft
    ? [...(draft.keyboardShortcut?.modifierKeys ?? []), (draft.keyboardShortcut?.key ?? '').trim()]
        .filter((x) => !!x)
        .join(' + ')
    : '—';

  if (!draft) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-[28px] font-semibold mb-2">Keyboard Shortcut</h1>
          <p className="text-sm text-muted-foreground">Configure global keyboard shortcuts</p>
        </div>

        <Card>
          <CardContent className="p-4 text-sm text-muted-foreground">
            Connect to the WPF host to view and edit these settings.
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-[28px] font-semibold mb-2">Keyboard Shortcut</h1>
        <p className="text-sm text-muted-foreground">Configure global keyboard shortcuts</p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Global Shortcut</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div>
            <Label className="text-sm font-semibold mb-3 block">Modifier Keys</Label>
            <div className="grid grid-cols-2 gap-4">
              <div className="flex items-center justify-between p-3 border rounded-md">
                <Label htmlFor="ctrl" className="font-normal">Ctrl</Label>
                <Switch
                  id="ctrl"
                  checked={hasModifier('Ctrl')}
                  onCheckedChange={(checked) => setModifier('Ctrl', checked)}
                  disabled={disabled}
                />
              </div>
              <div className="flex items-center justify-between p-3 border rounded-md">
                <Label htmlFor="alt" className="font-normal">Alt</Label>
                <Switch
                  id="alt"
                  checked={hasModifier('Alt')}
                  onCheckedChange={(checked) => setModifier('Alt', checked)}
                  disabled={disabled}
                />
              </div>
              <div className="flex items-center justify-between p-3 border rounded-md">
                <Label htmlFor="shift" className="font-normal">Shift</Label>
                <Switch
                  id="shift"
                  checked={hasModifier('Shift')}
                  onCheckedChange={(checked) => setModifier('Shift', checked)}
                  disabled={disabled}
                />
              </div>
              <div className="flex items-center justify-between p-3 border rounded-md">
                <Label htmlFor="win" className="font-normal">Win</Label>
                <Switch
                  id="win"
                  checked={hasModifier('Win')}
                  onCheckedChange={(checked) => setModifier('Win', checked)}
                  disabled={disabled}
                />
              </div>
            </div>
          </div>

          <div className="space-y-2">
            <Label className="text-sm font-semibold">Main Key</Label>
            <Input
              value={draft.keyboardShortcut?.key ?? ''}
              onChange={(e) => setKey(e.target.value)}
              placeholder="Press any key..."
              className="w-32"
              disabled={disabled}
            />
            <p className="text-xs text-muted-foreground">Current: {shortcutPreview}</p>
          </div>

          <Button
            variant="outline"
            onClick={() => toast('Shortcut test is not implemented yet.')}
            disabled={disabled}
          >
            Test Shortcut
          </Button>
        </CardContent>
      </Card>
    </div>
  );
}

function PerformanceSettings() {
  const webView2Available = useHostStore((state) => state.webView2Available);
  const hostConnected = useHostStore((state) => state.hostConnected);
  const draft = useAppSettingsStore((s) => s.draft);
  const setDraft = useAppSettingsStore((s) => s.setDraft);

  const disabled = !webView2Available || !hostConnected;

  function updateDraft(updater: (d: AppSettingsDto) => AppSettingsDto) {
    const current = useAppSettingsStore.getState().draft;
    if (!current) return;
    setDraft(updater(current));
  }

  function setPreset(preset: 'fast' | 'balanced' | 'reliable') {
    updateDraft((d) => {
      switch (preset) {
        case 'fast':
          return { ...d, clipboardWaitTimeout: 2000, windowActivationDelay: 50, clipboardClearDelay: 200 };
        case 'reliable':
          return { ...d, clipboardWaitTimeout: 8000, windowActivationDelay: 150, clipboardClearDelay: 1200 };
        case 'balanced':
        default:
          return { ...d, clipboardWaitTimeout: 5000, windowActivationDelay: 100, clipboardClearDelay: 800 };
      }
    });
  }

  if (!draft) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-[28px] font-semibold mb-2">Performance</h1>
          <p className="text-sm text-muted-foreground">Configure timing and performance settings</p>
        </div>

        <Card>
          <CardContent className="p-4 text-sm text-muted-foreground">
            Connect to the WPF host to view and edit these settings.
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-[28px] font-semibold mb-2">Performance</h1>
        <p className="text-sm text-muted-foreground">Configure timing and performance settings</p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Timing Settings</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="space-y-2">
            <Label className="text-sm font-semibold">Clipboard Wait Timeout (ms)</Label>
            <Input
              type="number"
              value={draft.clipboardWaitTimeout ?? 0}
              onChange={(e) =>
                updateDraft((d) => ({
                  ...d,
                  clipboardWaitTimeout: Number(e.target.value),
                }))
              }
              disabled={disabled}
            />
            <p className="text-xs text-muted-foreground">Time to wait for clipboard content</p>
          </div>

          <div className="space-y-2">
            <Label className="text-sm font-semibold">Window Activation Delay (ms)</Label>
            <Input
              type="number"
              value={draft.windowActivationDelay ?? 0}
              onChange={(e) =>
                updateDraft((d) => ({
                  ...d,
                  windowActivationDelay: Number(e.target.value),
                }))
              }
              disabled={disabled}
            />
            <p className="text-xs text-muted-foreground">Delay before activating window</p>
          </div>

          <div className="space-y-2">
            <Label className="text-sm font-semibold">Clipboard Clear Delay (ms)</Label>
            <Input
              type="number"
              value={draft.clipboardClearDelay ?? 0}
              onChange={(e) =>
                updateDraft((d) => ({
                  ...d,
                  clipboardClearDelay: Number(e.target.value),
                }))
              }
              disabled={disabled}
            />
            <p className="text-xs text-muted-foreground">Delay before clearing clipboard</p>
          </div>

          <div className="flex gap-2 pt-2">
            <Button variant="outline" size="sm" onClick={() => setPreset('fast')} disabled={disabled}>
              Fast
            </Button>
            <Button variant="outline" size="sm" onClick={() => setPreset('balanced')} disabled={disabled}>
              Balanced
            </Button>
            <Button variant="outline" size="sm" onClick={() => setPreset('reliable')} disabled={disabled}>
              Reliable
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

function ConfigSystemSettings() {
  const webView2Available = useHostStore((state) => state.webView2Available);
  const hostConnected = useHostStore((state) => state.hostConnected);
  const draft = useAppSettingsStore((s) => s.draft);
  const setDraft = useAppSettingsStore((s) => s.setDraft);

  const disabled = !webView2Available || !hostConnected;

  function updateDraft(updater: (d: AppSettingsDto) => AppSettingsDto) {
    const current = useAppSettingsStore.getState().draft;
    if (!current) return;
    setDraft(updater(current));
  }

  async function browseConfigFile() {
    if (disabled) return;
    const r = await openFileDialog({
      title: 'Select Config File',
      filter: 'INI Files (*.ini)|*.ini|All Files (*.*)|*.*',
    });
    if (r.cancelled) return;
    const path = r.path ?? r.paths?.[0];
    if (!path) return;
    updateDraft((d) => ({
      ...d,
      configSystem: {
        ...d.configSystem,
        configFilePath: path,
      },
    }));
  }

  async function browseSecretsFile() {
    if (disabled) return;
    const r = await openFileDialog({
      title: 'Select Secrets File',
      filter: 'INI Files (*.ini)|*.ini|All Files (*.*)|*.*',
    });
    if (r.cancelled) return;
    const path = r.path ?? r.paths?.[0];
    if (!path) return;
    updateDraft((d) => ({
      ...d,
      configSystem: {
        ...d.configSystem,
        secretsFilePath: path,
      },
    }));
  }

  if (!draft) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-[28px] font-semibold mb-2">Config System</h1>
          <p className="text-sm text-muted-foreground">Configure external configuration files</p>
        </div>

        <Card>
          <CardContent className="p-4 text-sm text-muted-foreground">
            Connect to the WPF host to view and edit these settings.
          </CardContent>
        </Card>
      </div>
    );
  }

  const configEnabled = !!draft.configSystem?.enabled;
  const configInputsDisabled = disabled || !configEnabled;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-[28px] font-semibold mb-2">Config System</h1>
        <p className="text-sm text-muted-foreground">Configure external configuration files</p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Configuration Files</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center justify-between p-3 border rounded-md">
            <div>
              <Label className="font-semibold">Enable Config Files</Label>
              <p className="text-xs text-muted-foreground">Use external config.ini and secrets.ini files</p>
            </div>
            <Switch
              checked={!!draft.configSystem?.enabled}
              onCheckedChange={(checked) =>
                updateDraft((d) => ({
                  ...d,
                  configSystem: {
                    ...d.configSystem,
                    enabled: checked,
                  },
                }))
              }
              disabled={disabled}
            />
          </div>

          <div className="space-y-2">
            <Label className="text-sm font-semibold">Config File Path</Label>
            <div className="flex gap-2">
              <Input
                value={draft.configSystem?.configFilePath ?? ''}
                onChange={(e) =>
                  updateDraft((d) => ({
                    ...d,
                    configSystem: {
                      ...d.configSystem,
                      configFilePath: e.target.value,
                    },
                  }))
                }
                placeholder="C:\\App\\config.ini"
                className="flex-1"
                disabled={configInputsDisabled}
              />
              <Button variant="outline" onClick={() => void browseConfigFile()} disabled={configInputsDisabled}>
                Browse
              </Button>
            </div>
            <p className="text-xs text-muted-foreground">Path to config.ini file</p>
          </div>

          <div className="space-y-2">
            <Label className="text-sm font-semibold">Secrets File Path</Label>
            <div className="flex gap-2">
              <Input
                value={draft.configSystem?.secretsFilePath ?? ''}
                onChange={(e) =>
                  updateDraft((d) => ({
                    ...d,
                    configSystem: {
                      ...d.configSystem,
                      secretsFilePath: e.target.value,
                    },
                  }))
                }
                placeholder="C:\\App\\secrets.ini"
                className="flex-1"
                disabled={configInputsDisabled}
              />
              <Button variant="outline" onClick={() => void browseSecretsFile()} disabled={configInputsDisabled}>
                Browse
              </Button>
            </div>
            <p className="text-xs text-muted-foreground">Path to secrets.ini file (encrypted)</p>
          </div>

          <div className="space-y-2">
            <Label className="text-sm font-semibold">File Format</Label>
            <Select
              value={(draft.configSystem?.fileFormat ?? 'ini').toLowerCase()}
              onValueChange={(value) =>
                updateDraft((d) => ({
                  ...d,
                  configSystem: {
                    ...d.configSystem,
                    fileFormat: value,
                  },
                }))
              }
              disabled={configInputsDisabled}
            >
              <SelectTrigger disabled={configInputsDisabled} className="w-48">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="ini">INI</SelectItem>
                <SelectItem value="json">JSON</SelectItem>
              </SelectContent>
            </Select>
          </div>

          <div className="flex items-center justify-between p-3 border rounded-md">
            <div>
              <Label className="font-semibold">Auto-create files if missing</Label>
              <p className="text-xs text-muted-foreground">Automatically create config files with defaults</p>
            </div>
            <Switch
              checked={!!draft.configSystem?.autoCreateFiles}
              onCheckedChange={(checked) =>
                updateDraft((d) => ({
                  ...d,
                  configSystem: {
                    ...d.configSystem,
                    autoCreateFiles: checked,
                  },
                }))
              }
              disabled={configInputsDisabled}
            />
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

function NetworkUpdateSettings() {
  const webView2Available = useHostStore((state) => state.webView2Available);
  const hostConnected = useHostStore((state) => state.hostConnected);
  const draft = useAppSettingsStore((s) => s.draft);
  const setDraft = useAppSettingsStore((s) => s.setDraft);

  const disabled = !webView2Available || !hostConnected;

  function updateDraft(updater: (d: AppSettingsDto) => AppSettingsDto) {
    const current = useAppSettingsStore.getState().draft;
    if (!current) return;
    setDraft(updater(current));
  }

  async function browseNetworkUpdatePath() {
    if (disabled) return;
    const r = await openFolderDialog({
      title: 'Select Network Update Path',
      initialPath: draft?.uiSettings?.networkUpdateSettings?.networkUpdatePath,
    });
    if (r.cancelled) return;
    if (!r.path) return;
    updateDraft((d) => ({
      ...d,
      uiSettings: {
        ...d.uiSettings,
        networkUpdateSettings: {
          ...d.uiSettings.networkUpdateSettings,
          networkUpdatePath: r.path ?? d.uiSettings.networkUpdateSettings.networkUpdatePath,
        },
      },
    }));
  }

  async function browseUpdateScriptPath() {
    if (disabled) return;
    const r = await openFileDialog({
      title: 'Select Update Script',
      filter: 'Batch Files (*.bat)|*.bat|All Files (*.*)|*.*',
    });
    if (r.cancelled) return;
    const path = r.path ?? r.paths?.[0];
    if (!path) return;
    updateDraft((d) => ({
      ...d,
      uiSettings: {
        ...d.uiSettings,
        networkUpdateSettings: {
          ...d.uiSettings.networkUpdateSettings,
          updateScriptPath: path,
        },
      },
    }));
  }

  if (!draft) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-[28px] font-semibold mb-2">Network Updates</h1>
          <p className="text-sm text-muted-foreground">Configure automatic updates from network share</p>
        </div>

        <Card>
          <CardContent className="p-4 text-sm text-muted-foreground">
            Connect to the WPF host to view and edit these settings.
          </CardContent>
        </Card>
      </div>
    );
  }

  const updatesEnabled = !!draft.uiSettings?.networkUpdateSettings?.enableNetworkUpdates;
  const updateInputsDisabled = disabled || !updatesEnabled;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-[28px] font-semibold mb-2">Network Updates</h1>
        <p className="text-sm text-muted-foreground">Configure automatic updates from network share</p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Update Settings</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center justify-between p-3 border rounded-md">
            <div>
              <Label className="font-semibold">Enable Network Updates</Label>
              <p className="text-xs text-muted-foreground">Check for updates from network share</p>
            </div>
            <Switch
              checked={updatesEnabled}
              onCheckedChange={(checked) =>
                updateDraft((d) => ({
                  ...d,
                  uiSettings: {
                    ...d.uiSettings,
                    networkUpdateSettings: {
                      ...d.uiSettings.networkUpdateSettings,
                      enableNetworkUpdates: checked,
                    },
                  },
                }))
              }
              disabled={disabled}
            />
          </div>

          <div className="space-y-2">
            <Label className="text-sm font-semibold">Network Update Path</Label>
            <div className="flex gap-2">
              <Input
                value={draft.uiSettings?.networkUpdateSettings?.networkUpdatePath ?? ''}
                onChange={(e) =>
                  updateDraft((d) => ({
                    ...d,
                    uiSettings: {
                      ...d.uiSettings,
                      networkUpdateSettings: {
                        ...d.uiSettings.networkUpdateSettings,
                        networkUpdatePath: e.target.value,
                      },
                    },
                  }))
                }
                placeholder="\\\\server\\updates"
                className="flex-1"
                disabled={updateInputsDisabled}
              />
              <Button variant="outline" onClick={() => void browseNetworkUpdatePath()} disabled={updateInputsDisabled}>
                Browse
              </Button>
            </div>
            <p className="text-xs text-muted-foreground">UNC path to update share</p>
          </div>

          <div className="space-y-2">
            <Label className="text-sm font-semibold">Update Script</Label>
            <div className="flex gap-2">
              <Input
                value={draft.uiSettings?.networkUpdateSettings?.updateScriptPath ?? ''}
                onChange={(e) =>
                  updateDraft((d) => ({
                    ...d,
                    uiSettings: {
                      ...d.uiSettings,
                      networkUpdateSettings: {
                        ...d.uiSettings.networkUpdateSettings,
                        updateScriptPath: e.target.value,
                      },
                    },
                  }))
                }
                placeholder="\\\\server\\updates\\install_update.bat"
                className="flex-1"
                disabled={updateInputsDisabled}
              />
              <Button variant="outline" onClick={() => void browseUpdateScriptPath()} disabled={updateInputsDisabled}>
                Browse
              </Button>
            </div>
          </div>

          <div className="space-y-2">
            <Label className="text-sm font-semibold">Check Interval (hours)</Label>
            <Input
              type="number"
              value={draft.uiSettings?.networkUpdateSettings?.checkIntervalHours ?? 24}
              onChange={(e) =>
                updateDraft((d) => ({
                  ...d,
                  uiSettings: {
                    ...d.uiSettings,
                    networkUpdateSettings: {
                      ...d.uiSettings.networkUpdateSettings,
                      checkIntervalHours: Number(e.target.value),
                    },
                  },
                }))
              }
              disabled={updateInputsDisabled}
              className="w-32"
            />
          </div>

          <div className="flex items-center justify-between p-3 border rounded-md">
            <div>
              <Label className="font-semibold">Auto-install non-mandatory updates</Label>
              <p className="text-xs text-muted-foreground">Install optional updates automatically</p>
            </div>
            <Switch
              checked={!!draft.uiSettings?.networkUpdateSettings?.autoInstallNonMandatory}
              onCheckedChange={(checked) =>
                updateDraft((d) => ({
                  ...d,
                  uiSettings: {
                    ...d.uiSettings,
                    networkUpdateSettings: {
                      ...d.uiSettings.networkUpdateSettings,
                      autoInstallNonMandatory: checked,
                    },
                  },
                }))
              }
              disabled={updateInputsDisabled}
            />
          </div>

          <div className="flex items-center justify-between p-3 border rounded-md">
            <div>
              <Label className="font-semibold">Auto-install mandatory updates</Label>
              <p className="text-xs text-muted-foreground">Install required updates automatically</p>
            </div>
            <Switch
              checked={!!draft.uiSettings?.networkUpdateSettings?.autoInstallMandatory}
              onCheckedChange={(checked) =>
                updateDraft((d) => ({
                  ...d,
                  uiSettings: {
                    ...d.uiSettings,
                    networkUpdateSettings: {
                      ...d.uiSettings.networkUpdateSettings,
                      autoInstallMandatory: checked,
                    },
                  },
                }))
              }
              disabled={updateInputsDisabled}
            />
          </div>

          <div className="flex gap-2">
            <Button variant="outline" onClick={() => toast('Check Now is not wired yet.')} disabled={disabled}>
              Check Now
            </Button>
            <Button variant="outline" onClick={() => toast('Test Network is not wired yet.')} disabled={disabled}>
              Test Network
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

function DeploymentSettings() {
  const webView2Available = useHostStore((state) => state.webView2Available);
  const hostConnected = useHostStore((state) => state.hostConnected);
  const draft = useAppSettingsStore((s) => s.draft);
  const setDraft = useAppSettingsStore((s) => s.setDraft);

  const disabled = !webView2Available || !hostConnected;

  function updateDraft(updater: (d: AppSettingsDto) => AppSettingsDto) {
    const current = useAppSettingsStore.getState().draft;
    if (!current) return;
    setDraft(updater(current));
  }

  async function browseSourcePath() {
    if (disabled) return;
    const r = await openFolderDialog({
      title: 'Select Initial Deployment Source Path',
      initialPath: draft?.uiSettings?.initialDeploymentSettings?.sourcePath,
    });
    if (r.cancelled) return;
    if (!r.path) return;
    updateDraft((d) => ({
      ...d,
      uiSettings: {
        ...d.uiSettings,
        initialDeploymentSettings: {
          ...d.uiSettings.initialDeploymentSettings,
          sourcePath: r.path ?? d.uiSettings.initialDeploymentSettings.sourcePath,
        },
      },
    }));
  }

  if (!draft) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-[28px] font-semibold mb-2">Initial Deployment</h1>
          <p className="text-sm text-muted-foreground">Configure portable app setup from network source</p>
        </div>

        <Card>
          <CardContent className="p-4 text-sm text-muted-foreground">
            Connect to the WPF host to view and edit these settings.
          </CardContent>
        </Card>
      </div>
    );
  }

  const deploymentEnabled = !!draft.uiSettings?.initialDeploymentSettings?.enabled;
  const deploymentInputsDisabled = disabled || !deploymentEnabled;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-[28px] font-semibold mb-2">Initial Deployment</h1>
        <p className="text-sm text-muted-foreground">Configure portable app setup from network source</p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Setup Configuration</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center justify-between p-3 border rounded-md">
            <div>
              <Label className="font-semibold">Enable Initial Deployment</Label>
              <p className="text-xs text-muted-foreground">Copy files from network source on first run</p>
            </div>
            <Switch
              checked={deploymentEnabled}
              onCheckedChange={(checked) =>
                updateDraft((d) => ({
                  ...d,
                  uiSettings: {
                    ...d.uiSettings,
                    initialDeploymentSettings: {
                      ...d.uiSettings.initialDeploymentSettings,
                      enabled: checked,
                    },
                  },
                }))
              }
              disabled={disabled}
            />
          </div>

          <div className="flex items-center justify-between p-3 border rounded-md bg-muted/30">
            <div>
              <Label className="font-semibold">Deployment Status</Label>
              <p className="text-xs text-muted-foreground">Tracks whether initial setup has been completed</p>
            </div>
            <span className={cn(
              'text-xs px-2 py-1 rounded-md border',
              draft.uiSettings?.initialDeploymentSettings?.isCompleted
                ? 'bg-green-50 dark:bg-green-950/20 border-green-200 dark:border-green-900 text-green-700 dark:text-green-300'
                : 'bg-yellow-50 dark:bg-yellow-950/20 border-yellow-200 dark:border-yellow-900 text-yellow-700 dark:text-yellow-300',
            )}>
              {draft.uiSettings?.initialDeploymentSettings?.isCompleted ? 'Completed' : 'Not completed'}
            </span>
          </div>

          <div className="space-y-2">
            <Label className="text-sm font-semibold">Source Path</Label>
            <div className="flex gap-2">
              <Input
                value={draft.uiSettings?.initialDeploymentSettings?.sourcePath ?? ''}
                onChange={(e) =>
                  updateDraft((d) => ({
                    ...d,
                    uiSettings: {
                      ...d.uiSettings,
                      initialDeploymentSettings: {
                        ...d.uiSettings.initialDeploymentSettings,
                        sourcePath: e.target.value,
                      },
                    },
                  }))
                }
                placeholder="\\\\server\\deployment"
                className="flex-1"
                disabled={deploymentInputsDisabled}
              />
              <Button variant="outline" onClick={() => void browseSourcePath()} disabled={deploymentInputsDisabled}>
                Browse
              </Button>
            </div>
            <p className="text-xs text-muted-foreground">Folder containing Exchange/, Installed/, and Plugins/</p>
          </div>

          <div className="space-y-3">
            <Label className="text-sm font-semibold">Copy Options</Label>
            <div className="space-y-2">
              <div className="flex items-center justify-between p-3 border rounded-md">
                <Label className="font-normal">Copy Exchange Handlers</Label>
                <Switch
                  checked={!!draft.uiSettings?.initialDeploymentSettings?.copyExchangeHandlers}
                  onCheckedChange={(checked) =>
                    updateDraft((d) => ({
                      ...d,
                      uiSettings: {
                        ...d.uiSettings,
                        initialDeploymentSettings: {
                          ...d.uiSettings.initialDeploymentSettings,
                          copyExchangeHandlers: checked,
                        },
                      },
                    }))
                  }
                  disabled={deploymentInputsDisabled}
                />
              </div>
              <div className="flex items-center justify-between p-3 border rounded-md">
                <Label className="font-normal">Copy Installed Handlers</Label>
                <Switch
                  checked={!!draft.uiSettings?.initialDeploymentSettings?.copyInstalledHandlers}
                  onCheckedChange={(checked) =>
                    updateDraft((d) => ({
                      ...d,
                      uiSettings: {
                        ...d.uiSettings,
                        initialDeploymentSettings: {
                          ...d.uiSettings.initialDeploymentSettings,
                          copyInstalledHandlers: checked,
                        },
                      },
                    }))
                  }
                  disabled={deploymentInputsDisabled}
                />
              </div>
              <div className="flex items-center justify-between p-3 border rounded-md">
                <Label className="font-normal">Copy Plugins</Label>
                <Switch
                  checked={!!draft.uiSettings?.initialDeploymentSettings?.copyPlugins}
                  onCheckedChange={(checked) =>
                    updateDraft((d) => ({
                      ...d,
                      uiSettings: {
                        ...d.uiSettings,
                        initialDeploymentSettings: {
                          ...d.uiSettings.initialDeploymentSettings,
                          copyPlugins: checked,
                        },
                      },
                    }))
                  }
                  disabled={deploymentInputsDisabled}
                />
              </div>
            </div>
          </div>

          <div className="flex gap-2">
            <Button variant="outline" onClick={() => toast('Run Setup is not wired yet.')} disabled={disabled}>
              Run Setup Now
            </Button>
            <Button variant="outline" onClick={() => toast('Reset Setup State is not wired yet.')} disabled={disabled}>
              Reset Setup State
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

function LoggingSettings() {
  const webView2Available = useHostStore((state) => state.webView2Available);
  const hostConnected = useHostStore((state) => state.hostConnected);
  const draft = useAppSettingsStore((s) => s.draft);
  const setDraft = useAppSettingsStore((s) => s.setDraft);
  const lastLogClearResult = useAppSettingsStore((s) => s.lastLogClearResult);

  const disabled = !webView2Available || !hostConnected;

  function updateDraft(updater: (d: AppSettingsDto) => AppSettingsDto) {
    const current = useAppSettingsStore.getState().draft;
    if (!current) return;
    setDraft(updater(current));
  }

  async function browseLogFolder() {
    if (disabled) return;
    const r = await openFolderDialog({
      title: 'Select Log Folder',
      initialPath: draft?.loggingSettings?.localLogPath,
    });
    if (r.cancelled) return;
    if (!r.path) return;
    updateDraft((d) => ({
      ...d,
      loggingSettings: {
        ...d.loggingSettings,
        localLogPath: r.path ?? d.loggingSettings.localLogPath,
      },
    }));
  }

  function clearLogs() {
    if (!draft) return;
    if (disabled) return;
    requestClearLogs(draft.loggingSettings?.localLogPath);
  }

  function writeTestLogs() {
    if (disabled) return;
    requestLoggingTest();
  }

  function sendTestUsageEvent() {
    if (disabled) return;
    requestUsageTest();
  }

  if (!draft) {
    return (
      <div className="space-y-6">
        <div>
          <h1 className="text-[28px] font-semibold mb-2">Logging & Diagnostics</h1>
          <p className="text-sm text-muted-foreground">Configure logging and usage analytics</p>
        </div>

        <Card>
          <CardContent className="p-4 text-sm text-muted-foreground">
            Connect to the WPF host to view and edit these settings.
          </CardContent>
        </Card>
      </div>
    );
  }

  const localLoggingEnabled = !!draft.loggingSettings?.enableLocalLogging;
  const localInputsDisabled = disabled || !localLoggingEnabled;
  const usageEnabled = !!draft.loggingSettings?.enableUsageTracking;
  const usageInputsDisabled = disabled || !usageEnabled;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-[28px] font-semibold mb-2">Logging & Diagnostics</h1>
        <p className="text-sm text-muted-foreground">Configure logging and usage analytics</p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Local Logging</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center justify-between p-3 border rounded-md">
            <Label className="font-semibold">Enable Local Logging</Label>
            <Switch
              checked={localLoggingEnabled}
              onCheckedChange={(checked) =>
                updateDraft((d) => ({
                  ...d,
                  loggingSettings: {
                    ...d.loggingSettings,
                    enableLocalLogging: checked,
                  },
                }))
              }
              disabled={disabled}
            />
          </div>

          <div className="space-y-2">
            <Label className="text-sm font-semibold">Log Folder</Label>
            <div className="flex gap-2">
              <Input
                value={draft.loggingSettings?.localLogPath ?? ''}
                onChange={(e) =>
                  updateDraft((d) => ({
                    ...d,
                    loggingSettings: {
                      ...d.loggingSettings,
                      localLogPath: e.target.value,
                    },
                  }))
                }
                placeholder="C:\\App\\Logs"
                className="flex-1"
                disabled={localInputsDisabled}
              />
              <Button variant="outline" onClick={() => void browseLogFolder()} disabled={localInputsDisabled}>
                Browse
              </Button>
            </div>
          </div>

          <div className="space-y-2">
            <Label className="text-sm font-semibold">Minimum Log Level</Label>
            <Select
              value={(draft.loggingSettings?.minimumLogLevel ?? 'Info') as LogLevel}
              onValueChange={(value) =>
                updateDraft((d) => ({
                  ...d,
                  loggingSettings: {
                    ...d.loggingSettings,
                    minimumLogLevel: value as LogLevel,
                  },
                }))
              }
              disabled={localInputsDisabled}
            >
              <SelectTrigger disabled={localInputsDisabled}>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="Debug">Debug</SelectItem>
                <SelectItem value="Info">Info</SelectItem>
                <SelectItem value="Warning">Warning</SelectItem>
                <SelectItem value="Error">Error</SelectItem>
                <SelectItem value="Critical">Critical</SelectItem>
              </SelectContent>
            </Select>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label className="text-sm font-semibold">Max Log File Size (MB)</Label>
              <Input
                type="number"
                value={draft.loggingSettings?.maxLogFileSizeMB ?? 10}
                onChange={(e) =>
                  updateDraft((d) => ({
                    ...d,
                    loggingSettings: {
                      ...d.loggingSettings,
                      maxLogFileSizeMB: Number(e.target.value),
                    },
                  }))
                }
                disabled={localInputsDisabled}
              />
            </div>
            <div className="space-y-2">
              <Label className="text-sm font-semibold">Max Log File Count</Label>
              <Input
                type="number"
                value={draft.loggingSettings?.maxLogFileCount ?? 5}
                onChange={(e) =>
                  updateDraft((d) => ({
                    ...d,
                    loggingSettings: {
                      ...d.loggingSettings,
                      maxLogFileCount: Number(e.target.value),
                    },
                  }))
                }
                disabled={localInputsDisabled}
              />
            </div>
          </div>

          <div className="flex items-center justify-between p-3 border rounded-md">
            <Label className="font-semibold">Enable Debug Mode</Label>
            <Switch
              checked={!!draft.loggingSettings?.enableDebugMode}
              onCheckedChange={(checked) =>
                updateDraft((d) => ({
                  ...d,
                  loggingSettings: {
                    ...d.loggingSettings,
                    enableDebugMode: checked,
                  },
                }))
              }
              disabled={localInputsDisabled}
            />
          </div>

          <div className="flex flex-wrap gap-2">
            <Button variant="outline" onClick={writeTestLogs} disabled={disabled}>
              Write Test Logs
            </Button>
            <Button variant="outline" onClick={clearLogs} disabled={disabled}>
              Clear Logs
            </Button>
            {lastLogClearResult && (
              <span className="text-xs text-muted-foreground self-center">
                Last clear: deleted {lastLogClearResult.deletedCount} file(s)
              </span>
            )}
          </div>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Usage Analytics</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center justify-between p-3 border rounded-md">
            <Label className="font-semibold">Enable Usage Tracking</Label>
            <Switch
              checked={usageEnabled}
              onCheckedChange={(checked) =>
                updateDraft((d) => ({
                  ...d,
                  loggingSettings: {
                    ...d.loggingSettings,
                    enableUsageTracking: checked,
                  },
                }))
              }
              disabled={disabled}
            />
          </div>

          <div className="space-y-2">
            <Label className="text-sm font-semibold">Usage Endpoint URL</Label>
            <Input
              value={draft.loggingSettings?.usageEndpointUrl ?? ''}
              onChange={(e) =>
                updateDraft((d) => ({
                  ...d,
                  loggingSettings: {
                    ...d.loggingSettings,
                    usageEndpointUrl: e.target.value,
                  },
                }))
              }
              placeholder="https://analytics.example.com/events"
              disabled={usageInputsDisabled}
            />
          </div>

          <Button variant="outline" onClick={sendTestUsageEvent} disabled={disabled}>
            Send Test Event
          </Button>
        </CardContent>
      </Card>
    </div>
  );
}

function AdvancedSettings() {
  const webView2Available = useHostStore((state) => state.webView2Available);
  const hostConnected = useHostStore((state) => state.hostConnected);
  const hostInfo = useHostStore((state) => state.hostInfo);
  const lastPongAt = useHostStore((state) => state.lastPongAt);
  const draft = useAppSettingsStore((s) => s.draft);
  const setDraft = useAppSettingsStore((s) => s.setDraft);

  const disabled = !webView2Available || !hostConnected;

  function updateDraft(updater: (d: AppSettingsDto) => AppSettingsDto) {
    const current = useAppSettingsStore.getState().draft;
    if (!current) return;
    setDraft(updater(current));
  }

  const mcpEnabled = !!draft?.mcpSettings?.enabled;
  const mcpPort = draft?.mcpSettings?.port ?? 5000;
  const mcpMgmtToolsEnabled = !!draft?.mcpSettings?.managementToolsEnabled;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-[28px] font-semibold mb-2">Advanced</h1>
        <p className="text-sm text-muted-foreground">MCP Server and advanced configuration</p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">MCP HTTP Server</CardTitle>
          <CardDescription>Expose handlers as MCP tools via HTTP</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          {!draft ? (
            <div className="text-sm text-muted-foreground">
              Settings are loaded from the WPF host. Connect to the host to configure MCP.
            </div>
          ) : (
            <>
              <div className="flex items-center justify-between p-3 border rounded-md bg-yellow-50 dark:bg-yellow-950/20 border-yellow-200 dark:border-yellow-900">
                <div>
                  <Label className="font-semibold">Enable MCP HTTP Server</Label>
                  <p className="text-xs text-muted-foreground">Requires application restart</p>
                </div>
                <Switch
                  checked={mcpEnabled}
                  onCheckedChange={(checked) =>
                    updateDraft((d) => ({
                      ...d,
                      mcpSettings: {
                        ...d.mcpSettings,
                        enabled: checked,
                      },
                    }))
                  }
                  disabled={disabled}
                />
              </div>

              <div className="space-y-2">
                <Label className="text-sm font-semibold">Port</Label>
                <Input
                  type="number"
                  value={mcpPort}
                  onChange={(e) =>
                    updateDraft((d) => ({
                      ...d,
                      mcpSettings: {
                        ...d.mcpSettings,
                        port: Number(e.target.value),
                      },
                    }))
                  }
                  className="w-32"
                  disabled={disabled || !mcpEnabled}
                />
                <p className="text-xs text-muted-foreground">Port must be between 1024-65535</p>
              </div>

              <div className="flex items-center justify-between p-3 border rounded-md">
                <div>
                  <Label className="font-semibold">Use Native UI for MCP Prompts</Label>
                  <p className="text-xs text-muted-foreground">
                    Show confirmation dialogs and user inputs as native Windows dialogs instead of in-app prompts
                  </p>
                </div>
                <Switch
                  checked={draft?.mcpSettings?.useNativeUi ?? true}
                  onCheckedChange={(checked) =>
                    updateDraft((d) => ({
                      ...d,
                      mcpSettings: {
                        ...d.mcpSettings,
                        useNativeUi: checked,
                      },
                    }))
                  }
                  disabled={disabled || !mcpEnabled}
                />
              </div>

              <div className="flex items-center justify-between p-3 border rounded-md bg-red-50 dark:bg-red-950/20 border-red-200 dark:border-red-900">
                <div>
                  <Label className="font-semibold">Enable MCP Management Tools</Label>
                  <p className="text-xs text-muted-foreground">
                    Exposes handler/config/plugin management tools over MCP. Keep disabled unless you trust the MCP client.
                  </p>
                </div>
                <Switch
                  checked={mcpMgmtToolsEnabled}
                  onCheckedChange={(checked) =>
                    updateDraft((d) => ({
                      ...d,
                      mcpSettings: {
                        ...d.mcpSettings,
                        managementToolsEnabled: checked,
                      },
                    }))
                  }
                  disabled={disabled || !mcpEnabled}
                />
              </div>

              <div className="p-3 bg-muted rounded-md">
                <Label className="text-sm font-semibold mb-2 block">Endpoint URL</Label>
                <code className="text-xs">http://127.0.0.1:{mcpPort}/mcp/sse</code>
              </div>

              <div className="flex items-center gap-3">
                <div
                  className={cn(
                    "h-2 w-2 rounded-full",
                    mcpEnabled ? "bg-yellow-500" : "bg-muted-foreground/40",
                  )}
                />
                <span className="text-sm">{mcpEnabled ? 'Enabled (restart required)' : 'Disabled'}</span>
              </div>

              <div className="flex gap-2">
                <Button variant="outline" onClick={() => toast('Start/Stop is not wired yet.')} disabled>
                  Start Server
                </Button>
                <Button variant="outline" onClick={() => toast('Start/Stop is not wired yet.')} disabled>
                  Stop Server
                </Button>
              </div>
            </>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">WPF WebView2 Host Bridge</CardTitle>
          <CardDescription>Hybrid communication via `window.chrome.webview` messaging</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center justify-between p-3 border rounded-md">
            <div>
              <Label className="font-semibold">WebView2 Detected</Label>
              <p className="text-xs text-muted-foreground">
                {webView2Available
                  ? 'Running inside WPF WebView2'
                  : 'Running in a regular browser (dev/build)'}
              </p>
            </div>
            <div
              className={cn(
                'h-2 w-2 rounded-full',
                webView2Available ? 'bg-green-500' : 'bg-muted-foreground/40',
              )}
            />
          </div>

          <div className="flex items-center justify-between p-3 border rounded-md">
            <div>
              <Label className="font-semibold">Host Connection</Label>
              <p className="text-xs text-muted-foreground">
                {hostConnected ? 'Connected' : 'Waiting for host handshake'}
              </p>
            </div>
            <div
              className={cn(
                'h-2 w-2 rounded-full',
                hostConnected ? 'bg-green-500' : 'bg-yellow-500',
              )}
            />
          </div>

          {hostInfo && (
            <div className="p-3 bg-muted rounded-md text-xs space-y-1">
              <div>
                <span className="font-semibold">Host version:</span>{' '}
                <span className="text-muted-foreground">{hostInfo.appVersion ?? '—'}</span>
              </div>
              <div>
                <span className="font-semibold">Host theme:</span>{' '}
                <span className="text-muted-foreground">{hostInfo.theme ?? '—'}</span>
              </div>
              <div>
                <span className="font-semibold">API base:</span>{' '}
                <span className="text-muted-foreground">{hostInfo.apiBaseUrl ?? '—'}</span>
              </div>
              <div>
                <span className="font-semibold">MCP SSE:</span>{' '}
                <span className="text-muted-foreground">{hostInfo.mcpSseUrl ?? '—'}</span>
              </div>
              <div>
                <span className="font-semibold">Last pong:</span>{' '}
                <span className="text-muted-foreground">
                  {lastPongAt ? lastPongAt.toLocaleString() : '—'}
                </span>
              </div>
            </div>
          )}

          <div className="flex gap-2">
            <Button variant="outline" onClick={sendPing} disabled={!webView2Available}>
              Send Ping
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
