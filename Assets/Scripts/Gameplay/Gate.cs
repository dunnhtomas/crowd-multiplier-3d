using UnityEngine;
using Unity.Mathematics;

namespace CrowdMultiplier.Gameplay
{
    /// <summary>
    /// Gate system for crowd multiplication and enemy encounters
    /// Supports different gate types with configurable effects
    /// </summary>
    public class Gate : MonoBehaviour
    {
        [Header("Gate Configuration")]
        [SerializeField] private GateType gateType = GateType.Multiplier;
        [SerializeField] private float multiplierValue = 2f;
        [SerializeField] private int enemyCrowdSize = 50;
        [SerializeField] private Color gateColor = Color.green;
        
        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem gateEffect;
        [SerializeField] private GameObject gateModel;
        [SerializeField] private TextMesh multiplierText;
        
        [Header("Audio")]
        [SerializeField] private AudioClip activationSound;
        [SerializeField] private AudioSource audioSource;
        
        private bool hasBeenTriggered = false;
        private Collider gateCollider;
        private Renderer gateRenderer;
        
        // Events
        public event System.Action<Gate, int> OnGateTriggered;
        
        private void Start()
        {
            InitializeGate();
        }
        
        private void InitializeGate()
        {
            gateCollider = GetComponent<Collider>();
            if (gateCollider == null)
            {
                gateCollider = gameObject.AddComponent<BoxCollider>();
                gateCollider.isTrigger = true;
            }
            
            gateRenderer = GetComponent<Renderer>();
            if (gateRenderer == null && gateModel != null)
            {
                gateRenderer = gateModel.GetComponent<Renderer>();
            }
            
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            
            UpdateGateVisuals();
        }
        
        private void UpdateGateVisuals()
        {
            // Update gate color based on type
            if (gateRenderer != null)
            {
                gateRenderer.material.color = gateColor;
            }
            
            // Update multiplier text
            if (multiplierText != null)
            {
                switch (gateType)
                {
                    case GateType.Multiplier:
                        multiplierText.text = $"×{multiplierValue:F1}";
                        multiplierText.color = Color.green;
                        break;
                    case GateType.Enemy:
                        multiplierText.text = $"VS {enemyCrowdSize}";
                        multiplierText.color = Color.red;
                        break;
                    case GateType.Obstacle:
                        multiplierText.text = "!";
                        multiplierText.color = Color.yellow;
                        break;
                }
            }
            
            // Setup particle effects
            if (gateEffect != null)
            {
                var main = gateEffect.main;
                main.startColor = gateColor;
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (hasBeenTriggered) return;
            
            // Check if player or crowd unit triggered the gate
            if (other.CompareTag("Player") || other.GetComponent<Core.CrowdUnit>() != null)
            {
                TriggerGate(other);
            }
        }
        
        private void TriggerGate(Collider triggeringObject)
        {
            if (hasBeenTriggered) return;
            
            hasBeenTriggered = true;
            
            // Get current crowd size
            var crowdController = FindFirstObjectByType<Core.CrowdController>();
            int currentCrowdSize = crowdController?.GetCrowdSize() ?? 0;
            
            int resultingCrowdSize = ProcessGateEffect(currentCrowdSize, crowdController);
            
            // Play effects
            PlayGateEffects();
            
            // Trigger analytics
            TrackGateInteraction(currentCrowdSize, resultingCrowdSize);
            
            // Notify listeners
            OnGateTriggered?.Invoke(this, resultingCrowdSize);
            
            // Disable gate after use
            StartCoroutine(DisableGate());
        }
        
        private int ProcessGateEffect(int currentCrowdSize, Core.CrowdController crowdController)
        {
            switch (gateType)
            {
                case GateType.Multiplier:
                    crowdController?.MultiplyCrowd(multiplierValue);
                    return Mathf.RoundToInt(currentCrowdSize * multiplierValue);
                    
                case GateType.Enemy:
                    return ProcessEnemyEncounter(currentCrowdSize, crowdController);
                    
                case GateType.Obstacle:
                    // Reduce crowd size
                    float reductionFactor = 0.5f;
                    crowdController?.MultiplyCrowd(reductionFactor);
                    return Mathf.RoundToInt(currentCrowdSize * reductionFactor);
                    
                default:
                    return currentCrowdSize;
            }
        }
        
        private int ProcessEnemyEncounter(int playerCrowdSize, Core.CrowdController crowdController)
        {
            if (playerCrowdSize >= enemyCrowdSize)
            {
                // Player wins - keep most of the crowd
                int survivors = playerCrowdSize - Mathf.RoundToInt(enemyCrowdSize * 0.3f);
                survivors = Mathf.Max(1, survivors);
                
                float multiplier = (float)survivors / playerCrowdSize;
                crowdController?.MultiplyCrowd(multiplier);
                
                // Victory effects
                PlayVictoryEffects();
                
                return survivors;
            }
            else
            {
                // Player loses - significant crowd reduction
                int survivors = Mathf.Max(1, Mathf.RoundToInt(playerCrowdSize * 0.2f));
                
                float multiplier = (float)survivors / playerCrowdSize;
                crowdController?.MultiplyCrowd(multiplier);
                
                // Defeat effects
                PlayDefeatEffects();
                
                return survivors;
            }
        }
        
        private void PlayGateEffects()
        {
            // Visual effects
            if (gateEffect != null)
            {
                gateEffect.Play();
            }
            
            // Audio effects
            if (audioSource != null && activationSound != null)
            {
                audioSource.PlayOneShot(activationSound);
            }
            
            // Screen shake for impact
            if (Camera.main != null)
            {
                StartCoroutine(ScreenShake(0.1f, 0.2f));
            }
        }
        
        private void PlayVictoryEffects()
        {
            // Green particles for victory
            if (gateEffect != null)
            {
                var main = gateEffect.main;
                main.startColor = Color.green;
                gateEffect.Play();
            }
        }
        
        private void PlayDefeatEffects()
        {
            // Red particles for defeat
            if (gateEffect != null)
            {
                var main = gateEffect.main;
                main.startColor = Color.red;
                gateEffect.Play();
            }
        }
        
        private System.Collections.IEnumerator ScreenShake(float duration, float intensity)
        {
            Vector3 originalPosition = Camera.main.transform.position;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                float x = UnityEngine.Random.Range(-1f, 1f) * intensity;
                float y = UnityEngine.Random.Range(-1f, 1f) * intensity;
                
                Camera.main.transform.position = originalPosition + new Vector3(x, y, 0);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            Camera.main.transform.position = originalPosition;
        }
        
        private void TrackGateInteraction(int beforeSize, int afterSize)
        {
            if (Core.GameManager.Instance != null)
            {
                var analyticsManager = Core.GameManager.Instance.GetComponent<Core.AnalyticsManager>();
                analyticsManager?.TrackEvent("gate_interaction", new System.Collections.Generic.Dictionary<string, object>
                {
                    { "gate_type", gateType.ToString() },
                    { "multiplier_value", multiplierValue },
                    { "enemy_size", enemyCrowdSize },
                    { "crowd_before", beforeSize },
                    { "crowd_after", afterSize },
                    { "gate_position", transform.position.ToString() },
                    { "timestamp", System.DateTime.UtcNow.ToString() }
                });
            }
        }
        
        private System.Collections.IEnumerator DisableGate()
        {
            yield return new WaitForSeconds(0.5f);
            
            // Fade out gate
            float fadeTime = 1f;
            float elapsed = 0f;
            Color originalColor = gateRenderer != null ? gateRenderer.material.color : Color.white;
            
            while (elapsed < fadeTime)
            {
                float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
                if (gateRenderer != null)
                {
                    Color newColor = originalColor;
                    newColor.a = alpha;
                    gateRenderer.material.color = newColor;
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            gameObject.SetActive(false);
        }
        
        // Public methods for dynamic gate configuration
        public void SetGateType(GateType type)
        {
            gateType = type;
            UpdateGateVisuals();
        }
        
        public void SetMultiplierValue(float value)
        {
            multiplierValue = value;
            UpdateGateVisuals();
        }
        
        public void SetEnemyCrowdSize(int size)
        {
            enemyCrowdSize = size;
            UpdateGateVisuals();
        }
        
        public void SetGateColor(Color color)
        {
            gateColor = color;
            UpdateGateVisuals();
        }
        
        public GateType GetGateType() => gateType;
        public float GetMultiplierValue() => multiplierValue;
        public int GetEnemyCrowdSize() => enemyCrowdSize;
        public bool HasBeenTriggered() => hasBeenTriggered;
    }
    
    public enum GateType
    {
        Multiplier,
        Enemy,
        Obstacle,
        Bonus,
        Shield
    }
}
