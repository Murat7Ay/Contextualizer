using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Contextualizer.Core;
using Contextualizer.Core.Services;
using Contextualizer.PluginContracts;
using WpfInteractionApp.Services.Mcp.McpModels;

namespace WpfInteractionApp.Services.Mcp.McpToolHandlers
{
    internal static class UiToolHandler
    {
        private static IUserInteractionService GetUserInteractionService()
        {
            var settings = ServiceLocator.SafeGet<SettingsService>();
            var useNative = settings?.Settings.McpSettings?.UseNativeUi ?? true;

            if (useNative)
            {
                return ServiceLocator.SafeGet<NativeUserInteractionService>()
                    ?? ServiceLocator.SafeGet<IUserInteractionService>()
                    ?? throw new InvalidOperationException("No user interaction service available");
            }

            return ServiceLocator.SafeGet<WebViewUserInteractionService>()
                ?? ServiceLocator.SafeGet<IUserInteractionService>()
                ?? throw new InvalidOperationException("No user interaction service available");
        }

        private static bool PromptUserInputsWithNavigation(IUserInteractionService ui, List<UserInputRequest> userInputs, Dictionary<string, string> context)
        {
            if (userInputs == null || userInputs.Count == 0)
                return true;

            int currentIndex = 0;

            while (currentIndex < userInputs.Count)
            {
                var input = userInputs[currentIndex];

                if (string.IsNullOrWhiteSpace(input?.Key))
                {
                    currentIndex++;
                    continue;
                }

                if (!string.IsNullOrEmpty(input.DependentKey) &&
                    context.TryGetValue(input.DependentKey, out var dependentValue) &&
                    input.DependentSelectionItemMap?.TryGetValue(dependentValue, out var dependentSelection) == true)
                {
                    input.SelectionItems = dependentSelection.SelectionItems;
                    input.DefaultValue = dependentSelection.DefaultValue;
                }

                var result = ui.GetUserInputWithNavigation(input, context, currentIndex > 0, currentIndex, userInputs.Count);

                switch (result.Action)
                {
                    case NavigationAction.Next:
                        if (!string.IsNullOrWhiteSpace(result.Value))
                        {
                            context[input.Key] = result.Value;
                        }
                        currentIndex++;
                        break;

                    case NavigationAction.Back:
                        if (currentIndex > 0)
                        {
                            currentIndex--;
                            context.Remove(input.Key);
                        }
                        break;

                    case NavigationAction.Cancel:
                        return false;
                }
            }

            return true;
        }

        public static async Task<JsonRpcResponse> HandleUiConfirmAsync(JsonRpcRequest request, McpToolsCallParams callParams, JsonSerializerOptions jsonOptions)
        {
            IUserInteractionService ui;
            try
            {
                ui = GetUserInteractionService();
            }
            catch
            {
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError { Code = -32002, Message = "IUserInteractionService is not available" }
                };
            }

            UiConfirmArgs? args = null;
            try
            {
                if (callParams.Arguments.HasValue)
                    args = callParams.Arguments.Value.Deserialize<UiConfirmArgs>(jsonOptions);
            }
            catch (JsonException)
            {
                // ignore, handled below
            }

            string title = args?.Title ?? "Confirmation";
            string message = args?.Message ?? string.Empty;

            if (string.IsNullOrWhiteSpace(message))
            {
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError { Code = -32602, Message = "ui_confirm requires arguments.message" }
                };
            }

            var confirmRequest = new ConfirmationRequest
            {
                Title = title,
                Message = message,
                Details = args?.Details
            };

            bool confirmed = await ui.ShowConfirmationAsync(confirmRequest);
            var payload = new Dictionary<string, object> { ["confirmed"] = confirmed };
            var text = JsonSerializer.Serialize(payload, jsonOptions);

            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = new McpToolsCallResult
                {
                    Content = new List<McpContentItem> { new McpContentItem { Type = "text", Text = text } },
                    IsError = false
                }
            };
        }

        public static async Task<JsonRpcResponse> HandleUiUserInputsAsync(JsonRpcRequest request, McpToolsCallParams callParams, JsonSerializerOptions jsonOptions)
        {
            IUserInteractionService ui;
            try
            {
                ui = GetUserInteractionService();
            }
            catch
            {
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError { Code = -32002, Message = "IUserInteractionService is not available" }
                };
            }

            UiUserInputsArgs? args = null;
            try
            {
                if (callParams.Arguments.HasValue)
                    args = callParams.Arguments.Value.Deserialize<UiUserInputsArgs>(jsonOptions);
            }
            catch (JsonException)
            {
                // ignore, handled below
            }

            if (args == null || args.UserInputs == null || args.UserInputs.Count == 0)
            {
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError { Code = -32602, Message = "ui_user_inputs requires arguments.user_inputs (non-empty array)" }
                };
            }

            var context = args.Context ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            bool completed = PromptUserInputsWithNavigation(ui, args.UserInputs, context);

            var payload = new Dictionary<string, object>
            {
                ["cancelled"] = !completed,
                ["values"] = context
            };

            var text = JsonSerializer.Serialize(payload, jsonOptions);

            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = new McpToolsCallResult
                {
                    Content = new List<McpContentItem> { new McpContentItem { Type = "text", Text = text } },
                    IsError = false
                }
            };
        }

        public static async Task<JsonRpcResponse> HandleUiNotifyAsync(JsonRpcRequest request, McpToolsCallParams callParams, JsonSerializerOptions jsonOptions)
        {
            IUserInteractionService ui;
            try
            {
                ui = GetUserInteractionService();
            }
            catch
            {
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError { Code = -32002, Message = "IUserInteractionService is not available" }
                };
            }

            string title = string.Empty;
            string message = string.Empty;
            string level = "info";
            int durationSeconds = 5;

            if (callParams.Arguments.HasValue && callParams.Arguments.Value.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in callParams.Arguments.Value.EnumerateObject())
                {
                    if (prop.NameEquals("title"))
                        title = prop.Value.GetString() ?? string.Empty;
                    else if (prop.NameEquals("message"))
                        message = prop.Value.GetString() ?? string.Empty;
                    else if (prop.NameEquals("level"))
                        level = prop.Value.GetString() ?? level;
                    else if (prop.NameEquals("durationSeconds") || prop.NameEquals("duration_seconds"))
                    {
                        if (prop.Value.ValueKind == JsonValueKind.Number && prop.Value.TryGetInt32(out var v))
                            durationSeconds = v;
                        else if (int.TryParse(prop.Value.ToString(), out var parsed))
                            durationSeconds = parsed;
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError { Code = -32602, Message = "ui_notify requires arguments.message" }
                };
            }

            if (durationSeconds < 1) durationSeconds = 1;
            if (durationSeconds > 600) durationSeconds = 600;

            var logType = level.Trim().ToLowerInvariant() switch
            {
                "success" => LogType.Success,
                "warn" => LogType.Warning,
                "warning" => LogType.Warning,
                "error" => LogType.Error,
                "critical" => LogType.Critical,
                "debug" => LogType.Debug,
                _ => LogType.Info
            };

            ui.ShowNotification(message, logType, title, durationSeconds, null);

            var payload = new Dictionary<string, object> { ["ok"] = true };
            var text = JsonSerializer.Serialize(payload, jsonOptions);

            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = new McpToolsCallResult
                {
                    Content = new List<McpContentItem> { new McpContentItem { Type = "text", Text = text } },
                    IsError = false
                }
            };
        }

        public static async Task<JsonRpcResponse> HandleUiShowMarkdownAsync(JsonRpcRequest request, McpToolsCallParams callParams, JsonSerializerOptions jsonOptions)
        {
            var ui = ServiceLocator.SafeGet<WebViewUserInteractionService>()
                ?? ServiceLocator.SafeGet<IUserInteractionService>();

            if (ui == null)
            {
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError { Code = -32002, Message = "IUserInteractionService is not available" }
                };
            }

            string title = "Markdown";
            string markdown = string.Empty;
            bool autoFocus = false;
            bool bringToFront = false;

            if (callParams.Arguments.HasValue && callParams.Arguments.Value.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in callParams.Arguments.Value.EnumerateObject())
                {
                    if (prop.NameEquals("title"))
                        title = prop.Value.GetString() ?? title;
                    else if (prop.NameEquals("markdown"))
                        markdown = prop.Value.GetString() ?? string.Empty;
                    else if (prop.NameEquals("autoFocus") || prop.NameEquals("auto_focus"))
                        autoFocus = prop.Value.ValueKind == JsonValueKind.True || (prop.Value.ValueKind == JsonValueKind.String && bool.TryParse(prop.Value.GetString(), out var b1) && b1);
                    else if (prop.NameEquals("bringToFront") || prop.NameEquals("bring_to_front"))
                        bringToFront = prop.Value.ValueKind == JsonValueKind.True || (prop.Value.ValueKind == JsonValueKind.String && bool.TryParse(prop.Value.GetString(), out var b2) && b2);
                }
            }

            if (string.IsNullOrWhiteSpace(markdown))
            {
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError { Code = -32602, Message = "ui_show_markdown requires arguments.markdown" }
                };
            }

            var context = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [ContextKey._body] = markdown
            };

            ui.ShowWindow("markdown2", title, context, null, autoFocus, bringToFront);

            var payload = new Dictionary<string, object> { ["shown"] = true };
            var text = JsonSerializer.Serialize(payload, jsonOptions);

            return new JsonRpcResponse
            {
                Id = request.Id,
                Result = new McpToolsCallResult
                {
                    Content = new List<McpContentItem> { new McpContentItem { Type = "text", Text = text } },
                    IsError = false
                }
            };
        }
    }
}
