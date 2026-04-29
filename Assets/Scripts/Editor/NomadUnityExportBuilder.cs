using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class NomadUnityExportBuilder
{
    private static readonly string[] ScenePaths = EditorBuildSettings.scenes
        .Where((scene) => scene.enabled)
        .Select((scene) => scene.path)
        .ToArray();

    private static string ProjectRoot
    {
        get
        {
            string assetsPath = Path.GetFullPath("Assets");
            return Directory.GetParent(assetsPath)?.FullName ?? Directory.GetCurrentDirectory();
        }
    }

    private static string NomadAppRoot => Path.Combine(ProjectRoot, "NomadApp");
    private static string UnitySourceIosRoot => Path.Combine(NomadAppRoot, "unity", "source", "ios");

    [MenuItem("Nomad Adventure/Build/Export iOS Xcode Project")]
    public static void ExportIosXcodeProjectMenu()
    {
        ExportIosXcodeProject();
    }

    public static void ExportIosXcodeProject()
    {
        if (ScenePaths.Length == 0)
        {
            throw new InvalidOperationException("No enabled scenes were found in Build Settings.");
        }

        Directory.CreateDirectory(UnitySourceIosRoot);

        BuildPlayerOptions buildOptions = new BuildPlayerOptions
        {
            scenes = ScenePaths,
            locationPathName = UnitySourceIosRoot,
            target = BuildTarget.iOS,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(buildOptions);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Unity iOS export failed: {report.summary.result} ({report.summary.totalErrors} errors).");
        }

        string exportedProjectPath = Path.Combine(UnitySourceIosRoot, "Unity-iPhone.xcodeproj");
        if (!Directory.Exists(exportedProjectPath))
        {
            throw new InvalidOperationException(
                $"Unity export finished but {exportedProjectPath} was not created.");
        }

        UnityEngine.Debug.Log(
            $"[NomadUnityExportBuilder] iOS Xcode export completed at {UnitySourceIosRoot}");
    }
}
