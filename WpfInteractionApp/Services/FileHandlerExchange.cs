using Contextualizer.Core;
using Contextualizer.Core.Services;
using Contextualizer.PluginContracts.Interfaces;
using Contextualizer.PluginContracts.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace WpfInteractionApp.Services
{
    public class FileHandlerExchange : IHandlerExchange
    {
        private readonly string _exchangePath;
        private readonly string _handlersFilePath;
        private readonly string _installedHandlersPath;
        private readonly JsonSerializerOptions _jsonOptions;

        public FileHandlerExchange()
        {
            ISettingsService settingsService = ServiceLocator.Get<SettingsService>();
            _exchangePath = settingsService.ExchangeDirectory;
            _handlersFilePath = settingsService.HandlersFilePath;
            _installedHandlersPath = Path.Combine(Path.GetDirectoryName(_handlersFilePath), "installed");
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            // Gerekli dizinleri oluştur
            Directory.CreateDirectory(_exchangePath);
            Directory.CreateDirectory(_installedHandlersPath);
        }

        public async Task<IEnumerable<HandlerPackage>> ListAvailableHandlersAsync(string searchTerm = null, string[] tags = null)
        {
            var handlers = new List<HandlerPackage>();
            var installedHandlers = await GetInstalledHandlersAsync();
            var files = Directory.GetFiles(_exchangePath, "*.json")
                               .Where(f => !f.Contains("installed"));

            foreach (var file in files)
            {
                try
                {
                    var content = await File.ReadAllTextAsync(file);
                    var package = JsonSerializer.Deserialize<HandlerPackage>(content, _jsonOptions);
                    
                    // Arama ve tag filtreleme
                    if (!string.IsNullOrEmpty(searchTerm) && 
                        !package.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) &&
                        !package.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (tags != null && tags.Any() && 
                        !tags.Any(t => package.Tags.Contains(t, StringComparer.OrdinalIgnoreCase)))
                        continue;

                    // Handler'ın yüklü olup olmadığını kontrol et
                    var installedHandler = installedHandlers.FirstOrDefault(h => h.Id == package.Id);
                    package.IsInstalled = installedHandler != null;

                    // Güncelleme kontrolü
                    if (package.IsInstalled && installedHandler != null)
                    {
                        try
                        {
                            var installedVersion = Version.Parse(installedHandler.Version);
                            var exchangeVersion = Version.Parse(package.Version);
                            package.HasUpdate = exchangeVersion > installedVersion;
                        }
                        catch
                        {
                            package.HasUpdate = false;
                        }
                    }
                    else
                    {
                        package.HasUpdate = false;
                    }

                    // Action'ları dependency olarak ekle
                    if (package.HandlerJson.TryGetProperty("actions", out var actions))
                    {
                        var actionDependencies = new List<string>();
                        foreach (var action in actions.EnumerateArray())
                        {
                            if (action.TryGetProperty("name", out var type))
                            {
                                actionDependencies.Add(type.GetString());
                            }
                        }
                        package.Dependencies = package.Dependencies?.Concat(actionDependencies).Distinct().ToArray() 
                            ?? actionDependencies.ToArray();
                    }

                    handlers.Add(package);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Handler yüklenirken hata oluştu: {ex.Message}");
                }
            }

            return handlers;
        }

        public async Task<HandlerPackage> GetHandlerDetailsAsync(string handlerId)
        {
            var filePath = Path.Combine(_exchangePath, $"{handlerId}.json");
            if (!File.Exists(filePath))
                return null;

            var content = await File.ReadAllTextAsync(filePath);
            var package = JsonSerializer.Deserialize<HandlerPackage>(content, _jsonOptions);
            package.IsInstalled = File.Exists(Path.Combine(_installedHandlersPath, $"{handlerId}.json"));
            return package;
        }

        public async Task<bool> InstallHandlerAsync(string handlerId)
        {
            var package = await GetHandlerDetailsAsync(handlerId);
            if (package == null) return false;

            // Handler'ı handlers.json'a ekle
            var currentHandlers = await File.ReadAllTextAsync(_handlersFilePath);
            using var handlersDoc = JsonDocument.Parse(currentHandlers);
            var handlersElement = handlersDoc.RootElement.GetProperty("handlers");
            var handlersArray = JsonSerializer.Deserialize<List<JsonElement>>(handlersElement, _jsonOptions);
            
            handlersArray.Add(package.HandlerJson);
            
            var updatedHandlers = new { handlers = handlersArray };
            await File.WriteAllTextAsync(_handlersFilePath, 
                JsonSerializer.Serialize(updatedHandlers, _jsonOptions));

            // Kurulum kaydını oluştur
            await File.WriteAllTextAsync(
                Path.Combine(_installedHandlersPath, $"{package.Id}.json"),
                JsonSerializer.Serialize(package, _jsonOptions)
            );

            return true;
        }

        public async Task<bool> UpdateHandlerAsync(string handlerId)
        {
            var exchangePackage = await GetHandlerDetailsAsync(handlerId);
            if (exchangePackage == null) return false;

            var installedPath = Path.Combine(_installedHandlersPath, $"{handlerId}.json");
            if (!File.Exists(installedPath)) return false;

            var installedJson = await File.ReadAllTextAsync(installedPath);
            var installedPackage = JsonSerializer.Deserialize<HandlerPackage>(installedJson, _jsonOptions);
            if (installedPackage == null) return false;

            try
            {
                var installedVersion = Version.Parse(installedPackage.Version);
                var exchangeVersion = Version.Parse(exchangePackage.Version);

                if (exchangeVersion <= installedVersion)
                {
                    return false;
                }

                // Önce eski handler'ı kaldır
                await RemoveHandlerAsync(handlerId);
                
                // Yeni versiyonu kur
                return await InstallHandlerAsync(handlerId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating handler {handlerId}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RemoveHandlerAsync(string handlerId)
        {
            var package = await GetHandlerDetailsAsync(handlerId);
            if (package == null) return false;

            // handlers.json'dan kaldır
            var currentHandlers = await File.ReadAllTextAsync(_handlersFilePath);
            using var handlersDoc = JsonDocument.Parse(currentHandlers);
            var handlersElement = handlersDoc.RootElement.GetProperty("handlers");
            var handlersArray = JsonSerializer.Deserialize<List<JsonElement>>(handlersElement, _jsonOptions);
            
            var handlerToRemove = handlersArray.FirstOrDefault(h => 
                h.GetProperty("name").GetString() == package.Name);
            
            if (handlerToRemove.ValueKind != JsonValueKind.Undefined)
                handlersArray.Remove(handlerToRemove);
            
            var updatedHandlers = new { handlers = handlersArray };
            await File.WriteAllTextAsync(_handlersFilePath, 
                JsonSerializer.Serialize(updatedHandlers, _jsonOptions));

            // Kurulum kaydını sil
            var installedPath = Path.Combine(_installedHandlersPath, $"{handlerId}.json");
            if (File.Exists(installedPath))
                File.Delete(installedPath);

            return true;
        }

        public async Task<bool> PublishHandlerAsync(HandlerPackage package)
        {
            if (!await ValidateHandlerAsync(package))
                return false;

            var filePath = Path.Combine(_exchangePath, $"{package.Id}.json");
            await File.WriteAllTextAsync(filePath, 
                JsonSerializer.Serialize(package, _jsonOptions));
            return true;
        }

        public async Task<bool> ValidateHandlerAsync(HandlerPackage package)
        {
            if (string.IsNullOrEmpty(package.Id) || 
                string.IsNullOrEmpty(package.Name) || 
                string.IsNullOrEmpty(package.Version) ||
                package.HandlerJson.ValueKind == JsonValueKind.Undefined)
                return false;

            // Handler JSON formatını kontrol et
            try
            {
                var handlerJson = package.HandlerJson;
                if (!handlerJson.TryGetProperty("name", out _) || 
                    !handlerJson.TryGetProperty("type", out _))
                    return false;
            }
            catch
            {
                return false;
            }

            return true;
        }

        public async Task<IEnumerable<string>> GetAvailableTagsAsync()
        {
            var handlers = await ListAvailableHandlersAsync();
            return handlers.SelectMany(h => h.Tags).Distinct();
        }

        public async Task<bool> CheckForUpdatesAsync()
        {
            var installedHandlers = Directory.GetFiles(_installedHandlersPath, "*.json")
                                           .Select(f => Path.GetFileNameWithoutExtension(f));

            foreach (var handlerId in installedHandlers)
            {
                var installed = await GetHandlerDetailsAsync(handlerId);
                if (installed == null) continue;

                var available = await GetHandlerDetailsAsync(handlerId);
                if (available == null) continue;

                if (available.Version != installed.Version)
                    return true;
            }

            return false;
        }

        public async Task<Dictionary<string, string>> GetHandlerMetadataAsync(string handlerId)
        {
            var package = await GetHandlerDetailsAsync(handlerId);
            return package?.Metadata ?? new Dictionary<string, string>();
        }

        public async Task<IEnumerable<HandlerPackage>> ListAvailableHandlersAsync()
        {
            var handlers = new List<HandlerPackage>();
            var installedHandlers = await GetInstalledHandlersAsync();

            Console.WriteLine($"Found {installedHandlers.Count()} installed handlers");

            foreach (var file in Directory.GetFiles(_exchangePath, "*.json"))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var handler = JsonSerializer.Deserialize<HandlerPackage>(json, _jsonOptions);

                    if (handler != null)
                    {
                        // Handler'ın yüklü olup olmadığını kontrol et
                        var installedHandler = installedHandlers.FirstOrDefault(h => h.Id == handler.Id);
                        handler.IsInstalled = installedHandler != null;

                        Console.WriteLine($"Handler: {handler.Id}, Installed: {handler.IsInstalled}");

                        // Güncelleme kontrolü
                        if (handler.IsInstalled && installedHandler != null)
                        {
                            try
                            {
                                var installedVersion = Version.Parse(installedHandler.Version);
                                var exchangeVersion = Version.Parse(handler.Version);
                                handler.HasUpdate = exchangeVersion > installedVersion;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Version comparison error for handler {handler.Id}: {ex.Message}");
                                handler.HasUpdate = false;
                            }
                        }
                        else
                        {
                            handler.HasUpdate = false;
                        }

                        handlers.Add(handler);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading handler {file}: {ex.Message}");
                }
            }

            return handlers;
        }

        private async Task<IEnumerable<HandlerPackage>> GetInstalledHandlersAsync()
        {
            var installedHandlers = new List<HandlerPackage>();
            var installedFiles = Directory.GetFiles(_installedHandlersPath, "*.json");

            foreach (var file in installedFiles)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var handler = JsonSerializer.Deserialize<HandlerPackage>(json, _jsonOptions);
                    if (handler != null)
                    {
                        installedHandlers.Add(handler);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Yüklü handler yüklenirken hata oluştu: {ex.Message}");
                }
            }

            return installedHandlers;
        }
    }
} 