using UnityEngine;
using UnityEditor;

namespace CrowdMultiplier.BuildManagement
{
    /// <summary>
    /// Minimal Windows build script for local development
    /// </summary>
    public class WindowsBuildScript
    {
        [MenuItem("Build/Windows Executable (Fast)")]
        public static void BuildWindows()
        {
            Debug.Log("Starting Windows build...");
            
            string[] scenes = { "Assets/Scenes/GameScene.unity" };
            string buildPath = "Builds/Windows/CrowdMultiplier3D.exe";
            
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = scenes;
            buildPlayerOptions.locationPathName = buildPath;
            buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
            buildPlayerOptions.options = BuildOptions.Development;
            
            BuildPipeline.BuildPlayer(buildPlayerOptions);
            Debug.Log("Windows build completed!");
        }
    }
}
