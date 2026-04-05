import { describe, it, expect, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import React from "react";

vi.mock("@tauri-apps/api/core", () => ({
  invoke: vi.fn(),
}));

vi.mock("@tauri-apps/api/event", () => ({
  listen: vi.fn(),
  emit: vi.fn(),
}));

import App from "../App";

describe("App", () => {
  it("renders the dashboard heading", () => {
    render(<App />);
    expect(screen.getByText("Contextualizer")).toBeInTheDocument();
  });

  it("renders the phase description", () => {
    render(<App />);
    expect(
      screen.getByText("Tauri v2 Migration - Phase 1 Scaffold"),
    ).toBeInTheDocument();
  });

  it("redirects unknown routes to dashboard", () => {
    window.history.pushState({}, "", "/unknown-route");
    render(<App />);
    expect(screen.getByText("Contextualizer")).toBeInTheDocument();
  });
});
