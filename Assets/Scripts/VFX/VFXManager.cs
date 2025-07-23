using UnityEngine;

namespace CrowdMultiplier.VFX
{
    /// <summary>
    /// Minimal VFX manager for local gameplay
    /// </summary>
    public class VFXManager : MonoBehaviour
    {
        private void Start()
        {
            Debug.Log("Minimal VFX system initialized");
        }
        
        public void PlayGateEffect(Vector3 position)
        {
            // Simple particle effect placeholder
        }
        
        public void PlayCrowdMultiplyEffect(Vector3 position, int multiplier)
        {
            // Simple effect placeholder
        }
    }
}
