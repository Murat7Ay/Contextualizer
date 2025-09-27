using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.Core
{
    public class FileHandler : Dispatch, IHandler
    {
        public static string TypeName => "File";
        
        // Constants for internationalization
        private const string FILE_NOT_FOUND = "File not found";
        private const string NOT_AVAILABLE = "N/A";

        protected override string OutputFormat => base.HandlerConfig.OutputFormat;

        public FileHandler(HandlerConfig handlerConfig) : base(handlerConfig)
        {
            
        }

        protected override async Task<bool> CanHandleAsync(ClipboardContent clipboardContent)
        {
            if (!clipboardContent.IsFile || !clipboardContent.Files.Any())
                return false;

            // Pre-validate all files before processing
            for (int i = 0; i < clipboardContent.Files.Length; i++)
            {
                try
                {
                    var fileProperties = GetFullFileInfoDictionary(clipboardContent.Files[i], i);
                    
                    // Check if file was not found
                    if (fileProperties.ContainsKey(nameof(FileInfoKeys.NotFound)))
                        return false;
                    
                    // Check extension
                    fileProperties.TryGetValue(FileInfoKeys.Extension + i, out var extension);
                    if (string.IsNullOrWhiteSpace(extension) || 
                        !base.HandlerConfig.FileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    // Log error and return false for any file processing issues
                    System.Diagnostics.Debug.WriteLine($"FileHandler: Error processing file {clipboardContent.Files[i]}: {ex.Message}");
                    return false;
                }
            }
            
            return true;
        }

        async Task<bool> IHandler.CanHandle(ClipboardContent clipboardContent)
        {
            return await CanHandleAsync(clipboardContent);
        }

        protected override async Task<Dictionary<string, string>> CreateContextAsync(ClipboardContent clipboardContent)
        {
            // Create context dictionary with proper capacity
            var context = new Dictionary<string, string>(clipboardContent.Files.Length * 25 + 1);

            try
            {
                // Process all files and build context
                for (int i = 0; i < clipboardContent.Files.Length; i++)
                {
                    var fileProperties = GetFullFileInfoDictionary(clipboardContent.Files[i], i);
                    
                    // Add all properties to context (performance optimized)
                    foreach (var kvp in fileProperties)
                    {
                        context[kvp.Key] = kvp.Value;
                    }
                }
                
                // Add file count
                context[ContextKey._count] = clipboardContent.Files.Length.ToString();
                
                return context;
            }
            catch (Exception ex)
            {
                // Log error and return empty context
                System.Diagnostics.Debug.WriteLine($"FileHandler: Error creating context: {ex.Message}");
                return new Dictionary<string, string> 
                { 
                    [ContextKey._error] = $"Error processing files: {ex.Message}" 
                };
            }
        }

        private static Dictionary<string, string> GetFullFileInfoDictionary(string filePath, int fileIndex)
        {
            // Initialize with proper capacity (we know we'll add ~25 properties)
            var fileInfoDictionary = new Dictionary<string, string>(25);

            if (!File.Exists(filePath))
            {
                fileInfoDictionary.Add(nameof(FileInfoKeys.NotFound), FILE_NOT_FOUND);
                return fileInfoDictionary;
            }

            try
            {
                var fileInfo = new FileInfo(filePath);
                var attributes = fileInfo.Attributes;

                // Basic file information
                fileInfoDictionary.Add(nameof(FileInfoKeys.FileName) + fileIndex, fileInfo.Name);
                fileInfoDictionary.Add(nameof(FileInfoKeys.FullPath) + fileIndex, fileInfo.FullName);
                fileInfoDictionary.Add(nameof(FileInfoKeys.Extension) + fileIndex, fileInfo.Extension);
                fileInfoDictionary.Add(nameof(FileInfoKeys.SizeBytes) + fileIndex, fileInfo.Length.ToString());
                
                // Date/time information
                fileInfoDictionary.Add(nameof(FileInfoKeys.CreationDate) + fileIndex, fileInfo.CreationTime.ToString());
                fileInfoDictionary.Add(nameof(FileInfoKeys.CreationDateUtc) + fileIndex, fileInfo.CreationTimeUtc.ToString());
                fileInfoDictionary.Add(nameof(FileInfoKeys.LastAccess) + fileIndex, fileInfo.LastAccessTime.ToString());
                fileInfoDictionary.Add(nameof(FileInfoKeys.LastAccessUtc) + fileIndex, fileInfo.LastAccessTimeUtc.ToString());
                fileInfoDictionary.Add(nameof(FileInfoKeys.LastWrite) + fileIndex, fileInfo.LastWriteTime.ToString());
                fileInfoDictionary.Add(nameof(FileInfoKeys.LastWriteUtc) + fileIndex, fileInfo.LastWriteTimeUtc.ToString());
                
                // File properties
                fileInfoDictionary.Add(nameof(FileInfoKeys.ReadOnly) + fileIndex, fileInfo.IsReadOnly.ToString());
                fileInfoDictionary.Add(nameof(FileInfoKeys.Exists) + fileIndex, fileInfo.Exists.ToString());
                
                // File attributes
                fileInfoDictionary.Add(nameof(FileInfoKeys.Hidden) + fileIndex, attributes.HasFlag(FileAttributes.Hidden).ToString());
                fileInfoDictionary.Add(nameof(FileInfoKeys.System) + fileIndex, attributes.HasFlag(FileAttributes.System).ToString());
                fileInfoDictionary.Add(nameof(FileInfoKeys.Archive) + fileIndex, attributes.HasFlag(FileAttributes.Archive).ToString());
                fileInfoDictionary.Add(nameof(FileInfoKeys.Compressed) + fileIndex, attributes.HasFlag(FileAttributes.Compressed).ToString());
                fileInfoDictionary.Add(nameof(FileInfoKeys.Temporary) + fileIndex, attributes.HasFlag(FileAttributes.Temporary).ToString());
                fileInfoDictionary.Add(nameof(FileInfoKeys.Offline) + fileIndex, attributes.HasFlag(FileAttributes.Offline).ToString());
                fileInfoDictionary.Add(nameof(FileInfoKeys.Encrypted) + fileIndex, attributes.HasFlag(FileAttributes.Encrypted).ToString());
                fileInfoDictionary.Add(nameof(FileInfoKeys.DirectoryOnly) + fileIndex, attributes.HasFlag(FileAttributes.Directory).ToString());
                fileInfoDictionary.Add(nameof(FileInfoKeys.ReparsePoint) + fileIndex, attributes.HasFlag(FileAttributes.ReparsePoint).ToString());
                fileInfoDictionary.Add(nameof(FileInfoKeys.Sparse) + fileIndex, attributes.HasFlag(FileAttributes.SparseFile).ToString());
                fileInfoDictionary.Add(nameof(FileInfoKeys.Device) + fileIndex, attributes.HasFlag(FileAttributes.Device).ToString());
                fileInfoDictionary.Add(nameof(FileInfoKeys.Normal) + fileIndex, attributes.HasFlag(FileAttributes.Normal).ToString());
                
                // Directory information
                fileInfoDictionary.Add(nameof(FileInfoKeys.DirectoryPath) + fileIndex, fileInfo.DirectoryName ?? NOT_AVAILABLE);
                fileInfoDictionary.Add(nameof(FileInfoKeys.DirectoryObject) + fileIndex, fileInfo.Directory?.FullName ?? NOT_AVAILABLE);

                return fileInfoDictionary;
            }
            catch (Exception ex)
            {
                // Return error information if file processing fails
                return new Dictionary<string, string>(2)
                {
                    [nameof(FileInfoKeys.NotFound)] = FILE_NOT_FOUND,
                    [ContextKey._error] = $"Error accessing file: {ex.Message}"
                };
            }
        }

        protected override List<ConfigAction> GetActions()
        {
            return base.HandlerConfig.Actions;
        }
    }
}
