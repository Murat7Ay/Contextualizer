import { describe, it, expect, vi, beforeEach } from "vitest";
import { renderHook, act } from "@testing-library/react";

const mockUseCopilotAction = vi.fn();
const mockUseCopilotReadable = vi.fn();

vi.mock("@copilotkit/react-core", () => ({
  useCopilotAction: (...args: unknown[]) => mockUseCopilotAction(...args),
  useCopilotReadable: (...args: unknown[]) => mockUseCopilotReadable(...args),
}));

import { useHandlerActions } from "../ai/useHandlerActions";

describe("useHandlerActions", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("registers copilot readable and two actions", () => {
    renderHook(() => useHandlerActions());
    expect(mockUseCopilotReadable).toHaveBeenCalledTimes(1);
    expect(mockUseCopilotAction).toHaveBeenCalledTimes(2);
  });

  it("registers list_handlers action", () => {
    renderHook(() => useHandlerActions());
    const listCall = mockUseCopilotAction.mock.calls.find(
      (call: unknown[]) => (call[0] as { name: string }).name === "list_handlers",
    );
    expect(listCall).toBeDefined();
    expect((listCall![0] as { description: string }).description).toContain("List all");
  });

  it("registers toggle_handler action with parameters", () => {
    renderHook(() => useHandlerActions());
    const toggleCall = mockUseCopilotAction.mock.calls.find(
      (call: unknown[]) => (call[0] as { name: string }).name === "toggle_handler",
    );
    expect(toggleCall).toBeDefined();
    const params = (toggleCall![0] as { parameters: Array<{ name: string }> }).parameters;
    expect(params).toHaveLength(2);
    expect(params[0].name).toBe("handlerName");
    expect(params[1].name).toBe("enabled");
  });

  it("refreshHandlers updates handler state", () => {
    const { result } = renderHook(() => useHandlerActions());
    expect(result.current.handlers).toHaveLength(0);

    act(() => {
      result.current.refreshHandlers([
        { name: "test", type: "regex", enabled: true },
      ]);
    });

    expect(result.current.handlers).toHaveLength(1);
    expect(result.current.handlers[0].name).toBe("test");
  });

  it("readable context includes handler list", () => {
    renderHook(() => useHandlerActions());
    const readableCall = mockUseCopilotReadable.mock.calls[0][0] as {
      description: string;
    };
    expect(readableCall.description).toContain("handler");
  });

  it("list_handlers action has correct structure", () => {
    renderHook(() => useHandlerActions());
    const listCall = mockUseCopilotAction.mock.calls.find(
      (call: unknown[]) => (call[0] as { name: string }).name === "list_handlers",
    );
    expect(listCall).toBeDefined();
    const config = listCall![0] as { handler: () => Promise<string>; parameters: unknown[] };
    expect(config.parameters).toEqual([]);
    expect(typeof config.handler).toBe("function");
  });

  it("toggle_handler action handler toggles handler state", async () => {
    const { result } = renderHook(() => useHandlerActions());

    act(() => {
      result.current.refreshHandlers([
        { name: "myHandler", type: "regex", enabled: true },
      ]);
    });

    const toggleCall = mockUseCopilotAction.mock.calls.find(
      (call: unknown[]) => (call[0] as { name: string }).name === "toggle_handler",
    );
    const handler = (toggleCall![0] as {
      handler: (args: { handlerName: string; enabled: boolean }) => Promise<string>;
    }).handler;

    let output: string = "";
    await act(async () => {
      output = await handler({ handlerName: "myHandler", enabled: false });
    });
    expect(output).toContain("disabled");
  });
});
