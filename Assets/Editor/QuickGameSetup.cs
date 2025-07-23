using UnityEngine;
using UnityEditor;

namespace CrowdMultiplier.Editor
{
    /// <summary>
    /// Quick setup script to populate the game scene with essential objects
    /// </summary>
    public class QuickGameSetup
    {
        [MenuItem("CrowdMultiplier/Setup Game Scene")]
        public static void SetupGameScene()
        {
            // Clear existing objects (except defaults)
            var existingGameObjects = GameObject.FindObjectsOfType<GameObject>();
            
            // Create Game Manager
            var gameManager = new GameObject("GameManager");
            gameManager.AddComponent<Core.GameManager>();
            gameManager.AddComponent<Core.AnalyticsManager>();
            
            // Create Level Manager
            var levelManager = new GameObject("LevelManager");
            levelManager.AddComponent<Core.LevelManager>();
            
            // Create Player
            var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.tag = "Player";
            player.AddComponent<Core.PlayerController>();
            player.transform.position = new Vector3(0, 1, -5);
            
            // Create initial crowd member
            var crowdController = new GameObject("CrowdController");
            crowdController.AddComponent<Core.CrowdController>();
            
            // Create a simple ground
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(10, 1, 20);
            ground.transform.position = new Vector3(0, 0, 0);
            
            // Create some sample gates
            for (int i = 0; i < 3; i++)
            {
                var gate = GameObject.CreatePrimitive(PrimitiveType.Cube);
                gate.name = $"Gate_{i + 1}";
                gate.AddComponent<Gameplay.Gate>();
                gate.transform.position = new Vector3(0, 1, i * 10 + 5);
                gate.transform.localScale = new Vector3(3, 2, 0.5f);
                
                // Make it look like a gate
                var renderer = gate.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = Color.green;
                }
            }
            
            // Create UI Canvas
            var canvas = new GameObject("Canvas");
            var canvasComponent = canvas.AddComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            // Add UI Manager
            canvas.AddComponent<UI.UIManager>();
            
            // Setup camera to follow player
            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                var cameraObj = new GameObject("Main Camera");
                mainCamera = cameraObj.AddComponent<Camera>();
                cameraObj.tag = "MainCamera";
            }
            
            mainCamera.transform.position = new Vector3(0, 5, -8);
            mainCamera.transform.rotation = Quaternion.Euler(15, 0, 0);
            
            Debug.Log("✅ Game Scene Setup Complete! Press Play to test your crowd multiplier game!");
            
            // Mark scene as dirty so it saves
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }
    }
}
