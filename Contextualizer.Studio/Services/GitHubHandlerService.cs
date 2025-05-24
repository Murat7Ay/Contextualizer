using Octokit;
using System.Text.Json;

namespace Contextualizer.Studio.Services;

public interface IGitHubHandlerService
{
    Task<IReadOnlyList<HandlerInfo>> GetOfficialHandlersAsync();
    Task<IReadOnlyList<HandlerInfo>> GetCommunityHandlersAsync();
    Task<HandlerInfo> GetHandlerDetailsAsync(string owner, string name);
    Task CreateHandlerAsync(HandlerInfo handler, string content);
    Task UpdateHandlerAsync(string owner, string name, HandlerInfo handler, string content);
}

public class GitHubHandlerService : IGitHubHandlerService
{
    private readonly GitHubClient _github;
    private const string OfficialHandlersPath = "official";
    private const string CommunityHandlersPath = "community";
    private const string DefaultBranch = "main";
    private const string DefaultCommitMessage = "Update handler via Contextualizer Studio";

    public GitHubHandlerService(IConfiguration configuration)
    {
        var token = configuration["GitHub:Token"];
        _github = new GitHubClient(new ProductHeaderValue("Contextualizer"))
        {
            Credentials = new Credentials(token)
        };
    }

    public async Task<IReadOnlyList<HandlerInfo>> GetOfficialHandlersAsync()
    {
        return await GetHandlersFromPathAsync(OfficialHandlersPath);
    }

    public async Task<IReadOnlyList<HandlerInfo>> GetCommunityHandlersAsync()
    {
        return await GetHandlersFromPathAsync(CommunityHandlersPath);
    }

    public async Task<HandlerInfo> GetHandlerDetailsAsync(string owner, string name)
    {
        try
        {
            var repo = await _github.Repository.Get(owner, name);
            var metadataContent = await _github.Repository.Content.GetAllContents(owner, name, "metadata.json");
            var metadata = JsonSerializer.Deserialize<HandlerInfo>(metadataContent[0].Content);
            return metadata;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error getting handler details: {ex.Message}");
        }
    }

    public async Task CreateHandlerAsync(HandlerInfo handler, string content)
    {
        try
        {
            var files = new Dictionary<string, string>
            {
                { "handler.json", content },
                { "metadata.json", JsonSerializer.Serialize(handler, new JsonSerializerOptions { WriteIndented = true }) },
                { "README.md", GenerateReadme(handler) }
            };

            foreach (var file in files)
            {
                await _github.Repository.Content.CreateFile(
                    handler.Owner,
                    handler.Name,
                    file.Key,
                    new CreateFileRequest(DefaultCommitMessage, file.Value, DefaultBranch)
                );
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error creating handler: {ex.Message}");
        }
    }

    public async Task UpdateHandlerAsync(string owner, string name, HandlerInfo handler, string content)
    {
        try
        {
            var files = new Dictionary<string, string>
            {
                { "handler.json", content },
                { "metadata.json", JsonSerializer.Serialize(handler, new JsonSerializerOptions { WriteIndented = true }) }
            };

            foreach (var file in files)
            {
                var existingFile = await _github.Repository.Content.GetAllContents(owner, name, file.Key);
                await _github.Repository.Content.UpdateFile(
                    owner,
                    name,
                    file.Key,
                    new UpdateFileRequest(DefaultCommitMessage, file.Value, existingFile[0].Sha, DefaultBranch)
                );
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Error updating handler: {ex.Message}");
        }
    }

    private async Task<IReadOnlyList<HandlerInfo>> GetHandlersFromPathAsync(string path)
    {
        try
        {
            var handlers = new List<HandlerInfo>();
            var contents = await _github.Repository.Content.GetAllContents("Murat7Ay", "contextualizer-handlers", path);
            
            foreach (var content in contents)
            {
                if (content.Type == ContentType.Dir)
                {
                    var metadataContent = await _github.Repository.Content.GetAllContents(
                        "Murat7Ay",
                        "contextualizer-handlers",
                        $"{path}/{content.Name}/metadata.json"
                    );
                    var metadata = JsonSerializer.Deserialize<HandlerInfo>(metadataContent[0].Content);
                    handlers.Add(metadata);
                }
            }

            return handlers;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error getting handlers from path {path}: {ex.Message}");
        }
    }

    private string GenerateReadme(HandlerInfo handler)
    {
        return $@"# {handler.Name}

{handler.Description}

## Version
{handler.Version}

## Author
{handler.Author}

## Tags
{string.Join(", ", handler.Tags)}

## Created
{handler.CreatedAt:yyyy-MM-dd}

## Dependencies
{string.Join(", ", handler.Dependencies)}
";
    }
}

public class HandlerInfo
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Owner { get; set; }
    public string Version { get; set; }
    public string Author { get; set; }
    public List<string> Tags { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Compatibility { get; set; }
    public string Description { get; set; }
    public List<string> Dependencies { get; set; }
} 