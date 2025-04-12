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
        public string Name => "Lookup";

        protected override string OutputFormat => HandlerConfig.OutputFormat;

        public FileHandler(HandlerConfig handlerConfig) : base(handlerConfig)
        {
            fileInfo = new Dictionary<string,string>();
        }

        protected override bool CanHandle(string input)
        {
            //todo: birden fazla dosya seçebilir.



            fileInfo = GetFullFileInfoDictionary(input);

            if (!fileInfo.TryGetValue(FileInfoKeys.Extension, out var extension) || fileInfo.ContainsKey(FileInfoKeys.NotFound) || string.IsNullOrWhiteSpace(extension) || !HandlerConfig.FileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        bool IHandler.CanHandle(string input)
        {
            return CanHandle(input);
        }


        protected override Dictionary<string, string> CreateContext(string input)
        {
            return fileInfo;
        }

        private static Dictionary<string, string> GetFullFileInfoDictionary(string filePath)
        {
            Dictionary<string, string> fileInfoDictionary = new();

            if (!File.Exists(filePath))
            {
                fileInfoDictionary.Add(nameof(FileInfoKeys.NotFound), "Dosya bulunamadı");
                return fileInfoDictionary;
            }

            FileInfo fileInfo = new(filePath);
            FileAttributes attributes = fileInfo.Attributes;

            fileInfoDictionary.Add(nameof(FileInfoKeys.FileName), fileInfo.Name);
            fileInfoDictionary.Add(nameof(FileInfoKeys.FullPath), fileInfo.FullName);
            fileInfoDictionary.Add(nameof(FileInfoKeys.Extension), fileInfo.Extension);
            fileInfoDictionary.Add(nameof(FileInfoKeys.SizeBytes), fileInfo.Length.ToString());
            fileInfoDictionary.Add(nameof(FileInfoKeys.CreationDate), fileInfo.CreationTime.ToString());
            fileInfoDictionary.Add(nameof(FileInfoKeys.CreationDateUtc), fileInfo.CreationTimeUtc.ToString());
            fileInfoDictionary.Add(nameof(FileInfoKeys.LastAccess), fileInfo.LastAccessTime.ToString());
            fileInfoDictionary.Add(nameof(FileInfoKeys.LastAccessUtc), fileInfo.LastAccessTimeUtc.ToString());
            fileInfoDictionary.Add(nameof(FileInfoKeys.LastWrite), fileInfo.LastWriteTime.ToString());
            fileInfoDictionary.Add(nameof(FileInfoKeys.LastWriteUtc), fileInfo.LastWriteTimeUtc.ToString());
            fileInfoDictionary.Add(nameof(FileInfoKeys.ReadOnly), fileInfo.IsReadOnly.ToString());

            fileInfoDictionary.Add(nameof(FileInfoKeys.Hidden), attributes.HasFlag(FileAttributes.Hidden).ToString());
            fileInfoDictionary.Add(nameof(FileInfoKeys.System), attributes.HasFlag(FileAttributes.System).ToString());
            fileInfoDictionary.Add(nameof(FileInfoKeys.Archive), attributes.HasFlag(FileAttributes.Archive).ToString());
            fileInfoDictionary.Add(nameof(FileInfoKeys.Compressed), attributes.HasFlag(FileAttributes.Compressed).ToString());
            fileInfoDictionary.Add(nameof(FileInfoKeys.Temporary), attributes.HasFlag(FileAttributes.Temporary).ToString());
            fileInfoDictionary.Add(nameof(FileInfoKeys.Offline), attributes.HasFlag(FileAttributes.Offline).ToString());
            fileInfoDictionary.Add(nameof(FileInfoKeys.Encrypted), attributes.HasFlag(FileAttributes.Encrypted).ToString());
            fileInfoDictionary.Add(nameof(FileInfoKeys.DirectoryOnly), attributes.HasFlag(FileAttributes.Directory).ToString());
            fileInfoDictionary.Add(nameof(FileInfoKeys.ReparsePoint), attributes.HasFlag(FileAttributes.ReparsePoint).ToString());
            fileInfoDictionary.Add(nameof(FileInfoKeys.Sparse), attributes.HasFlag(FileAttributes.SparseFile).ToString());
            fileInfoDictionary.Add(nameof(FileInfoKeys.Device), attributes.HasFlag(FileAttributes.Device).ToString());
            fileInfoDictionary.Add(nameof(FileInfoKeys.Normal), attributes.HasFlag(FileAttributes.Normal).ToString());

            fileInfoDictionary.Add(nameof(FileInfoKeys.Exists), fileInfo.Exists.ToString());
            fileInfoDictionary.Add(nameof(FileInfoKeys.DirectoryPath), fileInfo.DirectoryName ?? "Yok");
            fileInfoDictionary.Add(nameof(FileInfoKeys.DirectoryObject), fileInfo.Directory?.FullName ?? "Yok");

            return fileInfoDictionary;
        }
        
        protected override List<ConfigAction> GetActions()
        {
            return HandlerConfig.Actions;
        }
    }
}
