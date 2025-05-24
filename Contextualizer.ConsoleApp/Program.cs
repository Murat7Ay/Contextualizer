using Contextualizer.ConsoleApp;
using Contextualizer.Core;
using Contextualizer.PluginContracts;

IUserInteractionService userInteractionService = new ConsoleUserInteraction();

using var listener = new HandlerManager(userInteractionService, @"C:\Finder\handlers.json");
await listener.StartAsync();

System.Console.ReadKey();
