import { CopilotSidebar } from "@copilotkit/react-ui";
import "@copilotkit/react-ui/styles.css";

interface ChatPanelProps {
  title?: string;
}

export function ChatPanel({ title = "Contextualizer AI" }: ChatPanelProps) {
  return (
    <CopilotSidebar
      labels={{
        title,
        initial: "How can I help you with your clipboard workflows?",
      }}
      defaultOpen={false}
    />
  );
}
