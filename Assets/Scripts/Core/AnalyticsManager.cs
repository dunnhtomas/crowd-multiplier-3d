using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.Services.Analytics;
using Unity.Services.Core;

namespace CrowdMultiplier.Core
{
    /// <summary>
    /// Enterprise-grade analytics system with real-time tracking and business intelligence
    /// Supports Unity Analytics, Firebase, and custom enterprise solutions
    /// </summary>
    public class AnalyticsManager : MonoBehaviour
    {
        [Header("Analytics Configuration")]
        [SerializeField] private bool enableAnalytics = true;
        [SerializeField] private bool enableDebugLogging = false;
        [SerializeField] private string customEndpoint = "";
        [SerializeField] private float batchUploadInterval = 30f;
        
        [Header("Performance Monitoring")]
        [SerializeField] private bool trackPerformanceMetrics = true;
        [SerializeField] private float performanceTrackingInterval = 5f;
        
        [Header("Business Intelligence")]
        [SerializeField] private bool enableRealtimeDashboard = true;
        [SerializeField] private bool enableCohortTracking = true;
        [SerializeField] private bool enableFunnelAnalysis = true;
        
        // Analytics data storage
        private Queue<AnalyticsEvent> eventQueue = new Queue<AnalyticsEvent>();
        private Dictionary<string, object> sessionData = new Dictionary<string, object>();
        private Dictionary<string, int> eventCounters = new Dictionary<string, int>();
        
        // Performance metrics
        private float sessionStartTime;
        private int totalGatesTriggered = 0;
        private int totalCrowdMultiplications = 0;
        private float totalPlayTime = 0f;
        private List<float> fpsHistory = new List<float>();
        
        // Business metrics
        private Dictionary<string, float> revenueTracking = new Dictionary<string, float>();
        private Dictionary<string, DateTime> cohortData = new Dictionary<string, DateTime>();
        private List<FunnelStep> userFunnel = new List<FunnelStep>();
        
        // Events
        public event Action<string, Dictionary<string, object>> OnEventTracked;
        public event Action<PerformanceMetrics> OnPerformanceUpdated;
        
        private void Start()
        {
            InitializeAnalytics();
            StartSession();
        }
        
        private async void InitializeAnalytics()
        {
            if (!enableAnalytics) return;
            
            try
            {
                // Initialize Unity Services
                await UnityServices.InitializeAsync();
                
                // Initialize Unity Analytics
                if (AnalyticsService.Instance != null)
                {
                    await AnalyticsService.Instance.CheckForRequiredConsents();
                }
                
                if (enableDebugLogging)
                {
                    Debug.Log("Analytics initialized successfully");
                }
                
                // Start performance monitoring
                if (trackPerformanceMetrics)
                {
                    InvokeRepeating(nameof(TrackPerformanceMetrics), 1f, performanceTrackingInterval);
                }
                
                // Start batch upload
                InvokeRepeating(nameof(ProcessEventQueue), batchUploadInterval, batchUploadInterval);
                
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to initialize analytics: {e.Message}");
            }
        }
        
        private void StartSession()
        {
            sessionStartTime = Time.time;
            
            sessionData = new Dictionary<string, object>
            {
                { "session_id", Guid.NewGuid().ToString() },
                { "device_model", SystemInfo.deviceModel },
                { "device_type", SystemInfo.deviceType.ToString() },
                { "operating_system", SystemInfo.operatingSystem },
                { "graphics_device", SystemInfo.graphicsDeviceName },
                { "memory_size", SystemInfo.systemMemorySize },
                { "processor_type", SystemInfo.processorType },
                { "app_version", Application.version },
                { "unity_version", Application.unityVersion },
                { "screen_resolution", $"{Screen.width}x{Screen.height}" },
                { "session_start_time", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ") }
            };
            
            TrackEvent("session_start", sessionData);
            
            // Initialize user funnel
            InitializeFunnel();
        }
        
        public void TrackEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            if (!enableAnalytics) return;
            
            if (parameters == null)
            {
                parameters = new Dictionary<string, object>();
            }
            
            // Add standard parameters
            parameters["timestamp"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
            parameters["session_time"] = Time.time - sessionStartTime;
            parameters["level"] = GameManager.Instance?.CurrentLevel ?? 0;
            parameters["crowd_size"] = FindObjectOfType<CrowdController>()?.GetCrowdSize() ?? 0;
            
            // Create analytics event
            var analyticsEvent = new AnalyticsEvent
            {
                EventName = eventName,
                Parameters = parameters,
                Timestamp = DateTime.UtcNow
            };
            
            // Queue for batch processing
            eventQueue.Enqueue(analyticsEvent);
            
            // Update counters
            if (eventCounters.ContainsKey(eventName))
            {
                eventCounters[eventName]++;
            }
            else
            {
                eventCounters[eventName] = 1;
            }
            
            // Immediate processing for critical events
            if (IsCriticalEvent(eventName))
            {
                ProcessEventImmediate(analyticsEvent);
            }
            
            // Debug logging
            if (enableDebugLogging)
            {
                Debug.Log($"Analytics Event: {eventName} with {parameters.Count} parameters");
            }
            
            // Trigger event for external listeners
            OnEventTracked?.Invoke(eventName, parameters);
            
            // Update funnel tracking
            UpdateFunnelTracking(eventName, parameters);
        }
        
        private void ProcessEventQueue()
        {
            if (eventQueue.Count == 0) return;
            
            List<AnalyticsEvent> batchEvents = new List<AnalyticsEvent>();
            int batchSize = Mathf.Min(100, eventQueue.Count); // Process max 100 events per batch
            
            for (int i = 0; i < batchSize; i++)
            {
                batchEvents.Add(eventQueue.Dequeue());
            }
            
            // Send to Unity Analytics
            foreach (var analyticsEvent in batchEvents)
            {
                try
                {
                    AnalyticsService.Instance?.CustomData(analyticsEvent.EventName, analyticsEvent.Parameters);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to send analytics event: {e.Message}");
                }
            }
            
            // Send to custom endpoint if configured
            if (!string.IsNullOrEmpty(customEndpoint))
            {
                SendToCustomEndpoint(batchEvents);
            }
        }
        
        private void ProcessEventImmediate(AnalyticsEvent analyticsEvent)
        {
            try
            {
                AnalyticsService.Instance?.CustomData(analyticsEvent.EventName, analyticsEvent.Parameters);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to send immediate analytics event: {e.Message}");
            }
        }
        
        private bool IsCriticalEvent(string eventName)
        {
            return eventName == "game_crash" || 
                   eventName == "purchase_completed" || 
                   eventName == "level_failed" ||
                   eventName == "session_end";
        }
        
        private void TrackPerformanceMetrics()
        {
            float currentFPS = 1f / Time.unscaledDeltaTime;
            fpsHistory.Add(currentFPS);
            
            // Keep only last 100 FPS readings
            if (fpsHistory.Count > 100)
            {
                fpsHistory.RemoveAt(0);
            }
            
            var performanceMetrics = new PerformanceMetrics
            {
                CurrentFPS = currentFPS,
                AverageFPS = CalculateAverageFPS(),
                MemoryUsage = GC.GetTotalMemory(false) / (1024 * 1024), // MB
                FrameTime = Time.deltaTime * 1000f, // ms
                GraphicsMemory = Profiler.GetAllocatedMemoryForGraphicsDriver() / (1024 * 1024) // MB
            };
            
            // Track performance event
            TrackEvent("performance_metrics", new Dictionary<string, object>
            {
                { "fps", performanceMetrics.CurrentFPS },
                { "avg_fps", performanceMetrics.AverageFPS },
                { "memory_mb", performanceMetrics.MemoryUsage },
                { "frame_time_ms", performanceMetrics.FrameTime },
                { "graphics_memory_mb", performanceMetrics.GraphicsMemory },
                { "crowd_count", FindObjectOfType<CrowdController>()?.GetCrowdSize() ?? 0 }
            });
            
            OnPerformanceUpdated?.Invoke(performanceMetrics);
        }
        
        private float CalculateAverageFPS()
        {
            if (fpsHistory.Count == 0) return 0f;
            
            float sum = 0f;
            foreach (float fps in fpsHistory)
            {
                sum += fps;
            }
            return sum / fpsHistory.Count;
        }
        
        public void TrackRevenue(string productId, float amount, string currency = "USD")
        {
            var revenueParams = new Dictionary<string, object>
            {
                { "product_id", productId },
                { "amount", amount },
                { "currency", currency },
                { "total_spent", GetTotalRevenue() + amount }
            };
            
            TrackEvent("revenue", revenueParams);
            
            // Update revenue tracking
            if (revenueTracking.ContainsKey(currency))
            {
                revenueTracking[currency] += amount;
            }
            else
            {
                revenueTracking[currency] = amount;
            }
        }
        
        public void TrackLevelEvent(string eventType, int level, Dictionary<string, object> additionalParams = null)
        {
            var levelParams = new Dictionary<string, object>
            {
                { "event_type", eventType },
                { "level", level },
                { "total_play_time", Time.time - sessionStartTime },
                { "gates_triggered", totalGatesTriggered },
                { "crowd_multiplications", totalCrowdMultiplications }
            };
            
            if (additionalParams != null)
            {
                foreach (var param in additionalParams)
                {
                    levelParams[param.Key] = param.Value;
                }
            }
            
            TrackEvent($"level_{eventType}", levelParams);
        }
        
        public void TrackUserProgression(string milestone, Dictionary<string, object> progressData = null)
        {
            var progressParams = new Dictionary<string, object>
            {
                { "milestone", milestone },
                { "user_level", GameManager.Instance?.UserLevel ?? 1 },
                { "total_sessions", GetSessionCount() },
                { "days_since_install", GetDaysSinceInstall() }
            };
            
            if (progressData != null)
            {
                foreach (var data in progressData)
                {
                    progressParams[data.Key] = data.Value;
                }
            }
            
            TrackEvent("user_progression", progressParams);
        }
        
        private void InitializeFunnel()
        {
            userFunnel = new List<FunnelStep>
            {
                new FunnelStep("game_start", false),
                new FunnelStep("first_gate", false),
                new FunnelStep("level_complete", false),
                new FunnelStep("first_purchase", false),
                new FunnelStep("retention_day_1", false),
                new FunnelStep("retention_day_7", false)
            };
        }
        
        private void UpdateFunnelTracking(string eventName, Dictionary<string, object> parameters)
        {
            if (!enableFunnelAnalysis) return;
            
            foreach (var step in userFunnel)
            {
                if (step.StepName == eventName && !step.Completed)
                {
                    step.Completed = true;
                    step.CompletionTime = DateTime.UtcNow;
                    
                    TrackEvent("funnel_step_completed", new Dictionary<string, object>
                    {
                        { "step_name", step.StepName },
                        { "completion_time", step.CompletionTime.ToString() },
                        { "session_time_to_complete", Time.time - sessionStartTime }
                    });
                    break;
                }
            }
        }
        
        private async void SendToCustomEndpoint(List<AnalyticsEvent> events)
        {
            try
            {
                // Implementation for custom analytics endpoint
                // This would typically involve HTTP requests to your custom server
                var jsonData = JsonUtility.ToJson(new { events = events });
                
                // Example using UnityWebRequest (implement as needed)
                using (var request = UnityEngine.Networking.UnityWebRequest.Post(customEndpoint, jsonData))
                {
                    request.SetRequestHeader("Content-Type", "application/json");
                    await request.SendWebRequest();
                    
                    if (request.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"Failed to send to custom endpoint: {request.error}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Custom endpoint error: {e.Message}");
            }
        }
        
        public Dictionary<string, object> GetSessionSummary()
        {
            return new Dictionary<string, object>
            {
                { "session_duration", Time.time - sessionStartTime },
                { "events_tracked", eventCounters.Count },
                { "total_events", eventCounters.Values.Sum() },
                { "average_fps", CalculateAverageFPS() },
                { "total_revenue", GetTotalRevenue() },
                { "funnel_completion", GetFunnelCompletionRate() }
            };
        }
        
        private float GetTotalRevenue()
        {
            float total = 0f;
            foreach (var revenue in revenueTracking.Values)
            {
                total += revenue;
            }
            return total;
        }
        
        private int GetSessionCount()
        {
            return PlayerPrefs.GetInt("SessionCount", 1);
        }
        
        private int GetDaysSinceInstall()
        {
            string installDate = PlayerPrefs.GetString("InstallDate", DateTime.UtcNow.ToString());
            DateTime install = DateTime.Parse(installDate);
            return (int)(DateTime.UtcNow - install).TotalDays;
        }
        
        private float GetFunnelCompletionRate()
        {
            int completed = userFunnel.Count(step => step.Completed);
            return (float)completed / userFunnel.Count * 100f;
        }
        
        private void OnDestroy()
        {
            // Send final session data
            TrackEvent("session_end", GetSessionSummary());
            ProcessEventQueue(); // Final batch
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                TrackEvent("app_paused", new Dictionary<string, object>
                {
                    { "session_time", Time.time - sessionStartTime }
                });
            }
            else
            {
                TrackEvent("app_resumed", new Dictionary<string, object>
                {
                    { "session_time", Time.time - sessionStartTime }
                });
            }
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                TrackEvent("app_lost_focus");
            }
            else
            {
                TrackEvent("app_gained_focus");
            }
        }
    }
    
    [Serializable]
    public class AnalyticsEvent
    {
        public string EventName;
        public Dictionary<string, object> Parameters;
        public DateTime Timestamp;
    }
    
    [Serializable]
    public class PerformanceMetrics
    {
        public float CurrentFPS;
        public float AverageFPS;
        public long MemoryUsage;
        public float FrameTime;
        public long GraphicsMemory;
    }
    
    [Serializable]
    public class FunnelStep
    {
        public string StepName;
        public bool Completed;
        public DateTime CompletionTime;
        
        public FunnelStep(string stepName, bool completed)
        {
            StepName = stepName;
            Completed = completed;
        }
    }
}

// Extension methods for LINQ functionality
namespace System.Linq
{
    public static class LinqExtensions
    {
        public static int Sum(this IEnumerable<int> source)
        {
            int sum = 0;
            foreach (int value in source)
            {
                sum += value;
            }
            return sum;
        }
        
        public static int Count<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            int count = 0;
            foreach (T item in source)
            {
                if (predicate(item))
                {
                    count++;
                }
            }
            return count;
        }
    }
}
