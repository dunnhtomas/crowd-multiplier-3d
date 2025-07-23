using UnityEngine;

namespace CrowdMultiplier.Audio
{
    /// <summary>
    /// Minimal audio manager for local gameplay
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Header("Audio Settings")]
        [SerializeField] private bool enableAudio = true;
        
        private void Start()
        {
            Debug.Log("Minimal audio system initialized");
        }
        
        public void PlayGateSound()
        {
            // Simple audio placeholder
            if (enableAudio)
            {
                Debug.Log("Gate sound effect");
            }
        }
        
        public void PlayCrowdSound(int crowdSize)
        {
            // Simple audio placeholder
            if (enableAudio)
            {
                Debug.Log($"Crowd sound for {crowdSize} people");
            }
        }
    }
}
