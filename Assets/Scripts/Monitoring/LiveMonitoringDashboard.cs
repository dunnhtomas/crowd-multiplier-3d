using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CrowdMultiplier.Monitoring
{
    /// <summary>
    /// Live monitoring dashboard for real-time analytics and performance tracking
    /// Features enterprise-grade metrics visualization and alert system
    /// </summary>
    public class LiveMonitoringDashboard : MonoBehaviour
    {
        [Header("Dashboard UI")]
        [SerializeField] private Canvas dashboardCanvas;
        [SerializeField] private GameObject dashboardPanel;
        [SerializeField] private Button toggleDashboardButton;
        [SerializeField] private bool showOnStart = false;
        
        [Header("Performance Metrics")]
        [SerializeField] private TextMeshProUGUI fpsText;
        [SerializeField] private TextMeshProUGUI memoryText;
        [SerializeField] private TextMeshProUGUI crowdSizeText;
        [SerializeField] private TextMeshProUGUI drawCallsText;
        [SerializeField] private Slider performanceSlider;
        
        [Header("Analytics Metrics")]
        [SerializeField] private TextMeshProUGUI sessionTimeText;
        [SerializeField] private TextMeshProUGUI eventsTrackedText;
        [SerializeField] private TextMeshProUGUI userLevelText;
        [SerializeField] private TextMeshProUGUI revenueText;
        
        [Header("Game Metrics")]
        [SerializeField] private TextMeshProUGUI currentLevelText;
        [SerializeField] private TextMeshProUGUI gatesTriggeredText;
        [SerializeField] private TextMeshProUGUI multiplicationsText;
        [SerializeField] private TextMeshProUGUI scoreText;
        
        [Header("System Status")]
        [SerializeField] private Image systemStatusIndicator;
        [SerializeField] private TextMeshProUGUI systemStatusText;
        [SerializeField] private Color goodStatus = Color.green;
        [SerializeField] private Color warningStatus = Color.yellow;
        [SerializeField] private Color criticalStatus = Color.red;
        
        [Header("Alert System")]
        [SerializeField] private GameObject alertPrefab;
        [SerializeField] private Transform alertContainer;
        [SerializeField] private bool enableAlerts = true;
        [SerializeField] private float alertDisplayDuration = 5f;
        
        [Header("Data Visualization")]
        [SerializeField] private LineRenderer fpsChart;
        [SerializeField] private LineRenderer memoryChart;
        [SerializeField] private int maxDataPoints = 100;
        [SerializeField] private float chartUpdateInterval = 1f;
        
        // Internal data
        private bool isDashboardVisible = false;
        private List<float> fpsHistory = new List<float>();
        private List<float> memoryHistory = new List<float>();
        private List<MonitoringAlert> activeAlerts = new List<MonitoringAlert>();
        
        // Thresholds for alerts
        private float lowFPSThreshold = 30f;
        private float highMemoryThreshold = 1024f; // MB
        private float criticalMemoryThreshold = 2048f; // MB
        
        // Component references
        private Core.AnalyticsManager analyticsManager;
        private Core.GameManager gameManager;
        private Core.CrowdController crowdController;
        private Build.BuildManager buildManager;
        
        private void Start()
        {
            InitializeDashboard();
            SetupComponentReferences();
            StartMonitoring();
        }
        
        private void InitializeDashboard()
        {
            if (dashboardPanel != null)
            {
                dashboardPanel.SetActive(showOnStart);
                isDashboardVisible = showOnStart;
            }
            
            if (toggleDashboardButton != null)
            {
                toggleDashboardButton.onClick.AddListener(ToggleDashboard);
            }
            
            // Initialize charts
            InitializeCharts();
            
            // Setup keyboard shortcut for toggle (Ctrl + M)
            StartCoroutine(KeyboardShortcutHandler());
        }
        
        private void SetupComponentReferences()
        {
            analyticsManager = FindObjectOfType<Core.AnalyticsManager>();
            gameManager = Core.GameManager.Instance;
            crowdController = FindObjectOfType<Core.CrowdController>();
            buildManager = FindObjectOfType<Build.BuildManager>();
            
            // Subscribe to analytics events
            if (analyticsManager != null)
            {
                analyticsManager.OnPerformanceUpdated += OnPerformanceUpdated;
                analyticsManager.OnEventTracked += OnEventTracked;
            }
        }
        
        private void StartMonitoring()
        {
            // Start regular monitoring updates
            InvokeRepeating(nameof(UpdateDashboard), 0.5f, 0.5f);
            InvokeRepeating(nameof(UpdateCharts), 1f, chartUpdateInterval);
            InvokeRepeating(nameof(CheckAlerts), 2f, 5f);
        }
        
        private void InitializeCharts()
        {
            if (fpsChart != null)
            {
                fpsChart.positionCount = 0;
                fpsChart.color = Color.green;
                fpsChart.startWidth = 0.02f;
                fpsChart.endWidth = 0.02f;
            }
            
            if (memoryChart != null)
            {
                memoryChart.positionCount = 0;
                memoryChart.color = Color.blue;
                memoryChart.startWidth = 0.02f;
                memoryChart.endWidth = 0.02f;
            }
        }
        
        private IEnumerator KeyboardShortcutHandler()
        {
            while (true)
            {
                if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.M))
                {
                    ToggleDashboard();
                }
                yield return null;
            }
        }
        
        public void ToggleDashboard()
        {
            isDashboardVisible = !isDashboardVisible;
            
            if (dashboardPanel != null)
            {
                dashboardPanel.SetActive(isDashboardVisible);
            }
            
            // Track dashboard usage
            if (analyticsManager != null)
            {
                analyticsManager.TrackEvent("monitoring_dashboard_toggled", new Dictionary<string, object>
                {
                    { "visible", isDashboardVisible },
                    { "timestamp", System.DateTime.UtcNow.ToString() }
                });
            }
        }
        
        private void UpdateDashboard()
        {
            if (!isDashboardVisible) return;
            
            UpdatePerformanceMetrics();
            UpdateAnalyticsMetrics();
            UpdateGameMetrics();
            UpdateSystemStatus();
        }
        
        private void UpdatePerformanceMetrics()
        {
            // FPS
            float currentFPS = 1f / Time.unscaledDeltaTime;
            if (fpsText != null)
            {
                fpsText.text = $"FPS: {currentFPS:F1}";
                fpsText.color = GetFPSColor(currentFPS);
            }
            
            // Memory
            long memoryUsage = System.GC.GetTotalMemory(false) / (1024 * 1024);
            if (memoryText != null)
            {
                memoryText.text = $"Memory: {memoryUsage}MB";
                memoryText.color = GetMemoryColor(memoryUsage);
            }
            
            // Crowd size
            int crowdSize = crowdController?.GetCrowdSize() ?? 0;
            if (crowdSizeText != null)
            {
                crowdSizeText.text = $"Crowd: {crowdSize}";
            }
            
            // Draw calls (approximation)
            if (drawCallsText != null)
            {
                drawCallsText.text = $"Draw Calls: {GetEstimatedDrawCalls()}";
            }
            
            // Performance slider (0-100 scale)
            if (performanceSlider != null)
            {
                float performanceScore = CalculatePerformanceScore(currentFPS, memoryUsage);
                performanceSlider.value = performanceScore / 100f;
            }
        }
        
        private void UpdateAnalyticsMetrics()
        {
            if (analyticsManager == null) return;
            
            var sessionSummary = analyticsManager.GetSessionSummary();
            
            // Session time
            if (sessionTimeText != null && sessionSummary.ContainsKey("session_duration"))
            {
                float sessionTime = (float)sessionSummary["session_duration"];
                sessionTimeText.text = $"Session: {FormatTime(sessionTime)}";
            }
            
            // Events tracked
            if (eventsTrackedText != null && sessionSummary.ContainsKey("total_events"))
            {
                int totalEvents = (int)sessionSummary["total_events"];
                eventsTrackedText.text = $"Events: {totalEvents}";
            }
            
            // Revenue
            if (revenueText != null && sessionSummary.ContainsKey("total_revenue"))
            {
                float totalRevenue = (float)sessionSummary["total_revenue"];
                revenueText.text = $"Revenue: ${totalRevenue:F2}";
            }
        }
        
        private void UpdateGameMetrics()
        {
            if (gameManager == null) return;
            
            // Current level
            if (currentLevelText != null)
            {
                currentLevelText.text = $"Level: {gameManager.CurrentLevel}";
            }
            
            // User level
            if (userLevelText != null)
            {
                userLevelText.text = $"User Lvl: {gameManager.UserLevel}";
            }
            
            // Score
            if (scoreText != null)
            {
                scoreText.text = $"Score: {gameManager.CurrentScore:N0}";
            }
            
            // Gates and multiplications would need to be tracked by the respective managers
            // For now, using placeholder values
            if (gatesTriggeredText != null)
            {
                gatesTriggeredText.text = "Gates: --";
            }
            
            if (multiplicationsText != null)
            {
                multiplicationsText.text = "Mult: --";
            }
        }
        
        private void UpdateSystemStatus()
        {
            float currentFPS = 1f / Time.unscaledDeltaTime;
            long memoryUsage = System.GC.GetTotalMemory(false) / (1024 * 1024);
            
            SystemStatus status = DetermineSystemStatus(currentFPS, memoryUsage);
            
            if (systemStatusIndicator != null)
            {
                systemStatusIndicator.color = GetStatusColor(status);
            }
            
            if (systemStatusText != null)
            {
                systemStatusText.text = status.ToString().ToUpper();
                systemStatusText.color = GetStatusColor(status);
            }
        }
        
        private void UpdateCharts()
        {
            if (!isDashboardVisible) return;
            
            // Update FPS chart
            float currentFPS = 1f / Time.unscaledDeltaTime;
            fpsHistory.Add(currentFPS);
            if (fpsHistory.Count > maxDataPoints)
            {
                fpsHistory.RemoveAt(0);
            }
            UpdateLineChart(fpsChart, fpsHistory, 0f, 120f);
            
            // Update memory chart
            long memoryUsage = System.GC.GetTotalMemory(false) / (1024 * 1024);
            memoryHistory.Add(memoryUsage);
            if (memoryHistory.Count > maxDataPoints)
            {
                memoryHistory.RemoveAt(0);
            }
            UpdateLineChart(memoryChart, memoryHistory, 0f, 2048f);
        }
        
        private void UpdateLineChart(LineRenderer chart, List<float> data, float minValue, float maxValue)
        {
            if (chart == null || data.Count == 0) return;
            
            chart.positionCount = data.Count;
            
            for (int i = 0; i < data.Count; i++)
            {
                float x = (float)i / (maxDataPoints - 1) * 2f - 1f; // -1 to 1
                float y = Mathf.InverseLerp(minValue, maxValue, data[i]) * 2f - 1f; // -1 to 1
                chart.SetPosition(i, new Vector3(x, y, 0));
            }
        }
        
        private void CheckAlerts()
        {
            if (!enableAlerts) return;
            
            float currentFPS = 1f / Time.unscaledDeltaTime;
            long memoryUsage = System.GC.GetTotalMemory(false) / (1024 * 1024);
            
            // FPS alert
            if (currentFPS < lowFPSThreshold)
            {
                TriggerAlert($"Low FPS: {currentFPS:F1}", AlertType.Warning);
            }
            
            // Memory alerts
            if (memoryUsage > criticalMemoryThreshold)
            {
                TriggerAlert($"Critical Memory: {memoryUsage}MB", AlertType.Critical);
            }
            else if (memoryUsage > highMemoryThreshold)
            {
                TriggerAlert($"High Memory: {memoryUsage}MB", AlertType.Warning);
            }
            
            // Clean up expired alerts
            CleanupExpiredAlerts();
        }
        
        private void TriggerAlert(string message, AlertType type)
        {
            // Check if similar alert already exists
            if (activeAlerts.Any(alert => alert.Message == message && !alert.IsExpired))
            {
                return;
            }
            
            var newAlert = new MonitoringAlert
            {
                Message = message,
                Type = type,
                Timestamp = System.DateTime.UtcNow,
                Duration = alertDisplayDuration
            };
            
            activeAlerts.Add(newAlert);
            DisplayAlert(newAlert);
            
            // Track alert
            if (analyticsManager != null)
            {
                analyticsManager.TrackEvent("monitoring_alert", new Dictionary<string, object>
                {
                    { "message", message },
                    { "type", type.ToString() },
                    { "timestamp", newAlert.Timestamp.ToString() }
                });
            }
        }
        
        private void DisplayAlert(MonitoringAlert alert)
        {
            if (alertPrefab == null || alertContainer == null) return;
            
            GameObject alertObj = Instantiate(alertPrefab, alertContainer);
            var alertText = alertObj.GetComponentInChildren<TextMeshProUGUI>();
            var alertImage = alertObj.GetComponent<Image>();
            
            if (alertText != null)
            {
                alertText.text = $"[{alert.Type}] {alert.Message}";
            }
            
            if (alertImage != null)
            {
                alertImage.color = GetAlertColor(alert.Type);
            }
            
            // Auto-destroy after duration
            Destroy(alertObj, alertDisplayDuration);
        }
        
        private void CleanupExpiredAlerts()
        {
            activeAlerts.RemoveAll(alert => alert.IsExpired);
        }
        
        // Event handlers
        private void OnPerformanceUpdated(Core.PerformanceMetrics metrics)
        {
            // Additional performance tracking if needed
        }
        
        private void OnEventTracked(string eventName, Dictionary<string, object> parameters)
        {
            // Track specific events of interest
            if (eventName == "level_complete" || eventName == "game_over")
            {
                // Could trigger special monitoring events
            }
        }
        
        // Utility methods
        private Color GetFPSColor(float fps)
        {
            if (fps >= 50f) return Color.green;
            if (fps >= 30f) return Color.yellow;
            return Color.red;
        }
        
        private Color GetMemoryColor(long memoryMB)
        {
            if (memoryMB < 512) return Color.green;
            if (memoryMB < 1024) return Color.yellow;
            return Color.red;
        }
        
        private Color GetStatusColor(SystemStatus status)
        {
            switch (status)
            {
                case SystemStatus.Good: return goodStatus;
                case SystemStatus.Warning: return warningStatus;
                case SystemStatus.Critical: return criticalStatus;
                default: return Color.white;
            }
        }
        
        private Color GetAlertColor(AlertType type)
        {
            switch (type)
            {
                case AlertType.Info: return Color.blue;
                case AlertType.Warning: return Color.yellow;
                case AlertType.Critical: return Color.red;
                default: return Color.white;
            }
        }
        
        private SystemStatus DetermineSystemStatus(float fps, long memoryMB)
        {
            if (fps < lowFPSThreshold || memoryMB > criticalMemoryThreshold)
            {
                return SystemStatus.Critical;
            }
            
            if (fps < 45f || memoryMB > highMemoryThreshold)
            {
                return SystemStatus.Warning;
            }
            
            return SystemStatus.Good;
        }
        
        private float CalculatePerformanceScore(float fps, long memoryMB)
        {
            float fpsScore = Mathf.Clamp01(fps / 60f) * 50f;
            float memoryScore = Mathf.Clamp01(1f - (memoryMB / 2048f)) * 50f;
            return fpsScore + memoryScore;
        }
        
        private int GetEstimatedDrawCalls()
        {
            // Estimate based on crowd size and scene complexity
            int crowdSize = crowdController?.GetCrowdSize() ?? 0;
            int baseDrawCalls = 10; // UI and environment
            int crowdDrawCalls = Mathf.CeilToInt(crowdSize / 50f); // Batched rendering
            return baseDrawCalls + crowdDrawCalls;
        }
        
        private string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            return $"{minutes:00}:{secs:00}";
        }
        
        // Public API
        public void SetAlertThreshold(string metric, float value)
        {
            switch (metric.ToLower())
            {
                case "fps":
                    lowFPSThreshold = value;
                    break;
                case "memory":
                    highMemoryThreshold = value;
                    break;
                case "critical_memory":
                    criticalMemoryThreshold = value;
                    break;
            }
        }
        
        public Dictionary<string, object> GetCurrentMetrics()
        {
            return new Dictionary<string, object>
            {
                { "fps", 1f / Time.unscaledDeltaTime },
                { "memory_mb", System.GC.GetTotalMemory(false) / (1024 * 1024) },
                { "crowd_size", crowdController?.GetCrowdSize() ?? 0 },
                { "system_status", DetermineSystemStatus(1f / Time.unscaledDeltaTime, System.GC.GetTotalMemory(false) / (1024 * 1024)).ToString() }
            };
        }
        
        private void OnDestroy()
        {
            // Cleanup event subscriptions
            if (analyticsManager != null)
            {
                analyticsManager.OnPerformanceUpdated -= OnPerformanceUpdated;
                analyticsManager.OnEventTracked -= OnEventTracked;
            }
        }
    }
    
    [System.Serializable]
    public class MonitoringAlert
    {
        public string Message;
        public AlertType Type;
        public System.DateTime Timestamp;
        public float Duration;
        
        public bool IsExpired => (System.DateTime.UtcNow - Timestamp).TotalSeconds > Duration;
    }
    
    public enum SystemStatus
    {
        Good,
        Warning,
        Critical
    }
    
    public enum AlertType
    {
        Info,
        Warning,
        Critical
    }
}
