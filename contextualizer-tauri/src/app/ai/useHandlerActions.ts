import { useCopilotAction, useCopilotReadable } from "@copilotkit/react-core";
import { useCallback, useState } from "react";

interface HandlerSummary {
  name: string;
  type: string;
  enabled: boolean;
}

export function useHandlerActions() {
  const [handlers, setHandlers] = useState<HandlerSummary[]>([]);

  useCopilotReadable({
    description: "List of configured clipboard handlers",
    value: handlers,
  });

  useCopilotAction({
    name: "list_handlers",
    description: "List all configured clipboard handlers",
    parameters: [],
    handler: async () => {
      return handlers.map((h) => `${h.name} (${h.type}) - ${h.enabled ? "enabled" : "disabled"}`).join("\n");
    },
  });

  useCopilotAction({
    name: "toggle_handler",
    description: "Enable or disable a handler by name",
    parameters: [
      { name: "handlerName", type: "string", description: "Name of the handler", required: true },
      { name: "enabled", type: "boolean", description: "Whether to enable or disable", required: true },
    ],
    handler: async ({ handlerName, enabled }: { handlerName: string; enabled: boolean }) => {
      setHandlers((prev) =>
        prev.map((h) => (h.name === handlerName ? { ...h, enabled } : h)),
      );
      return `Handler '${handlerName}' ${enabled ? "enabled" : "disabled"}`;
    },
  });

  const refreshHandlers = useCallback((newHandlers: HandlerSummary[]) => {
    setHandlers(newHandlers);
  }, []);

  return { handlers, refreshHandlers };
}
