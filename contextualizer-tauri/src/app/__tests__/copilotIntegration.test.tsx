import { describe, it, expect, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import React from "react";

vi.mock("@copilotkit/react-core", () => ({
  CopilotKit: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="copilot-kit">{children}</div>
  ),
  useCopilotAction: vi.fn(),
  useCopilotReadable: vi.fn(),
}));

vi.mock("@copilotkit/react-ui", () => ({
  CopilotSidebar: ({ labels }: { labels: { title: string; initial: string } }) => (
    <div data-testid="copilot-sidebar">
      <span data-testid="sidebar-title">{labels.title}</span>
      <span data-testid="sidebar-initial">{labels.initial}</span>
    </div>
  ),
}));

import { CopilotProvider } from "../ai/CopilotProvider";
import { ChatPanel } from "../ai/ChatPanel";

describe("CopilotProvider", () => {
  it("renders children inside CopilotKit wrapper", () => {
    render(
      <CopilotProvider>
        <div data-testid="child">Hello</div>
      </CopilotProvider>,
    );
    expect(screen.getByTestId("copilot-kit")).toBeInTheDocument();
    expect(screen.getByTestId("child")).toBeInTheDocument();
  });

  it("accepts custom runtime URL", () => {
    render(
      <CopilotProvider runtimeUrl="http://custom:8080/run">
        <span>test</span>
      </CopilotProvider>,
    );
    expect(screen.getByTestId("copilot-kit")).toBeInTheDocument();
  });
});

describe("ChatPanel", () => {
  it("renders sidebar with default title", () => {
    render(<ChatPanel />);
    expect(screen.getByTestId("copilot-sidebar")).toBeInTheDocument();
    expect(screen.getByTestId("sidebar-title")).toHaveTextContent("Contextualizer AI");
  });

  it("renders sidebar with custom title", () => {
    render(<ChatPanel title="Custom Title" />);
    expect(screen.getByTestId("sidebar-title")).toHaveTextContent("Custom Title");
  });

  it("displays initial message prompt", () => {
    render(<ChatPanel />);
    expect(screen.getByTestId("sidebar-initial")).toHaveTextContent(
      "How can I help you with your clipboard workflows?",
    );
  });
});
