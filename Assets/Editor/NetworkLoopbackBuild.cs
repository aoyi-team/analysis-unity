using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class NetworkLoopbackBuild
{
    public static void BuildWindows()
    {
        string outputPath = Environment.GetEnvironmentVariable("AOYI_LOOPBACK_BUILD_PATH");
        if (string.IsNullOrWhiteSpace(outputPath))
            outputPath = Path.GetFullPath("Builds/LanTest/NetworkLoopback/AoyiLoopback.exe");

        string directory = Path.GetDirectoryName(outputPath);
        if (string.IsNullOrWhiteSpace(directory))
            throw new InvalidOperationException($"Invalid loopback build path: {outputPath}");
        Directory.CreateDirectory(directory);

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();
        if (scenes.Length == 0)
            throw new InvalidOperationException("No enabled scenes are available for the loopback build.");

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Loopback build failed: {report.summary.result}, errors={report.summary.totalErrors}");
        }

        Debug.Log(
            $"[NetworkLoopbackBuild] Built {outputPath} ({report.summary.totalSize} bytes, {report.summary.totalTime}).");
    }
}
