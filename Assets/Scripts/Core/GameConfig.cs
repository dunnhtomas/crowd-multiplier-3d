using UnityEngine;

namespace CrowdMultiplier.Core
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "Crowd Multiplier/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Gameplay Settings")]
        public float difficultyMultiplier = 1.0f;
        public int maxLevels = 100;
        public float levelProgressionRate = 1.1f;
        
        [Header("Crowd Settings")]
        public int baseCrowdSize = 10;
        public int maxCrowdSize = 1000;
        public float crowdSpeed = 5f;
        
        [Header("Performance Settings")]
        public int targetFrameRate = 60;
        public bool enableDynamicOptimization = true;
        public bool enableLOD = true;
        
        [Header("Analytics Settings")]
        public bool enableAnalytics = true;
        public bool enableAIInsights = true;
        public string analyticsEndpoint = "https://api.crowdmultiplier.ai";
    }
}
