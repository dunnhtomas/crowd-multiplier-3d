using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

namespace CrowdMultiplier.VFX
{
    /// <summary>
    /// Enterprise visual effects system with modern 2025 design trends
    /// Features procedural particle systems, screen-space effects, and dynamic lighting
    /// </summary>
    public class VFXManager : MonoBehaviour
    {
        [Header("Core VFX Systems")]
        [SerializeField] private ParticleSystem crowdTrailEffect;
        [SerializeField] private ParticleSystem gateImpactEffect;
        [SerializeField] private ParticleSystem multiplicationEffect;
        [SerializeField] private ParticleSystem enemyDestructionEffect;
        [SerializeField] private ParticleSystem levelCompleteEffect;
        [SerializeField] private ParticleSystem environmentEffect;
        
        [Header("Screen Effects")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private Volume postProcessVolume;
        [SerializeField] private bool enableScreenShake = true;
        [SerializeField] private bool enableChromaticAberration = true;
        [SerializeField] private bool enableBloom = true;
        
        [Header("Dynamic Lighting")]
        [SerializeField] private Light mainLight;
        [SerializeField] private Light dynamicLight;
        [SerializeField] private Gradient lightColorGradient;
        [SerializeField] private AnimationCurve lightIntensityCurve;
        [SerializeField] private bool enableDynamicLighting = true;
        
        [Header("2025 Visual Trends")]
        [SerializeField] private Material holographicMaterial;
        [SerializeField] private Material neonGlowMaterial;
        [SerializeField] private Color primaryGlowColor = new Color(0.2f, 0.8f, 1f, 1f);
        [SerializeField] private Color secondaryGlowColor = new Color(1f, 0.4f, 0.6f, 1f);
        [SerializeField] private bool enableHolographicEffects = true;
        [SerializeField] private bool enableNeonGlow = true;
        
        [Header("Performance Settings")]
        [SerializeField] private int maxParticles = 1000;
        [SerializeField] private float particleQualityScale = 1f;
        [SerializeField] private bool enableLOD = true;
        [SerializeField] private bool enableGPUInstancing = true;
        
        [Header("Object Pooling")]
        [SerializeField] private int poolSize = 50;
        [SerializeField] private GameObject[] effectPrefabs;
        
        // Internal systems
        private Dictionary<string, Queue<ParticleSystem>> effectPools = new Dictionary<string, Queue<ParticleSystem>>();
        private List<ActiveEffect> activeEffects = new List<ActiveEffect>();
        private Coroutine lightAnimationCoroutine;
        private float currentIntensity = 0f;
        
        // Component references
        private Core.GameManager gameManager;
        private Core.CrowdController crowdController;
        private Core.LevelManager levelManager;
        private UI.UIManager uiManager;
        
        // Shaders and materials
        private Material crowdTrailMaterial;
        private Material impactRippleMaterial;
        private int shaderColorID;
        private int shaderIntensityID;
        
        // Events
        public event System.Action<string> OnEffectTriggered;
        public event System.Action<float> OnIntensityChanged;
        
        private void Awake()
        {
            InitializeVFXSystem();
            CreateEffectPools();
            InitializeShaderProperties();
        }
        
        private void Start()
        {
            InitializeReferences();
            StartEnvironmentEffects();
            ConfigureQualitySettings();
        }
        
        private void InitializeVFXSystem()
        {
            // Ensure we have required components
            if (mainCamera == null)
            {
                mainCamera = Camera.main ?? FindObjectOfType<Camera>();
            }
            
            // Setup post-processing volume
            if (postProcessVolume == null)
            {
                postProcessVolume = FindObjectOfType<Volume>();
            }
            
            // Setup lighting
            if (mainLight == null)
            {
                mainLight = FindObjectOfType<Light>();
            }
            
            if (dynamicLight == null && enableDynamicLighting)
            {
                CreateDynamicLight();
            }
        }
        
        private void CreateDynamicLight()
        {
            GameObject lightObj = new GameObject("DynamicLight");
            lightObj.transform.SetParent(transform);
            
            dynamicLight = lightObj.AddComponent<Light>();
            dynamicLight.type = LightType.Point;
            dynamicLight.color = primaryGlowColor;
            dynamicLight.intensity = 2f;
            dynamicLight.range = 20f;
            dynamicLight.shadows = LightShadows.Soft;
            
            // Position above the crowd
            lightObj.transform.position = new Vector3(0, 10, 0);
        }
        
        private void CreateEffectPools()
        {
            foreach (var prefab in effectPrefabs)
            {
                if (prefab == null) continue;
                
                string prefabName = prefab.name;
                effectPools[prefabName] = new Queue<ParticleSystem>();
                
                for (int i = 0; i < poolSize; i++)
                {
                    GameObject pooledObj = Instantiate(prefab, transform);
                    pooledObj.SetActive(false);
                    
                    ParticleSystem ps = pooledObj.GetComponent<ParticleSystem>();
                    if (ps != null)
                    {
                        effectPools[prefabName].Enqueue(ps);
                    }
                }
            }
        }
        
        private void InitializeShaderProperties()
        {
            shaderColorID = Shader.PropertyToID("_GlowColor");
            shaderIntensityID = Shader.PropertyToID("_Intensity");
            
            // Create dynamic materials
            if (holographicMaterial != null)
            {
                crowdTrailMaterial = new Material(holographicMaterial);
            }
            
            if (neonGlowMaterial != null)
            {
                impactRippleMaterial = new Material(neonGlowMaterial);
            }
        }
        
        private void InitializeReferences()
        {
            gameManager = Core.GameManager.Instance;
            crowdController = FindObjectOfType<Core.CrowdController>();
            levelManager = FindObjectOfType<Core.LevelManager>();
            uiManager = FindObjectOfType<UI.UIManager>();
            
            // Subscribe to events
            if (gameManager != null)
            {
                gameManager.OnGameStateChanged += OnGameStateChanged;
            }
            
            if (crowdController != null)
            {
                crowdController.OnCrowdSizeChanged += OnCrowdSizeChanged;
                crowdController.OnGateTriggered += OnGateTriggered;
            }
            
            if (levelManager != null)
            {
                levelManager.OnLevelCompleted += OnLevelCompleted;
                levelManager.OnLevelStarted += OnLevelStarted;
            }
        }
        
        private void StartEnvironmentEffects()
        {
            if (environmentEffect != null)
            {
                var main = environmentEffect.main;
                main.loop = true;
                main.startLifetime = 5f;
                main.startSpeed = 2f;
                main.maxParticles = 100;
                
                environmentEffect.Play();
            }
        }
        
        private void ConfigureQualitySettings()
        {
            // Adjust particle quality based on device performance
            float deviceQuality = GetDeviceQualityScale();
            particleQualityScale *= deviceQuality;
            
            // Apply quality settings to all particle systems
            ApplyQualityToParticleSystem(crowdTrailEffect);
            ApplyQualityToParticleSystem(gateImpactEffect);
            ApplyQualityToParticleSystem(multiplicationEffect);
            ApplyQualityToParticleSystem(enemyDestructionEffect);
            ApplyQualityToParticleSystem(levelCompleteEffect);
            ApplyQualityToParticleSystem(environmentEffect);
        }
        
        private float GetDeviceQualityScale()
        {
            // Determine device quality based on system specs
            int memorySize = SystemInfo.systemMemorySize;
            int graphicsMemory = SystemInfo.graphicsMemorySize;
            
            if (memorySize >= 8192 && graphicsMemory >= 2048) return 1f; // High-end
            if (memorySize >= 4096 && graphicsMemory >= 1024) return 0.7f; // Mid-range
            return 0.5f; // Low-end
        }
        
        private void ApplyQualityToParticleSystem(ParticleSystem ps)
        {
            if (ps == null) return;
            
            var main = ps.main;
            main.maxParticles = Mathf.RoundToInt(main.maxParticles * particleQualityScale);
            
            var emission = ps.emission;
            emission.rateOverTime = emission.rateOverTime.constant * particleQualityScale;
        }
        
        // Core VFX Methods
        public void PlayCrowdTrailEffect(Vector3 position, int crowdSize)
        {
            if (crowdTrailEffect == null) return;
            
            crowdTrailEffect.transform.position = position;
            
            var main = crowdTrailEffect.main;
            main.maxParticles = Mathf.Clamp(crowdSize * 2, 10, maxParticles);
            
            var emission = crowdTrailEffect.emission;
            emission.rateOverTime = crowdSize * 0.5f;
            
            // Dynamic color based on crowd size
            Color trailColor = Color.Lerp(primaryGlowColor, secondaryGlowColor, Mathf.Clamp01(crowdSize / 100f));
            main.startColor = trailColor;
            
            crowdTrailEffect.Play();
            
            OnEffectTriggered?.Invoke("crowd_trail");
        }
        
        public void PlayGateImpactEffect(Vector3 position, Core.GateType gateType)
        {
            if (gateImpactEffect == null) return;
            
            gateImpactEffect.transform.position = position;
            
            var main = gateImpactEffect.main;
            
            // Different effects for different gate types
            switch (gateType)
            {
                case Core.GateType.Multiplier:
                    main.startColor = primaryGlowColor;
                    main.startSize = 2f;
                    break;
                case Core.GateType.Enemy:
                    main.startColor = Color.red;
                    main.startSize = 1.5f;
                    break;
                case Core.GateType.Obstacle:
                    main.startColor = Color.yellow;
                    main.startSize = 1f;
                    break;
            }
            
            gateImpactEffect.Play();
            
            // Screen shake for impact
            if (enableScreenShake)
            {
                TriggerScreenShake(0.3f, 0.1f);
            }
            
            OnEffectTriggered?.Invoke($"gate_impact_{gateType}");
        }
        
        public void PlayMultiplicationEffect(Vector3 position, int multiplier)
        {
            if (multiplicationEffect == null) return;
            
            multiplicationEffect.transform.position = position;
            
            var main = multiplicationEffect.main;
            main.maxParticles = multiplier * 20;
            main.startSize = Mathf.Clamp(multiplier * 0.3f, 0.5f, 3f);
            
            var burst = multiplicationEffect.emission.GetBurst(0);
            burst.count = multiplier * 5;
            multiplicationEffect.emission.SetBurst(0, burst);
            
            // Color intensity based on multiplier
            Color effectColor = Color.Lerp(primaryGlowColor, Color.white, Mathf.Clamp01(multiplier / 10f));
            main.startColor = effectColor;
            
            multiplicationEffect.Play();
            
            // Dynamic lighting effect
            if (enableDynamicLighting)
            {
                StartCoroutine(LightPulse(position, effectColor, multiplier * 0.5f));
            }
            
            OnEffectTriggered?.Invoke("multiplication");
        }
        
        public void PlayEnemyDestructionEffect(Vector3 position)
        {
            if (enemyDestructionEffect == null) return;
            
            enemyDestructionEffect.transform.position = position;
            
            var main = enemyDestructionEffect.main;
            main.startColor = Color.red;
            main.startSize = 1.5f;
            
            enemyDestructionEffect.Play();
            
            // Screen effects
            if (enableScreenShake)
            {
                TriggerScreenShake(0.5f, 0.15f);
            }
            
            if (enableChromaticAberration)
            {
                StartCoroutine(ChromaticAberrationPulse(0.3f));
            }
            
            OnEffectTriggered?.Invoke("enemy_destruction");
        }
        
        public void PlayLevelCompleteEffect(Vector3 position)
        {
            if (levelCompleteEffect == null) return;
            
            levelCompleteEffect.transform.position = position;
            
            var main = levelCompleteEffect.main;
            main.startColor = Color.white;
            main.maxParticles = 200;
            
            var shape = levelCompleteEffect.shape;
            shape.radius = 5f;
            
            levelCompleteEffect.Play();
            
            // Celebration lighting sequence
            if (enableDynamicLighting)
            {
                StartCoroutine(CelebrationLightSequence());
            }
            
            OnEffectTriggered?.Invoke("level_complete");
        }
        
        // Screen Effects
        public void TriggerScreenShake(float intensity, float duration)
        {
            if (!enableScreenShake || mainCamera == null) return;
            
            mainCamera.transform.DOShakePosition(duration, intensity, 10, 90, false, true);
        }
        
        private IEnumerator ChromaticAberrationPulse(float duration)
        {
            if (!enableChromaticAberration || postProcessVolume == null) yield break;
            
            // Implementation would depend on the specific post-processing setup
            // This is a placeholder for chromatic aberration effect
            yield return new WaitForSeconds(duration);
        }
        
        private IEnumerator LightPulse(Vector3 position, Color color, float intensity)
        {
            if (dynamicLight == null) yield break;
            
            Vector3 originalPosition = dynamicLight.transform.position;
            Color originalColor = dynamicLight.color;
            float originalIntensity = dynamicLight.intensity;
            
            // Move light to effect position
            dynamicLight.transform.position = position;
            dynamicLight.color = color;
            
            // Pulse effect
            float targetIntensity = originalIntensity * (1f + intensity);
            yield return dynamicLight.DOIntensity(targetIntensity, 0.1f).WaitForCompletion();
            yield return dynamicLight.DOIntensity(originalIntensity, 0.3f).WaitForCompletion();
            
            // Return to original state
            dynamicLight.transform.position = originalPosition;
            dynamicLight.color = originalColor;
        }
        
        private IEnumerator CelebrationLightSequence()
        {
            if (dynamicLight == null) yield break;
            
            Color originalColor = dynamicLight.color;
            float originalIntensity = dynamicLight.intensity;
            
            // Rainbow sequence
            Color[] celebrationColors = { 
                Color.red, Color.yellow, Color.green, 
                Color.cyan, Color.blue, Color.magenta 
            };
            
            foreach (Color color in celebrationColors)
            {
                dynamicLight.DOColor(color, 0.2f);
                dynamicLight.DOIntensity(originalIntensity * 1.5f, 0.1f);
                yield return new WaitForSeconds(0.2f);
            }
            
            // Return to original
            dynamicLight.DOColor(originalColor, 0.5f);
            dynamicLight.DOIntensity(originalIntensity, 0.5f);
        }
        
        // Pooled Effect System
        public ParticleSystem GetPooledEffect(string effectName)
        {
            if (!effectPools.ContainsKey(effectName)) return null;
            
            var pool = effectPools[effectName];
            if (pool.Count > 0)
            {
                var effect = pool.Dequeue();
                effect.gameObject.SetActive(true);
                return effect;
            }
            
            return null;
        }
        
        public void ReturnEffectToPool(ParticleSystem effect, string effectName)
        {
            if (effect == null || !effectPools.ContainsKey(effectName)) return;
            
            effect.Stop();
            effect.gameObject.SetActive(false);
            effectPools[effectName].Enqueue(effect);
        }
        
        // Event Handlers
        private void OnGameStateChanged(Core.GameState newState)
        {
            switch (newState)
            {
                case Core.GameState.Menu:
                    StopAllEffects();
                    SetIntensity(0f);
                    break;
                case Core.GameState.Playing:
                    StartEnvironmentEffects();
                    SetIntensity(0.3f);
                    break;
                case Core.GameState.GameOver:
                    StopAllEffects();
                    SetIntensity(0.1f);
                    break;
            }
        }
        
        private void OnCrowdSizeChanged(int newSize)
        {
            float intensity = Mathf.Clamp01(newSize / 100f);
            SetIntensity(intensity);
            
            // Update crowd trail effect
            if (crowdController != null)
            {
                Vector3 crowdPosition = crowdController.GetCrowdCenter();
                PlayCrowdTrailEffect(crowdPosition, newSize);
            }
        }
        
        private void OnGateTriggered(Core.Gate gate, Core.GateType gateType)
        {
            Vector3 gatePosition = gate.transform.position;
            
            PlayGateImpactEffect(gatePosition, gateType);
            
            if (gateType == Core.GateType.Multiplier)
            {
                int multiplier = gate.GetMultiplier();
                PlayMultiplicationEffect(gatePosition, multiplier);
            }
            else if (gateType == Core.GateType.Enemy)
            {
                PlayEnemyDestructionEffect(gatePosition);
            }
        }
        
        private void OnLevelCompleted(int level, Core.LevelResults results)
        {
            Vector3 centerPosition = Vector3.zero;
            if (crowdController != null)
            {
                centerPosition = crowdController.GetCrowdCenter();
            }
            
            PlayLevelCompleteEffect(centerPosition);
        }
        
        private void OnLevelStarted(int level)
        {
            // Reset effects for new level
            StopAllEffects();
            StartEnvironmentEffects();
        }
        
        private void SetIntensity(float intensity)
        {
            currentIntensity = intensity;
            
            // Update lighting
            if (enableDynamicLighting && mainLight != null)
            {
                Color targetColor = lightColorGradient.Evaluate(intensity);
                float targetIntensity = lightIntensityCurve.Evaluate(intensity);
                
                mainLight.DOColor(targetColor, 1f);
                mainLight.DOIntensity(targetIntensity, 1f);
            }
            
            OnIntensityChanged?.Invoke(intensity);
        }
        
        private void StopAllEffects()
        {
            ParticleSystem[] allParticles = FindObjectsOfType<ParticleSystem>();
            foreach (var ps in allParticles)
            {
                if (ps != environmentEffect) // Keep environment running
                {
                    ps.Stop();
                }
            }
        }
        
        // Utility Methods
        public void SetQuality(float quality)
        {
            particleQualityScale = Mathf.Clamp01(quality);
            ConfigureQualitySettings();
        }
        
        public void EnableEffect(string effectName, bool enabled)
        {
            switch (effectName.ToLower())
            {
                case "screeneffects":
                    enableScreenShake = enabled;
                    enableChromaticAberration = enabled;
                    break;
                case "lighting":
                    enableDynamicLighting = enabled;
                    break;
                case "holographic":
                    enableHolographicEffects = enabled;
                    break;
                case "neon":
                    enableNeonGlow = enabled;
                    break;
            }
        }
        
        private void OnDestroy()
        {
            // Cleanup event subscriptions
            if (gameManager != null)
            {
                gameManager.OnGameStateChanged -= OnGameStateChanged;
            }
            
            if (crowdController != null)
            {
                crowdController.OnCrowdSizeChanged -= OnCrowdSizeChanged;
                crowdController.OnGateTriggered -= OnGateTriggered;
            }
            
            if (levelManager != null)
            {
                levelManager.OnLevelCompleted -= OnLevelCompleted;
                levelManager.OnLevelStarted -= OnLevelStarted;
            }
        }
    }
    
    [System.Serializable]
    public class ActiveEffect
    {
        public ParticleSystem particleSystem;
        public string effectName;
        public float startTime;
        public float duration;
        
        public bool IsFinished => Time.time >= startTime + duration;
    }
}
