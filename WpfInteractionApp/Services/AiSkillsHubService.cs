using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using WpfInteractionApp.Settings;

namespace WpfInteractionApp.Services
{
    public sealed class AiSkillsHubService
    {
        private static readonly Regex s_skillNameSafe = new(@"^[a-zA-Z0-9][a-zA-Z0-9._-]*$", RegexOptions.Compiled);

        private readonly AiSkillsHubSettings _hub;

        public AiSkillsHubService(AiSkillsHubSettings hub)
        {
            _hub = hub ?? new AiSkillsHubSettings();
        }

        public string GetCursorSkillsRoot()
        {
            if (!string.IsNullOrWhiteSpace(_hub.CursorSkillsPath))
                return Path.GetFullPath(_hub.CursorSkillsPath.Trim());
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cursor", "skills");
        }

        public string GetCopilotSkillsRoot()
        {
            if (!string.IsNullOrWhiteSpace(_hub.CopilotSkillsPath))
                return Path.GetFullPath(_hub.CopilotSkillsPath.Trim());
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".copilot", "skills");
        }

        /// <summary>Validates a user-chosen skill root; rejects skills-cursor and obvious system paths.</summary>
        public static bool TryValidateCustomDestinationRoot(string? rawPath, out string normalized, out string? error)
        {
            normalized = string.Empty;
            error = null;
            if (string.IsNullOrWhiteSpace(rawPath))
            {
                error = "Path is empty.";
                return false;
            }

            try
            {
                var full = Path.GetFullPath(rawPath.Trim());
                normalized = full;
            }
            catch
            {
                error = "Invalid path.";
                return false;
            }

            if (IsDeniedPath(normalized))
            {
                error = "This path is not allowed (reserved or unsafe).";
                return false;
            }

            return true;
        }

        public static bool IsSkillNameValid(string name) =>
            !string.IsNullOrWhiteSpace(name) && s_skillNameSafe.IsMatch(name.Trim());

        public AiSkillsHubListResult BuildList()
        {
            var cursorRoot = GetCursorSkillsRoot();
            var copilotRoot = GetCopilotSkillsRoot();
            var rows = new List<AiSkillsHubSkillRow>();
            var byName = new Dictionary<string, List<(string SourceId, string? Label)>>(StringComparer.OrdinalIgnoreCase);

            foreach (var src in _hub.Sources ?? new List<AiSkillsSourceEntry>())
            {
                if (string.IsNullOrWhiteSpace(src.Id) || string.IsNullOrWhiteSpace(src.Path))
                    continue;
                string root;
                try
                {
                    root = Path.GetFullPath(src.Path.Trim());
                }
                catch
                {
                    continue;
                }

                if (!Directory.Exists(root))
                    continue;

                foreach (var dir in Directory.GetDirectories(root))
                {
                    var skillName = Path.GetFileName(dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                    if (!IsSkillNameValid(skillName))
                        continue;

                    if (!byName.TryGetValue(skillName, out var list))
                    {
                        list = new List<(string, string?)>();
                        byName[skillName] = list;
                    }
                    list.Add((src.Id, src.Label));
                }
            }

            foreach (var kv in byName.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
            {
                var skillName = kv.Key;
                var sourcesForName = kv.Value;
                var conflict = sourcesForName.Count > 1;

                foreach (var (sourceId, label) in sourcesForName)
                {
                    var sourceEntry = _hub.Sources?.FirstOrDefault(s => s.Id == sourceId);
                    if (sourceEntry == null)
                        continue;
                    string sourceSkillDir;
                    try
                    {
                        sourceSkillDir = Path.Combine(Path.GetFullPath(sourceEntry.Path.Trim()), skillName);
                    }
                    catch
                    {
                        continue;
                    }

                    var hasSkillMd = File.Exists(Path.Combine(sourceSkillDir, "SKILL.md"));
                    var sourceHash = ComputeDirectoryHash(sourceSkillDir);

                    var cursorSkillDir = Path.Combine(cursorRoot, skillName);
                    var copilotSkillDir = Path.Combine(copilotRoot, skillName);

                    var cursorHash = Directory.Exists(cursorSkillDir) ? ComputeDirectoryHash(cursorSkillDir) : null;
                    var copilotHash = Directory.Exists(copilotSkillDir) ? ComputeDirectoryHash(copilotSkillDir) : null;

                    rows.Add(new AiSkillsHubSkillRow
                    {
                        SkillName = skillName,
                        SourceId = sourceId,
                        SourceLabel = label ?? sourceEntry.Label,
                        HasSkillMd = hasSkillMd,
                        NameConflict = conflict,
                        SourceHash = sourceHash,
                        CursorSync = GetSyncState(sourceHash, cursorHash, Directory.Exists(cursorSkillDir)),
                        CopilotSync = GetSyncState(sourceHash, copilotHash, Directory.Exists(copilotSkillDir)),
                    });
                }
            }

            var fromSourceNames = new HashSet<string>(byName.Keys, StringComparer.OrdinalIgnoreCase);
            var globalOnlyRows = BuildGlobalOnlyRows(cursorRoot, copilotRoot, fromSourceNames);

            return new AiSkillsHubListResult
            {
                CursorSkillsRoot = cursorRoot,
                CopilotSkillsRoot = copilotRoot,
                Sources = (_hub.Sources ?? new List<AiSkillsSourceEntry>()).Select(s => new AiSkillsHubSourceDto
                {
                    Id = s.Id,
                    Path = s.Path,
                    Label = s.Label
                }).ToList(),
                Skills = rows,
                GlobalOnlySkills = globalOnlyRows
            };
        }

        private static List<AiSkillsHubGlobalOnlyRow> BuildGlobalOnlyRows(string cursorRoot, string copilotRoot, HashSet<string> fromSourceNames)
        {
            var inCursor = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var inCopilot = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            CollectSkillFolderNames(cursorRoot, inCursor);
            CollectSkillFolderNames(copilotRoot, inCopilot);

            var globalOnly = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var n in inCursor)
                globalOnly.Add(n);
            foreach (var n in inCopilot)
                globalOnly.Add(n);
            globalOnly.ExceptWith(fromSourceNames);

            var list = new List<AiSkillsHubGlobalOnlyRow>();
            foreach (var name in globalOnly.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                var cDir = Path.Combine(cursorRoot, name);
                var pDir = Path.Combine(copilotRoot, name);
                var inC = Directory.Exists(cDir);
                var inP = Directory.Exists(pDir);
                list.Add(new AiSkillsHubGlobalOnlyRow
                {
                    SkillName = name,
                    InCursor = inC,
                    InCopilot = inP,
                    HasSkillMdCursor = inC && File.Exists(Path.Combine(cDir, "SKILL.md")),
                    HasSkillMdCopilot = inP && File.Exists(Path.Combine(pDir, "SKILL.md"))
                });
            }

            return list;
        }

        private static void CollectSkillFolderNames(string root, HashSet<string> into)
        {
            if (!Directory.Exists(root))
                return;
            foreach (var dir in Directory.GetDirectories(root))
            {
                var skillName = Path.GetFileName(dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                if (IsSkillNameValid(skillName))
                    into.Add(skillName);
            }
        }

        private static string GetSyncState(string? sourceHash, string? targetHash, bool targetExists)
        {
            if (!targetExists)
                return "needs_deploy";
            if (string.IsNullOrEmpty(sourceHash) || string.IsNullOrEmpty(targetHash))
                return "needs_deploy";
            if (string.Equals(sourceHash, targetHash, StringComparison.Ordinal))
                return "synced";
            return "diverged";
        }

        public string? ComputeDirectoryHashOrNull(string directoryPath)
        {
            try
            {
                if (!Directory.Exists(directoryPath))
                    return null;
                return ComputeDirectoryHash(directoryPath);
            }
            catch
            {
                return null;
            }
        }

        public static string ComputeDirectoryHash(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
                return string.Empty;

            var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories)
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToList();

            using var ms = new MemoryStream();
            foreach (var file in files)
            {
                var rel = GetRelativePath(directoryPath, file).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                var relBytes = Encoding.UTF8.GetBytes(rel + "\0");
                ms.Write(relBytes, 0, relBytes.Length);
                ms.Write(File.ReadAllBytes(file));
            }

            return Convert.ToHexString(SHA256.HashData(ms.ToArray()));
        }

        private static string GetRelativePath(string root, string fullPath)
        {
            root = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var fp = Path.GetFullPath(fullPath);
            if (fp.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                return fp[root.Length..].Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            return Path.GetFileName(fullPath);
        }

        public AiSkillsHubOpResult DeploySkill(string skillName, string sourceId, bool toCursor, bool toCopilot, string? customDestinationRoot)
        {
            if (!toCursor && !toCopilot && string.IsNullOrWhiteSpace(customDestinationRoot))
                return AiSkillsHubOpResult.Fail("No deployment targets selected.");

            var err = ValidateSkillAndSource(skillName, sourceId, out var sourceDir);
            if (err != null)
                return AiSkillsHubOpResult.Fail(err);

            var results = new List<string>();
            if (toCursor)
            {
                var e = CopySkillToRoot(sourceDir, Path.Combine(GetCursorSkillsRoot(), skillName));
                if (e != null) return AiSkillsHubOpResult.Fail($"Cursor: {e}");
                results.Add("cursor");
            }
            if (toCopilot)
            {
                var e = CopySkillToRoot(sourceDir, Path.Combine(GetCopilotSkillsRoot(), skillName));
                if (e != null) return AiSkillsHubOpResult.Fail($"Copilot: {e}");
                results.Add("copilot");
            }
            if (!string.IsNullOrWhiteSpace(customDestinationRoot))
            {
                if (!TryValidateCustomDestinationRoot(customDestinationRoot, out var norm, out var verr))
                    return AiSkillsHubOpResult.Fail(verr ?? "Invalid custom path");
                var e = CopySkillToRoot(sourceDir, Path.Combine(norm, skillName));
                if (e != null) return AiSkillsHubOpResult.Fail($"Custom: {e}");
                results.Add("custom");
            }

            return AiSkillsHubOpResult.Succeeded(string.Join(", ", results));
        }

        public AiSkillsHubOpResult RemoveSkill(string skillName, bool fromCursor, bool fromCopilot)
        {
            if (!IsSkillNameValid(skillName))
                return AiSkillsHubOpResult.Fail("Invalid skill name.");
            if (!fromCursor && !fromCopilot)
                return AiSkillsHubOpResult.Fail("No remove targets selected.");

            var errors = new List<string>();
            if (fromCursor)
            {
                var dir = Path.Combine(GetCursorSkillsRoot(), skillName);
                var e = DeleteSkillDirIfExists(dir);
                if (e != null) errors.Add($"Cursor: {e}");
            }
            if (fromCopilot)
            {
                var dir = Path.Combine(GetCopilotSkillsRoot(), skillName);
                var e = DeleteSkillDirIfExists(dir);
                if (e != null) errors.Add($"Copilot: {e}");
            }

            if (errors.Count > 0)
                return AiSkillsHubOpResult.Fail(string.Join(" ", errors));
            return AiSkillsHubOpResult.Succeeded("removed");
        }

        public AiSkillsHubOpResult PullSkill(string skillName, string fromTarget, string toSourceId)
        {
            if (!IsSkillNameValid(skillName))
                return AiSkillsHubOpResult.Fail("Invalid skill name.");

            string? remoteDir = fromTarget.Equals("cursor", StringComparison.OrdinalIgnoreCase)
                ? Path.Combine(GetCursorSkillsRoot(), skillName)
                : fromTarget.Equals("copilot", StringComparison.OrdinalIgnoreCase)
                    ? Path.Combine(GetCopilotSkillsRoot(), skillName)
                    : null;
            if (remoteDir == null || !Directory.Exists(remoteDir))
                return AiSkillsHubOpResult.Fail("Source target not found.");

            var srcEntry = _hub.Sources?.FirstOrDefault(s => s.Id == toSourceId);
            if (srcEntry == null || string.IsNullOrWhiteSpace(srcEntry.Path))
                return AiSkillsHubOpResult.Fail("Destination source not found.");

            string destDir;
            try
            {
                destDir = Path.Combine(Path.GetFullPath(srcEntry.Path.Trim()), skillName);
            }
            catch (Exception ex)
            {
                return AiSkillsHubOpResult.Fail(ex.Message);
            }

            var err = CopySkillToRoot(remoteDir, destDir);
            return err != null ? AiSkillsHubOpResult.Fail(err) : AiSkillsHubOpResult.Succeeded("pulled");
        }

        private string? ValidateSkillAndSource(string skillName, string sourceId, out string sourceSkillDir)
        {
            sourceSkillDir = string.Empty;
            if (!IsSkillNameValid(skillName))
                return "Invalid skill name.";
            var src = _hub.Sources?.FirstOrDefault(s => s.Id == sourceId);
            if (src == null || string.IsNullOrWhiteSpace(src.Path))
                return "Unknown source.";
            try
            {
                sourceSkillDir = Path.Combine(Path.GetFullPath(src.Path.Trim()), skillName);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

            if (!Directory.Exists(sourceSkillDir))
                return "Source skill folder does not exist.";
            return null;
        }

        private static string? CopySkillToRoot(string sourceSkillDir, string destSkillDir)
        {
            try
            {
                if (IsDeniedPath(destSkillDir))
                    return "Destination path is not allowed.";

                if (Directory.Exists(destSkillDir))
                    Directory.Delete(destSkillDir, recursive: true);

                Directory.CreateDirectory(Path.GetDirectoryName(destSkillDir) ?? destSkillDir);
                CopyDirectoryRecursive(sourceSkillDir, destSkillDir);
                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private static void CopyDirectoryRecursive(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var name = Path.GetFileName(file);
                File.Copy(file, Path.Combine(destDir, name), overwrite: true);
            }
            foreach (var sub in Directory.GetDirectories(sourceDir))
            {
                var name = Path.GetFileName(sub);
                CopyDirectoryRecursive(sub, Path.Combine(destDir, name));
            }
        }

        private static string? DeleteSkillDirIfExists(string dir)
        {
            try
            {
                if (!Directory.Exists(dir))
                    return null;
                Directory.Delete(dir, recursive: true);
                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        private static bool IsDeniedPath(string fullPath)
        {
            try
            {
                var norm = Path.GetFullPath(fullPath);
                var parts = norm.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Any(p => string.Equals(p, "skills-cursor", StringComparison.OrdinalIgnoreCase)))
                    return true;
            }
            catch
            {
                return true;
            }
            return false;
        }
    }

    public sealed class AiSkillsHubListResult
    {
        public string CursorSkillsRoot { get; set; } = string.Empty;
        public string CopilotSkillsRoot { get; set; } = string.Empty;
        public List<AiSkillsHubSourceDto> Sources { get; set; } = new();
        public List<AiSkillsHubSkillRow> Skills { get; set; } = new();
        /// <summary>Skills present under global Cursor/Copilot roots but not under any configured source root.</summary>
        public List<AiSkillsHubGlobalOnlyRow> GlobalOnlySkills { get; set; } = new();
    }

    public sealed class AiSkillsHubGlobalOnlyRow
    {
        public string SkillName { get; set; } = string.Empty;
        public bool InCursor { get; set; }
        public bool InCopilot { get; set; }
        public bool HasSkillMdCursor { get; set; }
        public bool HasSkillMdCopilot { get; set; }
    }

    public sealed class AiSkillsHubSourceDto
    {
        public string Id { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string? Label { get; set; }
    }

    public sealed class AiSkillsHubSkillRow
    {
        public string SkillName { get; set; } = string.Empty;
        public string SourceId { get; set; } = string.Empty;
        public string? SourceLabel { get; set; }
        public bool HasSkillMd { get; set; }
        public bool NameConflict { get; set; }
        public string? SourceHash { get; set; }
        public string CursorSync { get; set; } = "needs_deploy";
        public string CopilotSync { get; set; } = "needs_deploy";
    }

    public sealed class AiSkillsHubOpResult
    {
        public bool Ok { get; set; }
        public string? Error { get; set; }
        public string? Detail { get; set; }

        public static AiSkillsHubOpResult Succeeded(string? detail = null) => new() { Ok = true, Detail = detail };
        public static AiSkillsHubOpResult Fail(string error) => new() { Ok = false, Error = error };
    }
}
