import { describe, it, expect, vi, beforeEach } from "vitest";
import { listen } from "@tauri-apps/api/event";
import {
  TauriEvents,
  onHandlerProcessed,
  onClipboardCaptured,
  onHandlersReloaded,
  onMcpRequest,
  onSettingsChanged,
  onCronTriggered,
  onThemeChanged,
  onLog,
  onToast,
  onOpenTab,
  onHostReady,
  onUiConfirmRequest,
  onUiUserInputRequest,
  onUiUserInputNavRequest,
} from "../host/events";

const mockListen = vi.mocked(listen);

describe("TauriEvents constants", () => {
  it("defines all event names", () => {
    expect(TauriEvents.HANDLER_PROCESSED).toBe("handler-processed");
    expect(TauriEvents.CLIPBOARD_CAPTURED).toBe("clipboard-captured");
    expect(TauriEvents.HANDLERS_RELOADED).toBe("handlers-reloaded");
    expect(TauriEvents.MCP_REQUEST).toBe("mcp-request");
    expect(TauriEvents.SETTINGS_CHANGED).toBe("settings-changed");
    expect(TauriEvents.CRON_TRIGGERED).toBe("cron-triggered");
    expect(TauriEvents.THEME_CHANGED).toBe("theme_changed");
    expect(TauriEvents.LOG).toBe("log");
    expect(TauriEvents.TOAST).toBe("toast");
    expect(TauriEvents.OPEN_TAB).toBe("open_tab");
    expect(TauriEvents.HOST_READY).toBe("host_ready");
    expect(TauriEvents.UI_CONFIRM_REQUEST).toBe("ui_confirm_request");
    expect(TauriEvents.UI_USER_INPUT_REQUEST).toBe("ui_user_input_request");
    expect(TauriEvents.UI_USER_INPUT_NAV_REQUEST).toBe("ui_user_input_navigation_request");
  });
});

describe("Event listeners", () => {
  const mockUnlisten = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
    mockListen.mockResolvedValue(mockUnlisten);
  });

  it("onHandlerProcessed listens to handler-processed event", async () => {
    const callback = vi.fn();
    const unlisten = await onHandlerProcessed(callback);
    expect(mockListen).toHaveBeenCalledWith("handler-processed", expect.any(Function));
    expect(unlisten).toBe(mockUnlisten);
  });

  it("onHandlerProcessed callback receives payload", async () => {
    const callback = vi.fn();
    mockListen.mockImplementation(async (_event, handler) => {
      handler({
        id: 1,
        event: "handler-processed",
        payload: {
          handlerName: "test",
          handlerType: "regex",
          status: "processed",
          output: "result",
          timestamp: 123,
        },
      } as never);
      return mockUnlisten;
    });
    await onHandlerProcessed(callback);
    expect(callback).toHaveBeenCalledWith({
      handlerName: "test",
      handlerType: "regex",
      status: "processed",
      output: "result",
      timestamp: 123,
    });
  });

  it("onClipboardCaptured listens to clipboard-captured event", async () => {
    const callback = vi.fn();
    await onClipboardCaptured(callback);
    expect(mockListen).toHaveBeenCalledWith("clipboard-captured", expect.any(Function));
  });

  it("onHandlersReloaded listens to handlers-reloaded event", async () => {
    const callback = vi.fn();
    await onHandlersReloaded(callback);
    expect(mockListen).toHaveBeenCalledWith("handlers-reloaded", expect.any(Function));
  });

  it("onMcpRequest listens to mcp-request event", async () => {
    const callback = vi.fn();
    await onMcpRequest(callback);
    expect(mockListen).toHaveBeenCalledWith("mcp-request", expect.any(Function));
  });

  it("onSettingsChanged listens to settings-changed event", async () => {
    const callback = vi.fn();
    await onSettingsChanged(callback);
    expect(mockListen).toHaveBeenCalledWith("settings-changed", expect.any(Function));
  });

  it("onCronTriggered listens to cron-triggered event", async () => {
    const callback = vi.fn();
    await onCronTriggered(callback);
    expect(mockListen).toHaveBeenCalledWith("cron-triggered", expect.any(Function));
  });

  it("onThemeChanged listens to theme_changed event", async () => {
    const callback = vi.fn();
    await onThemeChanged(callback);
    expect(mockListen).toHaveBeenCalledWith("theme_changed", expect.any(Function));
  });

  it("onLog listens to log event", async () => {
    const callback = vi.fn();
    await onLog(callback);
    expect(mockListen).toHaveBeenCalledWith("log", expect.any(Function));
  });

  it("onToast listens to toast event", async () => {
    const callback = vi.fn();
    await onToast(callback);
    expect(mockListen).toHaveBeenCalledWith("toast", expect.any(Function));
  });

  it("onOpenTab listens to open_tab event", async () => {
    const callback = vi.fn();
    await onOpenTab(callback);
    expect(mockListen).toHaveBeenCalledWith("open_tab", expect.any(Function));
  });

  it("onHostReady listens to host_ready event", async () => {
    const callback = vi.fn();
    await onHostReady(callback);
    expect(mockListen).toHaveBeenCalledWith("host_ready", expect.any(Function));
  });

  it("onUiConfirmRequest listens to ui_confirm_request event", async () => {
    const callback = vi.fn();
    await onUiConfirmRequest(callback);
    expect(mockListen).toHaveBeenCalledWith("ui_confirm_request", expect.any(Function));
  });

  it("onUiUserInputRequest listens to ui_user_input_request event", async () => {
    const callback = vi.fn();
    await onUiUserInputRequest(callback);
    expect(mockListen).toHaveBeenCalledWith("ui_user_input_request", expect.any(Function));
  });

  it("onUiUserInputNavRequest listens to ui_user_input_navigation_request event", async () => {
    const callback = vi.fn();
    await onUiUserInputNavRequest(callback);
    expect(mockListen).toHaveBeenCalledWith("ui_user_input_navigation_request", expect.any(Function));
  });
});
