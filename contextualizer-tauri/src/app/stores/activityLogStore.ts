import { create } from 'zustand';

export type LogLevel = 'success' | 'error' | 'warning' | 'info' | 'debug' | 'critical';

export interface LogEntry {
  id: string;
  timestamp: Date;
  level: LogLevel;
  message: string;
  details?: string;
}

interface ActivityLogStore {
  logs: LogEntry[];
  filter: LogLevel | 'all';
  searchQuery: string;
  addLog: (level: LogLevel, message: string, details?: string) => void;
  clearLogs: () => void;
  setFilter: (filter: LogLevel | 'all') => void;
  setSearchQuery: (query: string) => void;
}

export const useActivityLogStore = create<ActivityLogStore>((set) => ({
  logs: [],
  filter: 'all',
  searchQuery: '',
  
  addLog: (level, message, details) => {
    set((state) => ({
      logs: [{
        id: `${Date.now()}_${Math.random()}`,
        timestamp: new Date(),
        level,
        message,
        details
      }, ...state.logs].slice(0, 1000) // Keep only last 1000 logs
    }));
  },
  
  clearLogs: () => set({ logs: [] }),
  setFilter: (filter) => set({ filter }),
  setSearchQuery: (query) => set({ searchQuery: query }),
}));
