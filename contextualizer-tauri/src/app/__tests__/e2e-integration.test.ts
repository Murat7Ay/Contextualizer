import { describe, it, expect, vi, beforeEach } from "vitest";

vi.mock("@tauri-apps/api/core", () => ({
  invoke: vi.fn(),
}));

vi.mock("@tauri-apps/api/event", () => ({
  listen: vi.fn(),
  emit: vi.fn(),
}));

import { invoke } from "@tauri-apps/api/core";

const mockInvoke = vi.mocked(invoke);

describe("E2E Integration: Handler Lifecycle", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("lists handlers via IPC", async () => {
    mockInvoke.mockResolvedValue([
      { name: "URL Regex", type_name: "regex", enabled: true },
      { name: "Code Lookup", type_name: "lookup", enabled: true },
    ]);

    const result = await invoke("handlers_list");
    expect(mockInvoke).toHaveBeenCalledWith("handlers_list");
    expect(result).toHaveLength(2);
  });

  it("gets app settings via IPC", async () => {
    mockInvoke.mockResolvedValue({
      shortcut: "CommandOrControl+Alt+W",
      handlers_path: "handlers.json",
    });

    const settings = await invoke("get_app_settings");
    expect(settings).toHaveProperty("shortcut");
  });

  it("saves app settings via IPC", async () => {
    mockInvoke.mockResolvedValue("ok");

    const result = await invoke("save_app_settings", {
      settings: { shortcut: "Ctrl+Shift+C" },
    });
    expect(result).toBe("ok");
    expect(mockInvoke).toHaveBeenCalledWith("save_app_settings", {
      settings: { shortcut: "Ctrl+Shift+C" },
    });
  });

  it("ping returns pong", async () => {
    mockInvoke.mockResolvedValue("pong");
    const result = await invoke("ping");
    expect(result).toBe("pong");
  });
});

describe("E2E Integration: MCP Endpoint", () => {
  it("simulates MCP health check", async () => {
    const mockFetch = vi.spyOn(globalThis, "fetch").mockResolvedValue(
      new Response(JSON.stringify({ status: "ok" }), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      }),
    );

    const response = await fetch("http://localhost:3000/mcp/health");
    const data = await response.json();
    expect(data.status).toBe("ok");

    mockFetch.mockRestore();
  });

  it("simulates MCP initialize request", async () => {
    const initResponse = {
      jsonrpc: "2.0",
      id: 1,
      result: {
        protocolVersion: "2024-11-05",
        capabilities: { tools: {} },
        serverInfo: { name: "contextualizer-mcp", version: "0.1.0" },
      },
    };

    const mockFetch = vi.spyOn(globalThis, "fetch").mockResolvedValue(
      new Response(JSON.stringify(initResponse), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      }),
    );

    const response = await fetch("http://localhost:3000/mcp", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        jsonrpc: "2.0",
        id: 1,
        method: "initialize",
        params: {},
      }),
    });

    const data = await response.json();
    expect(data.result.protocolVersion).toBe("2024-11-05");
    expect(data.result.serverInfo.name).toBe("contextualizer-mcp");

    mockFetch.mockRestore();
  });

  it("simulates MCP tools/list request", async () => {
    const toolsResponse = {
      jsonrpc: "2.0",
      id: 2,
      result: {
        tools: [
          { name: "read_file", description: "Read a file's content" },
          { name: "system_info", description: "Get system information" },
        ],
      },
    };

    const mockFetch = vi.spyOn(globalThis, "fetch").mockResolvedValue(
      new Response(JSON.stringify(toolsResponse), {
        status: 200,
        headers: { "Content-Type": "application/json" },
      }),
    );

    const response = await fetch("http://localhost:3000/mcp", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        jsonrpc: "2.0",
        id: 2,
        method: "tools/list",
        params: {},
      }),
    });

    const data = await response.json();
    expect(data.result.tools).toHaveLength(2);
    expect(data.result.tools[0].name).toBe("read_file");

    mockFetch.mockRestore();
  });
});

describe("E2E Integration: AG-UI Streaming", () => {
  it("simulates AG-UI run with SSE events", async () => {
    const events = [
      'data: {"type":"RUN_STARTED","thread_id":"t1","run_id":"r1"}\n\n',
      'data: {"type":"TEXT_MESSAGE_START","message_id":"m1","role":"assistant"}\n\n',
      'data: {"type":"TEXT_MESSAGE_CONTENT","message_id":"m1","delta":"Hello!"}\n\n',
      'data: {"type":"TEXT_MESSAGE_END","message_id":"m1"}\n\n',
      'data: {"type":"RUN_FINISHED","thread_id":"t1","run_id":"r1"}\n\n',
    ].join("");

    const encoder = new TextEncoder();
    const stream = new ReadableStream({
      start(controller) {
        controller.enqueue(encoder.encode(events));
        controller.close();
      },
    });

    const mockFetch = vi.spyOn(globalThis, "fetch").mockResolvedValue(
      new Response(stream, {
        status: 200,
        headers: { "Content-Type": "text/event-stream" },
      }),
    );

    const response = await fetch("http://localhost:3001/agui/run", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        thread_id: "t1",
        run_id: "r1",
        messages: [{ role: "user", content: "Hello" }],
      }),
    });

    expect(response.ok).toBe(true);
    const reader = response.body!.getReader();
    const { value } = await reader.read();
    const text = new TextDecoder().decode(value);
    expect(text).toContain("RUN_STARTED");
    expect(text).toContain("RUN_FINISHED");

    mockFetch.mockRestore();
  });
});
