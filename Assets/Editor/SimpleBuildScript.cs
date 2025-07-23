using UnityEngine;
using UnityEditor;
using System.IO;

public class SimpleBuildScript
{
    [MenuItem("Build/Build Game Now")]
    public static void BuildGame()
    {
        // Create build directory
        string buildPath = "Builds/Windows";
        if (!Directory.Exists(buildPath))
        {
            Directory.CreateDirectory(buildPath);
        }
        
        // Define scenes to build
        string[] scenes = new string[]
        {
            "Assets/Scenes/GameScene.unity"
        };
        
        // Build settings
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = scenes;
        buildPlayerOptions.locationPathName = Path.Combine(buildPath, "CrowdMultiplier3D.exe");
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
        buildPlayerOptions.options = BuildOptions.None; // Use None instead of Development to avoid issues
        
        // Build the player
        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log("Build succeeded! Location: " + buildPlayerOptions.locationPathName);
            
            // Try to run the executable
            try
            {
                System.Diagnostics.Process.Start(buildPlayerOptions.locationPathName);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Could not start executable: " + e.Message);
            }
        }
        else
        {
            Debug.LogError("Build failed!");
        }
    }
}
