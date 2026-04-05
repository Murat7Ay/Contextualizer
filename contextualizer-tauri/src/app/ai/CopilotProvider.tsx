import { CopilotKit } from "@copilotkit/react-core";
import type { ReactNode } from "react";

interface CopilotProviderProps {
  children: ReactNode;
  runtimeUrl?: string;
}

const DEFAULT_RUNTIME_URL = "http://localhost:3001/agui/run";

export function CopilotProvider({
  children,
  runtimeUrl = DEFAULT_RUNTIME_URL,
}: CopilotProviderProps) {
  return (
    <CopilotKit runtimeUrl={runtimeUrl}>
      {children}
    </CopilotKit>
  );
}
