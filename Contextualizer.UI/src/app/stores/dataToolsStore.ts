import { create } from 'zustand';

export interface DataToolParameterDefinitionDto {
  name: string;
  db_parameter_name?: string | null;
  type: string;
  description?: string | null;
  required: boolean;
  default_value?: unknown;
  enum?: string[] | null;
  array_item_type?: string | null;
  direction: string;
  db_type?: string | null;
  serialize_as_json: boolean;
}

export interface DataToolResultOptionsDto {
  mode?: string | null;
  max_rows: number;
  include_execution_metadata: boolean;
  include_output_parameters: boolean;
  output_scalar_parameter?: string | null;
}

export interface DataToolDefinitionDto {
  id: string;
  name?: string | null;
  tool_name?: string | null;
  description?: string | null;
  provider: string;
  operation: string;
  connection: string;
  statement?: string | null;
  procedure_name?: string | null;
  enabled: boolean;
  expose_as_tool: boolean;
  command_timeout_seconds?: number | null;
  connection_timeout_seconds?: number | null;
  max_pool_size?: number | null;
  min_pool_size?: number | null;
  disable_pooling?: boolean | null;
  parameters: DataToolParameterDefinitionDto[];
  input_schema?: unknown;
  tags: string[];
  result: DataToolResultOptionsDto;
  provider_options?: unknown;
  resolved_tool_name?: string;
  is_supported?: boolean;
}

interface DataToolsStore {
  loaded: boolean;
  registryPath: string | null;
  definitions: DataToolDefinitionDto[];
  error: string | null;
  setFromHost: (registryPath: string | null, definitions: DataToolDefinitionDto[]) => void;
  setError: (error: string | null) => void;
}

export const useDataToolsStore = create<DataToolsStore>((set) => ({
  loaded: false,
  registryPath: null,
  definitions: [],
  error: null,
  setFromHost: (registryPath, definitions) =>
    set({
      loaded: true,
      registryPath,
      definitions,
      error: null,
    }),
  setError: (error) => set({ error }),
}));