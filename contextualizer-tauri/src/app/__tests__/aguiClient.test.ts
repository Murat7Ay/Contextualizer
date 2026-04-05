import { describe, it, expect, vi, beforeEach } from "vitest";
import {
  streamAgUiEvents,
  createAgUiRunInput,
  type AgUiRunInput,
} from "../ai/aguiClient";

function createMockSSEResponse(events: Array<Record<string, unknown>>): Response {
  const sseText = events.map((e) => `data: ${JSON.stringify(e)}\n\n`).join("");
  const encoder = new TextEncoder();
  const stream = new ReadableStream({
    start(controller) {
      controller.enqueue(encoder.encode(sseText));
      controller.close();
    },
  });
  return new Response(stream, {
    status: 200,
    headers: { "Content-Type": "text/event-stream" },
  });
}

describe("aguiClient", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  describe("createAgUiRunInput", () => {
    it("creates valid run input with thread id and message", () => {
      const input = createAgUiRunInput("thread-1", "Hello");
      expect(input.thread_id).toBe("thread-1");
      expect(input.messages).toHaveLength(1);
      expect(input.messages[0].role).toBe("user");
      expect(input.messages[0].content).toBe("Hello");
      expect(input.run_id).toBeTruthy();
    });

    it("generates unique run ids", () => {
      const input1 = createAgUiRunInput("t1", "a");
      const input2 = createAgUiRunInput("t1", "b");
      expect(input1.run_id).not.toBe(input2.run_id);
    });
  });

  describe("streamAgUiEvents", () => {
    it("parses SSE events from response stream", async () => {
      const mockEvents = [
        { type: "RUN_STARTED", thread_id: "t1", run_id: "r1" },
        { type: "TEXT_MESSAGE_START", message_id: "m1", role: "assistant" },
        { type: "TEXT_MESSAGE_CONTENT", message_id: "m1", delta: "Hello" },
        { type: "TEXT_MESSAGE_END", message_id: "m1" },
        { type: "RUN_FINISHED", thread_id: "t1", run_id: "r1" },
      ];

      vi.spyOn(globalThis, "fetch").mockResolvedValue(
        createMockSSEResponse(mockEvents),
      );

      const input: AgUiRunInput = createAgUiRunInput("t1", "test");
      const events: Array<Record<string, unknown>> = [];
      for await (const event of streamAgUiEvents("/agui/run", input)) {
        events.push(event);
      }

      expect(events).toHaveLength(5);
      expect(events[0].type).toBe("RUN_STARTED");
      expect(events[4].type).toBe("RUN_FINISHED");
    });

    it("throws on non-ok response", async () => {
      vi.spyOn(globalThis, "fetch").mockResolvedValue(
        new Response("error", { status: 500 }),
      );

      const input = createAgUiRunInput("t1", "test");
      await expect(async () => {
        // eslint-disable-next-line @typescript-eslint/no-unused-vars
        for await (const _event of streamAgUiEvents("/agui/run", input)) {
          // should not reach here
        }
      }).rejects.toThrow("AG-UI request failed: 500");
    });

    it("skips malformed SSE data lines", async () => {
      const encoder = new TextEncoder();
      const sseText = [
        'data: {"type":"RUN_STARTED"}',
        "",
        "data: not-json",
        "",
        'data: {"type":"RUN_FINISHED"}',
        "",
      ].join("\n");
      const stream = new ReadableStream({
        start(controller) {
          controller.enqueue(encoder.encode(sseText));
          controller.close();
        },
      });

      vi.spyOn(globalThis, "fetch").mockResolvedValue(
        new Response(stream, { status: 200 }),
      );

      const input = createAgUiRunInput("t1", "test");
      const events: Array<Record<string, unknown>> = [];
      for await (const event of streamAgUiEvents("/agui/run", input)) {
        events.push(event);
      }

      expect(events).toHaveLength(2);
      expect(events[0].type).toBe("RUN_STARTED");
      expect(events[1].type).toBe("RUN_FINISHED");
    });

    it("handles tool call events in stream", async () => {
      const mockEvents = [
        { type: "RUN_STARTED", thread_id: "t1", run_id: "r1" },
        { type: "TOOL_CALL_START", tool_call_id: "tc1", tool_call_name: "search" },
        { type: "TOOL_CALL_ARGS", tool_call_id: "tc1", delta: '{"query":"test"}' },
        { type: "TOOL_CALL_END", tool_call_id: "tc1" },
        { type: "TOOL_CALL_RESULT", tool_call_id: "tc1", result: "found 3 results" },
        { type: "RUN_FINISHED", thread_id: "t1", run_id: "r1" },
      ];

      vi.spyOn(globalThis, "fetch").mockResolvedValue(
        createMockSSEResponse(mockEvents),
      );

      const input = createAgUiRunInput("t1", "search for test");
      const events: Array<Record<string, unknown>> = [];
      for await (const event of streamAgUiEvents("/agui/run", input)) {
        events.push(event);
      }

      expect(events).toHaveLength(6);
      const toolStart = events.find((e) => e.type === "TOOL_CALL_START");
      expect(toolStart).toBeDefined();
      expect((toolStart as Record<string, unknown>).tool_call_name).toBe("search");
    });

    it("supports abort signal", async () => {
      const controller = new AbortController();
      vi.spyOn(globalThis, "fetch").mockImplementation(
        async (_url, init) => {
          expect((init as RequestInit).signal).toBe(controller.signal);
          controller.abort();
          throw new DOMException("Aborted", "AbortError");
        },
      );

      const input = createAgUiRunInput("t1", "test");
      await expect(async () => {
        for await (const _event of streamAgUiEvents(
          "/agui/run",
          input,
          controller.signal,
        )) {
          // should not reach here
        }
      }).rejects.toThrow();
    });
  });
});
