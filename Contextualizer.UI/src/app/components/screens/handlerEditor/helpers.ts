import type { ConditionDraft, ConfigActionDraft, UserInputDraft } from '../../../lib/handlerSchemas';

export const toLines = (values?: string[]) => (values ?? []).join('\n');

export const fromLines = (text: string) =>
  text
    .split(/\r?\n/g)
    .map((v) => v.trim())
    .filter((v) => v.length > 0);

export const toNumberLines = (values?: number[]) => (values ?? []).join('\n');

export const fromNumberLines = (text: string) =>
  text
    .split(/\r?\n/g)
    .map((v) => v.trim())
    .filter((v) => v.length > 0)
    .map((v) => Number(v))
    .filter((v) => !Number.isNaN(v));

export const createEmptyCondition = (): ConditionDraft => ({
  operator: 'equals',
  field: '',
  value: '',
});

export const createEmptyUserInput = (): UserInputDraft => ({
  key: '',
  title: '',
  message: '',
  is_required: true,
  default_value: '',
});

export const createEmptyAction = (): ConfigActionDraft => ({
  name: '',
  requires_confirmation: false,
  user_inputs: [],
  seeder: {},
  constant_seeder: {},
});

export const getHandlerEditorTitle = (mode: 'new' | 'edit', handlerName?: string) =>
  mode === 'new' ? 'New Handler' : `Edit Handler: ${handlerName ?? ''}`;
