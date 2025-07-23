using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace CrowdMultiplier.ML
{
    /// <summary>
    /// Advanced machine learning analytics system for predictive insights and optimization
    /// Features player behavior prediction, churn analysis, and automated difficulty adjustment
    /// </summary>
    public class MLAnalyticsSystem : MonoBehaviour
    {
        [Header("ML Configuration")]
        [SerializeField] private bool enableMLAnalytics = true;
        [SerializeField] private float modelUpdateInterval = 300f; // 5 minutes
        [SerializeField] private int minimumDataPoints = 50;
        [SerializeField] private float predictionConfidenceThreshold = 0.7f;
        
        [Header("Prediction Models")]
        [SerializeField] private bool enableChurnPrediction = true;
        [SerializeField] private bool enableLTVPrediction = true;
        [SerializeField] private bool enableDifficultyOptimization = true;
        [SerializeField] private bool enablePersonalization = true;
        
        [Header("Data Collection")]
        [SerializeField] private int maxSessionHistory = 1000;
        [SerializeField] private int maxBehaviorEvents = 5000;
        [SerializeField] private bool enableRealTimePrediction = true;
        
        // ML Models and Data
        private ChurnPredictionModel churnModel;
        private LTVPredictionModel ltvModel;
        private DifficultyOptimizationModel difficultyModel;
        private PersonalizationModel personalizationModel;
        
        // Data storage
        private List<PlayerSession> sessionHistory = new List<PlayerSession>();
        private List<BehaviorEvent> behaviorEvents = new List<BehaviorEvent>();
        private PlayerProfile currentPlayerProfile;
        private Dictionary<string, float> realtimePredictions = new Dictionary<string, float>();
        
        // Component references
        private Core.AnalyticsManager analyticsManager;
        private Core.GameManager gameManager;
        private Core.LevelManager levelManager;
        private ABTesting.ABTestingFramework abTestingFramework;
        
        // Events
        public event Action<string, float> OnPredictionUpdated;
        public event Action<PlayerInsights> OnInsightsGenerated;
        public event Action<string, Dictionary<string, object>> OnMLRecommendation;
        
        private void Start()
        {
            InitializeMLSystem();
        }
        
        private void InitializeMLSystem()
        {
            if (!enableMLAnalytics) return;
            
            // Get component references
            analyticsManager = FindObjectOfType<Core.AnalyticsManager>();
            gameManager = Core.GameManager.Instance;
            levelManager = FindObjectOfType<Core.LevelManager>();
            abTestingFramework = FindObjectOfType<ABTesting.ABTestingFramework>();
            
            // Initialize models
            InitializeModels();
            
            // Load existing data
            LoadPlayerData();
            
            // Start model updates
            InvokeRepeating(nameof(UpdateModels), modelUpdateInterval, modelUpdateInterval);
            
            // Subscribe to events
            SubscribeToEvents();
            
            Debug.Log("ML Analytics System initialized");
        }
        
        private void InitializeModels()
        {
            churnModel = new ChurnPredictionModel();
            ltvModel = new LTVPredictionModel();
            difficultyModel = new DifficultyOptimizationModel();
            personalizationModel = new PersonalizationModel();
            
            // Initialize player profile
            currentPlayerProfile = new PlayerProfile
            {
                PlayerId = GetPlayerId(),
                FirstSession = DateTime.UtcNow,
                TotalSessions = 1,
                TotalPlayTime = 0f,
                AvgSessionLength = 0f,
                PreferredDifficulty = 0.5f,
                ChurnRisk = 0f,
                PredictedLTV = 0f
            };
        }
        
        private void LoadPlayerData()
        {
            // Load session history
            string sessionJson = PlayerPrefs.GetString("MLSessionHistory", "");
            if (!string.IsNullOrEmpty(sessionJson))
            {
                try
                {
                    // In production, use proper JSON deserialization
                    sessionHistory = new List<PlayerSession>();
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to load session history: {e.Message}");
                }
            }
            
            // Load player profile
            LoadPlayerProfile();
        }
        
        private void LoadPlayerProfile()
        {
            string profileJson = PlayerPrefs.GetString("MLPlayerProfile", "");
            if (!string.IsNullOrEmpty(profileJson))
            {
                try
                {
                    currentPlayerProfile = JsonUtility.FromJson<PlayerProfile>(profileJson);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to load player profile: {e.Message}");
                    // Use default profile
                }
            }
        }
        
        private void SubscribeToEvents()
        {
            if (analyticsManager != null)
            {
                // Simple local analytics tracking
                // analyticsManager.OnEventTracked += OnAnalyticsEventTracked;
            }
            
            if (gameManager != null)
            {
                gameManager.OnGameStateChanged += OnGameStateChanged;
            }
        }
        
        private void OnAnalyticsEventTracked(string eventName, Dictionary<string, object> parameters)
        {
            // Convert analytics events to behavior events for ML processing
            var behaviorEvent = new BehaviorEvent
            {
                EventName = eventName,
                Timestamp = DateTime.UtcNow,
                Parameters = new Dictionary<string, float>()
            };
            
            // Extract numeric parameters
            foreach (var param in parameters)
            {
                if (param.Value is float floatValue)
                {
                    behaviorEvent.Parameters[param.Key] = floatValue;
                }
                else if (param.Value is int intValue)
                {
                    behaviorEvent.Parameters[param.Key] = (float)intValue;
                }
                else if (param.Value is double doubleValue)
                {
                    behaviorEvent.Parameters[param.Key] = (float)doubleValue;
                }
            }
            
            AddBehaviorEvent(behaviorEvent);
            
            // Real-time prediction updates
            if (enableRealTimePrediction)
            {
                UpdateRealtimePredictions();
            }
        }
        
        private void OnGameStateChanged(Core.GameState newState)
        {
            if (newState == Core.GameState.GameOver)
            {
                // End current session
                EndCurrentSession();
            }
            else if (newState == Core.GameState.Playing)
            {
                // Start new session
                StartNewSession();
            }
        }
        
        private void StartNewSession()
        {
            var session = new PlayerSession
            {
                SessionId = Guid.NewGuid().ToString(),
                StartTime = DateTime.UtcNow,
                PlayerId = currentPlayerProfile.PlayerId
            };
            
            sessionHistory.Add(session);
            
            // Update player profile
            currentPlayerProfile.TotalSessions++;
            currentPlayerProfile.LastSession = DateTime.UtcNow;
            
            // Trim history if needed
            if (sessionHistory.Count > maxSessionHistory)
            {
                sessionHistory.RemoveAt(0);
            }
        }
        
        private void EndCurrentSession()
        {
            if (sessionHistory.Count > 0)
            {
                var currentSession = sessionHistory.Last();
                currentSession.EndTime = DateTime.UtcNow;
                currentSession.Duration = (float)(currentSession.EndTime.Value - currentSession.StartTime).TotalMinutes;
                
                // Extract session metrics
                ExtractSessionMetrics(currentSession);
                
                // Update player profile
                UpdatePlayerProfile();
                
                // Save data
                SavePlayerData();
            }
        }
        
        private void ExtractSessionMetrics(PlayerSession session)
        {
            var sessionEvents = behaviorEvents.Where(e => 
                e.Timestamp >= session.StartTime && 
                e.Timestamp <= (session.EndTime ?? DateTime.UtcNow)).ToList();
            
            // Calculate session metrics
            session.LevelsPlayed = sessionEvents.Count(e => e.EventName == "level_start");
            session.GatesTriggered = sessionEvents.Count(e => e.EventName == "gate_triggered");
            session.MaxCrowdSize = sessionEvents.Where(e => e.Parameters.ContainsKey("crowd_size"))
                                               .Max(e => e.Parameters.GetValueOrDefault("crowd_size", 0f));
            session.AvgFPS = sessionEvents.Where(e => e.Parameters.ContainsKey("fps"))
                                         .Average(e => e.Parameters.GetValueOrDefault("fps", 60f));
            session.CompletionRate = CalculateCompletionRate(sessionEvents);
            session.EngagementScore = CalculateEngagementScore(sessionEvents);
        }
        
        private float CalculateCompletionRate(List<BehaviorEvent> sessionEvents)
        {
            int levelsStarted = sessionEvents.Count(e => e.EventName == "level_start");
            int levelsCompleted = sessionEvents.Count(e => e.EventName == "level_complete");
            
            return levelsStarted > 0 ? (float)levelsCompleted / levelsStarted : 0f;
        }
        
        private float CalculateEngagementScore(List<BehaviorEvent> sessionEvents)
        {
            // Calculate engagement based on various factors
            float interactionRate = sessionEvents.Count / Mathf.Max(1f, sessionEvents.Count);
            float progressRate = sessionEvents.Count(e => e.EventName.Contains("progress")) / (float)sessionEvents.Count;
            float retentionIndicator = sessionEvents.Any(e => e.EventName == "session_extended") ? 1.2f : 1f;
            
            return Mathf.Clamp01(interactionRate * progressRate * retentionIndicator);
        }
        
        private void UpdatePlayerProfile()
        {
            if (sessionHistory.Count == 0) return;
            
            var recentSessions = sessionHistory.TakeLast(10).ToList();
            
            // Update averages
            currentPlayerProfile.AvgSessionLength = recentSessions.Average(s => s.Duration);
            currentPlayerProfile.TotalPlayTime = sessionHistory.Sum(s => s.Duration);
            currentPlayerProfile.AvgCompletionRate = recentSessions.Average(s => s.CompletionRate);
            currentPlayerProfile.AvgEngagementScore = recentSessions.Average(s => s.EngagementScore);
            
            // Calculate trends
            var trendWindow = recentSessions.TakeLast(5).ToList();
            if (trendWindow.Count >= 2)
            {
                var firstHalf = trendWindow.Take(trendWindow.Count / 2);
                var secondHalf = trendWindow.Skip(trendWindow.Count / 2);
                
                currentPlayerProfile.EngagementTrend = secondHalf.Average(s => s.EngagementScore) - 
                                                      firstHalf.Average(s => s.EngagementScore);
            }
        }
        
        private void UpdateModels()
        {
            if (sessionHistory.Count < minimumDataPoints) return;
            
            // Update churn prediction
            if (enableChurnPrediction)
            {
                UpdateChurnPrediction();
            }
            
            // Update LTV prediction
            if (enableLTVPrediction)
            {
                UpdateLTVPrediction();
            }
            
            // Update difficulty optimization
            if (enableDifficultyOptimization)
            {
                UpdateDifficultyOptimization();
            }
            
            // Update personalization
            if (enablePersonalization)
            {
                UpdatePersonalization();
            }
            
            // Generate insights
            GeneratePlayerInsights();
        }
        
        private void UpdateChurnPrediction()
        {
            float churnRisk = churnModel.PredictChurnRisk(currentPlayerProfile, sessionHistory);
            currentPlayerProfile.ChurnRisk = churnRisk;
            realtimePredictions["churn_risk"] = churnRisk;
            
            OnPredictionUpdated?.Invoke("churn_risk", churnRisk);
            
            // Trigger interventions for high-risk players
            if (churnRisk > 0.7f)
            {
                TriggerChurnPrevention();
            }
        }
        
        private void UpdateLTVPrediction()
        {
            float predictedLTV = ltvModel.PredictLTV(currentPlayerProfile, sessionHistory);
            currentPlayerProfile.PredictedLTV = predictedLTV;
            realtimePredictions["predicted_ltv"] = predictedLTV;
            
            OnPredictionUpdated?.Invoke("predicted_ltv", predictedLTV);
        }
        
        private void UpdateDifficultyOptimization()
        {
            float optimalDifficulty = difficultyModel.OptimizeDifficulty(currentPlayerProfile, sessionHistory);
            currentPlayerProfile.PreferredDifficulty = optimalDifficulty;
            realtimePredictions["optimal_difficulty"] = optimalDifficulty;
            
            OnPredictionUpdated?.Invoke("optimal_difficulty", optimalDifficulty);
            
            // Apply difficulty adjustment
            ApplyDifficultyAdjustment(optimalDifficulty);
        }
        
        private void UpdatePersonalization()
        {
            var recommendations = personalizationModel.GenerateRecommendations(currentPlayerProfile, sessionHistory);
            
            foreach (var recommendation in recommendations)
            {
                OnMLRecommendation?.Invoke(recommendation.Key, recommendation.Value);
            }
        }
        
        private void UpdateRealtimePredictions()
        {
            // Quick prediction updates based on recent behavior
            var recentEvents = behaviorEvents.TakeLast(10).ToList();
            
            if (recentEvents.Any())
            {
                // Update engagement prediction
                float currentEngagement = CalculateCurrentEngagement(recentEvents);
                realtimePredictions["current_engagement"] = currentEngagement;
                
                // Update session extension probability
                float extensionProbability = CalculateSessionExtensionProbability(recentEvents);
                realtimePredictions["session_extension_probability"] = extensionProbability;
                
                OnPredictionUpdated?.Invoke("current_engagement", currentEngagement);
                OnPredictionUpdated?.Invoke("session_extension_probability", extensionProbability);
            }
        }
        
        private float CalculateCurrentEngagement(List<BehaviorEvent> recentEvents)
        {
            if (recentEvents.Count == 0) return 0f;
            
            float timeSpan = (float)(recentEvents.Last().Timestamp - recentEvents.First().Timestamp).TotalMinutes;
            float eventRate = recentEvents.Count / Mathf.Max(0.1f, timeSpan);
            
            // Factor in event types
            float positiveEvents = recentEvents.Count(e => IsPositiveEvent(e.EventName));
            float negativeEvents = recentEvents.Count(e => IsNegativeEvent(e.EventName));
            
            float eventScore = (positiveEvents - negativeEvents * 0.5f) / recentEvents.Count;
            
            return Mathf.Clamp01(eventRate * 0.1f + eventScore);
        }
        
        private float CalculateSessionExtensionProbability(List<BehaviorEvent> recentEvents)
        {
            // Factors that indicate session continuation
            bool hasRecentProgress = recentEvents.Any(e => e.EventName.Contains("level") || e.EventName.Contains("gate"));
            bool hasPositiveEngagement = recentEvents.Count(e => IsPositiveEvent(e.EventName)) > 
                                        recentEvents.Count(e => IsNegativeEvent(e.EventName));
            bool maintainingPerformance = recentEvents.Where(e => e.Parameters.ContainsKey("fps"))
                                                     .Average(e => e.Parameters["fps"]) > 30f;
            
            float probability = 0.3f; // Base probability
            if (hasRecentProgress) probability += 0.3f;
            if (hasPositiveEngagement) probability += 0.2f;
            if (maintainingPerformance) probability += 0.2f;
            
            return Mathf.Clamp01(probability);
        }
        
        private bool IsPositiveEvent(string eventName)
        {
            return eventName.Contains("level_complete") || 
                   eventName.Contains("gate_triggered") || 
                   eventName.Contains("crowd_multiply") ||
                   eventName.Contains("achievement");
        }
        
        private bool IsNegativeEvent(string eventName)
        {
            return eventName.Contains("level_failed") || 
                   eventName.Contains("game_over") || 
                   eventName.Contains("error") ||
                   eventName.Contains("crash");
        }
        
        private void TriggerChurnPrevention()
        {
            var interventions = new Dictionary<string, object>
            {
                { "show_help", true },
                { "reduce_difficulty", 0.2f },
                { "offer_bonus", true },
                { "extended_tutorial", true }
            };
            
            OnMLRecommendation?.Invoke("churn_prevention", interventions);
            
            // Track intervention
            if (analyticsManager != null)
            {
                analyticsManager.TrackEvent("ml_churn_intervention", new Dictionary<string, object>
                {
                    { "churn_risk", currentPlayerProfile.ChurnRisk },
                    { "interventions", interventions.Count }
                });
            }
        }
        
        private void ApplyDifficultyAdjustment(float optimalDifficulty)
        {
            if (levelManager != null)
            {
                // Apply difficulty through level manager
                var adjustmentParams = new Dictionary<string, object>
                {
                    { "difficulty_multiplier", optimalDifficulty },
                    { "adaptive_scaling", true },
                    { "ml_optimized", true }
                };
                
                OnMLRecommendation?.Invoke("difficulty_adjustment", adjustmentParams);
            }
        }
        
        private void GeneratePlayerInsights()
        {
            var insights = new PlayerInsights
            {
                PlayerId = currentPlayerProfile.PlayerId,
                GeneratedAt = DateTime.UtcNow,
                ChurnRisk = currentPlayerProfile.ChurnRisk,
                PredictedLTV = currentPlayerProfile.PredictedLTV,
                OptimalDifficulty = currentPlayerProfile.PreferredDifficulty,
                PlayerType = DeterminePlayerType(),
                Recommendations = GenerateRecommendations()
            };
            
            OnInsightsGenerated?.Invoke(insights);
        }
        
        private string DeterminePlayerType()
        {
            float avgSession = currentPlayerProfile.AvgSessionLength;
            float engagement = currentPlayerProfile.AvgEngagementScore;
            float completion = currentPlayerProfile.AvgCompletionRate;
            
            if (avgSession > 10f && engagement > 0.7f && completion > 0.8f)
                return "Power Player";
            else if (avgSession > 5f && engagement > 0.5f)
                return "Regular Player";
            else if (avgSession < 3f && completion < 0.3f)
                return "Struggling Player";
            else if (currentPlayerProfile.TotalSessions < 5)
                return "New Player";
            else
                return "Casual Player";
        }
        
        private List<string> GenerateRecommendations()
        {
            var recommendations = new List<string>();
            
            if (currentPlayerProfile.ChurnRisk > 0.6f)
                recommendations.Add("Implement churn prevention measures");
            
            if (currentPlayerProfile.AvgCompletionRate < 0.5f)
                recommendations.Add("Reduce difficulty or provide more hints");
            
            if (currentPlayerProfile.AvgSessionLength < 2f)
                recommendations.Add("Improve onboarding and early engagement");
            
            if (currentPlayerProfile.EngagementTrend < -0.1f)
                recommendations.Add("Add new content or features to re-engage");
            
            return recommendations;
        }
        
        private void AddBehaviorEvent(BehaviorEvent behaviorEvent)
        {
            behaviorEvents.Add(behaviorEvent);
            
            // Trim if needed
            if (behaviorEvents.Count > maxBehaviorEvents)
            {
                behaviorEvents.RemoveAt(0);
            }
        }
        
        private void SavePlayerData()
        {
            try
            {
                // Save player profile
                string profileJson = JsonUtility.ToJson(currentPlayerProfile);
                PlayerPrefs.SetString("MLPlayerProfile", profileJson);
                
                // Save session history (simplified)
                PlayerPrefs.SetString("MLSessionHistory", ""); // Implement proper serialization
                
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save ML player data: {e.Message}");
            }
        }
        
        private string GetPlayerId()
        {
            string playerId = PlayerPrefs.GetString("MLPlayerId", "");
            if (string.IsNullOrEmpty(playerId))
            {
                playerId = Guid.NewGuid().ToString();
                PlayerPrefs.SetString("MLPlayerId", playerId);
                PlayerPrefs.Save();
            }
            return playerId;
        }
        
        // Public API
        public float GetPrediction(string predictionType)
        {
            return realtimePredictions.GetValueOrDefault(predictionType, 0f);
        }
        
        public PlayerProfile GetPlayerProfile()
        {
            return currentPlayerProfile;
        }
        
        public Dictionary<string, object> GetMLRecommendations()
        {
            var recommendations = new Dictionary<string, object>();
            
            if (currentPlayerProfile.ChurnRisk > predictionConfidenceThreshold)
            {
                recommendations["reduce_churn"] = new Dictionary<string, object>
                {
                    { "priority", "high" },
                    { "risk_score", currentPlayerProfile.ChurnRisk }
                };
            }
            
            if (Math.Abs(currentPlayerProfile.PreferredDifficulty - 0.5f) > 0.2f)
            {
                recommendations["adjust_difficulty"] = new Dictionary<string, object>
                {
                    { "target_difficulty", currentPlayerProfile.PreferredDifficulty },
                    { "confidence", "high" }
                };
            }
            
            return recommendations;
        }
        
        private void OnDestroy()
        {
            // Save data before destroying
            SavePlayerData();
            
            // Unsubscribe from events
            if (analyticsManager != null)
            {
                // Simple local analytics tracking
                // analyticsManager.OnEventTracked -= OnAnalyticsEventTracked;
            }
            
            if (gameManager != null)
            {
                gameManager.OnGameStateChanged -= OnGameStateChanged;
            }
        }
    }
    
    // ML Model Classes (Simplified implementations)
    public class ChurnPredictionModel
    {
        public float PredictChurnRisk(PlayerProfile profile, List<PlayerSession> sessions)
        {
            if (sessions.Count < 3) return 0.5f; // Default for new players
            
            var recentSessions = sessions.TakeLast(5).ToList();
            
            // Factors affecting churn
            float sessionFrequency = CalculateSessionFrequency(recentSessions);
            float engagementTrend = profile.EngagementTrend;
            float completionRate = profile.AvgCompletionRate;
            float sessionLength = profile.AvgSessionLength;
            
            // Simple weighted model
            float churnRisk = 0.5f;
            churnRisk += (1f - sessionFrequency) * 0.3f;
            churnRisk += Math.Max(0f, -engagementTrend) * 0.2f;
            churnRisk += (1f - completionRate) * 0.2f;
            churnRisk += (sessionLength < 2f ? 0.3f : 0f);
            
            return Mathf.Clamp01(churnRisk);
        }
        
        private float CalculateSessionFrequency(List<PlayerSession> sessions)
        {
            if (sessions.Count < 2) return 0.5f;
            
            var timeSpans = new List<float>();
            for (int i = 1; i < sessions.Count; i++)
            {
                float hours = (float)(sessions[i].StartTime - sessions[i-1].StartTime).TotalHours;
                timeSpans.Add(hours);
            }
            
            float avgHoursBetweenSessions = timeSpans.Average();
            return Mathf.Clamp01(48f / avgHoursBetweenSessions); // Normalize to 2-day frequency
        }
    }
    
    public class LTVPredictionModel
    {
        public float PredictLTV(PlayerProfile profile, List<PlayerSession> sessions)
        {
            // Simplified LTV prediction
            float baseLTV = 5f; // Base value
            
            // Engagement multiplier
            float engagementMultiplier = 1f + profile.AvgEngagementScore;
            
            // Session frequency bonus
            float sessionBonus = Mathf.Min(sessions.Count * 0.1f, 2f);
            
            // Completion rate bonus
            float completionBonus = profile.AvgCompletionRate * 3f;
            
            return baseLTV * engagementMultiplier + sessionBonus + completionBonus;
        }
    }
    
    public class DifficultyOptimizationModel
    {
        public float OptimizeDifficulty(PlayerProfile profile, List<PlayerSession> sessions)
        {
            float currentDifficulty = profile.PreferredDifficulty;
            float completionRate = profile.AvgCompletionRate;
            float engagementScore = profile.AvgEngagementScore;
            
            // Adjust based on performance
            if (completionRate > 0.8f && engagementScore > 0.7f)
            {
                return Mathf.Min(currentDifficulty + 0.1f, 1f); // Increase difficulty
            }
            else if (completionRate < 0.3f || engagementScore < 0.3f)
            {
                return Mathf.Max(currentDifficulty - 0.15f, 0.1f); // Decrease difficulty
            }
            
            return currentDifficulty; // Maintain current difficulty
        }
    }
    
    public class PersonalizationModel
    {
        public Dictionary<string, Dictionary<string, object>> GenerateRecommendations(PlayerProfile profile, List<PlayerSession> sessions)
        {
            var recommendations = new Dictionary<string, Dictionary<string, object>>();
            
            // UI personalization
            if (profile.AvgSessionLength > 5f)
            {
                recommendations["ui_theme"] = new Dictionary<string, object>
                {
                    { "complexity", "advanced" },
                    { "show_detailed_stats", true }
                };
            }
            
            // Content recommendations
            if (profile.AvgCompletionRate > 0.7f)
            {
                recommendations["content"] = new Dictionary<string, object>
                {
                    { "suggest_harder_levels", true },
                    { "unlock_advanced_features", true }
                };
            }
            
            return recommendations;
        }
    }
    
    // Data Models
    [System.Serializable]
    public class PlayerProfile
    {
        public string PlayerId;
        public DateTime FirstSession;
        public DateTime LastSession;
        public int TotalSessions;
        public float TotalPlayTime;
        public float AvgSessionLength;
        public float AvgCompletionRate;
        public float AvgEngagementScore;
        public float EngagementTrend;
        public float PreferredDifficulty;
        public float ChurnRisk;
        public float PredictedLTV;
    }
    
    [System.Serializable]
    public class PlayerSession
    {
        public string SessionId;
        public string PlayerId;
        public DateTime StartTime;
        public DateTime? EndTime;
        public float Duration;
        public int LevelsPlayed;
        public int GatesTriggered;
        public float MaxCrowdSize;
        public float AvgFPS;
        public float CompletionRate;
        public float EngagementScore;
    }
    
    [System.Serializable]
    public class BehaviorEvent
    {
        public string EventName;
        public DateTime Timestamp;
        public Dictionary<string, float> Parameters;
    }
    
    [System.Serializable]
    public class PlayerInsights
    {
        public string PlayerId;
        public DateTime GeneratedAt;
        public float ChurnRisk;
        public float PredictedLTV;
        public float OptimalDifficulty;
        public string PlayerType;
        public List<string> Recommendations;
    }
}
