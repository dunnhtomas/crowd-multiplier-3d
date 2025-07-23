using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace CrowdMultiplier.ABTesting
{
    /// <summary>
    /// Enterprise A/B testing framework for data-driven optimization
    /// Features cohort management, statistical analysis, and automated variant selection
    /// </summary>
    public class ABTestingFramework : MonoBehaviour
    {
        [Header("A/B Testing Configuration")]
        [SerializeField] private bool enableABTesting = true;
        [SerializeField] private string userId = "";
        [SerializeField] private float testDuration = 7f; // days
        [SerializeField] private int minimumSampleSize = 100;
        
        [Header("Statistical Settings")]
        [SerializeField] private float confidenceLevel = 0.95f;
        [SerializeField] private float minimumDetectableEffect = 0.05f; // 5%
        [SerializeField] private bool enableAutoPromote = true;
        [SerializeField] private float autoPromoteThreshold = 0.9f;
        
        [Header("Test Management")]
        [SerializeField] private List<ABTest> activeTests = new List<ABTest>();
        [SerializeField] private bool enableRemoteConfig = true;
        [SerializeField] private string remoteConfigEndpoint = "";
        
        // Internal state
        private Dictionary<string, ABTestVariant> userAssignments = new Dictionary<string, ABTestVariant>();
        private Dictionary<string, ABTestResults> testResults = new Dictionary<string, ABTestResults>();
        private Dictionary<string, List<ABTestEvent>> testEvents = new Dictionary<string, List<ABTestEvent>>();
        
        // Component references
        private Core.AnalyticsManager analyticsManager;
        private Core.GameManager gameManager;
        
        // Events
        public event Action<string, ABTestVariant> OnVariantAssigned;
        public event Action<string, ABTestResults> OnTestCompleted;
        public event Action<string, string> OnTestPromoted;
        
        private void Start()
        {
            InitializeABTesting();
        }
        
        private void InitializeABTesting()
        {
            if (!enableABTesting) return;
            
            // Get component references
            analyticsManager = FindObjectOfType<Core.AnalyticsManager>();
            gameManager = Core.GameManager.Instance;
            
            // Generate or load user ID
            InitializeUserId();
            
            // Load user assignments from storage
            LoadUserAssignments();
            
            // Setup default tests
            SetupDefaultTests();
            
            // Start monitoring
            InvokeRepeating(nameof(AnalyzeTests), 60f, 300f); // Every 5 minutes
            
            Debug.Log($"A/B Testing Framework initialized for user: {userId}");
        }
        
        private void InitializeUserId()
        {
            if (string.IsNullOrEmpty(userId))
            {
                userId = PlayerPrefs.GetString("ABTestUserId", "");
                
                if (string.IsNullOrEmpty(userId))
                {
                    userId = Guid.NewGuid().ToString();
                    PlayerPrefs.SetString("ABTestUserId", userId);
                    PlayerPrefs.Save();
                }
            }
        }
        
        private void LoadUserAssignments()
        {
            string assignmentsJson = PlayerPrefs.GetString("ABTestAssignments", "{}");
            try
            {
                // In a real implementation, you'd use a proper JSON library
                // For now, we'll initialize empty and populate during runtime
                userAssignments = new Dictionary<string, ABTestVariant>();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load A/B test assignments: {e.Message}");
                userAssignments = new Dictionary<string, ABTestVariant>();
            }
        }
        
        private void SetupDefaultTests()
        {
            // Gate Multiplier Test
            var gateMultiplierTest = new ABTest
            {
                TestId = "gate_multiplier_values",
                TestName = "Gate Multiplier Values",
                Description = "Test different multiplier values for gates",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(testDuration),
                IsActive = true,
                TrafficSplit = 0.5f, // 50% of users
                Variants = new List<ABTestVariant>
                {
                    new ABTestVariant 
                    { 
                        VariantId = "control", 
                        VariantName = "Control",
                        TrafficAllocation = 0.5f,
                        Parameters = new Dictionary<string, object> 
                        { 
                            { "max_multiplier", 5 },
                            { "min_multiplier", 2 }
                        }
                    },
                    new ABTestVariant 
                    { 
                        VariantId = "higher_multipliers", 
                        VariantName = "Higher Multipliers",
                        TrafficAllocation = 0.5f,
                        Parameters = new Dictionary<string, object> 
                        { 
                            { "max_multiplier", 8 },
                            { "min_multiplier", 3 }
                        }
                    }
                }
            };
            
            // UI Color Theme Test
            var uiColorTest = new ABTest
            {
                TestId = "ui_color_theme",
                TestName = "UI Color Theme",
                Description = "Test different UI color schemes",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(testDuration),
                IsActive = true,
                TrafficSplit = 0.3f, // 30% of users
                Variants = new List<ABTestVariant>
                {
                    new ABTestVariant 
                    { 
                        VariantId = "blue_theme", 
                        VariantName = "Blue Theme",
                        TrafficAllocation = 0.5f,
                        Parameters = new Dictionary<string, object> 
                        { 
                            { "primary_color", "#2196F3" },
                            { "secondary_color", "#03DAC6" }
                        }
                    },
                    new ABTestVariant 
                    { 
                        VariantId = "purple_theme", 
                        VariantName = "Purple Theme",
                        TrafficAllocation = 0.5f,
                        Parameters = new Dictionary<string, object> 
                        { 
                            { "primary_color", "#9C27B0" },
                            { "secondary_color", "#FF5722" }
                        }
                    }
                }
            };
            
            // Level Difficulty Test
            var difficultyTest = new ABTest
            {
                TestId = "level_difficulty",
                TestName = "Level Difficulty Curve",
                Description = "Test different difficulty progression curves",
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(testDuration),
                IsActive = true,
                TrafficSplit = 0.4f, // 40% of users
                Variants = new List<ABTestVariant>
                {
                    new ABTestVariant 
                    { 
                        VariantId = "gradual_increase", 
                        VariantName = "Gradual Increase",
                        TrafficAllocation = 0.5f,
                        Parameters = new Dictionary<string, object> 
                        { 
                            { "difficulty_scale", 1.1f },
                            { "enemy_spawn_rate", 0.2f }
                        }
                    },
                    new ABTestVariant 
                    { 
                        VariantId = "steep_increase", 
                        VariantName = "Steep Increase",
                        TrafficAllocation = 0.5f,
                        Parameters = new Dictionary<string, object> 
                        { 
                            { "difficulty_scale", 1.3f },
                            { "enemy_spawn_rate", 0.35f }
                        }
                    }
                }
            };
            
            activeTests.Add(gateMultiplierTest);
            activeTests.Add(uiColorTest);
            activeTests.Add(difficultyTest);
            
            // Assign users to variants
            foreach (var test in activeTests)
            {
                AssignUserToVariant(test);
            }
        }
        
        public ABTestVariant AssignUserToVariant(ABTest test)
        {
            if (!enableABTesting || test == null || !test.IsActive) return null;
            
            // Check if user is already assigned to this test
            string assignmentKey = $"{test.TestId}_{userId}";
            if (userAssignments.ContainsKey(assignmentKey))
            {
                return userAssignments[assignmentKey];
            }
            
            // Check if user should be included in this test
            float userHash = GetUserHash(userId, test.TestId);
            if (userHash > test.TrafficSplit)
            {
                return null; // User not in test
            }
            
            // Assign user to variant based on traffic allocation
            float cumulativeAllocation = 0f;
            foreach (var variant in test.Variants)
            {
                cumulativeAllocation += variant.TrafficAllocation;
                if (userHash <= cumulativeAllocation * test.TrafficSplit)
                {
                    userAssignments[assignmentKey] = variant;
                    SaveUserAssignments();
                    
                    // Track assignment
                    TrackVariantAssignment(test, variant);
                    
                    OnVariantAssigned?.Invoke(test.TestId, variant);
                    return variant;
                }
            }
            
            // Fallback to first variant
            var fallbackVariant = test.Variants.FirstOrDefault();
            if (fallbackVariant != null)
            {
                userAssignments[assignmentKey] = fallbackVariant;
                SaveUserAssignments();
                OnVariantAssigned?.Invoke(test.TestId, fallbackVariant);
            }
            
            return fallbackVariant;
        }
        
        public T GetVariantParameter<T>(string testId, string parameterName, T defaultValue)
        {
            var variant = GetUserVariant(testId);
            if (variant?.Parameters != null && variant.Parameters.ContainsKey(parameterName))
            {
                try
                {
                    return (T)Convert.ChangeType(variant.Parameters[parameterName], typeof(T));
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to convert parameter {parameterName}: {e.Message}");
                }
            }
            
            return defaultValue;
        }
        
        public ABTestVariant GetUserVariant(string testId)
        {
            string assignmentKey = $"{testId}_{userId}";
            return userAssignments.ContainsKey(assignmentKey) ? userAssignments[assignmentKey] : null;
        }
        
        public void TrackConversion(string testId, string conversionType, float value = 1f)
        {
            if (!enableABTesting) return;
            
            var variant = GetUserVariant(testId);
            if (variant == null) return;
            
            var conversionEvent = new ABTestEvent
            {
                TestId = testId,
                VariantId = variant.VariantId,
                EventType = "conversion",
                ConversionType = conversionType,
                Value = value,
                Timestamp = DateTime.UtcNow,
                UserId = userId
            };
            
            if (!testEvents.ContainsKey(testId))
            {
                testEvents[testId] = new List<ABTestEvent>();
            }
            
            testEvents[testId].Add(conversionEvent);
            
            // Track in analytics
            if (analyticsManager != null)
            {
                analyticsManager.TrackEvent("ab_test_conversion", new Dictionary<string, object>
                {
                    { "test_id", testId },
                    { "variant_id", variant.VariantId },
                    { "conversion_type", conversionType },
                    { "value", value },
                    { "user_id", userId }
                });
            }
        }
        
        public void TrackMetric(string testId, string metricName, float value)
        {
            if (!enableABTesting) return;
            
            var variant = GetUserVariant(testId);
            if (variant == null) return;
            
            var metricEvent = new ABTestEvent
            {
                TestId = testId,
                VariantId = variant.VariantId,
                EventType = "metric",
                MetricName = metricName,
                Value = value,
                Timestamp = DateTime.UtcNow,
                UserId = userId
            };
            
            if (!testEvents.ContainsKey(testId))
            {
                testEvents[testId] = new List<ABTestEvent>();
            }
            
            testEvents[testId].Add(metricEvent);
            
            // Track in analytics
            if (analyticsManager != null)
            {
                analyticsManager.TrackEvent("ab_test_metric", new Dictionary<string, object>
                {
                    { "test_id", testId },
                    { "variant_id", variant.VariantId },
                    { "metric_name", metricName },
                    { "value", value },
                    { "user_id", userId }
                });
            }
        }
        
        private void AnalyzeTests()
        {
            foreach (var test in activeTests.Where(t => t.IsActive))
            {
                var results = CalculateTestResults(test);
                testResults[test.TestId] = results;
                
                // Check if test should be completed
                if (ShouldCompleteTest(test, results))
                {
                    CompleteTest(test, results);
                }
                
                // Check for auto-promotion
                if (enableAutoPromote && ShouldAutoPromote(results))
                {
                    PromoteWinningVariant(test, results);
                }
            }
        }
        
        private ABTestResults CalculateTestResults(ABTest test)
        {
            var results = new ABTestResults
            {
                TestId = test.TestId,
                AnalysisDate = DateTime.UtcNow,
                VariantResults = new List<ABTestVariantResults>()
            };
            
            if (!testEvents.ContainsKey(test.TestId))
            {
                return results;
            }
            
            var events = testEvents[test.TestId];
            
            foreach (var variant in test.Variants)
            {
                var variantEvents = events.Where(e => e.VariantId == variant.VariantId).ToList();
                
                var variantResult = new ABTestVariantResults
                {
                    VariantId = variant.VariantId,
                    VariantName = variant.VariantName,
                    SampleSize = variantEvents.Select(e => e.UserId).Distinct().Count(),
                    Conversions = variantEvents.Count(e => e.EventType == "conversion"),
                    TotalValue = variantEvents.Where(e => e.EventType == "conversion").Sum(e => e.Value),
                    Metrics = new Dictionary<string, float>()
                };
                
                // Calculate conversion rate
                if (variantResult.SampleSize > 0)
                {
                    variantResult.ConversionRate = (float)variantResult.Conversions / variantResult.SampleSize;
                    variantResult.AverageValue = variantResult.TotalValue / variantResult.SampleSize;
                }
                
                // Calculate metrics
                var metricGroups = variantEvents.Where(e => e.EventType == "metric")
                                                .GroupBy(e => e.MetricName);
                
                foreach (var metricGroup in metricGroups)
                {
                    variantResult.Metrics[metricGroup.Key] = metricGroup.Average(e => e.Value);
                }
                
                results.VariantResults.Add(variantResult);
            }
            
            // Calculate statistical significance
            if (results.VariantResults.Count == 2)
            {
                results.StatisticalSignificance = CalculateStatisticalSignificance(
                    results.VariantResults[0], results.VariantResults[1]);
                results.IsSignificant = results.StatisticalSignificance < (1 - confidenceLevel);
            }
            
            // Determine winner
            results.WinningVariant = DetermineWinningVariant(results.VariantResults);
            
            return results;
        }
        
        private float CalculateStatisticalSignificance(ABTestVariantResults control, ABTestVariantResults treatment)
        {
            // Simplified statistical significance calculation
            // In production, use proper statistical libraries
            
            if (control.SampleSize < minimumSampleSize || treatment.SampleSize < minimumSampleSize)
            {
                return 1f; // Not significant due to small sample size
            }
            
            float p1 = control.ConversionRate;
            float p2 = treatment.ConversionRate;
            int n1 = control.SampleSize;
            int n2 = treatment.SampleSize;
            
            if (p1 == 0 && p2 == 0) return 1f;
            
            float pooledP = (control.Conversions + treatment.Conversions) / (float)(n1 + n2);
            float se = Mathf.Sqrt(pooledP * (1 - pooledP) * (1f/n1 + 1f/n2));
            
            if (se == 0) return 1f;
            
            float zScore = Mathf.Abs(p2 - p1) / se;
            
            // Approximate p-value (simplified)
            return Mathf.Exp(-0.717f * zScore - 0.416f * zScore * zScore);
        }
        
        private string DetermineWinningVariant(List<ABTestVariantResults> variantResults)
        {
            if (variantResults.Count < 2) return null;
            
            // Find variant with highest conversion rate
            var winner = variantResults.OrderByDescending(v => v.ConversionRate).First();
            
            // Check if improvement is meaningful
            var control = variantResults.OrderBy(v => v.ConversionRate).First();
            float improvement = (winner.ConversionRate - control.ConversionRate) / control.ConversionRate;
            
            return improvement >= minimumDetectableEffect ? winner.VariantId : null;
        }
        
        private bool ShouldCompleteTest(ABTest test, ABTestResults results)
        {
            // Test duration exceeded
            if (DateTime.UtcNow > test.EndDate) return true;
            
            // Sufficient sample size and statistical significance
            if (results.IsSignificant && 
                results.VariantResults.All(v => v.SampleSize >= minimumSampleSize))
            {
                return true;
            }
            
            return false;
        }
        
        private bool ShouldAutoPromote(ABTestResults results)
        {
            return results.IsSignificant && 
                   results.StatisticalSignificance < (1 - autoPromoteThreshold) &&
                   !string.IsNullOrEmpty(results.WinningVariant);
        }
        
        private void CompleteTest(ABTest test, ABTestResults results)
        {
            test.IsActive = false;
            test.CompletionDate = DateTime.UtcNow;
            
            OnTestCompleted?.Invoke(test.TestId, results);
            
            // Track test completion
            if (analyticsManager != null)
            {
                analyticsManager.TrackEvent("ab_test_completed", new Dictionary<string, object>
                {
                    { "test_id", test.TestId },
                    { "duration_days", (DateTime.UtcNow - test.StartDate).TotalDays },
                    { "winning_variant", results.WinningVariant ?? "none" },
                    { "is_significant", results.IsSignificant },
                    { "statistical_significance", results.StatisticalSignificance }
                });
            }
            
            Debug.Log($"A/B Test '{test.TestName}' completed. Winner: {results.WinningVariant ?? "No clear winner"}");
        }
        
        private void PromoteWinningVariant(ABTest test, ABTestResults results)
        {
            if (string.IsNullOrEmpty(results.WinningVariant)) return;
            
            OnTestPromoted?.Invoke(test.TestId, results.WinningVariant);
            
            // Track promotion
            if (analyticsManager != null)
            {
                analyticsManager.TrackEvent("ab_test_promoted", new Dictionary<string, object>
                {
                    { "test_id", test.TestId },
                    { "winning_variant", results.WinningVariant },
                    { "improvement", CalculateImprovement(results) }
                });
            }
            
            Debug.Log($"Auto-promoted winning variant '{results.WinningVariant}' for test '{test.TestName}'");
        }
        
        private float CalculateImprovement(ABTestResults results)
        {
            if (results.VariantResults.Count < 2) return 0f;
            
            var sortedResults = results.VariantResults.OrderBy(v => v.ConversionRate).ToList();
            var control = sortedResults.First();
            var winner = sortedResults.Last();
            
            if (control.ConversionRate == 0) return 0f;
            
            return (winner.ConversionRate - control.ConversionRate) / control.ConversionRate;
        }
        
        private void TrackVariantAssignment(ABTest test, ABTestVariant variant)
        {
            if (analyticsManager != null)
            {
                analyticsManager.TrackEvent("ab_test_assignment", new Dictionary<string, object>
                {
                    { "test_id", test.TestId },
                    { "variant_id", variant.VariantId },
                    { "user_id", userId },
                    { "assignment_date", DateTime.UtcNow.ToString() }
                });
            }
        }
        
        private float GetUserHash(string userId, string testId)
        {
            // Create deterministic hash for consistent assignment
            string combined = userId + testId;
            int hash = combined.GetHashCode();
            
            // Convert to 0-1 range
            return (float)((uint)hash) / uint.MaxValue;
        }
        
        private void SaveUserAssignments()
        {
            try
            {
                // In production, use proper JSON serialization
                // For now, just save to PlayerPrefs
                PlayerPrefs.SetString("ABTestAssignments", JsonUtility.ToJson(userAssignments));
                PlayerPrefs.Save();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save A/B test assignments: {e.Message}");
            }
        }
        
        // Public API
        public List<ABTest> GetActiveTests()
        {
            return activeTests.Where(t => t.IsActive).ToList();
        }
        
        public ABTestResults GetTestResults(string testId)
        {
            return testResults.ContainsKey(testId) ? testResults[testId] : null;
        }
        
        public void StartTest(ABTest test)
        {
            if (test != null)
            {
                test.IsActive = true;
                test.StartDate = DateTime.UtcNow;
                
                if (!activeTests.Contains(test))
                {
                    activeTests.Add(test);
                }
                
                AssignUserToVariant(test);
            }
        }
        
        public void StopTest(string testId)
        {
            var test = activeTests.FirstOrDefault(t => t.TestId == testId);
            if (test != null)
            {
                test.IsActive = false;
                test.CompletionDate = DateTime.UtcNow;
            }
        }
    }
    
    [System.Serializable]
    public class ABTest
    {
        public string TestId;
        public string TestName;
        public string Description;
        public DateTime StartDate;
        public DateTime EndDate;
        public DateTime? CompletionDate;
        public bool IsActive;
        public float TrafficSplit; // 0-1, percentage of users to include
        public List<ABTestVariant> Variants;
    }
    
    [System.Serializable]
    public class ABTestVariant
    {
        public string VariantId;
        public string VariantName;
        public float TrafficAllocation; // 0-1, split within test participants
        public Dictionary<string, object> Parameters;
    }
    
    [System.Serializable]
    public class ABTestEvent
    {
        public string TestId;
        public string VariantId;
        public string UserId;
        public string EventType; // "conversion", "metric"
        public string ConversionType;
        public string MetricName;
        public float Value;
        public DateTime Timestamp;
    }
    
    [System.Serializable]
    public class ABTestResults
    {
        public string TestId;
        public DateTime AnalysisDate;
        public List<ABTestVariantResults> VariantResults;
        public float StatisticalSignificance;
        public bool IsSignificant;
        public string WinningVariant;
    }
    
    [System.Serializable]
    public class ABTestVariantResults
    {
        public string VariantId;
        public string VariantName;
        public int SampleSize;
        public int Conversions;
        public float ConversionRate;
        public float TotalValue;
        public float AverageValue;
        public Dictionary<string, float> Metrics;
    }
}
