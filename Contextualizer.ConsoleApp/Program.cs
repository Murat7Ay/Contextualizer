using Contextualizer.ConsoleApp;
using Contextualizer.Core;
using Contextualizer.Core.Services;
using Contextualizer.PluginContracts;

IUserInteractionService userInteractionService = new ConsoleUserInteraction();
ISettingsService settingsService = new ConsoleSettingsService();

using var listener = new HandlerManager(userInteractionService, settingsService);
await listener.StartAsync();

System.Console.ReadKey();
