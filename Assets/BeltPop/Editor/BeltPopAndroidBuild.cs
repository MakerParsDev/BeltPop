using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BeltPop.Editor
{
    /// <summary>Repeatable, non-publishing Android debug build for local validation.</summary>
    public static class BeltPopAndroidBuild
    {
        private const string PrototypeScene = "Assets/Scenes/BeltPopPrototype.unity";
        // Debug-only identifier; never use this as the production Play Store app ID.
        private const string DebugPackageId = "com.makerparsdev.beltpop.debug";

        // Preserve the original ARM64/default debug build and offer a separate
        // 32-bit Android debug APK for physical test devices without arm64-v8a.
        public static void BuildDebug()
        {
            BuildDebugTo("BeltPop-debug.apk", PlayerSettings.Android.targetArchitectures);
        }

        public static void BuildArmv7Debug()
        {
            BuildDebugTo("BeltPop-debug-armv7.apk", AndroidArchitecture.ARMv7);
        }

        private static void BuildDebugTo(string apkName, AndroidArchitecture architectures)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                throw new BuildFailedException("Android Build Support is unavailable in this Unity Editor.");

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string scene = Path.Combine(projectRoot, PrototypeScene.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(scene))
                throw new BuildFailedException("Playable BeltPop scene was not found: " + PrototypeScene);

            string output = Path.Combine(projectRoot, "Builds", "Android", apkName);
            Directory.CreateDirectory(Path.GetDirectoryName(output));

            string previousId = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            AndroidArchitecture previousArchitectures = PlayerSettings.Android.targetArchitectures;
            try
            {
                // These settings are for a local debug build only. Restore both
                // the application ID and CPU architecture after build completion.
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, DebugPackageId);
                PlayerSettings.Android.targetArchitectures = architectures;

                var options = new BuildPlayerOptions
                {
                    scenes = new[] { PrototypeScene },
                    target = BuildTarget.Android,
                    locationPathName = output,
                    options = BuildOptions.Development | BuildOptions.AllowDebugging
                };

                BuildReport report = BuildPipeline.BuildPlayer(options);
                if (report.summary.result != BuildResult.Succeeded)
                    throw new BuildFailedException(
                        "BeltPop Android debug build failed: " + report.summary.result +
                        "; errors=" + report.summary.totalErrors);

                Debug.Log("BeltPop debug APK created: " + output +
                          ", bytes=" + new FileInfo(output).Length);
            }
            finally
            {
                PlayerSettings.Android.targetArchitectures = previousArchitectures;
                PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, previousId);
            }
        }
    }
}
