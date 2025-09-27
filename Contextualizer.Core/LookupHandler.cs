using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.Core
{
    public class LookupHandler : Dispatch, IHandler
    {
        private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> _data;
        public static string TypeName => "Lookup";
        
        // Constants for error messages
        private const string FILE_LOAD_ERROR = "Failed to load lookup data";
        private const string INVALID_LINE_FORMAT = "Invalid line format in lookup file";
        
        public LookupHandler(HandlerConfig handlerConfig) : base(handlerConfig)
        {
            _data = LoadData();
        }

        private IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> LoadData()
        {
            var data = new Dictionary<string, Dictionary<string, string>>();
            var processedLines = 0;
            var skippedLines = 0;
            
            try
            {
                // Resolve config patterns in path
                var resolvedPath = HandlerContextProcessor.ReplaceDynamicValues(
                    base.HandlerConfig.Path, 
                    new Dictionary<string, string>() // Empty context for config-only resolution
                );

                // Validate file exists before processing
                if (!File.Exists(resolvedPath))
                {
                    var errorMsg = $"Lookup file not found: {resolvedPath}";
                    UserFeedback.ShowError($"LookupHandler '{HandlerConfig.Name}': {errorMsg}");
                    return new ReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>(
                        new Dictionary<string, IReadOnlyDictionary<string, string>>());
                }

                using var reader = new StreamReader(resolvedPath);
                string? line;
                var lineNumber = 0;
                
                while ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;
                    
                    // Skip empty lines and comments
                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                    {
                        continue;
                    }
                    
                    try
                    {
                        if (ProcessLine(line, data))
                        {
                            processedLines++;
                        }
                        else
                        {
                            skippedLines++;
                            System.Diagnostics.Debug.WriteLine($"LookupHandler: Skipped invalid line {lineNumber}: {line}");
                        }
                    }
                    catch (Exception lineEx)
                    {
                        skippedLines++;
                        System.Diagnostics.Debug.WriteLine($"LookupHandler: Error processing line {lineNumber}: {lineEx.Message}");
                    }
                }
                
                UserFeedback.ShowActivity(LogType.Info, 
                    $"LookupHandler '{HandlerConfig.Name}': Loaded {processedLines} entries, skipped {skippedLines} lines from {resolvedPath}");
            }
            catch (UnauthorizedAccessException ex)
            {
                UserFeedback.ShowError($"LookupHandler '{HandlerConfig.Name}': Access denied to file {HandlerConfig.Path}: {ex.Message}");
            }
            catch (DirectoryNotFoundException ex)
            {
                UserFeedback.ShowError($"LookupHandler '{HandlerConfig.Name}': Directory not found for {HandlerConfig.Path}: {ex.Message}");
            }
            catch (FileNotFoundException ex)
            {
                UserFeedback.ShowError($"LookupHandler '{HandlerConfig.Name}': File not found {HandlerConfig.Path}: {ex.Message}");
            }
            catch (IOException ex)
            {
                UserFeedback.ShowError($"LookupHandler '{HandlerConfig.Name}': I/O error reading {HandlerConfig.Path}: {ex.Message}");
            }
            catch (Exception ex)
            {
                UserFeedback.ShowError($"LookupHandler '{HandlerConfig.Name}': {FILE_LOAD_ERROR} from {HandlerConfig.Path}: {ex.Message}");
            }

            // Convert to readonly collections for thread safety
            return new ReadOnlyDictionary<string, IReadOnlyDictionary<string, string>>(
                data.ToDictionary(
                    kvp => kvp.Key, 
                    kvp => (IReadOnlyDictionary<string, string>)new ReadOnlyDictionary<string, string>(kvp.Value)
                )
            );
        }
        
        private bool ProcessLine(string line, Dictionary<string, Dictionary<string, string>> data)
        {
            var parts = line.Split(new[] { base.HandlerConfig.Delimiter }, StringSplitOptions.None);
            if (parts.Length != base.HandlerConfig.ValueNames.Count)
            {
                return false; // Invalid line format
            }

            // Process newline replacements
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = parts[i].Replace("{{NEWLINE}}", Environment.NewLine);
            }

            // Create values dictionary
            var values = new Dictionary<string, string>(base.HandlerConfig.ValueNames.Count);
            for (int i = 0; i < base.HandlerConfig.ValueNames.Count; i++)
            {
                values[base.HandlerConfig.ValueNames[i]] = parts[i];
            }

            // Add entries for each key name
            foreach (var keyName in base.HandlerConfig.KeyNames.Where(values.ContainsKey))
            {
                var keyValue = values[keyName];
                if (!string.IsNullOrEmpty(keyValue))
                {
                    data[keyValue] = values;
                }
            }

            return true;
        }

        protected override async Task<bool> CanHandleAsync(ClipboardContent clipboardContent)
        {
            if (clipboardContent?.Text == null)
                return false;
                
            return _data.ContainsKey(clipboardContent.Text);
        }

        async Task<bool> IHandler.CanHandle(ClipboardContent clipboardContent)
        {
            return await CanHandleAsync(clipboardContent);
        }
        
        public HandlerConfig HandlerConfig => base.HandlerConfig;
        protected override string OutputFormat => base.HandlerConfig.OutputFormat;

        protected override async Task<Dictionary<string, string>> CreateContextAsync(ClipboardContent clipboardContent)
        {
            if (clipboardContent?.Text == null)
            {
                return new Dictionary<string, string>
                {
                    [ContextKey._error] = "Invalid clipboard content"
                };
            }
            
            string input = clipboardContent.Text;
            
            if (!_data.TryGetValue(input, out var lookupData))
            {
                return new Dictionary<string, string>
                {
                    [ContextKey._input] = input,
                    [ContextKey._error] = $"Lookup key not found: {input}"
                };
            }
            
            // Create a mutable copy of the readonly dictionary
            var context = new Dictionary<string, string>(lookupData);
            context[ContextKey._input] = input;
            
            return context;
        }

        protected override List<ConfigAction> GetActions()
        {
            return base.HandlerConfig.Actions;
        }
    }
}
