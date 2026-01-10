import { create } from 'zustand';

export interface TabAction {
  id: string;
  label: string;
}

export interface Tab {
  id: string;
  screenId: string;
  title: string;
  route: string;
  icon?: string;
  closable: boolean;
  context?: unknown;
  actions?: TabAction[];
}

interface TabStore {
  tabs: Tab[];
  activeTabId: string | null;
  openTab: (screenId: string, title: string, context?: unknown, autoFocus?: boolean, actions?: TabAction[]) => void;
  closeTab: (tabId: string) => void;
  setActiveTab: (tabId: string) => void;
}

export const useTabStore = create<TabStore>((set, get) => ({
  tabs: [],
  activeTabId: null,
  
  openTab: (screenId: string, title: string, context?: unknown, autoFocus: boolean = false, actions?: TabAction[]) => {
    const safeScreenId = (screenId ?? '').trim();
    const safeTitle = (title ?? '').trim();
    const route = getRouteForScreen(safeScreenId, safeTitle);
    const key = `${safeScreenId}_${safeTitle}`;
    
    set((state) => {
      // WPF behavior: one tab per unique screen (screenId + title).
      const existingTab = state.tabs.find((t) => t.id === key);
      if (existingTab) {
        const nextTabs = state.tabs.map((t) =>
          t.id === key
            ? { ...t, context: context ?? t.context, title: safeTitle, route, screenId: safeScreenId, actions: actions ?? t.actions }
            : t,
        );
        return { tabs: nextTabs, activeTabId: autoFocus ? existingTab.id : state.activeTabId };
      }
      
      return {
        tabs: [...state.tabs, {
          id: key,
          screenId: safeScreenId,
          title: safeTitle,
          route,
          context,
          actions,
          closable: true,
        }],
        activeTabId: autoFocus ? key : state.activeTabId
      };
    });
  },
  
  closeTab: (tabId: string) => {
    set((state) => {
      const newTabs = state.tabs.filter(t => t.id !== tabId);
      const wasActive = state.activeTabId === tabId;
      
      return {
        tabs: newTabs,
        activeTabId: wasActive 
          ? (newTabs.length > 0 ? newTabs[newTabs.length - 1].id : null)
          : state.activeTabId
      };
    });
  },
  
  setActiveTab: (tabId: string) => {
    set({ activeTabId: tabId });
  }
}));

function getRouteForScreen(screenId: string, title: string): string {
  switch (screenId) {
    case 'settings':
    case 'react_settings':
      return '/settings';
    case 'handler_management':
    case 'handlers':
      return '/handlers';
    case 'handler_exchange':
    case 'marketplace':
      return '/marketplace';
    case 'cron_manager':
    case 'cron':
      return '/cron';
    default:
      return `/tab/${screenId}/${encodeURIComponent(title)}`;
  }
}
