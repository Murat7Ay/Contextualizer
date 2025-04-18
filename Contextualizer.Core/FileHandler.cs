using Contextualizer.PluginContracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contextualizer.Core
{
    public class FileHandler : Dispatch, IHandler
    {
        private Dictionary<string, string> fileInfo;
        public static string TypeName => "File";

        protected override string OutputFormat => base.HandlerConfig.OutputFormat;

        public FileHandler(HandlerConfig handlerConfig) : base(handlerConfig)
        {
            fileInfo = new Dictionary<string,string>();
        }

        protected override bool CanHandle(ClipboardContent clipboardContent)
        {
            string filePath = clipboardContent.Files.FirstOrDefault()!;

            for (int i = 0; i < clipboardContent.Files.Length; i++)
            {
                var fileProperties = GetFullFileInfoDictionary(filePath, i);

                if (!fileInfo.TryGetValue(FileInfoKeys.Extension + i, out var extension) || fileInfo.ContainsKey(FileInfoKeys.NotFound) || string.IsNullOrWhiteSpace(extension) || !base.HandlerConfig.FileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                {
                    return false;
                }

                fileInfo.Concat(fileProperties);
            }
            return true;
        }

        bool IHandler.CanHandle(ClipboardContent clipboardContent)
        {
            return clipboardContent.IsFile || clipboardContent.Files.Any() ||  CanHandle(clipboardContent);
        }

        public HandlerConfig HandlerConfig => base.HandlerConfig;

        protected override Dictionary<string, string> CreateContext(ClipboardContent clipboardContent)
        {
            return fileInfo;
        }

        private static Dictionary<string, string> GetFullFileInfoDictionary(string filePath, int fileIndex)
        {
            Dictionary<string, string> fileInfoDictionary = new();

            if (!File.Exists(filePath))
            {
                fileInfoDictionary.Add(nameof(FileInfoKeys.NotFound), "Dosya bulunamadı");
                return fileInfoDictionary;
            }

            FileInfo fileInfo = new(filePath);
            FileAttributes attributes = fileInfo.Attributes;

            fileInfoDictionary.Add(nameof(FileInfoKeys.FileName) + fileIndex, fileInfo.Name);
            fileInfoDictionary.Add(nameof(FileInfoKeys.FullPath) + fileIndex, fileInfo.FullName);
            fileInfoDictionary.Add(nameof(FileInfoKeys.Extension) + fileIndex, fileInfo.Extension);
            fileInfoDictionary.Add(nameof(FileInfoKeys.SizeBytes) + fileIndex, fileInfo.Length.ToString());
            fileInfoDictionary.Add(nameof(FileInfoKeys.CreationDate) + fileIndex, fileInfo.CreationTime.ToString());
            fileInfoDictionary.Add(nameof(FileInfoKeys.CreationDateUtc) + fileIndex, fileInfo.CreationTimeUtc.ToString());
            fileInfoDictionary.Add(nameof(FileInfoKeys.LastAccess) + fileIndex, fileInfo.LastAccessTime.ToString());
            fileInfoDictionary.Add(nameof(FileInfoKeys.LastAccessUtc) + fileIndex, fileInfo.LastAccessTimeUtc.ToString());
            fileInfoDictionary.Add(nameof(FileInfoKeys.LastWrite) + fileIndex, fileInfo.LastWriteTime.ToString());
            fileInfoDictionary.Add(nameof(FileInfoKeys.LastWriteUtc) + fileIndex, fileInfo.LastWriteTimeUtc.ToString());
            fileInfoDictionary.Add(nameof(FileInfoKeys.ReadOnly) + fileIndex, fileInfo.IsReadOnly.ToString());

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

            fileInfoDictionary.Add(nameof(FileInfoKeys.Exists) + fileIndex, fileInfo.Exists.ToString());
            fileInfoDictionary.Add(nameof(FileInfoKeys.DirectoryPath) + fileIndex, fileInfo.DirectoryName ?? "Yok");
            fileInfoDictionary.Add(nameof(FileInfoKeys.DirectoryObject) + fileIndex, fileInfo.Directory?.FullName ?? "Yok");

            return fileInfoDictionary;
        }
        
        protected override List<ConfigAction> GetActions()
        {
            return base.HandlerConfig.Actions;
        }
    }
}
