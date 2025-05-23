using System.Text.Json;

namespace Contextualizer.Studio.Api.Models;

public class Handler
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public JsonDocument Config { get; set; } = JsonDocument.Parse("{}");
    public int Downloads { get; set; }
    public double Rating { get; set; }
} 