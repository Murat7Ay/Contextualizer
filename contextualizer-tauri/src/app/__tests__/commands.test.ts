import { describe, it, expect, vi, beforeEach } from "vitest";
import { invoke } from "@tauri-apps/api/core";
import {
  greet,
  ping,
  handlersList,
  handlerGet,
  handlerCreate,
  handlerDelete,
  handlerToggle,
  handlerReload,
  handlerUpdate,
  handlerSetMcp,
  manualHandlerExecute,
  getAppSettings,
  saveAppSettings,
  dispatchClipboard,
  mcpToolsList,
  pluginList,
  configGet,
  configSet,
  cronList,
  cronSetEnabled,
  cronTrigger,
  cronUpdate,
  uiConfirm,
  uiNotify,
  openExternal,
  setTheme,
  exchangeList,
  exchangeInstall,
  exchangeRemove,
  emitLog,
  emitToast,
  emitOpenTab,
  openFileDialog,
  openFolderDialog,
  requestConfirm,
  submitConfirmResponse,
  requestUserInput,
  submitUserInputResponse,
  tabActionExecute,
  tabClosed,
  toastActionExecute,
  toastClosed,
  loggingTest,
  usageTest,
  logClear,
  exchangeTags,
  exchangeDetails,
  exchangeUpdate,
  exchangePublish,
  aiSkillsHubList,
  aiSkillsHubDeploy,
  aiSkillsHubRemove,
  aiSkillsHubPull,
  pluginsListFull,
} from "../host/commands";

const mockInvoke = vi.mocked(invoke);

describe("Type-safe command wrappers", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("greet calls invoke with name", async () => {
    mockInvoke.mockResolvedValue("Hello, Murat!");
    const result = await greet("Murat");
    expect(mockInvoke).toHaveBeenCalledWith("greet", { name: "Murat" });
    expect(result).toBe("Hello, Murat!");
  });

  it("ping calls invoke without args", async () => {
    mockInvoke.mockResolvedValue({ pong: true, timestamp: 123 });
    const result = await ping();
    expect(mockInvoke).toHaveBeenCalledWith("ping");
    expect(result.pong).toBe(true);
  });

  it("handlersList returns handler summaries", async () => {
    mockInvoke.mockResolvedValue([
      { name: "test", handlerType: "regex", enabled: true, mcpEnabled: false, description: null },
    ]);
    const result = await handlersList();
    expect(mockInvoke).toHaveBeenCalledWith("handlers_list");
    expect(result).toHaveLength(1);
    expect(result[0].name).toBe("test");
  });

  it("handlerGet calls invoke with name", async () => {
    mockInvoke.mockResolvedValue({ name: "test", type: "regex" });
    const result = await handlerGet("test");
    expect(mockInvoke).toHaveBeenCalledWith("handler_get", { name: "test" });
    expect(result.name).toBe("test");
  });

  it("handlerCreate calls invoke with config", async () => {
    const config = { name: "new", type: "regex" };
    mockInvoke.mockResolvedValue({ name: "new", handlerType: "regex", enabled: true, mcpEnabled: false, description: null });
    const result = await handlerCreate(config);
    expect(mockInvoke).toHaveBeenCalledWith("handler_create", { config });
    expect(result.name).toBe("new");
  });

  it("handlerDelete calls invoke with name", async () => {
    mockInvoke.mockResolvedValue({ name: "deleted", handlerType: "regex", enabled: true, mcpEnabled: false, description: null });
    const result = await handlerDelete("deleted");
    expect(mockInvoke).toHaveBeenCalledWith("handler_delete", { name: "deleted" });
    expect(result.name).toBe("deleted");
  });

  it("handlerToggle calls invoke with name and enabled", async () => {
    mockInvoke.mockResolvedValue(false);
    const result = await handlerToggle("test", false);
    expect(mockInvoke).toHaveBeenCalledWith("handler_toggle", { name: "test", enabled: false });
    expect(result).toBe(false);
  });

  it("handlerReload calls invoke", async () => {
    mockInvoke.mockResolvedValue(5);
    const result = await handlerReload();
    expect(mockInvoke).toHaveBeenCalledWith("handler_reload");
    expect(result).toBe(5);
  });

  it("getAppSettings returns settings", async () => {
    mockInvoke.mockResolvedValue({
      handlersFilePath: "handlers.json",
      mcpSettings: { enabled: true, port: 3000, useNativeUi: false, managementToolsEnabled: true },
    });
    const result = await getAppSettings();
    expect(mockInvoke).toHaveBeenCalledWith("get_app_settings");
    expect(result.mcpSettings?.port).toBe(3000);
  });

  it("saveAppSettings calls invoke with settings", async () => {
    mockInvoke.mockResolvedValue(true);
    const settings = { uiSettings: { theme: "dark" } };
    const result = await saveAppSettings(settings);
    expect(mockInvoke).toHaveBeenCalledWith("save_app_settings", { settings });
    expect(result).toBe(true);
  });

  it("dispatchClipboard calls invoke with input", async () => {
    mockInvoke.mockResolvedValue({ handlerName: "url", status: "processed", output: "matched" });
    const result = await dispatchClipboard("https://example.com");
    expect(mockInvoke).toHaveBeenCalledWith("dispatch_clipboard", { input: "https://example.com" });
    expect(result.status).toBe("processed");
  });

  it("mcpToolsList returns tool summaries", async () => {
    mockInvoke.mockResolvedValue([
      { name: "read_file", description: "Read a file" },
    ]);
    const result = await mcpToolsList();
    expect(mockInvoke).toHaveBeenCalledWith("mcp_tools_list");
    expect(result[0].name).toBe("read_file");
  });

  it("pluginList returns plugin summaries", async () => {
    mockInvoke.mockResolvedValue([]);
    const result = await pluginList();
    expect(mockInvoke).toHaveBeenCalledWith("plugin_list");
    expect(result).toEqual([]);
  });

  it("configGet calls invoke with key", async () => {
    mockInvoke.mockResolvedValue("value1");
    const result = await configGet("key1");
    expect(mockInvoke).toHaveBeenCalledWith("config_get", { key: "key1" });
    expect(result).toBe("value1");
  });

  it("configSet calls invoke with key and value", async () => {
    mockInvoke.mockResolvedValue(true);
    const result = await configSet("key1", "value1");
    expect(mockInvoke).toHaveBeenCalledWith("config_set", { key: "key1", value: "value1" });
    expect(result).toBe(true);
  });

  // ── Cron commands ──────────────────────────────────────────────

  it("cronList returns cron list response", async () => {
    mockInvoke.mockResolvedValue({
      isRunning: true,
      jobs: [{ jobId: "j1", handlerName: "h1", cronExpression: "every 30s", enabled: true }],
    });
    const result = await cronList();
    expect(mockInvoke).toHaveBeenCalledWith("cron_list");
    expect(result.isRunning).toBe(true);
    expect(result.jobs).toHaveLength(1);
  });

  it("cronSetEnabled calls invoke with jobId and enabled", async () => {
    mockInvoke.mockResolvedValue(false);
    const result = await cronSetEnabled("j1", false);
    expect(mockInvoke).toHaveBeenCalledWith("cron_set_enabled", { jobId: "j1", enabled: false });
    expect(result).toBe(false);
  });

  it("cronTrigger calls invoke with jobId", async () => {
    mockInvoke.mockResolvedValue("handler1");
    const result = await cronTrigger("j1");
    expect(mockInvoke).toHaveBeenCalledWith("cron_trigger", { jobId: "j1" });
    expect(result).toBe("handler1");
  });

  it("cronUpdate calls invoke with jobId and expression", async () => {
    mockInvoke.mockResolvedValue(true);
    const result = await cronUpdate("j1", "every 5m");
    expect(mockInvoke).toHaveBeenCalledWith("cron_update", { jobId: "j1", cronExpression: "every 5m" });
    expect(result).toBe(true);
  });

  // ── Handler update / MCP / manual ─────────────────────────────

  it("handlerUpdate calls invoke with name and updates", async () => {
    mockInvoke.mockResolvedValue(true);
    const result = await handlerUpdate("test", { description: "updated" });
    expect(mockInvoke).toHaveBeenCalledWith("handler_update", {
      handlerName: "test",
      updates: { description: "updated" },
    });
    expect(result).toBe(true);
  });

  it("handlerSetMcp calls invoke with name and mcpEnabled", async () => {
    mockInvoke.mockResolvedValue(true);
    const result = await handlerSetMcp("test", true);
    expect(mockInvoke).toHaveBeenCalledWith("handler_set_mcp", { name: "test", mcpEnabled: true });
    expect(result).toBe(true);
  });

  it("manualHandlerExecute calls invoke with name", async () => {
    mockInvoke.mockResolvedValue({ handlerName: "manual1", status: "executed", output: null });
    const result = await manualHandlerExecute("manual1");
    expect(mockInvoke).toHaveBeenCalledWith("manual_handler_execute", { name: "manual1" });
    expect(result.status).toBe("executed");
  });

  // ── UI dialogs ────────────────────────────────────────────────

  it("uiConfirm calls invoke with title and message", async () => {
    mockInvoke.mockResolvedValue({ confirmed: true });
    const result = await uiConfirm("Title", "Are you sure?");
    expect(mockInvoke).toHaveBeenCalledWith("ui_confirm", { title: "Title", message: "Are you sure?" });
    expect(result.confirmed).toBe(true);
  });

  it("uiNotify calls invoke with message", async () => {
    mockInvoke.mockResolvedValue(true);
    const result = await uiNotify("Hello!", "Info");
    expect(mockInvoke).toHaveBeenCalledWith("ui_notify", { message: "Hello!", title: "Info" });
    expect(result).toBe(true);
  });

  it("openExternal calls invoke with url", async () => {
    mockInvoke.mockResolvedValue(true);
    const result = await openExternal("https://example.com");
    expect(mockInvoke).toHaveBeenCalledWith("open_external", { url: "https://example.com" });
    expect(result).toBe(true);
  });

  // ── Theme ─────────────────────────────────────────────────────

  it("setTheme calls invoke with theme", async () => {
    mockInvoke.mockResolvedValue(true);
    const result = await setTheme("dark");
    expect(mockInvoke).toHaveBeenCalledWith("set_theme", { theme: "dark" });
    expect(result).toBe(true);
  });

  // ── Exchange ──────────────────────────────────────────────────

  it("exchangeList calls invoke with optional params", async () => {
    mockInvoke.mockResolvedValue([]);
    const result = await exchangeList("test", ["tag1"]);
    expect(mockInvoke).toHaveBeenCalledWith("exchange_list", { searchTerm: "test", tags: ["tag1"] });
    expect(result).toEqual([]);
  });

  it("exchangeInstall calls invoke with handlerId", async () => {
    mockInvoke.mockRejectedValue({ kind: "general", message: "Exchange not yet implemented" });
    await expect(exchangeInstall("pkg-1")).rejects.toEqual({
      kind: "general",
      message: "Exchange not yet implemented",
    });
  });

  it("exchangeRemove calls invoke with handlerId", async () => {
    mockInvoke.mockRejectedValue({ kind: "general", message: "Exchange not yet implemented" });
    await expect(exchangeRemove("pkg-1")).rejects.toEqual({
      kind: "general",
      message: "Exchange not yet implemented",
    });
  });

  // ── Log / Toast / Tab ─────────────────────────────────────────

  it("emitLog calls invoke with level and message", async () => {
    mockInvoke.mockResolvedValue(true);
    const result = await emitLog("info", "Test log", "details");
    expect(mockInvoke).toHaveBeenCalledWith("emit_log", {
      level: "info",
      message: "Test log",
      details: "details",
    });
    expect(result).toBe(true);
  });

  it("emitToast calls invoke with level and message", async () => {
    mockInvoke.mockResolvedValue(true);
    const result = await emitToast("success", "Done!", "Title", 5);
    expect(mockInvoke).toHaveBeenCalledWith("emit_toast", {
      level: "success",
      message: "Done!",
      title: "Title",
      durationSeconds: 5,
    });
    expect(result).toBe(true);
  });

  it("emitOpenTab calls invoke with screen details", async () => {
    mockInvoke.mockResolvedValue(true);
    const result = await emitOpenTab("editor", "My Tab", { key: "value" });
    expect(mockInvoke).toHaveBeenCalledWith("emit_open_tab", {
      screenId: "editor",
      title: "My Tab",
      context: { key: "value" },
      autoFocus: undefined,
      bringToFront: undefined,
    });
    expect(result).toBe(true);
  });

  // ── Phase 1: File/Folder dialogs ──────────────────────────────

  it("openFileDialog calls invoke", async () => {
    mockInvoke.mockResolvedValue({ cancelled: false, path: "C:\\test.txt", paths: null });
    const result = await openFileDialog("Pick file", ["txt"], false);
    expect(mockInvoke).toHaveBeenCalledWith("open_file_dialog", {
      title: "Pick file", filters: ["txt"], multiple: false,
    });
    expect(result.cancelled).toBe(false);
  });

  it("openFolderDialog calls invoke", async () => {
    mockInvoke.mockResolvedValue({ cancelled: false, path: "C:\\folder", paths: null });
    const result = await openFolderDialog("Pick folder");
    expect(mockInvoke).toHaveBeenCalledWith("open_folder_dialog", { title: "Pick folder" });
    expect(result.path).toBe("C:\\folder");
  });

  // ── Phase 2: Prompt system ────────────────────────────────────

  it("requestConfirm calls invoke", async () => {
    mockInvoke.mockResolvedValue(true);
    const result = await requestConfirm("r1", "Title", "Sure?");
    expect(mockInvoke).toHaveBeenCalledWith("request_confirm", {
      requestId: "r1", title: "Title", message: "Sure?", details: undefined,
    });
    expect(result).toBe(true);
  });

  it("submitConfirmResponse calls invoke", async () => {
    mockInvoke.mockResolvedValue(true);
    await submitConfirmResponse("r1", true);
    expect(mockInvoke).toHaveBeenCalledWith("submit_confirm_response", {
      requestId: "r1", confirmed: true,
    });
  });

  it("requestUserInput calls invoke", async () => {
    mockInvoke.mockResolvedValue({ requestId: "i1", cancelled: false, value: "hi", selectedValues: null });
    const result = await requestUserInput("i1", { key: "name" });
    expect(mockInvoke).toHaveBeenCalledWith("request_user_input", {
      requestId: "i1", request: { key: "name" }, context: undefined,
    });
    expect(result.value).toBe("hi");
  });

  it("submitUserInputResponse calls invoke", async () => {
    mockInvoke.mockResolvedValue(true);
    await submitUserInputResponse({ requestId: "i1", cancelled: false, value: "ok", selectedValues: null });
    expect(mockInvoke).toHaveBeenCalledWith("submit_user_input_response", {
      response: { requestId: "i1", cancelled: false, value: "ok", selectedValues: null },
    });
  });

  // ── Phase 3: Tab/Toast lifecycle ──────────────────────────────

  it("tabActionExecute calls invoke", async () => {
    mockInvoke.mockResolvedValue(true);
    await tabActionExecute("tab1", "save");
    expect(mockInvoke).toHaveBeenCalledWith("tab_action_execute", {
      tabId: "tab1", actionId: "save", context: undefined,
    });
  });

  it("tabClosed calls invoke", async () => {
    mockInvoke.mockResolvedValue(true);
    await tabClosed("tab1");
    expect(mockInvoke).toHaveBeenCalledWith("tab_closed", { tabId: "tab1" });
  });

  it("toastActionExecute calls invoke", async () => {
    mockInvoke.mockResolvedValue(true);
    await toastActionExecute("toast1", "ok");
    expect(mockInvoke).toHaveBeenCalledWith("toast_action_execute", {
      toastId: "toast1", actionId: "ok",
    });
  });

  it("toastClosed calls invoke", async () => {
    mockInvoke.mockResolvedValue(true);
    await toastClosed("toast1");
    expect(mockInvoke).toHaveBeenCalledWith("toast_closed", { toastId: "toast1" });
  });

  // ── Phase 4: Logging / Usage ──────────────────────────────────

  it("loggingTest calls invoke", async () => {
    mockInvoke.mockResolvedValue(true);
    await loggingTest();
    expect(mockInvoke).toHaveBeenCalledWith("logging_test");
  });

  it("usageTest calls invoke", async () => {
    mockInvoke.mockResolvedValue(true);
    await usageTest();
    expect(mockInvoke).toHaveBeenCalledWith("usage_test");
  });

  it("logClear calls invoke", async () => {
    mockInvoke.mockResolvedValue({ deletedCount: 2 });
    const result = await logClear("C:\\logs");
    expect(mockInvoke).toHaveBeenCalledWith("log_clear", { path: "C:\\logs" });
    expect(result.deletedCount).toBe(2);
  });

  // ── Phase 5: Exchange full ────────────────────────────────────

  it("exchangeTags calls invoke", async () => {
    mockInvoke.mockResolvedValue(["tag1", "tag2"]);
    const result = await exchangeTags();
    expect(mockInvoke).toHaveBeenCalledWith("exchange_tags");
    expect(result).toEqual(["tag1", "tag2"]);
  });

  it("exchangeDetails calls invoke", async () => {
    mockInvoke.mockResolvedValue({ handlerId: "p1", name: "Test", description: null, version: null, tags: [] });
    const result = await exchangeDetails("p1");
    expect(mockInvoke).toHaveBeenCalledWith("exchange_details", { handlerId: "p1" });
    expect(result?.handlerId).toBe("p1");
  });

  it("exchangeUpdate calls invoke", async () => {
    mockInvoke.mockRejectedValue({ kind: "general", message: "Exchange update not yet implemented" });
    await expect(exchangeUpdate("p1")).rejects.toBeDefined();
  });

  it("exchangePublish calls invoke", async () => {
    mockInvoke.mockRejectedValue({ kind: "general", message: "Exchange publish not yet implemented" });
    const pkg = { handlerId: "p1", name: "T", description: null, version: null, tags: [] as string[] };
    await expect(exchangePublish(pkg)).rejects.toBeDefined();
  });

  // ── Phase 6: AI Skills Hub ────────────────────────────────────

  it("aiSkillsHubList calls invoke", async () => {
    mockInvoke.mockResolvedValue({
      cursorSkillsRoot: null, copilotSkillsRoot: null, sources: [], skills: [], globalOnlySkills: [],
    });
    const result = await aiSkillsHubList();
    expect(mockInvoke).toHaveBeenCalledWith("ai_skills_hub_list");
    expect(result.skills).toEqual([]);
  });

  it("aiSkillsHubDeploy calls invoke", async () => {
    mockInvoke.mockResolvedValue({ ok: true, results: [] });
    const result = await aiSkillsHubDeploy([{ skillName: "s1", sourceId: "d", targets: ["cursor"] }]);
    expect(mockInvoke).toHaveBeenCalledWith("ai_skills_hub_deploy", {
      deployments: [{ skillName: "s1", sourceId: "d", targets: ["cursor"] }],
      customDestinationRoot: undefined,
    });
    expect(result.ok).toBe(true);
  });

  it("aiSkillsHubRemove calls invoke", async () => {
    mockInvoke.mockResolvedValue({ ok: true, results: [] });
    await aiSkillsHubRemove(["s1"], ["cursor"]);
    expect(mockInvoke).toHaveBeenCalledWith("ai_skills_hub_remove", {
      skillNames: ["s1"], targets: ["cursor"],
    });
  });

  it("aiSkillsHubPull calls invoke", async () => {
    mockInvoke.mockResolvedValue({ ok: true, results: [] });
    await aiSkillsHubPull(["s1"], "cursor", "default");
    expect(mockInvoke).toHaveBeenCalledWith("ai_skills_hub_pull", {
      skillNames: ["s1"], fromTarget: "cursor", toSourceId: "default",
    });
  });

  // ── Phase 7: Plugin list (WPF shape) ──────────────────────────

  it("pluginsListFull calls invoke", async () => {
    mockInvoke.mockResolvedValue({
      handlerTypes: ["regex"], actions: [], validators: [], contextProviders: [],
    });
    const result = await pluginsListFull();
    expect(mockInvoke).toHaveBeenCalledWith("plugins_list_full");
    expect(result.handlerTypes).toEqual(["regex"]);
  });
});
