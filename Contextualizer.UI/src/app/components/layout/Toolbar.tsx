import { useEffect, useMemo } from 'react';
import { Home, Settings, Package, Calendar, ShoppingBag, Sun, Moon, Zap } from 'lucide-react';
import { Button } from '../ui/button';
import { Separator } from '../ui/separator';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '../ui/dropdown-menu';
import { useTabStore } from '../../stores/tabStore';
import { useAppStore, type Theme } from '../../stores/appStore';
import { useNavigate } from 'react-router-dom';
import { useHostStore } from '../../stores/hostStore';
import { useHandlersStore } from '../../stores/handlersStore';
import { useAppSettingsStore } from '../../stores/appSettingsStore';
import { executeManualHandler, postWebView2Message, requestHandlersList } from '../../host/webview2Bridge';

export function Toolbar() {
  const navigate = useNavigate();
  const openTab = useTabStore((state) => state.openTab);
  const tabs = useTabStore((state) => state.tabs);
  const { theme, setTheme } = useAppStore();
  const webView2Available = useHostStore((s) => s.webView2Available);
  const hostConnected = useHostStore((s) => s.hostConnected);
  const handlers = useHandlersStore((s) => s.handlers);
  const handlersLoaded = useHandlersStore((s) => s.loaded);
  const settingsDraft = useAppSettingsStore((s) => s.draft);
  const setSettingsDraft = useAppSettingsStore((s) => s.setDraft);

  const manualHandlers = useMemo(() => handlers.filter((h) => h.isManual), [handlers]);

  useEffect(() => {
    if (!webView2Available || !hostConnected) return;
    if (!handlersLoaded) requestHandlersList();
  }, [handlersLoaded, hostConnected, webView2Available]);

  const handleHome = () => {
    // Close all tabs and navigate to dashboard
    const closeTab = useTabStore.getState().closeTab;
    tabs.forEach(tab => closeTab(tab.id));
    navigate('/');
  };

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

  const themeIcons = {
    light: <Sun className="h-4 w-4" />,
    dark: <Moon className="h-4 w-4" />
  };

  const applyTheme = (next: Theme) => {
    setTheme(next);
    if (webView2Available) {
      postWebView2Message({ type: 'set_theme', theme: next });
    }
    // Keep Settings draft in sync so the user can persist it with "Save Changes".
    if (settingsDraft) {
      setSettingsDraft({
        ...settingsDraft,
        uiSettings: {
          ...settingsDraft.uiSettings,
          theme: next,
        },
      });
    }
  };

  return (
    <div className="h-12 border-b bg-card flex items-center px-4 gap-2">
      <Button variant="ghost" size="sm" onClick={handleHome} title="Home">
        <Home className="h-4 w-4" />
      </Button>
      
      <Separator orientation="vertical" className="h-6" />
      
      <Button variant="ghost" size="sm" onClick={handleOpenHandlers} title="Handler Management">
        <Package className="h-4 w-4 mr-2" />
        Handlers
      </Button>
      
      <Button variant="ghost" size="sm" onClick={handleOpenCron} title="Cron Manager">
        <Calendar className="h-4 w-4 mr-2" />
        Cron Jobs
      </Button>
      
      <Button variant="ghost" size="sm" onClick={handleOpenMarketplace} title="Marketplace">
        <ShoppingBag className="h-4 w-4 mr-2" />
        Marketplace
      </Button>
      
      <Button variant="ghost" size="sm" onClick={handleOpenSettings} title="Settings">
        <Settings className="h-4 w-4 mr-2" />
        Settings
      </Button>
      
      <Separator orientation="vertical" className="h-6" />

      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button variant="ghost" size="sm" title="Manual Handlers" disabled={!webView2Available || !hostConnected}>
            <Zap className="h-4 w-4 mr-2" />
            Manual
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="start">
          {manualHandlers.length === 0 ? (
            <DropdownMenuItem disabled>No manual handlers</DropdownMenuItem>
          ) : (
            manualHandlers.map((h) => (
              <DropdownMenuItem key={h.name} onSelect={() => executeManualHandler(h.name)}>
                {h.name}
              </DropdownMenuItem>
            ))
          )}
        </DropdownMenuContent>
      </DropdownMenu>

      <div className="flex-1" />
      
      <Separator orientation="vertical" className="h-6" />
      
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button variant="ghost" size="sm" title="Theme">
            {themeIcons[theme]}
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end">
          <DropdownMenuItem onSelect={() => applyTheme('light')}>
            <Sun className="h-4 w-4 mr-2" />
            Light
          </DropdownMenuItem>
          <DropdownMenuItem onSelect={() => applyTheme('dark')}>
            <Moon className="h-4 w-4 mr-2" />
            Dark
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
    </div>
  );
}
