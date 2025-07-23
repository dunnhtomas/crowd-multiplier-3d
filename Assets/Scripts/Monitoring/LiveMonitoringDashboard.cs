using UnityEngine;

namespace CrowdMultiplier.Monitoring
{
    /// <summary>
    /// Minimal monitoring dashboard for local gameplay
    /// </summary>
    public class LiveMonitoringDashboard : MonoBehaviour
    {
        private void Start()
        {
            Debug.Log("Minimal monitoring system initialized");
        }
        
        public void LogAlert(string message)
        {
            Debug.Log($"[ALERT] {message}");
        }
    }
}
