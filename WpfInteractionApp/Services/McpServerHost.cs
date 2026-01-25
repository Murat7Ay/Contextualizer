using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WpfInteractionApp.Services.Mcp;
using WpfInteractionApp.Services.Mcp.McpModels;

namespace WpfInteractionApp.Services
{
    public sealed class McpServerHost : IAsyncDisposable
    {
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private WebApplication? _app;
        private Task? _runTask;
        private CancellationTokenSource? _cts;

        public bool IsRunning => _app != null;

        public int Port { get; private set; }

        public async Task StartAsync(int port, CancellationToken cancellationToken = default)
        {
            if (_app != null)
                throw new InvalidOperationException("MCP server is already running.");

            Port = port;
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                ApplicationName = typeof(McpServerHost).Assembly.FullName,
            });

            builder.WebHost.UseKestrel();
            builder.WebHost.UseUrls($"http://127.0.0.1:{port}");

            var app = builder.Build();

            // Health endpoint
            app.MapGet("/mcp/health", () => Results.Json(new { ok = true, service = "contextualizer-mcp" }));

            // Streamable HTTP endpoint (single POST returns JSON or SSE stream)
            app.MapPost("/mcp", async (HttpContext httpContext) =>
            {
                string body;
                using (var reader = new StreamReader(httpContext.Request.Body, Encoding.UTF8))
                {
                    body = await reader.ReadToEndAsync(httpContext.RequestAborted);
                }

                if (string.IsNullOrWhiteSpace(body))
                {
                    httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await httpContext.Response.WriteAsync("Empty request body", httpContext.RequestAborted);
                    return;
                }

                JsonRpcRequest? request;
                try
                {
                    request = JsonSerializer.Deserialize<JsonRpcRequest>(body, _jsonOptions);
                }
                catch (JsonException)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await httpContext.Response.WriteAsync("Invalid JSON", httpContext.RequestAborted);
                    return;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.Method))
                {
                    httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await httpContext.Response.WriteAsync("Invalid JSON-RPC request", httpContext.RequestAborted);
                    return;
                }

                var response = await McpJsonRpcHandler.HandleAsync(request, _jsonOptions);
                if (response == null)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status204NoContent;
                    return;
                }

                var json = JsonSerializer.Serialize(response, _jsonOptions);
                var accept = httpContext.Request.Headers.Accept.ToString();
                if (!string.IsNullOrWhiteSpace(accept) &&
                    accept.Contains("text/event-stream", StringComparison.OrdinalIgnoreCase))
                {
                    httpContext.Response.Headers.CacheControl = "no-cache";
                    httpContext.Response.Headers.Connection = "keep-alive";
                    httpContext.Response.ContentType = "text/event-stream";
                    await WriteSseEventAsync(httpContext, "message", json, httpContext.RequestAborted);
                    return;
                }

                httpContext.Response.ContentType = "application/json";
                httpContext.Response.StatusCode = StatusCodes.Status200OK;
                await httpContext.Response.WriteAsync(json, httpContext.RequestAborted);
            });

            _app = app;
            await app.StartAsync(_cts.Token);
            
            // Wait for shutdown signal
            var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
            _runTask = Task.Run(async () =>
            {
                var tcs = new TaskCompletionSource();
                lifetime.ApplicationStopping.Register(() => tcs.SetResult());
                await tcs.Task;
            }, _cts.Token);
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            if (_app == null)
                return;

            try
            {
                _cts?.Cancel();
            }
            catch { /* ignore */ }

            try
            {
                await _app.StopAsync(cancellationToken);
            }
            catch { /* ignore */ }

            try
            {
                await _app.DisposeAsync();
            }
            catch { /* ignore */ }

            _app = null;
            _cts?.Dispose();
            _cts = null;

            if (_runTask != null)
            {
                try { await _runTask; } catch { /* ignore */ }
                _runTask = null;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync();
        }

        private static async Task WriteSseEventAsync(HttpContext context, string eventName, string data, CancellationToken cancellationToken)
        {
            await context.Response.WriteAsync($"event: {eventName}\n", cancellationToken);
            await context.Response.WriteAsync($"data: {data}\n\n", cancellationToken);
            await context.Response.Body.FlushAsync(cancellationToken);
        }
    }
}
