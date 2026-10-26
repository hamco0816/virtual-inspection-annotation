using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Inspection.EditorTools
{
    /// <summary>打包 Windows 桌面版：菜单 Inspection/打包 Windows 版，或命令行 -executeMethod Inspection.EditorTools.InspectionBuildTool.BuildWindows。</summary>
    public static class InspectionBuildTool
    {
        private const string ScenePath = "Assets/Scenes/InspectionSite.unity";
        private const string OutputPath = "Builds/Windows/InspectionClient.exe";

        [MenuItem("Inspection/打包 Windows 版")]
        public static void BuildWindows()
        {
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = OutputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[Inspection] 打包失败：{report.summary.result}，错误数 {report.summary.totalErrors}");
                return;
            }
            Debug.Log($"[Inspection] 打包完成 {OutputPath}，大小 {report.summary.totalSize / (1024 * 1024)} MB");
        }
    }
}
