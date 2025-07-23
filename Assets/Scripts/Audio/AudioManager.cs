using UnityEngine;
using UnityEngine.Audio;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

namespace CrowdMultiplier.Audio
{
    /// <summary>
    /// Enterprise audio system with adaptive soundtrack, spatial audio, and haptic feedback
    /// Features dynamic music based on crowd size, 3D audio positioning, and modern sound design
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Header("Audio Mixer")]
        [SerializeField] private AudioMixerGroup masterMixer;
        [SerializeField] private AudioMixerGroup musicMixer;
        [SerializeField] private AudioMixerGroup sfxMixer;
        [SerializeField] private AudioMixerGroup ambientMixer;
        
        [Header("Music System")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource adaptiveMusicSource; // For seamless transitions
        [SerializeField] private List<MusicTrack> musicTracks = new List<MusicTrack>();
        [SerializeField] private float crossfadeDuration = 2f;
        [SerializeField] private bool enableAdaptiveMusic = true;
        
        [Header("Sound Effects")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip buttonClickSound;
        [SerializeField] private AudioClip gateHitSound;
        [SerializeField] private AudioClip crowdMultiplySound;
        [SerializeField] private AudioClip enemyHitSound;
        [SerializeField] private AudioClip levelCompleteSound;
        [SerializeField] private AudioClip newRecordSound;
        [SerializeField] private AudioClip gameOverSound;
        
        [Header("Ambient Audio")]
        [SerializeField] private AudioSource ambientSource;
        [SerializeField] private AudioClip crowdAmbientSound;
        [SerializeField] private AudioClip environmentAmbientSound;
        [SerializeField] private float ambientVolume = 0.3f;
        
        [Header("3D Audio")]
        [SerializeField] private bool enable3DAudio = true;
        [SerializeField] private AudioReverbZone reverbZone;
        [SerializeField] private float maxAudioDistance = 50f;
        [SerializeField] private AnimationCurve audioDistanceCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        
        [Header("Haptic Feedback")]
        [SerializeField] private bool enableHaptics = true;
        [SerializeField] private List<HapticPattern> hapticPatterns = new List<HapticPattern>();
        
        [Header("Audio Settings")]
        [Range(0f, 1f)]
        [SerializeField] private float masterVolume = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float musicVolume = 0.7f;
        [Range(0f, 1f)]
        [SerializeField] private float sfxVolume = 0.8f;
        [SerializeField] private bool muteOnFocusLoss = true;
        
        [Header("Performance")]
        [SerializeField] private int maxSimultaneousSFX = 10;
        [SerializeField] private bool enableAudioOcclusion = false;
        [SerializeField] private LayerMask occlusionLayers = -1;
        
        // Internal state
        private MusicTrack currentTrack;
        private float currentCrowdIntensity = 0f;
        private bool isMusicPlaying = false;
        private bool isTransitioning = false;
        private Dictionary<string, AudioSource> activeSFXSources = new Dictionary<string, AudioSource>();
        private Queue<AudioSource> audioSourcePool = new Queue<AudioSource>();
        
        // Component references
        private Core.GameManager gameManager;
        private Core.CrowdController crowdController;
        private Core.LevelManager levelManager;
        
        // Events
        public event System.Action<MusicTrack> OnMusicTrackChanged;
        public event System.Action<float> OnVolumeChanged;
        
        private void Awake()
        {
            InitializeAudioSystem();
            CreateAudioSourcePool();
            SetupAudioMixer();
        }
        
        private void Start()
        {
            InitializeReferences();
            StartBackgroundMusic();
            LoadAudioSettings();
        }
        
        private void InitializeAudioSystem()
        {
            // Ensure we have required audio sources
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.outputAudioMixerGroup = musicMixer;
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }
            
            if (adaptiveMusicSource == null)
            {
                adaptiveMusicSource = gameObject.AddComponent<AudioSource>();
                adaptiveMusicSource.outputAudioMixerGroup = musicMixer;
                adaptiveMusicSource.loop = true;
                adaptiveMusicSource.playOnAwake = false;
            }
            
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.outputAudioMixerGroup = sfxMixer;
                sfxSource.playOnAwake = false;
            }
            
            if (ambientSource == null)
            {
                ambientSource = gameObject.AddComponent<AudioSource>();
                ambientSource.outputAudioMixerGroup = ambientMixer;
                ambientSource.loop = true;
                ambientSource.playOnAwake = false;
                ambientSource.volume = ambientVolume;
            }
            
            // Setup 3D audio if enabled
            if (enable3DAudio)
            {
                Setup3DAudio();
            }
        }
        
        private void CreateAudioSourcePool()
        {
            // Create pool of audio sources for efficient SFX playback
            for (int i = 0; i < maxSimultaneousSFX; i++)
            {
                GameObject audioSourceObj = new GameObject($"PooledAudioSource_{i}");
                audioSourceObj.transform.SetParent(transform);
                
                AudioSource source = audioSourceObj.AddComponent<AudioSource>();
                source.outputAudioMixerGroup = sfxMixer;
                source.playOnAwake = false;
                
                audioSourcePool.Enqueue(source);
            }
        }
        
        private void SetupAudioMixer()
        {
            if (masterMixer != null)
            {
                // Set initial mixer values
                masterMixer.audioMixer.SetFloat("MasterVolume", Mathf.Log10(masterVolume) * 20);
                musicMixer.audioMixer.SetFloat("MusicVolume", Mathf.Log10(musicVolume) * 20);
                sfxMixer.audioMixer.SetFloat("SFXVolume", Mathf.Log10(sfxVolume) * 20);
            }
        }
        
        private void Setup3DAudio()
        {
            // Configure 3D audio settings
            if (musicSource != null)
            {
                musicSource.spatialBlend = 0f; // Keep music 2D
            }
            
            if (ambientSource != null)
            {
                ambientSource.spatialBlend = 0.5f; // Partial 3D for ambient
                ambientSource.maxDistance = maxAudioDistance;
                ambientSource.rolloffMode = AudioRolloffMode.Custom;
                ambientSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, audioDistanceCurve);
            }
            
            // Setup reverb zone if available
            if (reverbZone == null)
            {
                reverbZone = gameObject.AddComponent<AudioReverbZone>();
                reverbZone.reverbPreset = AudioReverbPreset.Generic;
                reverbZone.minDistance = 10f;
                reverbZone.maxDistance = maxAudioDistance;
            }
        }
        
        private void InitializeReferences()
        {
            gameManager = Core.GameManager.Instance;
            crowdController = FindObjectOfType<Core.CrowdController>();
            levelManager = FindObjectOfType<Core.LevelManager>();
            
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
                levelManager.OnLevelFailed += OnLevelFailed;
            }
        }
        
        private void StartBackgroundMusic()
        {
            if (musicTracks.Count > 0 && enableAdaptiveMusic)
            {
                PlayMusicTrack(GetTrackForIntensity(0f));
            }
        }
        
        public void PlayMusicTrack(MusicTrack track)
        {
            if (track == null || track.audioClip == null) return;
            
            if (currentTrack == track && isMusicPlaying) return;
            
            if (isTransitioning) return;
            
            StartCoroutine(TransitionToTrack(track));
        }
        
        private IEnumerator TransitionToTrack(MusicTrack newTrack)
        {
            isTransitioning = true;
            
            // Setup the new track on the adaptive source
            adaptiveMusicSource.clip = newTrack.audioClip;
            adaptiveMusicSource.volume = 0f;
            adaptiveMusicSource.Play();
            
            // Crossfade between sources
            if (isMusicPlaying)
            {
                // Fade out current track
                musicSource.DOFade(0f, crossfadeDuration);
                
                // Fade in new track
                yield return adaptiveMusicSource.DOFade(musicVolume, crossfadeDuration).WaitForCompletion();
            }
            else
            {
                // First track, just fade in
                yield return adaptiveMusicSource.DOFade(musicVolume, crossfadeDuration * 0.5f).WaitForCompletion();
            }
            
            // Swap sources
            var tempSource = musicSource;
            musicSource = adaptiveMusicSource;
            adaptiveMusicSource = tempSource;
            
            if (adaptiveMusicSource.isPlaying)
            {
                adaptiveMusicSource.Stop();
            }
            
            currentTrack = newTrack;
            isMusicPlaying = true;
            isTransitioning = false;
            
            OnMusicTrackChanged?.Invoke(newTrack);
            
            // Track analytics
            TrackMusicEvent("track_changed", newTrack.trackName);
        }
        
        private MusicTrack GetTrackForIntensity(float intensity)
        {
            if (musicTracks.Count == 0) return null;
            
            // Find the best track for current intensity
            MusicTrack bestTrack = musicTracks[0];
            float bestScore = Mathf.Abs(bestTrack.intensityLevel - intensity);
            
            foreach (var track in musicTracks)
            {
                float score = Mathf.Abs(track.intensityLevel - intensity);
                if (score < bestScore)
                {
                    bestScore = score;
                    bestTrack = track;
                }
            }
            
            return bestTrack;
        }
        
        public void PlaySFX(AudioClip clip, float volume = 1f, float pitch = 1f, bool is3D = false, Vector3 position = default)
        {
            if (clip == null) return;
            
            AudioSource source = GetPooledAudioSource();
            if (source == null) return;
            
            source.clip = clip;
            source.volume = volume * sfxVolume;
            source.pitch = pitch;
            
            if (is3D && enable3DAudio)
            {
                source.spatialBlend = 1f;
                source.transform.position = position;
                source.maxDistance = maxAudioDistance;
                source.rolloffMode = AudioRolloffMode.Custom;
                source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, audioDistanceCurve);
            }
            else
            {
                source.spatialBlend = 0f;
            }
            
            source.Play();
            
            // Return to pool when finished
            StartCoroutine(ReturnToPoolWhenFinished(source, clip.length / pitch));
        }
        
        private AudioSource GetPooledAudioSource()
        {
            if (audioSourcePool.Count > 0)
            {
                return audioSourcePool.Dequeue();
            }
            
            // If no sources available, find one that's not playing
            foreach (var kvp in activeSFXSources)
            {
                if (!kvp.Value.isPlaying)
                {
                    activeSFXSources.Remove(kvp.Key);
                    return kvp.Value;
                }
            }
            
            return null; // All sources in use
        }
        
        private IEnumerator ReturnToPoolWhenFinished(AudioSource source, float duration)
        {
            yield return new WaitForSeconds(duration);
            
            if (source != null)
            {
                source.Stop();
                source.clip = null;
                source.spatialBlend = 0f;
                audioSourcePool.Enqueue(source);
            }
        }
        
        // Specific sound effect methods
        public void PlayButtonClickSound()
        {
            PlaySFX(buttonClickSound, 0.7f);
            PlayHapticFeedback("button_click");
        }
        
        public void PlayGateHitSound(Vector3 position)
        {
            PlaySFX(gateHitSound, 0.8f, Random.Range(0.9f, 1.1f), enable3DAudio, position);
            PlayHapticFeedback("gate_hit");
        }
        
        public void PlayCrowdMultiplySound(int multiplier, Vector3 position)
        {
            float pitch = Mathf.Clamp(1f + (multiplier - 1) * 0.1f, 0.8f, 2f);
            PlaySFX(crowdMultiplySound, 0.9f, pitch, enable3DAudio, position);
            PlayHapticFeedback("crowd_multiply");
        }
        
        public void PlayEnemyHitSound(Vector3 position)
        {
            PlaySFX(enemyHitSound, 0.6f, Random.Range(0.8f, 1.2f), enable3DAudio, position);
            PlayHapticFeedback("enemy_hit");
        }
        
        public void PlayLevelCompleteSound()
        {
            PlaySFX(levelCompleteSound, 1f);
            PlayHapticFeedback("level_complete");
        }
        
        public void PlayNewRecordSound()
        {
            PlaySFX(newRecordSound, 1f);
            PlayHapticFeedback("new_record");
        }
        
        public void PlayGameOverSound()
        {
            PlaySFX(gameOverSound, 0.8f);
            PlayHapticFeedback("game_over");
        }
        
        private void PlayHapticFeedback(string patternName)
        {
            if (!enableHaptics) return;
            
            var pattern = hapticPatterns.Find(p => p.name == patternName);
            if (pattern != null)
            {
                StartCoroutine(ExecuteHapticPattern(pattern));
            }
        }
        
        private IEnumerator ExecuteHapticPattern(HapticPattern pattern)
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            foreach (var pulse in pattern.pulses)
            {
                if (SystemInfo.supportsVibration)
                {
                    Handheld.Vibrate();
                }
                yield return new WaitForSeconds(pulse.duration);
                yield return new WaitForSeconds(pulse.pause);
            }
            #elif UNITY_IOS && !UNITY_EDITOR
            // iOS haptic implementation would go here
            yield return null;
            #else
            yield return null;
            #endif
        }
        
        // Event handlers
        private void OnGameStateChanged(Core.GameState newState)
        {
            switch (newState)
            {
                case Core.GameState.Menu:
                    UpdateMusicIntensity(0f);
                    StopAmbient();
                    break;
                case Core.GameState.Playing:
                    UpdateMusicIntensity(0.3f);
                    StartAmbient();
                    break;
                case Core.GameState.Paused:
                    PauseAllAudio();
                    break;
                case Core.GameState.GameOver:
                    UpdateMusicIntensity(0.1f);
                    PlayGameOverSound();
                    break;
            }
        }
        
        private void OnCrowdSizeChanged(int newSize)
        {
            // Update music intensity based on crowd size
            float intensity = Mathf.Clamp01(newSize / 100f); // Normalize to 0-1
            UpdateMusicIntensity(intensity);
            
            // Update ambient crowd sound
            UpdateCrowdAmbient(newSize);
        }
        
        private void OnGateTriggered(Core.Gate gate, Core.GateType gateType)
        {
            switch (gateType)
            {
                case Core.GateType.Multiplier:
                    PlayCrowdMultiplySound(gate.GetMultiplier(), gate.transform.position);
                    break;
                case Core.GateType.Enemy:
                    PlayEnemyHitSound(gate.transform.position);
                    break;
                case Core.GateType.Obstacle:
                    PlayGateHitSound(gate.transform.position);
                    break;
            }
        }
        
        private void OnLevelCompleted(int level, Core.LevelResults results)
        {
            PlayLevelCompleteSound();
            
            // Special effect for perfect level
            if (results.Perfect)
            {
                StartCoroutine(PlayPerfectLevelSequence());
            }
        }
        
        private void OnLevelFailed(int level, Core.LevelResults results)
        {
            PlayGameOverSound();
            UpdateMusicIntensity(0f);
        }
        
        private IEnumerator PlayPerfectLevelSequence()
        {
            // Play a special sequence for perfect levels
            for (int i = 0; i < 3; i++)
            {
                PlaySFX(levelCompleteSound, 0.5f, 1f + i * 0.2f);
                yield return new WaitForSeconds(0.2f);
            }
        }
        
        private void UpdateMusicIntensity(float intensity)
        {
            currentCrowdIntensity = intensity;
            
            if (enableAdaptiveMusic)
            {
                var targetTrack = GetTrackForIntensity(intensity);
                if (targetTrack != currentTrack)
                {
                    PlayMusicTrack(targetTrack);
                }
            }
        }
        
        private void StartAmbient()
        {
            if (environmentAmbientSound != null)
            {
                ambientSource.clip = environmentAmbientSound;
                ambientSource.Play();
            }
        }
        
        private void StopAmbient()
        {
            if (ambientSource.isPlaying)
            {
                ambientSource.DOFade(0f, 1f).OnComplete(() => ambientSource.Stop());
            }
        }
        
        private void UpdateCrowdAmbient(int crowdSize)
        {
            if (crowdAmbientSound != null && crowdSize > 5)
            {
                float targetVolume = Mathf.Clamp01(crowdSize / 50f) * ambientVolume;
                ambientSource.DOVolume(targetVolume, 0.5f);
                
                if (!ambientSource.isPlaying)
                {
                    ambientSource.clip = crowdAmbientSound;
                    ambientSource.Play();
                }
            }
        }
        
        private void PauseAllAudio()
        {
            AudioListener.pause = true;
        }
        
        private void ResumeAllAudio()
        {
            AudioListener.pause = false;
        }
        
        // Volume control methods
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            if (masterMixer != null)
            {
                float dbValue = masterVolume > 0 ? Mathf.Log10(masterVolume) * 20 : -80f;
                masterMixer.audioMixer.SetFloat("MasterVolume", dbValue);
            }
            OnVolumeChanged?.Invoke(masterVolume);
            SaveAudioSettings();
        }
        
        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            if (musicMixer != null)
            {
                float dbValue = musicVolume > 0 ? Mathf.Log10(musicVolume) * 20 : -80f;
                musicMixer.audioMixer.SetFloat("MusicVolume", dbValue);
            }
            SaveAudioSettings();
        }
        
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            if (sfxMixer != null)
            {
                float dbValue = sfxVolume > 0 ? Mathf.Log10(sfxVolume) * 20 : -80f;
                sfxMixer.audioMixer.SetFloat("SFXVolume", dbValue);
            }
            SaveAudioSettings();
        }
        
        private void SaveAudioSettings()
        {
            PlayerPrefs.SetFloat("MasterVolume", masterVolume);
            PlayerPrefs.SetFloat("MusicVolume", musicVolume);
            PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
            PlayerPrefs.SetInt("EnableHaptics", enableHaptics ? 1 : 0);
            PlayerPrefs.Save();
        }
        
        private void LoadAudioSettings()
        {
            SetMasterVolume(PlayerPrefs.GetFloat("MasterVolume", 1f));
            SetMusicVolume(PlayerPrefs.GetFloat("MusicVolume", 0.7f));
            SetSFXVolume(PlayerPrefs.GetFloat("SFXVolume", 0.8f));
            enableHaptics = PlayerPrefs.GetInt("EnableHaptics", 1) == 1;
        }
        
        private void TrackMusicEvent(string eventType, string trackName)
        {
            if (gameManager != null)
            {
                var analyticsManager = gameManager.GetComponent<Core.AnalyticsManager>();
                analyticsManager?.TrackEvent($"audio_{eventType}", new Dictionary<string, object>
                {
                    { "track_name", trackName },
                    { "intensity", currentCrowdIntensity },
                    { "volume", masterVolume }
                });
            }
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && muteOnFocusLoss)
            {
                PauseAllAudio();
            }
            else if (!pauseStatus)
            {
                ResumeAllAudio();
            }
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && muteOnFocusLoss)
            {
                PauseAllAudio();
            }
            else if (hasFocus)
            {
                ResumeAllAudio();
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
                levelManager.OnLevelFailed -= OnLevelFailed;
            }
        }
    }
    
    [System.Serializable]
    public class MusicTrack
    {
        public string trackName;
        public AudioClip audioClip;
        [Range(0f, 1f)]
        public float intensityLevel; // 0 = calm, 1 = intense
        public string description;
    }
    
    [System.Serializable]
    public class HapticPattern
    {
        public string name;
        public List<HapticPulse> pulses = new List<HapticPulse>();
    }
    
    [System.Serializable]
    public class HapticPulse
    {
        [Range(0.01f, 1f)]
        public float duration = 0.1f;
        [Range(0f, 1f)]
        public float pause = 0.05f;
        [Range(0f, 1f)]
        public float intensity = 1f;
    }
}
