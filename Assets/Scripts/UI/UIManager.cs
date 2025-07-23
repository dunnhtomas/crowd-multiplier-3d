using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CrowdMultiplier.UI
{
    /// <summary>
    /// Minimal UI manager for local gameplay
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI crowdCountText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private Image progressFill;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject levelCompletePanel;
        
        private void Start()
        {
            // Initialize UI
            if (crowdCountText != null) crowdCountText.text = "Crowd: 1";
            if (levelText != null) levelText.text = "Level 1";
            if (progressBar != null) progressBar.value = 0f;
            
            // Hide panels
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (levelCompletePanel != null) levelCompletePanel.SetActive(false);
        }
        
        public void UpdateCrowdCount(int count)
        {
            if (crowdCountText != null)
            {
                crowdCountText.text = $"Crowd: {count}";
            }
        }
        
        public void UpdateLevel(int level)
        {
            if (levelText != null)
            {
                levelText.text = $"Level {level}";
            }
        }
        
        public void UpdateProgress(float progress)
        {
            if (progressBar != null)
            {
                progressBar.value = progress;
            }
        }
        
        public void ShowGameOver()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }
        }
        
        public void ShowLevelComplete()
        {
            if (levelCompletePanel != null)
            {
                levelCompletePanel.SetActive(true);
            }
        }
        
        public void HideAllPanels()
        {
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (levelCompletePanel != null) levelCompletePanel.SetActive(false);
        }
        
        public void RestartLevel()
        {
            // Simple restart
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }
        
        public void NextLevel()
        {
            // Simple next level
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }
    }
}
