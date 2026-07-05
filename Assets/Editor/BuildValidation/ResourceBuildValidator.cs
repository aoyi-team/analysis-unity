using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Aoyi.Editor.BuildValidation
{
    public static class ResourceBuildValidator
    {
        private const string ResourcesRoot = "Assets/Resources";
        private const string ReportDirectory = "Library/BuildReports";
        private const int TopAssetCount = 20;
        private const long WarningSizeBytes = 25L * 1024L * 1024L;
        private const long ErrorSizeBytes = 50L * 1024L * 1024L;

        private static readonly string[] AllowedResourcesLoadFiles =
        {
            NormalizePath("Assets/正式开发项目制作/开发脚本/NetWorkScripts/Manager/ResMgr.cs"),
            NormalizePath("Assets/正式开发项目制作/开发脚本/NetWorkScripts/Supabase/SupabaseConfig.cs")
        };

        private static readonly string[] IgnoredCodeFolders =
        {
            "Assets/Editor/",
            "Assets/Mirror/",
            "Assets/Plugins/",
            "Assets/TextMesh Pro/"
        };

        [MenuItem("Aoyi/Build Validation/Generate Resources Report")]
        public static void GenerateResourcesReportMenu()
        {
            var report = BuildReportData();
            WriteReports(report);
            LogSummary(report, false);
        }

        [MenuItem("Aoyi/Build Validation/Validate Resources")]
        public static void ValidateResourcesMenu()
        {
            var report = BuildReportData();
            WriteReports(report);
            LogSummary(report, true);
        }

        private static ResourceReport BuildReportData()
        {
            var report = new ResourceReport
            {
                generatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                resourcesRoot = ResourcesRoot,
                topAssets = new List<ResourceAssetEntry>(),
                resourcesLoadUsages = new List<CodeUsageEntry>(),
                unityEditorRuntimeUsages = new List<CodeUsageEntry>(),
                notes = new List<string>()
            };

            if (!AssetDatabase.IsValidFolder(ResourcesRoot))
            {
                report.notes.Add($"Resources folder not found: {ResourcesRoot}");
                return report;
            }

            var assets = FindResourceAssets();
            report.assetCount = assets.Count;
            report.totalSizeBytes = assets.Sum(asset => asset.sizeBytes);
            report.totalSizeMB = ToMB(report.totalSizeBytes);
            report.topAssets = assets
                .OrderByDescending(asset => asset.sizeBytes)
                .Take(TopAssetCount)
                .ToList();

            report.resourcesLoadUsages = FindCodeUsages(
                new[] { "Resources.Load", "Resources.LoadAsync", "Resources.LoadAll" },
                path => IsProjectRuntimeCode(path) && !AllowedResourcesLoadFiles.Contains(NormalizePath(path)));

            report.unityEditorRuntimeUsages = FindCodeUsages(
                new[] { "using UnityEditor", "UnityEditor.", "AssetDatabase", "EditorWindow", "MenuItem", "CustomEditor" },
                path => IsProjectRuntimeCode(path));

            if (report.totalSizeBytes >= ErrorSizeBytes)
            {
                report.notes.Add($"Resources size is above the error threshold: {report.totalSizeMB:F2} MB >= {ToMB(ErrorSizeBytes):F2} MB.");
            }
            else if (report.totalSizeBytes >= WarningSizeBytes)
            {
                report.notes.Add($"Resources size is above the warning threshold: {report.totalSizeMB:F2} MB >= {ToMB(WarningSizeBytes):F2} MB.");
            }

            if (report.resourcesLoadUsages.Count > 0)
            {
                report.notes.Add("Found direct Resources.Load usages outside the allowed loading files.");
            }

            if (report.unityEditorRuntimeUsages.Count > 0)
            {
                report.notes.Add("Found UnityEditor API references outside Editor folders.");
            }

            return report;
        }

        private static List<ResourceAssetEntry> FindResourceAssets()
        {
            return AssetDatabase.FindAssets(string.Empty, new[] { ResourcesRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !AssetDatabase.IsValidFolder(path))
                .Select(path => new ResourceAssetEntry
                {
                    path = NormalizePath(path),
                    sizeBytes = GetFileSize(path),
                    sizeKB = GetFileSize(path) / 1024f,
                    sizeMB = ToMB(GetFileSize(path))
                })
                .OrderBy(entry => entry.path, StringComparer.Ordinal)
                .ToList();
        }

        private static List<CodeUsageEntry> FindCodeUsages(IEnumerable<string> patterns, Func<string, bool> includePath)
        {
            var usages = new List<CodeUsageEntry>();
            var patternList = patterns.ToArray();
            var files = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                var assetPath = NormalizePath("Assets" + file.Substring(Application.dataPath.Length));
                if (!includePath(assetPath))
                {
                    continue;
                }

                string[] lines;
                try
                {
                    lines = File.ReadAllLines(file);
                }
                catch (IOException)
                {
                    continue;
                }

                var unityEditorOnlyDepth = 0;
                for (var index = 0; index < lines.Length; index++)
                {
                    var line = lines[index];
                    var trimmedLine = line.Trim();

                    if (unityEditorOnlyDepth > 0)
                    {
                        if (trimmedLine.StartsWith("#if", StringComparison.Ordinal))
                        {
                            unityEditorOnlyDepth++;
                        }
                        else if (trimmedLine.StartsWith("#endif", StringComparison.Ordinal))
                        {
                            unityEditorOnlyDepth--;
                        }
                        continue;
                    }

                    if (trimmedLine.StartsWith("#if", StringComparison.Ordinal) && trimmedLine.Contains("UNITY_EDITOR"))
                    {
                        unityEditorOnlyDepth++;
                        continue;
                    }

                    var matchedPattern = patternList.FirstOrDefault(pattern => line.Contains(pattern));
                    if (matchedPattern == null)
                    {
                        continue;
                    }

                    usages.Add(new CodeUsageEntry
                    {
                        path = assetPath,
                        line = index + 1,
                        pattern = matchedPattern,
                        text = line.Trim()
                    });
                }
            }

            return usages
                .OrderBy(usage => usage.path, StringComparer.Ordinal)
                .ThenBy(usage => usage.line)
                .ToList();
        }

        private static void WriteReports(ResourceReport report)
        {
            Directory.CreateDirectory(ReportDirectory);

            var markdownPath = Path.Combine(ReportDirectory, "resources-report.md");
            var jsonPath = Path.Combine(ReportDirectory, "resources-report.json");

            File.WriteAllText(markdownPath, ToMarkdown(report), Encoding.UTF8);
            File.WriteAllText(jsonPath, JsonUtility.ToJson(report, true), Encoding.UTF8);

            Debug.Log($"Resource build report written to {markdownPath} and {jsonPath}");
        }

        private static string ToMarkdown(ResourceReport report)
        {
            var builder = new StringBuilder();
            builder.AppendLine("# Resources Build Report");
            builder.AppendLine();
            builder.AppendLine($"Generated at: `{report.generatedAt}`");
            builder.AppendLine($"Resources root: `{report.resourcesRoot}`");
            builder.AppendLine($"Asset count: `{report.assetCount}`");
            builder.AppendLine($"Total size: `{report.totalSizeMB:F2} MB`");
            builder.AppendLine();

            builder.AppendLine("## Notes");
            builder.AppendLine();
            if (report.notes.Count == 0)
            {
                builder.AppendLine("- No warnings.");
            }
            else
            {
                foreach (var note in report.notes)
                {
                    builder.AppendLine($"- {note}");
                }
            }
            builder.AppendLine();

            builder.AppendLine($"## Top {TopAssetCount} Resources Assets");
            builder.AppendLine();
            builder.AppendLine("| Path | Size MB | Size KB |");
            builder.AppendLine("| --- | ---: | ---: |");
            foreach (var asset in report.topAssets)
            {
                builder.AppendLine($"| `{asset.path}` | {asset.sizeMB:F2} | {asset.sizeKB:F1} |");
            }
            builder.AppendLine();

            AppendUsages(builder, "Direct Resources Load Usages", report.resourcesLoadUsages);
            AppendUsages(builder, "UnityEditor Runtime Usages", report.unityEditorRuntimeUsages);

            return builder.ToString();
        }

        private static void AppendUsages(StringBuilder builder, string title, List<CodeUsageEntry> usages)
        {
            builder.AppendLine($"## {title}");
            builder.AppendLine();
            if (usages.Count == 0)
            {
                builder.AppendLine("- None found.");
                builder.AppendLine();
                return;
            }

            builder.AppendLine("| File | Line | Pattern | Text |");
            builder.AppendLine("| --- | ---: | --- | --- |");
            foreach (var usage in usages)
            {
                builder.AppendLine($"| `{usage.path}` | {usage.line} | `{usage.pattern}` | `{EscapeMarkdown(usage.text)}` |");
            }
            builder.AppendLine();
        }

        private static void LogSummary(ResourceReport report, bool validate)
        {
            var message = $"Resources: {report.assetCount} assets, {report.totalSizeMB:F2} MB. " +
                          $"Direct load usages: {report.resourcesLoadUsages.Count}. " +
                          $"UnityEditor runtime usages: {report.unityEditorRuntimeUsages.Count}.";

            if (!validate)
            {
                Debug.Log(message);
                return;
            }

            if (report.totalSizeBytes >= ErrorSizeBytes || report.resourcesLoadUsages.Count > 0 || report.unityEditorRuntimeUsages.Count > 0)
            {
                Debug.LogError(message + " See Library/BuildReports/resources-report.md");
            }
            else if (report.totalSizeBytes >= WarningSizeBytes)
            {
                Debug.LogWarning(message + " See Library/BuildReports/resources-report.md");
            }
            else
            {
                Debug.Log(message);
            }
        }

        private static long GetFileSize(string assetPath)
        {
            var fullPath = Path.GetFullPath(assetPath);
            return File.Exists(fullPath) ? new FileInfo(fullPath).Length : 0L;
        }

        private static float ToMB(long bytes)
        {
            return bytes / 1024f / 1024f;
        }

        private static string NormalizePath(string path)
        {
            return path.Replace('\\', '/');
        }

        private static bool IsProjectRuntimeCode(string path)
        {
            var normalizedPath = NormalizePath(path);
            return !normalizedPath.Contains("/Editor/") &&
                   !IgnoredCodeFolders.Any(folder => normalizedPath.StartsWith(folder, StringComparison.Ordinal));
        }

        private static string EscapeMarkdown(string text)
        {
            return text.Replace("|", "\\|");
        }
    }

    [Serializable]
    public class ResourceReport
    {
        public string generatedAt;
        public string resourcesRoot;
        public int assetCount;
        public long totalSizeBytes;
        public float totalSizeMB;
        public List<ResourceAssetEntry> topAssets;
        public List<CodeUsageEntry> resourcesLoadUsages;
        public List<CodeUsageEntry> unityEditorRuntimeUsages;
        public List<string> notes;
    }

    [Serializable]
    public class ResourceAssetEntry
    {
        public string path;
        public long sizeBytes;
        public float sizeKB;
        public float sizeMB;
    }

    [Serializable]
    public class CodeUsageEntry
    {
        public string path;
        public int line;
        public string pattern;
        public string text;
    }
}
