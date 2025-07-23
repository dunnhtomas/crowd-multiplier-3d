using UnityEngine;

namespace CrowdMultiplier.Core
{
    /// <summary>
    /// Minimal analytics for local gameplay only
    /// </summary>
    public class AnalyticsManager : MonoBehaviour
    {
        // Empty methods to prevent build errors - no functionality
        public void TrackEvent(string eventName, object parameters = null) { }
        public void TrackUserProgression(string milestone, object progressData = null) { }
        public void TrackLevelEvent(string eventType, int level, object additionalParams = null) { }
        public object GetSessionSummary() { return new { session_duration = 0f }; }
    }
}
