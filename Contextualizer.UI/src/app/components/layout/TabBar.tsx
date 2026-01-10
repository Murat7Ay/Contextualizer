import { X } from 'lucide-react';
import { useTabStore } from '../../stores/tabStore';
import { useNavigate } from 'react-router-dom';
import { Button } from '../ui/button';
import { cn } from '../ui/utils';
import { notifyTabClosed } from '../../host/webview2Bridge';

export function TabBar() {
  const navigate = useNavigate();
  const { tabs, activeTabId, setActiveTab, closeTab } = useTabStore();

  const handleTabClick = (tabId: string, route: string) => {
    setActiveTab(tabId);
    navigate(route);
  };

  const handleCloseTab = (e: React.MouseEvent, tabId: string) => {
    e.stopPropagation();
    // Let the WPF host clean up any per-tab resources (e.g., stored action callbacks).
    notifyTabClosed(tabId);
    closeTab(tabId);
    
    // Navigate to dashboard if no tabs left
    const remainingTabs = useTabStore.getState().tabs;
    if (remainingTabs.length === 0) {
      navigate('/');
    } else {
      // Navigate to the newly active tab
      const newActiveId = useTabStore.getState().activeTabId;
      const newActiveTab = remainingTabs.find(t => t.id === newActiveId);
      if (newActiveTab) {
        navigate(newActiveTab.route);
      }
    }
  };

  return (
    <div className="h-10 border-b bg-muted/30 flex items-center px-2 gap-1 overflow-x-auto">
      {tabs.map((tab) => (
        <button
          key={tab.id}
          onClick={() => handleTabClick(tab.id, tab.route)}
          onAuxClick={(e) => {
            // Some Chromium builds fire middle-click as "auxclick".
            if (e.button === 1) {
              e.preventDefault();
              handleCloseTab(e, tab.id);
            }
          }}
          onMouseDown={(e) => {
            // Chrome-style middle click (mouse wheel click) closes the tab
            if (e.button === 1 || (typeof e.buttons === 'number' && (e.buttons & 4) === 4)) {
              e.preventDefault();
              handleCloseTab(e, tab.id);
            }
          }}
          className={cn(
            "h-8 px-3 rounded-md flex items-center gap-2 group transition-colors",
            "hover:bg-accent/50",
            activeTabId === tab.id 
              ? "bg-background shadow-sm border" 
              : "bg-transparent"
          )}
        >
          <span className="text-sm truncate max-w-[200px]">
            {tab.title}
          </span>
          {tab.closable && (
            <Button
              variant="ghost"
              size="sm"
              className="h-4 w-4 p-0 opacity-0 group-hover:opacity-100 hover:bg-destructive/10"
              onClick={(e) => handleCloseTab(e, tab.id)}
            >
              <X className="h-3 w-3" />
            </Button>
          )}
        </button>
      ))}
    </div>
  );
}
