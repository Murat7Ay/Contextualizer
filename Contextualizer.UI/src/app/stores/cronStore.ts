import { create } from 'zustand';

export interface CronJobDto {
  jobId: string;
  cronExpression: string;
  timezone?: string;
  enabled: boolean;
  lastExecution?: string;
  nextExecution?: string;
  executionCount: number;
  lastError?: string;
  handlerName?: string;
}

interface CronStore {
  isRunning: boolean;
  jobs: CronJobDto[];
  loaded: boolean;
  setCron: (payload: { isRunning: boolean; jobs: CronJobDto[] }) => void;
}

export const useCronStore = create<CronStore>((set) => ({
  isRunning: false,
  jobs: [],
  loaded: false,
  setCron: ({ isRunning, jobs }) => set({ isRunning, jobs, loaded: true }),
}));


