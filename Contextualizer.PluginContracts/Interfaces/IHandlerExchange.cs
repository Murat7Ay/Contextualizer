using System.Collections.Generic;
using System.Threading.Tasks;
using Contextualizer.PluginContracts.Models;

namespace Contextualizer.PluginContracts.Interfaces
{
    public interface IHandlerExchange
    {
        Task<IEnumerable<HandlerPackage>> ListAvailableHandlersAsync(string searchTerm = null, string[] tags = null);
        Task<HandlerPackage> GetHandlerDetailsAsync(string handlerId);
        Task<bool> InstallHandlerAsync(string handlerId);
        Task<bool> UpdateHandlerAsync(string handlerId);
        Task<bool> RemoveHandlerAsync(string handlerId);
        Task<bool> PublishHandlerAsync(HandlerPackage package);
        Task<bool> ValidateHandlerAsync(HandlerPackage package);
        Task<IEnumerable<string>> GetAvailableTagsAsync();
        Task<bool> CheckForUpdatesAsync();
        Task<Dictionary<string, string>> GetHandlerMetadataAsync(string handlerId);
    }
} 