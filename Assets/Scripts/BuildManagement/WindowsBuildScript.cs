using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.IO;
using System;

namespace CrowdMultiplier.BuildManagement
{
    /// <summary>
    /// Automated Windows build script for fast testing and deployment
    /// Creates optimized Windows executable with enterprise settings
    /// </summary>
    public class WindowsBuildScript
    {
        [MenuItem("Build/Windows Executable (Fast)")]
        public static void BuildWindowsExecutable()
        {
            BuildWindowsExecutableInternal(false);
        }
        
        [MenuItem("Build/Windows Executable (Release)")]
        public static void BuildWindowsExecutableRelease()
        {
            BuildWindowsExecutableInternal(true);
        }
        
        public static void BuildWindowsExecutableInternal(bool isRelease = false)
        {
            string buildName = isRelease ? "CrowdMultiplier3D_Release" : "CrowdMultiplier3D_Development";
            string buildPath = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "Windows", buildName);
            
            // Ensure build directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(buildPath));
            
            // Configure build settings
            ConfigureBuildSettings(isRelease);
            
            // Setup build options
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = GetScenePaths();
            buildPlayerOptions.locationPathName = buildPath + ".exe";
            buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
            buildPlayerOptions.options = isRelease ? 
                BuildOptions.None : 
                BuildOptions.Development | BuildOptions.AllowDebugging;
            
            Debug.Log($"Starting Windows build: {buildName}");
            Debug.Log($"Output path: {buildPlayerOptions.locationPathName}");
            
            // Start build
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;
            
            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"✅ Build succeeded! Size: {summary.totalSize / (1024 * 1024)} MB");
                Debug.Log($"📁 Build location: {buildPlayerOptions.locationPathName}");
                Debug.Log($"⏱️ Build time: {summary.totalTime.TotalSeconds:F1} seconds");
                
                // Open build folder
                EditorUtility.RevealInFinder(buildPlayerOptions.locationPathName);
                
                // Show completion dialog
                if (EditorUtility.DisplayDialog("Build Complete!", 
                    $"Windows executable built successfully!\n\n" +
                    $"Size: {summary.totalSize / (1024 * 1024)} MB\n" +
                    $"Time: {summary.totalTime.TotalSeconds:F1} seconds\n\n" +
                    $"Ready to test your enterprise game with full performance!",
                    "Run Game", "Close"))
                {
                    // Launch the game
                    System.Diagnostics.Process.Start(buildPlayerOptions.locationPathName);
                }
            }
            else
            {
                Debug.LogError($"❌ Build failed: {summary.result}");
                
                // Show error details
                foreach (BuildStep step in report.steps)
                {
                    foreach (BuildStepMessage message in step.messages)
                    {
                        if (message.type == LogType.Error || message.type == LogType.Exception)
                        {
                            Debug.LogError($"Build Error: {message.content}");
                        }
                    }
                }
                
                EditorUtility.DisplayDialog("Build Failed", 
                    "Windows build failed. Check the console for details.", "OK");
            }
        }
        
        private static void ConfigureBuildSettings(bool isRelease)
        {
            // Player settings optimization for Windows
            PlayerSettings.companyName = "Crowd Multiplier Studios";
            PlayerSettings.productName = "Crowd Multiplier 3D";
            PlayerSettings.bundleVersion = "1.0.0";
            
            // Performance settings
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.defaultIsNativeResolution = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.captureSingleScreen = false;
            PlayerSettings.usePlayerLog = !isRelease;
            
            // Graphics settings
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.gpuSkinning = true;
            PlayerSettings.graphicsJobs = true;
            
            // Windows-specific settings
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.visibleInBackground = false;
            
            // Optimization settings
            if (isRelease)
            {
                PlayerSettings.stripEngineCode = true;
                PlayerSettings.managedStrippingLevel = ManagedStrippingLevel.High;
                EditorUserBuildSettings.il2CppCodeGeneration = Il2CppCodeGeneration.OptimizeSpeed;
            }
            else
            {
                PlayerSettings.stripEngineCode = false;
                PlayerSettings.managedStrippingLevel = ManagedStrippingLevel.Minimal;
                EditorUserBuildSettings.il2CppCodeGeneration = Il2CppCodeGeneration.OptimizeSize;
            }
            
            // Analytics and enterprise features
            PlayerSettings.usePlayerLog = true; // Keep logs for analytics
            
            Debug.Log($"✅ Build settings configured for {(isRelease ? "Release" : "Development")} build");
        }
        
        private static string[] GetScenePaths()
        {
            var scenes = new System.Collections.Generic.List<string>();
            
            // Add all scenes from build settings
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled)
                {
                    scenes.Add(scene.path);
                }
            }
            
            // If no scenes in build settings, add current scene
            if (scenes.Count == 0)
            {
                string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
                if (!string.IsNullOrEmpty(currentScene))
                {
                    scenes.Add(currentScene);
                }
                else
                {
                    // Add default game scene
                    scenes.Add("Assets/Scenes/GameScene.unity");
                }
            }
            
            Debug.Log($"Building with {scenes.Count} scenes: {string.Join(", ", scenes)}");
            return scenes.ToArray();
        }
        
        // Quick build method for batch/command line
        public static void BuildFromCommandLine()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            bool isRelease = System.Array.IndexOf(args, "-release") >= 0;
            
            BuildWindowsExecutableInternal(isRelease);
            
            // Exit Unity after build (for automation)
            if (System.Array.IndexOf(args, "-quit") >= 0)
            {
                EditorApplication.Exit(0);
            }
        }
    }
}
