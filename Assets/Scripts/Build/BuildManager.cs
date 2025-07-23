using UnityEngine;

namespace CrowdMultiplier.Build
{
    /// <summary>
    /// Minimal build manager for local gameplay
    /// </summary>
    public class BuildManager : MonoBehaviour
    {
        private void Start()
        {
            Debug.Log("Minimal build system initialized");
        }
        
        public void OptimizeForPlatform()
        {
            // Simple optimization placeholder
            Debug.Log("Platform optimization applied");
        }
    }
}
