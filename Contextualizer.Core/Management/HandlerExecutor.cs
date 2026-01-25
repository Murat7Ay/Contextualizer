using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Contextualizer.PluginContracts;

namespace Contextualizer.Core.Management
{
    internal static class HandlerExecutor
    {
        public static async Task<bool> ExecuteHandlerAsync(
            IHandler handler,
            ClipboardContent clipboardContent,
            ILoggingService? logger,
            int contentLength)
        {
            using (logger?.BeginScope("HandlerExecution", new Dictionary<string, object>
            {
                ["handler_name"] = handler.HandlerConfig.Name,
                ["handler_type"] = handler.GetType().Name
            }))
            {
                var stopwatch = Stopwatch.StartNew();
                try
                {
                    bool wasProcessed = await handler.Execute(clipboardContent);
                    stopwatch.Stop();

                    return wasProcessed;
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();

                    UserFeedback.ShowError($"Handler {handler.GetType().Name} failed: {ex.Message}");

                    logger?.LogHandlerError(
                        handler.HandlerConfig.Name,
                        handler.GetType().Name,
                        ex,
                        new Dictionary<string, object>
                        {
                            ["content_length"] = contentLength,
                            ["execution_time_ms"] = stopwatch.ElapsedMilliseconds
                        });

                    return false;
                }
            }
        }

        public static async Task ExecuteManualHandlerAsync(
            IHandler handler,
            string handlerName)
        {
            if (handler == null)
            {
                UserFeedback.ShowWarning($"Handler not found: {handlerName}");
                return;
            }

            if (!handler.HandlerConfig.Enabled)
            {
                UserFeedback.ShowWarning($"Handler is disabled: {handlerName}");
                return;
            }

            try
            {
                if (handler is ISyntheticContent syntheticHandler)
                {
                    var clipboardContent = syntheticHandler.CreateSyntheticContent(handler.HandlerConfig.SyntheticInput);

                    if (!clipboardContent.Success)
                    {
                        UserFeedback.ShowError("Failed to create synthetic content");
                        return;
                    }

                    UserFeedback.ShowActivity(LogType.Info, $"Executing synthetic handler: {handler.HandlerConfig.Name}");
                    await handler.Execute(clipboardContent);
                    return;
                }

                var context = new ClipboardContent { Text = "", IsText = true };
                await handler.Execute(context);
            }
            catch (Exception ex)
            {
                var logger = ServiceLocator.SafeGet<ILoggingService>();
                logger?.LogError($"Failed to execute manual handler '{handlerName}': {ex.Message}");
                UserFeedback.ShowError($"Error executing handler '{handlerName}': {ex.Message}");
            }
        }

        public static async Task<string> ExecuteHandlerConfig(HandlerConfig handlerConfig)
        {
            IHandler? handler = null;
            try
            {
                handler = HandlerFactory.Create(handlerConfig);
                if (handler == null)
                {
                    var error = $"Failed to create handler of type: {handlerConfig.Type}";
                    UserFeedback.ShowError(error);
                    return error;
                }

                ClipboardContent clipboardContent;

                if (handler is ISyntheticContent syntheticHandler && handlerConfig.SyntheticInput != null)
                {
                    clipboardContent = syntheticHandler.CreateSyntheticContent(handlerConfig.SyntheticInput);
                    if (!clipboardContent.Success)
                    {
                        var error = "Failed to create synthetic content for cron job";
                        UserFeedback.ShowError(error);
                        return error;
                    }
                }
                else
                {
                    clipboardContent = new ClipboardContent
                    {
                        Success = true,
                        IsText = true,
                        Text = $"Cron trigger: {handlerConfig.Name} at {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
                    };
                }

                await Task.Run(() => handler.Execute(clipboardContent));

                var result = $"Successfully executed cron job: {handlerConfig.Name}";
                UserFeedback.ShowSuccess(result);
                return result;
            }
            catch (Exception ex)
            {
                var error = $"Error executing cron job {handlerConfig.Name}: {ex.Message}";
                UserFeedback.ShowError(error);
                return error;
            }
            finally
            {
                if (handler is IDisposable disposableHandler)
                {
                    disposableHandler.Dispose();
                }
            }
        }
    }
}
