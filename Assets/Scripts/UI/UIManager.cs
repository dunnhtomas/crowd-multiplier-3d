using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

namespace CrowdMultiplier.UI
{
    /// <summary>
    /// Modern UI/UX framework with 2025 design trends
    /// Features neumorphic design, gradient backgrounds, and smooth animations
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Canvas mainCanvas;
        [SerializeField] private CanvasGroup gameplayUI;
        [SerializeField] private CanvasGroup menuUI;
        [SerializeField] private CanvasGroup pauseUI;
        [SerializeField] private CanvasGroup gameOverUI;
        
        [Header("HUD Elements")]
        [SerializeField] private TextMeshProUGUI crowdCountText;
        [SerializeField] private TextMeshProUGUI levelText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private Slider progressBar;
        [SerializeField] private Image progressFill;
        
        [Header("Menu Elements")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button leaderboardButton;
        [SerializeField] private TextMeshProUGUI titleText;
        
        [Header("Game Over Elements")]
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private TextMeshProUGUI newRecordText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;
        
        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem confettiEffect;
        [SerializeField] private ParticleSystem crowdCountEffect;
        [SerializeField] private RectTransform floatingTextParent;
        [SerializeField] private GameObject floatingTextPrefab;
        
        [Header("2025 Design Settings")]
        [SerializeField] private Gradient backgroundGradient;
        [SerializeField] private Color primaryAccent = new Color(0.2f, 0.8f, 1f, 1f); // Electric Blue
        [SerializeField] private Color secondaryAccent = new Color(1f, 0.4f, 0.6f, 1f); // Coral Pink
        [SerializeField] private float neumorphicShadowDistance = 8f;
        [SerializeField] private float animationDuration = 0.3f;
        
        [Header("Audio Integration")]
        [SerializeField] private bool enableHapticFeedback = true;
        [SerializeField] private bool enableSoundEffects = true;
        
        // Internal state
        private UIState currentState = UIState.Menu;
        private int currentScore = 0;
        private int bestScore = 0;
        private bool isAnimating = false;
        
        // Component references
        private Core.GameManager gameManager;
        private Core.CrowdController crowdController;
        private Core.LevelManager levelManager;
        private AudioManager audioManager;
        
        // Events
        public event System.Action OnPlayButtonClicked;
        public event System.Action OnRestartButtonClicked;
        public event System.Action OnMainMenuButtonClicked;
        public event System.Action OnPauseToggled;
        
        private void Awake()
        {
            InitializeComponents();
            SetupEventListeners();
            ApplyModernDesign();
        }
        
        private void Start()
        {
            InitializeReferences();
            ShowMenu();
            LoadBestScore();
        }
        
        private void InitializeComponents()
        {
            // Ensure main canvas is properly configured
            if (mainCanvas == null)
            {
                mainCanvas = FindObjectOfType<Canvas>();
            }
            
            // Setup canvas for mobile optimization
            if (mainCanvas != null)
            {
                var canvasScaler = mainCanvas.GetComponent<CanvasScaler>();
                if (canvasScaler == null)
                {
                    canvasScaler = mainCanvas.gameObject.AddComponent<CanvasScaler>();
                }
                
                canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasScaler.referenceResolution = new Vector2(1080, 1920); // Modern mobile resolution
                canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                canvasScaler.matchWidthOrHeight = 0.5f;
            }
        }
        
        private void SetupEventListeners()
        {
            // Menu buttons
            if (playButton != null)
            {
                playButton.onClick.AddListener(OnPlayClicked);
            }
            
            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(OnSettingsClicked);
            }
            
            if (leaderboardButton != null)
            {
                leaderboardButton.onClick.AddListener(OnLeaderboardClicked);
            }
            
            // Game over buttons
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartClicked);
            }
            
            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            }
        }
        
        private void ApplyModernDesign()
        {
            // Apply 2025 design trends to UI elements
            ApplyNeumorphicStyle();
            ApplyGradientBackgrounds();
            SetupAnimations();
        }
        
        private void ApplyNeumorphicStyle()
        {
            // Apply neumorphic design to buttons and panels
            var buttons = FindObjectsOfType<Button>();
            foreach (var button in buttons)
            {
                ApplyNeumorphicEffect(button.gameObject);
            }
        }
        
        private void ApplyNeumorphicEffect(GameObject target)
        {
            var image = target.GetComponent<Image>();
            if (image != null)
            {
                // Create soft shadow effect
                var shadow = new GameObject("NeumorphicShadow");
                shadow.transform.SetParent(target.transform);
                shadow.transform.SetAsFirstSibling();
                
                var shadowImage = shadow.AddComponent<Image>();
                shadowImage.sprite = image.sprite;
                shadowImage.color = new Color(0, 0, 0, 0.1f);
                
                var shadowRect = shadow.GetComponent<RectTransform>();
                shadowRect.anchorMin = Vector2.zero;
                shadowRect.anchorMax = Vector2.one;
                shadowRect.sizeDelta = Vector2.zero;
                shadowRect.anchoredPosition = new Vector2(neumorphicShadowDistance, -neumorphicShadowDistance);
                
                // Add highlight
                var highlight = new GameObject("NeumorphicHighlight");
                highlight.transform.SetParent(target.transform);
                highlight.transform.SetAsLastSibling();
                
                var highlightImage = highlight.AddComponent<Image>();
                highlightImage.sprite = image.sprite;
                highlightImage.color = new Color(1, 1, 1, 0.1f);
                
                var highlightRect = highlight.GetComponent<RectTransform>();
                highlightRect.anchorMin = Vector2.zero;
                highlightRect.anchorMax = Vector2.one;
                highlightRect.sizeDelta = Vector2.zero;
                highlightRect.anchoredPosition = new Vector2(-neumorphicShadowDistance * 0.5f, neumorphicShadowDistance * 0.5f);
            }
        }
        
        private void ApplyGradientBackgrounds()
        {
            // Apply dynamic gradient backgrounds
            if (backgroundGradient != null && backgroundGradient.colorKeys.Length > 0)
            {
                StartCoroutine(AnimateBackgroundGradient());
            }
        }
        
        private IEnumerator AnimateBackgroundGradient()
        {
            var camera = Camera.main;
            if (camera == null) yield break;
            
            float time = 0f;
            while (true)
            {
                time += Time.deltaTime * 0.1f; // Slow animation
                Color currentColor = backgroundGradient.Evaluate(Mathf.PingPong(time, 1f));
                camera.backgroundColor = currentColor;
                yield return null;
            }
        }
        
        private void SetupAnimations()
        {
            // Setup smooth entrance animations for UI elements
            if (DOTween.instance == null)
            {
                DOTween.Init();
            }
        }
        
        private void InitializeReferences()
        {
            gameManager = Core.GameManager.Instance;
            crowdController = FindObjectOfType<Core.CrowdController>();
            levelManager = FindObjectOfType<Core.LevelManager>();
            audioManager = FindObjectOfType<AudioManager>();
            
            // Subscribe to game events
            if (gameManager != null)
            {
                gameManager.OnGameStateChanged += OnGameStateChanged;
            }
            
            if (crowdController != null)
            {
                crowdController.OnCrowdSizeChanged += OnCrowdSizeChanged;
            }
            
            if (levelManager != null)
            {
                levelManager.OnLevelProgressUpdated += OnLevelProgressUpdated;
                levelManager.OnLevelCompleted += OnLevelCompleted;
            }
        }
        
        public void ShowMenu()
        {
            SetUIState(UIState.Menu);
            AnimateUITransition(menuUI, true);
            AnimateUITransition(gameplayUI, false);
            AnimateUITransition(gameOverUI, false);
            
            // Animate title with modern effect
            if (titleText != null)
            {
                titleText.transform.localScale = Vector3.zero;
                titleText.transform.DOScale(1f, animationDuration * 2f)
                    .SetEase(Ease.OutBounce);
            }
        }
        
        public void ShowGameplay()
        {
            SetUIState(UIState.Gameplay);
            AnimateUITransition(gameplayUI, true);
            AnimateUITransition(menuUI, false);
            AnimateUITransition(gameOverUI, false);
        }
        
        public void ShowGameOver(int finalScore, bool isNewRecord = false)
        {
            SetUIState(UIState.GameOver);
            AnimateUITransition(gameOverUI, true);
            AnimateUITransition(gameplayUI, false);
            
            // Update final score with animation
            if (finalScoreText != null)
            {
                StartCoroutine(AnimateScoreCounter(0, finalScore, finalScoreText));
            }
            
            // Show new record indicator
            if (newRecordText != null)
            {
                newRecordText.gameObject.SetActive(isNewRecord);
                if (isNewRecord)
                {
                    PlayNewRecordEffect();
                }
            }
        }
        
        private void SetUIState(UIState newState)
        {
            currentState = newState;
            
            // Track UI state for analytics
            if (gameManager != null)
            {
                var analyticsManager = gameManager.GetComponent<Core.AnalyticsManager>();
                analyticsManager?.TrackEvent("ui_state_changed", new Dictionary<string, object>
                {
                    { "previous_state", currentState.ToString() },
                    { "new_state", newState.ToString() },
                    { "timestamp", System.DateTime.UtcNow.ToString() }
                });
            }
        }
        
        private void AnimateUITransition(CanvasGroup targetGroup, bool show)
        {
            if (targetGroup == null) return;
            
            targetGroup.interactable = show;
            targetGroup.blocksRaycasts = show;
            
            float targetAlpha = show ? 1f : 0f;
            targetGroup.DOFade(targetAlpha, animationDuration)
                .SetEase(Ease.OutQuart);
                
            // Scale animation for modern feel
            targetGroup.transform.localScale = show ? Vector3.one * 0.8f : Vector3.one;
            targetGroup.transform.DOScale(show ? 1f : 0.8f, animationDuration)
                .SetEase(Ease.OutBack);
        }
        
        private void OnGameStateChanged(Core.GameState newState)
        {
            switch (newState)
            {
                case Core.GameState.Menu:
                    ShowMenu();
                    break;
                case Core.GameState.Playing:
                    ShowGameplay();
                    break;
                case Core.GameState.GameOver:
                    bool isNewRecord = currentScore > bestScore;
                    ShowGameOver(currentScore, isNewRecord);
                    if (isNewRecord)
                    {
                        bestScore = currentScore;
                        SaveBestScore();
                    }
                    break;
            }
        }
        
        private void OnCrowdSizeChanged(int newSize)
        {
            if (crowdCountText != null)
            {
                // Animate crowd count with modern bounce effect
                crowdCountText.text = newSize.ToString();
                crowdCountText.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 1, 0.5f);
                
                // Show floating text for significant changes
                if (newSize > 0 && newSize % 10 == 0)
                {
                    ShowFloatingText($"+{newSize}", primaryAccent);
                }
            }
            
            // Play particle effect for crowd changes
            if (crowdCountEffect != null && newSize > 10)
            {
                crowdCountEffect.Play();
            }
        }
        
        private void OnLevelProgressUpdated(float progress)
        {
            if (progressBar != null)
            {
                progressBar.DOValue(progress, 0.1f).SetEase(Ease.OutQuart);
            }
            
            if (progressFill != null)
            {
                // Dynamic color based on progress
                Color progressColor = Color.Lerp(secondaryAccent, primaryAccent, progress);
                progressFill.DOColor(progressColor, 0.1f);
            }
        }
        
        private void OnLevelCompleted(int level, Core.LevelResults results)
        {
            currentScore = results.Score;
            
            // Update level text with celebration animation
            if (levelText != null)
            {
                levelText.text = $"LEVEL {level + 1}";
                levelText.transform.DOPunchScale(Vector3.one * 0.3f, 0.5f, 2, 0.8f);
            }
            
            // Play confetti effect
            if (confettiEffect != null)
            {
                confettiEffect.Play();
            }
            
            // Show level complete floating text
            ShowFloatingText("LEVEL COMPLETE!", primaryAccent);
            
            // Haptic feedback
            PlayHapticFeedback(HapticType.Success);
        }
        
        private void ShowFloatingText(string text, Color color)
        {
            if (floatingTextPrefab == null || floatingTextParent == null) return;
            
            GameObject floatingTextObj = Instantiate(floatingTextPrefab, floatingTextParent);
            var textComponent = floatingTextObj.GetComponent<TextMeshProUGUI>();
            
            if (textComponent != null)
            {
                textComponent.text = text;
                textComponent.color = color;
                
                // Animate floating text
                var rectTransform = floatingTextObj.GetComponent<RectTransform>();
                Vector3 startPos = rectTransform.anchoredPosition;
                Vector3 endPos = startPos + Vector3.up * 100f;
                
                rectTransform.DOAnchorPos(endPos, 1f).SetEase(Ease.OutQuart);
                textComponent.DOFade(0f, 1f).SetEase(Ease.InQuart)
                    .OnComplete(() => Destroy(floatingTextObj));
            }
        }
        
        private IEnumerator AnimateScoreCounter(int startScore, int endScore, TextMeshProUGUI textComponent)
        {
            float duration = 1f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                int currentDisplayScore = Mathf.RoundToInt(Mathf.Lerp(startScore, endScore, progress));
                
                textComponent.text = currentDisplayScore.ToString("N0");
                yield return null;
            }
            
            textComponent.text = endScore.ToString("N0");
        }
        
        private void PlayNewRecordEffect()
        {
            // Screen flash effect
            StartCoroutine(ScreenFlashEffect());
            
            // Confetti explosion
            if (confettiEffect != null)
            {
                confettiEffect.Play();
            }
            
            // Haptic feedback
            PlayHapticFeedback(HapticType.Success);
            
            // Audio cue
            if (audioManager != null)
            {
                audioManager.PlayNewRecordSound();
            }
        }
        
        private IEnumerator ScreenFlashEffect()
        {
            var flashPanel = new GameObject("FlashPanel");
            flashPanel.transform.SetParent(mainCanvas.transform);
            
            var image = flashPanel.AddComponent<Image>();
            image.color = new Color(1, 1, 1, 0);
            
            var rectTransform = flashPanel.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
            
            // Flash animation
            yield return image.DOFade(0.3f, 0.1f).WaitForCompletion();
            yield return image.DOFade(0f, 0.3f).WaitForCompletion();
            
            Destroy(flashPanel);
        }
        
        private void PlayHapticFeedback(HapticType type)
        {
            if (!enableHapticFeedback) return;
            
            #if UNITY_ANDROID && !UNITY_EDITOR
            if (SystemInfo.supportsVibration)
            {
                switch (type)
                {
                    case HapticType.Light:
                        Handheld.Vibrate(); // Light vibration
                        break;
                    case HapticType.Medium:
                        Handheld.Vibrate(); // Medium vibration
                        break;
                    case HapticType.Success:
                        // Custom success pattern (requires platform-specific implementation)
                        Handheld.Vibrate();
                        break;
                }
            }
            #endif
            
            #if UNITY_IOS && !UNITY_EDITOR
            // iOS haptic feedback implementation would go here
            // Using iOS native haptic feedback APIs
            #endif
        }
        
        private void LoadBestScore()
        {
            bestScore = PlayerPrefs.GetInt("BestScore", 0);
        }
        
        private void SaveBestScore()
        {
            PlayerPrefs.SetInt("BestScore", bestScore);
            PlayerPrefs.Save();
        }
        
        // Button event handlers
        private void OnPlayClicked()
        {
            PlayButtonSound();
            PlayHapticFeedback(HapticType.Light);
            OnPlayButtonClicked?.Invoke();
        }
        
        private void OnRestartClicked()
        {
            PlayButtonSound();
            PlayHapticFeedback(HapticType.Light);
            OnRestartButtonClicked?.Invoke();
        }
        
        private void OnMainMenuClicked()
        {
            PlayButtonSound();
            PlayHapticFeedback(HapticType.Light);
            OnMainMenuButtonClicked?.Invoke();
        }
        
        private void OnSettingsClicked()
        {
            PlayButtonSound();
            PlayHapticFeedback(HapticType.Light);
            // Open settings panel (implement as needed)
        }
        
        private void OnLeaderboardClicked()
        {
            PlayButtonSound();
            PlayHapticFeedback(HapticType.Light);
            // Open leaderboard (implement as needed)
        }
        
        private void PlayButtonSound()
        {
            if (enableSoundEffects && audioManager != null)
            {
                audioManager.PlayButtonClickSound();
            }
        }
        
        public void UpdateScore(int newScore)
        {
            currentScore = newScore;
            if (scoreText != null)
            {
                scoreText.text = newScore.ToString("N0");
            }
        }
        
        public void UpdateLevel(int level)
        {
            if (levelText != null)
            {
                levelText.text = $"LEVEL {level}";
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
            }
            
            if (levelManager != null)
            {
                levelManager.OnLevelProgressUpdated -= OnLevelProgressUpdated;
                levelManager.OnLevelCompleted -= OnLevelCompleted;
            }
        }
    }
    
    public enum UIState
    {
        Menu,
        Gameplay,
        Pause,
        GameOver,
        Settings,
        Leaderboard
    }
    
    public enum HapticType
    {
        Light,
        Medium,
        Success
    }
}
