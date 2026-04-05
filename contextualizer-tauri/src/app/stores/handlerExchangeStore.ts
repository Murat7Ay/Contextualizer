import { create } from 'zustand';

export type HandlerPackageDto = {
  id: string;
  name: string;
  description: string;
  version: string;
  author: string;
  downloadCount?: number;
  tags: string[];
  dependencies: string[];
  isInstalled: boolean;
  hasUpdate: boolean;
  metadata?: Record<string, string>;
};

type HandlerExchangeState = {
  packages: HandlerPackageDto[];
  tags: string[];
  loaded: boolean;
  loading: boolean;
  error: string | null;

  // Details modal state
  detailsOpen: boolean;
  detailsLoading: boolean;
  detailsError: string | null;
  detailsPackage: HandlerPackageDto | null;
  detailsHandlerId: string | null;
  // Operation states
  installingIds: Set<string>;
  updatingIds: Set<string>;
  removingIds: Set<string>;
};

type HandlerExchangeActions = {
  setPackages: (packages: HandlerPackageDto[]) => void;
  setTags: (tags: string[]) => void;
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;

  // Details actions
  openDetails: (handlerId: string) => void;
  closeDetails: () => void;
  setDetailsLoading: (loading: boolean) => void;
  setDetailsError: (error: string | null) => void;
  setDetailsPackage: (handlerId: string, pkg: HandlerPackageDto | null) => void;
  // Operation tracking
  setInstalling: (id: string, installing: boolean) => void;
  setUpdating: (id: string, updating: boolean) => void;
  setRemoving: (id: string, removing: boolean) => void;
  // Optimistic updates
  markInstalled: (id: string) => void;
  markUpdated: (id: string) => void;
  markRemoved: (id: string) => void;
  reset: () => void;
};

const initialState: HandlerExchangeState = {
  packages: [],
  tags: [],
  loaded: false,
  loading: false,
  error: null,
  detailsOpen: false,
  detailsLoading: false,
  detailsError: null,
  detailsPackage: null,
  detailsHandlerId: null,
  installingIds: new Set(),
  updatingIds: new Set(),
  removingIds: new Set(),
};

export const useHandlerExchangeStore = create<HandlerExchangeState & HandlerExchangeActions>((set) => ({
  ...initialState,

  setPackages: (packages) =>
    set({ packages, loaded: true, loading: false, error: null }),

  setTags: (tags) => set({ tags }),

  setLoading: (loading) => set({ loading }),

  setError: (error) => set({ error, loading: false }),

  openDetails: (handlerId) =>
    set({
      detailsOpen: true,
      detailsLoading: true,
      detailsError: null,
      detailsPackage: null,
      detailsHandlerId: handlerId,
    }),

  closeDetails: () =>
    set({
      detailsOpen: false,
      detailsLoading: false,
      detailsError: null,
      detailsPackage: null,
      detailsHandlerId: null,
    }),

  setDetailsLoading: (detailsLoading) => set({ detailsLoading }),

  setDetailsError: (detailsError) => set({ detailsError, detailsLoading: false }),

  setDetailsPackage: (detailsHandlerId, detailsPackage) =>
    set({ detailsHandlerId, detailsPackage, detailsLoading: false, detailsError: null }),

  setInstalling: (id, installing) =>
    set((state) => {
      const next = new Set(state.installingIds);
      if (installing) next.add(id);
      else next.delete(id);
      return { installingIds: next };
    }),

  setUpdating: (id, updating) =>
    set((state) => {
      const next = new Set(state.updatingIds);
      if (updating) next.add(id);
      else next.delete(id);
      return { updatingIds: next };
    }),

  setRemoving: (id, removing) =>
    set((state) => {
      const next = new Set(state.removingIds);
      if (removing) next.add(id);
      else next.delete(id);
      return { removingIds: next };
    }),

  markInstalled: (id) =>
    set((state) => ({
      packages: state.packages.map((p) =>
        p.id === id ? { ...p, isInstalled: true } : p
      ),
      installingIds: (() => {
        const next = new Set(state.installingIds);
        next.delete(id);
        return next;
      })(),
    })),

  markUpdated: (id) =>
    set((state) => ({
      packages: state.packages.map((p) =>
        p.id === id ? { ...p, hasUpdate: false } : p
      ),
      updatingIds: (() => {
        const next = new Set(state.updatingIds);
        next.delete(id);
        return next;
      })(),
    })),

  markRemoved: (id) =>
    set((state) => ({
      packages: state.packages.map((p) =>
        p.id === id ? { ...p, isInstalled: false, hasUpdate: false } : p
      ),
      removingIds: (() => {
        const next = new Set(state.removingIds);
        next.delete(id);
        return next;
      })(),
    })),

  reset: () => set(initialState),
}));

