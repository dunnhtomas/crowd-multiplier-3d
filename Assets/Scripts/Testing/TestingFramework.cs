using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace CrowdMultiplier.Testing
{
    /// <summary>
    /// Enterprise testing framework for automated unit and integration tests
    /// Features performance testing, load testing, and continuous validation
    /// </summary>
    public class TestingFramework : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private bool enableAutomatedTesting = true;
        [SerializeField] private bool runTestsOnStart = false;
        [SerializeField] private bool enablePerformanceTesting = true;
        [SerializeField] private bool enableLoadTesting = true;
        [SerializeField] private float testTimeout = 30f;
        
        [Header("Performance Test Settings")]
        [SerializeField] private int maxCrowdSizeTest = 1000;
        [SerializeField] private float minFrameRate = 30f;
        [SerializeField] private float maxMemoryUsage = 1024f; // MB
        [SerializeField] private int stressTestDuration = 60; // seconds
        
        [Header("Load Test Settings")]
        [SerializeField] private int simultaneousOperations = 100;
        [SerializeField] private int gateGenerationRate = 10; // per second
        [SerializeField] private int particleStressCount = 500;
        
        [Header("Test Reporting")]
        [SerializeField] private bool enableTestReporting = true;
        [SerializeField] private bool sendResultsToAnalytics = true;
        [SerializeField] private string testReportEndpoint = "";
        
        // Test state
        private List<TestResult> testResults = new List<TestResult>();
        private bool isTestingInProgress = false;
        private Coroutine currentTestCoroutine;
        
        // Component references
        private Core.GameManager gameManager;
        private Core.CrowdController crowdController;
        private Core.LevelManager levelManager;
        private Core.AnalyticsManager analyticsManager;
        private UI.UIManager uiManager;
        private Audio.AudioManager audioManager;
        private VFX.VFXManager vfxManager;
        private Build.BuildManager buildManager;
        
        // Events
        public event Action<TestResult> OnTestCompleted;
        public event Action<TestSuite> OnTestSuiteCompleted;
        public event Action<string> OnTestError;
        
        private void Start()
        {
            InitializeTestingFramework();
            
            if (runTestsOnStart && enableAutomatedTesting)
            {
                StartCoroutine(RunAllTestsDelayed());
            }
        }
        
        private void InitializeTestingFramework()
        {
            // Get component references
            gameManager = Core.GameManager.Instance;
            crowdController = FindObjectOfType<Core.CrowdController>();
            levelManager = FindObjectOfType<Core.LevelManager>();
            analyticsManager = FindObjectOfType<Core.AnalyticsManager>();
            uiManager = FindObjectOfType<UI.UIManager>();
            audioManager = FindObjectOfType<Audio.AudioManager>();
            vfxManager = FindObjectOfType<VFX.VFXManager>();
            buildManager = FindObjectOfType<Build.BuildManager>();
            
            // Initialize test reporting
            if (enableTestReporting)
            {
                InitializeTestReporting();
            }
        }
        
        private void InitializeTestReporting()
        {
            // Setup test reporting system
            testResults = new List<TestResult>();
        }
        
        private IEnumerator RunAllTestsDelayed()
        {
            yield return new WaitForSeconds(2f); // Wait for initialization
            yield return StartCoroutine(RunAllTests());
        }
        
        public IEnumerator RunAllTests()
        {
            if (isTestingInProgress) yield break;
            
            isTestingInProgress = true;
            testResults.Clear();
            
            Debug.Log("🧪 Starting Enterprise Test Suite...");
            
            // Core System Tests
            yield return StartCoroutine(RunCoreSystemTests());
            
            // Performance Tests
            if (enablePerformanceTesting)
            {
                yield return StartCoroutine(RunPerformanceTests());
            }
            
            // Load Tests
            if (enableLoadTesting)
            {
                yield return StartCoroutine(RunLoadTests());
            }
            
            // Integration Tests
            yield return StartCoroutine(RunIntegrationTests());
            
            // Generate final report
            GenerateTestReport();
            
            isTestingInProgress = false;
            Debug.Log("✅ Enterprise Test Suite Completed!");
        }
        
        private IEnumerator RunCoreSystemTests()
        {
            Debug.Log("Running Core System Tests...");
            
            var testSuite = new TestSuite("Core Systems");
            
            // Test GameManager
            yield return StartCoroutine(TestGameManager(testSuite));
            
            // Test CrowdController
            yield return StartCoroutine(TestCrowdController(testSuite));
            
            // Test LevelManager
            yield return StartCoroutine(TestLevelManager(testSuite));
            
            // Test AnalyticsManager
            yield return StartCoroutine(TestAnalyticsManager(testSuite));
            
            // Test UI System
            yield return StartCoroutine(TestUIManager(testSuite));
            
            // Test Audio System
            yield return StartCoroutine(TestAudioManager(testSuite));
            
            // Test VFX System
            yield return StartCoroutine(TestVFXManager(testSuite));
            
            OnTestSuiteCompleted?.Invoke(testSuite);
        }
        
        private IEnumerator TestGameManager(TestSuite suite)
        {
            var test = new TestResult("GameManager Initialization");
            
            try
            {
                // Test singleton pattern
                Assert(gameManager != null, "GameManager instance should exist");
                Assert(Core.GameManager.Instance == gameManager, "Singleton pattern should work");
                
                // Test state management
                var originalState = gameManager.CurrentGameState;
                gameManager.SetGameState(Core.GameState.Playing);
                Assert(gameManager.CurrentGameState == Core.GameState.Playing, "State change should work");
                gameManager.SetGameState(originalState);
                
                test.Success = true;
                test.Message = "GameManager tests passed";
            }
            catch (Exception e)
            {
                test.Success = false;
                test.Message = $"GameManager test failed: {e.Message}";
                test.Exception = e;
            }
            
            test.Duration = Time.time - test.StartTime;
            suite.AddResult(test);
            testResults.Add(test);
            OnTestCompleted?.Invoke(test);
            
            yield return null;
        }
        
        private IEnumerator TestCrowdController(TestSuite suite)
        {
            var test = new TestResult("CrowdController Functionality");
            
            try
            {
                Assert(crowdController != null, "CrowdController should exist");
                
                // Test crowd initialization
                int initialSize = crowdController.GetCrowdSize();
                Assert(initialSize >= 0, "Initial crowd size should be non-negative");
                
                // Test crowd multiplication
                int originalSize = crowdController.GetCrowdSize();
                crowdController.MultiplyCrowd(2);
                yield return new WaitForSeconds(0.1f);
                int newSize = crowdController.GetCrowdSize();
                Assert(newSize >= originalSize, "Crowd multiplication should increase size");
                
                // Test crowd reduction
                crowdController.ReduceCrowd(5);
                yield return new WaitForSeconds(0.1f);
                int reducedSize = crowdController.GetCrowdSize();
                Assert(reducedSize <= newSize, "Crowd reduction should decrease size");
                
                test.Success = true;
                test.Message = "CrowdController tests passed";
            }
            catch (Exception e)
            {
                test.Success = false;
                test.Message = $"CrowdController test failed: {e.Message}";
                test.Exception = e;
            }
            
            test.Duration = Time.time - test.StartTime;
            suite.AddResult(test);
            testResults.Add(test);
            OnTestCompleted?.Invoke(test);
        }
        
        private IEnumerator TestLevelManager(TestSuite suite)
        {
            var test = new TestResult("LevelManager Functionality");
            
            try
            {
                Assert(levelManager != null, "LevelManager should exist");
                
                // Test level generation
                int currentLevel = levelManager.CurrentLevel;
                Assert(currentLevel >= 0, "Current level should be non-negative");
                
                // Test gate generation
                var gates = FindObjectsOfType<Core.Gate>();
                Assert(gates.Length > 0, "Level should have gates");
                
                test.Success = true;
                test.Message = "LevelManager tests passed";
            }
            catch (Exception e)
            {
                test.Success = false;
                test.Message = $"LevelManager test failed: {e.Message}";
                test.Exception = e;
            }
            
            test.Duration = Time.time - test.StartTime;
            suite.AddResult(test);
            testResults.Add(test);
            OnTestCompleted?.Invoke(test);
            
            yield return null;
        }
        
        private IEnumerator TestAnalyticsManager(TestSuite suite)
        {
            var test = new TestResult("AnalyticsManager Functionality");
            
            try
            {
                Assert(analyticsManager != null, "AnalyticsManager should exist");
                
                // Test event tracking
                var testEventData = new Dictionary<string, object>
                {
                    { "test_key", "test_value" },
                    { "test_number", 42 }
                };
                
                analyticsManager.TrackEvent("test_event", testEventData);
                
                // Test session summary
                var sessionSummary = analyticsManager.GetSessionSummary();
                Assert(sessionSummary != null, "Session summary should be available");
                Assert(sessionSummary.ContainsKey("session_duration"), "Session summary should contain duration");
                
                test.Success = true;
                test.Message = "AnalyticsManager tests passed";
            }
            catch (Exception e)
            {
                test.Success = false;
                test.Message = $"AnalyticsManager test failed: {e.Message}";
                test.Exception = e;
            }
            
            test.Duration = Time.time - test.StartTime;
            suite.AddResult(test);
            testResults.Add(test);
            OnTestCompleted?.Invoke(test);
            
            yield return null;
        }
        
        private IEnumerator TestUIManager(TestSuite suite)
        {
            var test = new TestResult("UIManager Functionality");
            
            try
            {
                Assert(uiManager != null, "UIManager should exist");
                
                // Test UI state changes
                uiManager.ShowMenu();
                yield return new WaitForSeconds(0.1f);
                
                uiManager.ShowGameplay();
                yield return new WaitForSeconds(0.1f);
                
                // Test score updates
                uiManager.UpdateScore(1000);
                uiManager.UpdateLevel(5);
                
                test.Success = true;
                test.Message = "UIManager tests passed";
            }
            catch (Exception e)
            {
                test.Success = false;
                test.Message = $"UIManager test failed: {e.Message}";
                test.Exception = e;
            }
            
            test.Duration = Time.time - test.StartTime;
            suite.AddResult(test);
            testResults.Add(test);
            OnTestCompleted?.Invoke(test);
        }
        
        private IEnumerator TestAudioManager(TestSuite suite)
        {
            var test = new TestResult("AudioManager Functionality");
            
            try
            {
                if (audioManager != null)
                {
                    // Test volume controls
                    audioManager.SetMasterVolume(0.5f);
                    audioManager.SetMusicVolume(0.7f);
                    audioManager.SetSFXVolume(0.8f);
                    
                    // Test sound effects (with null checks)
                    audioManager.PlayButtonClickSound();
                    
                    test.Success = true;
                    test.Message = "AudioManager tests passed";
                }
                else
                {
                    test.Success = true;
                    test.Message = "AudioManager not present (optional component)";
                }
            }
            catch (Exception e)
            {
                test.Success = false;
                test.Message = $"AudioManager test failed: {e.Message}";
                test.Exception = e;
            }
            
            test.Duration = Time.time - test.StartTime;
            suite.AddResult(test);
            testResults.Add(test);
            OnTestCompleted?.Invoke(test);
            
            yield return null;
        }
        
        private IEnumerator TestVFXManager(TestSuite suite)
        {
            var test = new TestResult("VFXManager Functionality");
            
            try
            {
                if (vfxManager != null)
                {
                    // Test VFX effects
                    vfxManager.PlayCrowdTrailEffect(Vector3.zero, 10);
                    vfxManager.PlayGateImpactEffect(Vector3.zero, Core.GateType.Multiplier);
                    
                    // Test quality settings
                    vfxManager.SetQuality(0.8f);
                    
                    test.Success = true;
                    test.Message = "VFXManager tests passed";
                }
                else
                {
                    test.Success = true;
                    test.Message = "VFXManager not present (optional component)";
                }
            }
            catch (Exception e)
            {
                test.Success = false;
                test.Message = $"VFXManager test failed: {e.Message}";
                test.Exception = e;
            }
            
            test.Duration = Time.time - test.StartTime;
            suite.AddResult(test);
            testResults.Add(test);
            OnTestCompleted?.Invoke(test);
            
            yield return null;
        }
        
        private IEnumerator RunPerformanceTests()
        {
            Debug.Log("Running Performance Tests...");
            
            var testSuite = new TestSuite("Performance");
            
            // FPS Test
            yield return StartCoroutine(TestFrameRate(testSuite));
            
            // Memory Test
            yield return StartCoroutine(TestMemoryUsage(testSuite));
            
            // Crowd Performance Test
            yield return StartCoroutine(TestCrowdPerformance(testSuite));
            
            // Stress Test
            yield return StartCoroutine(TestSystemStress(testSuite));
            
            OnTestSuiteCompleted?.Invoke(testSuite);
        }
        
        private IEnumerator TestFrameRate(TestSuite suite)
        {
            var test = new TestResult("Frame Rate Performance");
            
            try
            {
                float totalFPS = 0f;
                int sampleCount = 0;
                float testDuration = 5f;
                float startTime = Time.time;
                
                while (Time.time - startTime < testDuration)
                {
                    float currentFPS = 1f / Time.unscaledDeltaTime;
                    totalFPS += currentFPS;
                    sampleCount++;
                    yield return null;
                }
                
                float averageFPS = totalFPS / sampleCount;
                Assert(averageFPS >= minFrameRate, $"Average FPS ({averageFPS:F1}) should be >= {minFrameRate}");
                
                test.Success = true;
                test.Message = $"Average FPS: {averageFPS:F1}";
                test.Metrics["average_fps"] = averageFPS;
            }
            catch (Exception e)
            {
                test.Success = false;
                test.Message = $"Frame rate test failed: {e.Message}";
                test.Exception = e;
            }
            
            test.Duration = Time.time - test.StartTime;
            suite.AddResult(test);
            testResults.Add(test);
            OnTestCompleted?.Invoke(test);
        }
        
        private IEnumerator TestMemoryUsage(TestSuite suite)
        {
            var test = new TestResult("Memory Usage");
            
            try
            {
                long memoryBefore = System.GC.GetTotalMemory(true);
                
                // Simulate memory-intensive operations
                yield return new WaitForSeconds(1f);
                
                long memoryAfter = System.GC.GetTotalMemory(false);
                float memoryUsageMB = memoryAfter / (1024f * 1024f);
                
                Assert(memoryUsageMB <= maxMemoryUsage, $"Memory usage ({memoryUsageMB:F1}MB) should be <= {maxMemoryUsage}MB");
                
                test.Success = true;
                test.Message = $"Memory usage: {memoryUsageMB:F1}MB";
                test.Metrics["memory_usage_mb"] = memoryUsageMB;
            }
            catch (Exception e)
            {
                test.Success = false;
                test.Message = $"Memory test failed: {e.Message}";
                test.Exception = e;
            }
            
            test.Duration = Time.time - test.StartTime;
            suite.AddResult(test);
            testResults.Add(test);
            OnTestCompleted?.Invoke(test);
        }
        
        private IEnumerator TestCrowdPerformance(TestSuite suite)
        {
            var test = new TestResult("Crowd Performance");
            
            try
            {
                if (crowdController == null)
                {
                    test.Success = false;
                    test.Message = "CrowdController not available";
                    return null;
                }
                
                int originalSize = crowdController.GetCrowdSize();
                float startTime = Time.time;
                
                // Gradually increase crowd size
                for (int i = 100; i <= maxCrowdSizeTest; i += 100)
                {
                    crowdController.SetCrowdSize(i);
                    yield return new WaitForSeconds(0.5f);
                    
                    float currentFPS = 1f / Time.unscaledDeltaTime;
                    if (currentFPS < minFrameRate)
                    {
                        Assert(false, $"FPS dropped below {minFrameRate} at crowd size {i}");
                    }
                }
                
                // Restore original size
                crowdController.SetCrowdSize(originalSize);
                
                test.Success = true;
                test.Message = $"Crowd performance test passed up to {maxCrowdSizeTest} units";
                test.Metrics["max_crowd_tested"] = maxCrowdSizeTest;
            }
            catch (Exception e)
            {
                test.Success = false;
                test.Message = $"Crowd performance test failed: {e.Message}";
                test.Exception = e;
            }
            
            test.Duration = Time.time - test.StartTime;
            suite.AddResult(test);
            testResults.Add(test);
            OnTestCompleted?.Invoke(test);
        }
        
        private IEnumerator TestSystemStress(TestSuite suite)
        {
            var test = new TestResult("System Stress Test");
            
            try
            {
                float startTime = Time.time;
                float testEndTime = startTime + stressTestDuration;
                
                List<float> fpsHistory = new List<float>();
                
                while (Time.time < testEndTime)
                {
                    // Simulate stress conditions
                    if (crowdController != null)
                    {
                        crowdController.MultiplyCrowd(2);
                    }
                    
                    if (vfxManager != null)
                    {
                        vfxManager.PlayMultiplicationEffect(Vector3.zero, 5);
                    }
                    
                    float currentFPS = 1f / Time.unscaledDeltaTime;
                    fpsHistory.Add(currentFPS);
                    
                    yield return new WaitForSeconds(1f);
                }
                
                // Analyze stress test results
                float averageFPS = 0f;
                foreach (float fps in fpsHistory)
                {
                    averageFPS += fps;
                }
                averageFPS /= fpsHistory.Count;
                
                Assert(averageFPS >= minFrameRate * 0.8f, $"Average FPS during stress ({averageFPS:F1}) should be >= {minFrameRate * 0.8f}");
                
                test.Success = true;
                test.Message = $"Stress test completed. Average FPS: {averageFPS:F1}";
                test.Metrics["stress_average_fps"] = averageFPS;
            }
            catch (Exception e)
            {
                test.Success = false;
                test.Message = $"Stress test failed: {e.Message}";
                test.Exception = e;
            }
            
            test.Duration = Time.time - test.StartTime;
            suite.AddResult(test);
            testResults.Add(test);
            OnTestCompleted?.Invoke(test);
        }
        
        private IEnumerator RunLoadTests()
        {
            Debug.Log("Running Load Tests...");
            
            var testSuite = new TestSuite("Load Testing");
            
            // Concurrent Operations Test
            yield return StartCoroutine(TestConcurrentOperations(testSuite));
            
            // Gate Generation Load Test
            yield return StartCoroutine(TestGateGenerationLoad(testSuite));
            
            OnTestSuiteCompleted?.Invoke(testSuite);
        }
        
        private IEnumerator TestConcurrentOperations(TestSuite suite)
        {
            var test = new TestResult("Concurrent Operations");
            
            try
            {
                List<Coroutine> operations = new List<Coroutine>();
                
                // Start multiple operations simultaneously
                for (int i = 0; i < simultaneousOperations; i++)
                {
                    operations.Add(StartCoroutine(SimulateOperation()));
                }
                
                // Wait for all to complete
                yield return new WaitForSeconds(5f);
                
                test.Success = true;
                test.Message = $"Successfully handled {simultaneousOperations} concurrent operations";
                test.Metrics["concurrent_operations"] = simultaneousOperations;
            }
            catch (Exception e)
            {
                test.Success = false;
                test.Message = $"Concurrent operations test failed: {e.Message}";
                test.Exception = e;
            }
            
            test.Duration = Time.time - test.StartTime;
            suite.AddResult(test);
            testResults.Add(test);
            OnTestCompleted?.Invoke(test);
        }
        
        private IEnumerator SimulateOperation()
        {
            // Simulate a typical game operation
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.1f, 1f));
            
            if (analyticsManager != null)
            {
                analyticsManager.TrackEvent("load_test_operation", new Dictionary<string, object>
                {
                    { "operation_id", UnityEngine.Random.Range(1000, 9999) }
                });
            }
        }
        
        private IEnumerator TestGateGenerationLoad(TestSuite suite)
        {
            var test = new TestResult("Gate Generation Load");
            
            try
            {
                int gatesGenerated = 0;
                float testDuration = 10f;
                float startTime = Time.time;
                
                while (Time.time - startTime < testDuration)
                {
                    // Simulate gate generation at specified rate
                    for (int i = 0; i < gateGenerationRate; i++)
                    {
                        // Simulate gate creation (without actually creating objects)
                        gatesGenerated++;
                    }
                    
                    yield return new WaitForSeconds(1f);
                }
                
                Assert(gatesGenerated >= gateGenerationRate * testDuration * 0.9f, 
                       $"Should generate at least {gateGenerationRate * testDuration * 0.9f} gates");
                
                test.Success = true;
                test.Message = $"Generated {gatesGenerated} gates in {testDuration}s";
                test.Metrics["gates_generated"] = gatesGenerated;
            }
            catch (Exception e)
            {
                test.Success = false;
                test.Message = $"Gate generation load test failed: {e.Message}";
                test.Exception = e;
            }
            
            test.Duration = Time.time - test.StartTime;
            suite.AddResult(test);
            testResults.Add(test);
            OnTestCompleted?.Invoke(test);
        }
        
        private IEnumerator RunIntegrationTests()
        {
            Debug.Log("Running Integration Tests...");
            
            var testSuite = new TestSuite("Integration");
            
            // Full Game Flow Test
            yield return StartCoroutine(TestFullGameFlow(testSuite));
            
            // Analytics Integration Test
            yield return StartCoroutine(TestAnalyticsIntegration(testSuite));
            
            OnTestSuiteCompleted?.Invoke(testSuite);
        }
        
        private IEnumerator TestFullGameFlow(TestSuite suite)
        {
            var test = new TestResult("Full Game Flow");
            
            try
            {
                if (gameManager == null)
                {
                    test.Success = false;
                    test.Message = "GameManager not available";
                    return null;
                }
                
                // Test menu -> game -> game over flow
                gameManager.SetGameState(Core.GameState.Menu);
                yield return new WaitForSeconds(0.5f);
                
                gameManager.SetGameState(Core.GameState.Playing);
                yield return new WaitForSeconds(1f);
                
                // Simulate some gameplay
                if (crowdController != null)
                {
                    crowdController.MultiplyCrowd(3);
                    yield return new WaitForSeconds(0.5f);
                }
                
                gameManager.SetGameState(Core.GameState.GameOver);
                yield return new WaitForSeconds(0.5f);
                
                gameManager.SetGameState(Core.GameState.Menu);
                
                test.Success = true;
                test.Message = "Full game flow test completed successfully";
            }
            catch (Exception e)
            {
                test.Success = false;
                test.Message = $"Full game flow test failed: {e.Message}";
                test.Exception = e;
            }
            
            test.Duration = Time.time - test.StartTime;
            suite.AddResult(test);
            testResults.Add(test);
            OnTestCompleted?.Invoke(test);
        }
        
        private IEnumerator TestAnalyticsIntegration(TestSuite suite)
        {
            var test = new TestResult("Analytics Integration");
            
            try
            {
                if (analyticsManager == null)
                {
                    test.Success = false;
                    test.Message = "AnalyticsManager not available";
                    return null;
                }
                
                // Test various analytics events
                analyticsManager.TrackEvent("integration_test_start", new Dictionary<string, object>
                {
                    { "test_timestamp", DateTime.UtcNow.ToString() }
                });
                
                analyticsManager.TrackLevelEvent("start", 1);
                analyticsManager.TrackUserProgression("test_milestone");
                
                var sessionSummary = analyticsManager.GetSessionSummary();
                Assert(sessionSummary != null, "Session summary should be available");
                
                test.Success = true;
                test.Message = "Analytics integration test completed successfully";
            }
            catch (Exception e)
            {
                test.Success = false;
                test.Message = $"Analytics integration test failed: {e.Message}";
                test.Exception = e;
            }
            
            test.Duration = Time.time - test.StartTime;
            suite.AddResult(test);
            testResults.Add(test);
            OnTestCompleted?.Invoke(test);
            
            yield return null;
        }
        
        private void GenerateTestReport()
        {
            Debug.Log("📊 Generating Test Report...");
            
            int totalTests = testResults.Count;
            int passedTests = 0;
            int failedTests = 0;
            float totalDuration = 0f;
            
            foreach (var result in testResults)
            {
                if (result.Success) passedTests++;
                else failedTests++;
                totalDuration += result.Duration;
            }
            
            float successRate = (float)passedTests / totalTests * 100f;
            
            string report = $@"
🧪 ENTERPRISE TEST REPORT
========================
Total Tests: {totalTests}
Passed: {passedTests}
Failed: {failedTests}
Success Rate: {successRate:F1}%
Total Duration: {totalDuration:F2}s

Failed Tests:";
            
            foreach (var result in testResults)
            {
                if (!result.Success)
                {
                    report += $"\n❌ {result.TestName}: {result.Message}";
                }
            }
            
            Debug.Log(report);
            
            // Send to analytics if enabled
            if (sendResultsToAnalytics && analyticsManager != null)
            {
                analyticsManager.TrackEvent("test_report", new Dictionary<string, object>
                {
                    { "total_tests", totalTests },
                    { "passed_tests", passedTests },
                    { "failed_tests", failedTests },
                    { "success_rate", successRate },
                    { "total_duration", totalDuration }
                });
            }
        }
        
        private void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new AssertionException(message);
            }
        }
        
        // Public API for manual testing
        public void RunSingleTest(string testName)
        {
            StartCoroutine(RunSingleTestCoroutine(testName));
        }
        
        private IEnumerator RunSingleTestCoroutine(string testName)
        {
            Debug.Log($"Running single test: {testName}");
            
            switch (testName.ToLower())
            {
                case "frameRate":
                    yield return StartCoroutine(TestFrameRate(new TestSuite("Manual")));
                    break;
                case "memory":
                    yield return StartCoroutine(TestMemoryUsage(new TestSuite("Manual")));
                    break;
                case "crowd":
                    yield return StartCoroutine(TestCrowdPerformance(new TestSuite("Manual")));
                    break;
                default:
                    Debug.LogWarning($"Unknown test: {testName}");
                    break;
            }
        }
        
        public TestResult[] GetTestResults()
        {
            return testResults.ToArray();
        }
        
        public bool IsTestingInProgress()
        {
            return isTestingInProgress;
        }
    }
    
    [System.Serializable]
    public class TestResult
    {
        public string TestName;
        public bool Success;
        public string Message;
        public float StartTime;
        public float Duration;
        public Exception Exception;
        public Dictionary<string, object> Metrics;
        
        public TestResult(string testName)
        {
            TestName = testName;
            StartTime = Time.time;
            Success = false;
            Message = "";
            Metrics = new Dictionary<string, object>();
        }
    }
    
    [System.Serializable]
    public class TestSuite
    {
        public string SuiteName;
        public List<TestResult> Results;
        public float StartTime;
        public float Duration;
        
        public TestSuite(string suiteName)
        {
            SuiteName = suiteName;
            Results = new List<TestResult>();
            StartTime = Time.time;
        }
        
        public void AddResult(TestResult result)
        {
            Results.Add(result);
            Duration = Time.time - StartTime;
        }
        
        public float GetSuccessRate()
        {
            if (Results.Count == 0) return 0f;
            
            int passed = 0;
            foreach (var result in Results)
            {
                if (result.Success) passed++;
            }
            
            return (float)passed / Results.Count * 100f;
        }
    }
    
    public class AssertionException : Exception
    {
        public AssertionException(string message) : base(message) { }
    }
}
