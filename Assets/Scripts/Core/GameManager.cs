using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

namespace CrowdMultiplier.Core
{
    /// <summary>
    /// Enterprise-grade Game Manager with AI-driven analytics and real-time monitoring
    /// Implements Singleton pattern with dependency injection support
    /// </summary>
    public class GameManager : NetworkBehaviour, IGameManager
    {
        [Header("Game Configuration")]
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private bool enableAIAnalytics = true;
        [SerializeField] private bool enableRealTimeMonitoring = true;
        
        [Header("Performance Monitoring")]
        [SerializeField] private float targetFrameRate = 60f;
        [SerializeField] private int maxCrowdSize = 1000;
        [SerializeField] private bool enableDynamicOptimization = true;
        
        // Enterprise Analytics
        private AnalyticsManager analyticsManager;
        private PerformanceMonitor performanceMonitor;
        private SecurityManager securityManager;
        
        // Game State
        private GameState currentState = GameState.Loading;
        private int currentLevel = 1;
        private float sessionStartTime;
        
        // Crowd Management
        private CrowdController crowdController;
        private List<Gate> activeGates = new List<Gate>();
        
        public static GameManager Instance { get; private set; }
        
        // Events for reactive programming
        public event System.Action<GameState> OnGameStateChanged;
        public event System.Action<int> OnCrowdSizeChanged;
        public event System.Action<float> OnPerformanceAlert;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeManagers();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeManagers()
        {
            analyticsManager = GetComponent<AnalyticsManager>();
            performanceMonitor = GetComponent<PerformanceMonitor>();
            securityManager = GetComponent<SecurityManager>();
            
            sessionStartTime = Time.time;
            
            // Initialize AI-driven systems
            if (enableAIAnalytics)
            {
                analyticsManager?.InitializeAIModels();
            }
            
            // Setup real-time monitoring
            if (enableRealTimeMonitoring)
            {
                StartCoroutine(MonitorPerformance());
            }
        }
        
        public void StartGame()
        {
            ChangeGameState(GameState.Playing);
            analyticsManager?.TrackEvent("game_started", new Dictionary<string, object>
            {
                { "level", currentLevel },
                { "timestamp", System.DateTime.UtcNow.ToString() },
                { "session_id", System.Guid.NewGuid().ToString() }
            });
        }
        
        public void EndGame(bool victory)
        {
            ChangeGameState(victory ? GameState.Victory : GameState.GameOver);
            
            float sessionDuration = Time.time - sessionStartTime;
            analyticsManager?.TrackEvent("game_ended", new Dictionary<string, object>
            {
                { "victory", victory },
                { "duration", sessionDuration },
                { "level", currentLevel },
                { "final_crowd_size", crowdController?.GetCrowdSize() ?? 0 }
            });
        }
        
        private void ChangeGameState(GameState newState)
        {
            if (currentState != newState)
            {
                currentState = newState;
                OnGameStateChanged?.Invoke(newState);
                
                // AI-driven state analysis
                analyticsManager?.AnalyzeStateTransition(newState);
            }
        }
        
        private IEnumerator MonitorPerformance()
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);
                
                float fps = 1f / Time.unscaledDeltaTime;
                if (fps < targetFrameRate * 0.8f) // 80% threshold
                {
                    OnPerformanceAlert?.Invoke(fps);
                    
                    if (enableDynamicOptimization)
                    {
                        OptimizePerformance();
                    }
                }
            }
        }
        
        private void OptimizePerformance()
        {
            // Dynamic optimization based on current performance
            if (crowdController != null && crowdController.GetCrowdSize() > maxCrowdSize)
            {
                crowdController.OptimizeCrowd();
            }
            
            // Adjust quality settings
            if (QualitySettings.GetQualityLevel() > 0)
            {
                QualitySettings.DecreaseLevel();
            }
        }
        
        // Network synchronization for multiplayer features
        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                // Server-side game logic
                InitializeServerSideLogic();
            }
        }
        
        private void InitializeServerSideLogic()
        {
            // Anti-cheat validation
            securityManager?.EnableAntiCheatValidation();
            
            // Server-side analytics
            analyticsManager?.InitializeServerAnalytics();
        }
        
        // AI-driven difficulty adjustment
        public void AdjustDifficulty(float playerSkillScore)
        {
            // Machine learning model predicts optimal difficulty
            float recommendedDifficulty = analyticsManager.PredictOptimalDifficulty(playerSkillScore);
            gameConfig.difficultyMultiplier = recommendedDifficulty;
        }
        
        private void OnDestroy()
        {
            StopAllCoroutines();
        }
    }
    
    public enum GameState
    {
        Loading,
        MainMenu,
        Playing,
        Paused,
        Victory,
        GameOver
    }
    
    public interface IGameManager
    {
        void StartGame();
        void EndGame(bool victory);
    }
}
