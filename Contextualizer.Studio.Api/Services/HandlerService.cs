using System.Text.Json;
using Contextualizer.Studio.Api.Models;

namespace Contextualizer.Studio.Api.Services;

public interface IHandlerService
{
    Task<IEnumerable<Handler>> GetHandlersAsync();
    Task<Handler?> GetHandlerAsync(string id);
    Task<Handler> CreateHandlerAsync(Handler handler);
    Task<Handler> UpdateHandlerAsync(string id, Handler handler);
    Task DeleteHandlerAsync(string id);
    Task<Handler> UploadHandlerAsync(Stream fileStream);
    Task InstallHandlerAsync(string id);
}

public class HandlerService : IHandlerService
{
    private readonly string _handlerStoragePath;
    private readonly ILogger<HandlerService> _logger;

    public HandlerService(IConfiguration configuration, ILogger<HandlerService> logger)
    {
        _handlerStoragePath = configuration["HandlerStoragePath"] ?? "Handlers";
        _logger = logger;

        if (!Directory.Exists(_handlerStoragePath))
        {
            Directory.CreateDirectory(_handlerStoragePath);
        }
    }

    public async Task<IEnumerable<Handler>> GetHandlersAsync()
    {
        var handlers = new List<Handler>();
        var handlerDirs = Directory.GetDirectories(_handlerStoragePath);

        foreach (var dir in handlerDirs)
        {
            var metadataPath = Path.Combine(dir, "metadata.json");
            if (File.Exists(metadataPath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(metadataPath);
                    var handler = JsonSerializer.Deserialize<Handler>(json);
                    if (handler != null)
                    {
                        handlers.Add(handler);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading handler metadata from {Path}", metadataPath);
                }
            }
        }

        return handlers;
    }

    public async Task<Handler?> GetHandlerAsync(string id)
    {
        var metadataPath = Path.Combine(_handlerStoragePath, id, "metadata.json");
        if (!File.Exists(metadataPath))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(metadataPath);
            return JsonSerializer.Deserialize<Handler>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading handler metadata from {Path}", metadataPath);
            return null;
        }
    }

    public async Task<Handler> CreateHandlerAsync(Handler handler)
    {
        var handlerDir = Path.Combine(_handlerStoragePath, handler.Id);
        if (Directory.Exists(handlerDir))
        {
            throw new InvalidOperationException($"Handler with ID '{handler.Id}' already exists");
        }

        Directory.CreateDirectory(handlerDir);
        var metadataPath = Path.Combine(handlerDir, "metadata.json");
        var json = JsonSerializer.Serialize(handler, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(metadataPath, json);

        return handler;
    }

    public async Task<Handler> UpdateHandlerAsync(string id, Handler handler)
    {
        var handlerDir = Path.Combine(_handlerStoragePath, id);
        if (!Directory.Exists(handlerDir))
        {
            throw new InvalidOperationException($"Handler with ID '{id}' not found");
        }

        var metadataPath = Path.Combine(handlerDir, "metadata.json");
        var json = JsonSerializer.Serialize(handler, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(metadataPath, json);

        return handler;
    }

    public Task DeleteHandlerAsync(string id)
    {
        var handlerDir = Path.Combine(_handlerStoragePath, id);
        if (!Directory.Exists(handlerDir))
        {
            throw new InvalidOperationException($"Handler with ID '{id}' not found");
        }

        Directory.Delete(handlerDir, true);
        return Task.CompletedTask;
    }

    public async Task<Handler> UploadHandlerAsync(Stream fileStream)
    {
        using var reader = new StreamReader(fileStream);
        var json = await reader.ReadToEndAsync();
        var handler = JsonSerializer.Deserialize<Handler>(json);

        if (handler == null)
        {
            throw new InvalidOperationException("Invalid handler configuration");
        }

        return await CreateHandlerAsync(handler);
    }

    public async Task InstallHandlerAsync(string id)
    {
        var handler = await GetHandlerAsync(id);
        if (handler == null)
        {
            throw new InvalidOperationException($"Handler with ID '{id}' not found");
        }

        // TODO: Implement handler installation logic
        handler.Downloads++;
        await UpdateHandlerAsync(id, handler);
    }
} 