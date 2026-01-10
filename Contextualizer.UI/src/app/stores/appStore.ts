import { create } from 'zustand';

export type Theme = 'light' | 'dark';

interface AppSettings {
  theme: Theme;
  apiUrl: string;
  apiConnected: boolean;
  activityLogPosition: 'right' | 'bottom';
  activityLogSize: number;
}

interface AppStore extends AppSettings {
  setTheme: (theme: Theme) => void;
  setApiConnected: (connected: boolean) => void;
  setActivityLogPosition: (position: 'right' | 'bottom') => void;
  setActivityLogSize: (size: number) => void;
}

export const useAppStore = create<AppStore>((set) => ({
  theme: 'dark',
  apiUrl: 'http://localhost:5000/api',
  apiConnected: false,
  activityLogPosition: 'right',
  activityLogSize: 350,
  
  setTheme: (theme) => set({ theme }),
  setApiConnected: (connected) => set({ apiConnected: connected }),
  setActivityLogPosition: (position) => set({ activityLogPosition: position }),
  setActivityLogSize: (size) => set({ activityLogSize: size }),
}));