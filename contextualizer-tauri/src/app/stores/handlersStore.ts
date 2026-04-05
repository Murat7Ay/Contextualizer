import { create } from 'zustand';

export interface HandlerDto {
  name: string;
  description?: string;
  type?: string;
  enabled: boolean;
  mcpEnabled: boolean;
  screenId?: string;
  title?: string;
  isManual?: boolean;
}

interface HandlersStore {
  handlers: HandlerDto[];
  loaded: boolean;
  setHandlers: (handlers: HandlerDto[]) => void;
}

export const useHandlersStore = create<HandlersStore>((set) => ({
  handlers: [],
  loaded: false,
  setHandlers: (handlers) => set({ handlers, loaded: true }),
}));


