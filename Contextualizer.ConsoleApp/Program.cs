using Contextualizer.ConsoleApp;
using Contextualizer.Core;

using var listener = new HandlerManager(new ConsoleUserInteraction() , @"C:\Finder\handlers.json");
await listener.StartAsync();

System.Console.ReadKey();
