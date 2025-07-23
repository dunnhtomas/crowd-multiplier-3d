using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace CrowdMultiplier.Build
{
    /// <summary>
    /// Enterprise build system with Unity Cloud Build integration and deployment automation
    /// Handles platform-specific builds, asset optimization, and CI/CD pipeline integration
    /// </summary>
    public class BuildManager : MonoBehaviour
    {
        [Header("Build Configuration")]
        [SerializeField] private BuildTarget primaryBuildTarget = BuildTarget.Android;
        [SerializeField] private BuildTarget[] supportedPlatforms = { BuildTarget.Android, BuildTarget.iOS };
        [SerializeField] private bool enableCloudBuild = true;
        [SerializeField] private bool enableAutomatedTesting = true;
        
        [Header("Mobile Optimization")]
        [SerializeField] private bool enableTextureCompression = true;
        [SerializeField] private bool enableAudioCompression = true;
        [SerializeField] private bool enableAssetBundling = false;
        [SerializeField] private bool enableIL2CPP = true;
        [SerializeField] private int targetFrameRate = 60;
        
        [Header("Performance Settings")]
        [SerializeField] private int batchingThreshold = 100;
        [SerializeField] private bool enableGPUInstancing = true;
        [SerializeField] private bool enableDynamicBatching = true;
        [SerializeField] private bool enableStaticBatching = true;
        [SerializeField] private QualityLevel targetQualityLevel = QualityLevel.High;
        
        [Header("Security & Analytics")]
        [SerializeField] private bool enableObfuscation = true;
        [SerializeField] private bool enableCrashReporting = true;
        [SerializeField] private bool enableTelemetry = true;
        [SerializeField] private string buildVersion = "1.0.0";
        
        private static BuildManager instance;
        public static BuildManager Instance => instance;
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeBuildSystem();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeBuildSystem()
        {
            // Configure runtime settings based on platform
            ConfigurePlatformSettings();
            OptimizeRuntimePerformance();
            SetupAnalyticsIntegration();
        }
        
        private void ConfigurePlatformSettings()
        {
            Application.targetFrameRate = targetFrameRate;
            
            #if UNITY_ANDROID
            ConfigureAndroidSettings();
            #elif UNITY_IOS
            ConfigureiOSSettings();
            #endif
            
            // Universal settings
            QualitySettings.vSyncCount = 0; // Disable VSync for mobile
            Screen.sleepTimeout = SleepTimeout.NeverSleep; // Prevent sleep during gameplay
        }
        
        #if UNITY_ANDROID
        private void ConfigureAndroidSettings()
        {
            // Android-specific optimizations
            if (enableIL2CPP)
            {
                // IL2CPP provides better performance on Android
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            }
            
            // Target API levels
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel31;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            
            // Performance settings
            PlayerSettings.Android.blitType = AndroidBlitType.Always;
            PlayerSettings.Android.startInFullscreen = true;
            
            // Security
            PlayerSettings.Android.useAPKExpansionFiles = false;
            PlayerSettings.Android.keystoreName = ""; // Set in CI/CD
        }
        #endif
        
        #if UNITY_IOS
        private void ConfigureiOSSettings()
        {
            // iOS-specific optimizations
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS, ScriptingImplementation.IL2CPP);
            
            // Target iOS versions
            PlayerSettings.iOS.targetOSVersionString = "12.0";
            
            // Performance settings
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
            PlayerSettings.iOS.hideHomeButton = true;
            
            // Metal rendering
            PlayerSettings.SetGraphicsAPIs(BuildTarget.iOS, new UnityEngine.Rendering.GraphicsDeviceType[] { 
                UnityEngine.Rendering.GraphicsDeviceType.Metal 
            });
        }
        #endif
        
        private void OptimizeRuntimePerformance()
        {
            // Batching settings
            if (enableDynamicBatching)
            {
                PlayerSettings.gpuSkinning = true;
            }
            
            // Quality settings based on device
            SetQualityBasedOnDevice();
            
            // Texture settings
            if (enableTextureCompression)
            {
                OptimizeTextureSettings();
            }
            
            // Audio settings
            if (enableAudioCompression)
            {
                OptimizeAudioSettings();
            }
        }
        
        private void SetQualityBasedOnDevice()
        {
            int memorySize = SystemInfo.systemMemorySize;
            int graphicsMemory = SystemInfo.graphicsMemorySize;
            string deviceModel = SystemInfo.deviceModel.ToLower();
            
            QualityLevel targetLevel = QualityLevel.Medium;
            
            // High-end devices
            if (memorySize >= 8192 && graphicsMemory >= 2048)
            {
                targetLevel = QualityLevel.Ultra;
            }
            // Mid-range devices
            else if (memorySize >= 4096 && graphicsMemory >= 1024)
            {
                targetLevel = QualityLevel.High;
            }
            // Low-end devices
            else if (memorySize >= 2048)
            {
                targetLevel = QualityLevel.Medium;
            }
            else
            {
                targetLevel = QualityLevel.Low;
            }
            
            // Device-specific overrides
            if (deviceModel.Contains("iphone") && deviceModel.Contains("pro"))
            {
                targetLevel = QualityLevel.Ultra;
            }
            else if (deviceModel.Contains("samsung") && deviceModel.Contains("s2"))
            {
                targetLevel = QualityLevel.Ultra;
            }
            
            SetQualityLevel(targetLevel);
        }
        
        private void SetQualityLevel(QualityLevel level)
        {
            int qualityIndex = (int)level;
            QualitySettings.SetQualityLevel(qualityIndex, true);
            
            // Adjust specific settings based on level
            switch (level)
            {
                case QualityLevel.Low:
                    QualitySettings.pixelLightCount = 1;
                    QualitySettings.shadowResolution = ShadowResolution.Low;
                    QualitySettings.shadowDistance = 20f;
                    QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
                    break;
                    
                case QualityLevel.Medium:
                    QualitySettings.pixelLightCount = 2;
                    QualitySettings.shadowResolution = ShadowResolution.Medium;
                    QualitySettings.shadowDistance = 50f;
                    QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
                    break;
                    
                case QualityLevel.High:
                    QualitySettings.pixelLightCount = 4;
                    QualitySettings.shadowResolution = ShadowResolution.High;
                    QualitySettings.shadowDistance = 100f;
                    QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
                    break;
                    
                case QualityLevel.Ultra:
                    QualitySettings.pixelLightCount = 8;
                    QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
                    QualitySettings.shadowDistance = 150f;
                    QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
                    break;
            }
            
            // Track quality change
            TrackQualityChange(level);
        }
        
        private void OptimizeTextureSettings()
        {
            // This would typically be done at build time, but we can set runtime compression
            Texture.streamingMipmapsActive = true;
            Texture.streamingMipmapsAddAllCameras = true;
            Texture.streamingMipmapsMaxLevelReduction = 2;
        }
        
        private void OptimizeAudioSettings()
        {
            // Audio optimization settings
            AudioSettings.GetConfiguration(out AudioConfiguration config);
            
            // Adjust based on device capability
            if (SystemInfo.systemMemorySize < 2048)
            {
                config.sampleRate = 22050; // Lower sample rate for low-end devices
            }
            else
            {
                config.sampleRate = 44100; // Standard sample rate
            }
            
            AudioSettings.Reset(config);
        }
        
        private void SetupAnalyticsIntegration()
        {
            if (!enableTelemetry) return;
            
            // Setup build-specific analytics
            var buildData = new Dictionary<string, object>
            {
                { "build_version", buildVersion },
                { "build_target", Application.platform.ToString() },
                { "device_model", SystemInfo.deviceModel },
                { "operating_system", SystemInfo.operatingSystem },
                { "graphics_device", SystemInfo.graphicsDeviceName },
                { "memory_size", SystemInfo.systemMemorySize },
                { "quality_level", QualitySettings.GetQualityLevel() },
                { "unity_version", Application.unityVersion }
            };
            
            // Send to analytics
            var analyticsManager = FindObjectOfType<Core.AnalyticsManager>();
            analyticsManager?.TrackEvent("build_started", buildData);
        }
        
        private void TrackQualityChange(QualityLevel newLevel)
        {
            var analyticsManager = FindObjectOfType<Core.AnalyticsManager>();
            analyticsManager?.TrackEvent("quality_changed", new Dictionary<string, object>
            {
                { "new_quality", newLevel.ToString() },
                { "device_memory", SystemInfo.systemMemorySize },
                { "graphics_memory", SystemInfo.graphicsMemorySize },
                { "device_model", SystemInfo.deviceModel }
            });
        }
        
        // Runtime build information
        public BuildInfo GetCurrentBuildInfo()
        {
            return new BuildInfo
            {
                Version = buildVersion,
                Platform = Application.platform.ToString(),
                BuildDate = GetBuildDate(),
                QualityLevel = (QualityLevel)QualitySettings.GetQualityLevel(),
                TargetFrameRate = Application.targetFrameRate,
                DeviceInfo = GetDeviceInfo()
            };
        }
        
        private string GetBuildDate()
        {
            // Get build date from build pipeline or use current date
            return System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
        
        private DeviceInfo GetDeviceInfo()
        {
            return new DeviceInfo
            {
                Model = SystemInfo.deviceModel,
                OperatingSystem = SystemInfo.operatingSystem,
                ProcessorType = SystemInfo.processorType,
                MemorySize = SystemInfo.systemMemorySize,
                GraphicsDeviceName = SystemInfo.graphicsDeviceName,
                GraphicsMemorySize = SystemInfo.graphicsMemorySize,
                SupportsVibration = SystemInfo.supportsVibration,
                SupportsLocationService = SystemInfo.supportsLocationService
            };
        }
        
        // Performance monitoring
        public PerformanceMetrics GetPerformanceMetrics()
        {
            return new PerformanceMetrics
            {
                CurrentFPS = 1f / Time.unscaledDeltaTime,
                FrameTime = Time.deltaTime * 1000f,
                MemoryUsage = System.GC.GetTotalMemory(false) / (1024 * 1024),
                DrawCalls = UnityEngine.Profiling.Profiler.GetStatValue(UnityEngine.Profiling.ProfilerArea.Rendering, UnityEngine.Profiling.ProfilerStatisticsValues.DrawCalls),
                Triangles = UnityEngine.Profiling.Profiler.GetStatValue(UnityEngine.Profiling.ProfilerArea.Rendering, UnityEngine.Profiling.ProfilerStatisticsValues.Triangles),
                Vertices = UnityEngine.Profiling.Profiler.GetStatValue(UnityEngine.Profiling.ProfilerArea.Rendering, UnityEngine.Profiling.ProfilerStatisticsValues.Vertices)
            };
        }
        
        // Quality adjustment methods
        public void AdjustQualityForPerformance(float targetFPS = 60f)
        {
            float currentFPS = 1f / Time.unscaledDeltaTime;
            
            if (currentFPS < targetFPS * 0.8f) // If FPS is below 80% of target
            {
                int currentQuality = QualitySettings.GetQualityLevel();
                if (currentQuality > 0)
                {
                    SetQualityLevel((QualityLevel)(currentQuality - 1));
                }
            }
            else if (currentFPS > targetFPS * 1.2f) // If FPS is above 120% of target
            {
                int currentQuality = QualitySettings.GetQualityLevel();
                if (currentQuality < System.Enum.GetValues(typeof(QualityLevel)).Length - 1)
                {
                    SetQualityLevel((QualityLevel)(currentQuality + 1));
                }
            }
        }
        
        private void Start()
        {
            // Start performance monitoring
            if (enableTelemetry)
            {
                InvokeRepeating(nameof(MonitorPerformance), 5f, 10f);
            }
        }
        
        private void MonitorPerformance()
        {
            var metrics = GetPerformanceMetrics();
            
            // Auto-adjust quality if needed
            AdjustQualityForPerformance();
            
            // Track performance metrics
            var analyticsManager = FindObjectOfType<Core.AnalyticsManager>();
            analyticsManager?.TrackEvent("performance_monitoring", new Dictionary<string, object>
            {
                { "fps", metrics.CurrentFPS },
                { "frame_time", metrics.FrameTime },
                { "memory_usage", metrics.MemoryUsage },
                { "draw_calls", metrics.DrawCalls },
                { "triangles", metrics.Triangles },
                { "quality_level", QualitySettings.GetQualityLevel() }
            });
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // Optimize for background
                Application.targetFrameRate = 10;
            }
            else
            {
                // Restore normal framerate
                Application.targetFrameRate = targetFrameRate;
            }
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                // Reduce performance when not focused
                Application.targetFrameRate = 30;
            }
            else
            {
                // Restore full performance
                Application.targetFrameRate = targetFrameRate;
            }
        }
    }
    
    [System.Serializable]
    public class BuildInfo
    {
        public string Version;
        public string Platform;
        public string BuildDate;
        public QualityLevel QualityLevel;
        public int TargetFrameRate;
        public DeviceInfo DeviceInfo;
    }
    
    [System.Serializable]
    public class DeviceInfo
    {
        public string Model;
        public string OperatingSystem;
        public string ProcessorType;
        public int MemorySize;
        public string GraphicsDeviceName;
        public int GraphicsMemorySize;
        public bool SupportsVibration;
        public bool SupportsLocationService;
    }
    
    [System.Serializable]
    public class PerformanceMetrics
    {
        public float CurrentFPS;
        public float FrameTime;
        public long MemoryUsage;
        public uint DrawCalls;
        public uint Triangles;
        public uint Vertices;
    }
    
    public enum QualityLevel
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Ultra = 3
    }
}
