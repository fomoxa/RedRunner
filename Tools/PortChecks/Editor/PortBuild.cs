using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RedRunner.PortChecks
{
    public static class PortBuild
    {
        public const string PlayerPath = "Builds/Windows/RedRunner.exe";

        public static void Player()
        {
            var options = new BuildPlayerOptions
            {
                scenes = new[] { PortSetup.PlayScene },
                locationPathName = PlayerPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            bool passed = report.summary.result == BuildResult.Succeeded;
            Debug.Log($"PORT-CHECK build: {(passed ? "PASS" : "FAIL")} {report.summary.result} {report.summary.totalErrors} errors, {report.summary.totalSize} bytes, {PlayerPath}");
            if (!passed)
            {
                EditorApplication.Exit(1);
            }
        }
    }
}
