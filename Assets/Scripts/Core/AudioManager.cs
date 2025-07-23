using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;
using System.Collections;

namespace CrowdMultiplier.Core
{
    /// <summary>
    /// Enterprise-grade audio system with adaptive mixing and spatial audio
    /// Supports dynamic music, layered SFX, and real-time audio processing
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer mainMixer;
        [SerializeField] private AudioMixerGroup musicGroup;
        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField] private AudioMixerGroup voiceGroup;
        [SerializeField] private AudioMixerGroup uiGroup;
        
        [Header("Music System")]
        [SerializeField] private AudioClip[] backgroundMusic;
        [SerializeField] private AudioClip menuMusic;
        [SerializeField] private AudioClip gameplayMusic;
        [SerializeField] private AudioClip victoryMusic;
        [SerializeField] private float musicFadeTime = 2f;
        [SerializeField] private bool enableAdaptiveMusic = true;
        
        [Header("Sound Effects")]
        [SerializeField] private AudioClip[] crowdMovementSounds;
        [SerializeField] private AudioClip[] gateActivationSounds;
        [SerializeField] private AudioClip[] multiplicationSounds;
        [SerializeField] private AudioClip[] uiClickSounds;
        [SerializeField] private AudioClip[] victoryStingers;
        [SerializeField] private AudioClip[] defeatSounds;
        
        [Header("3D Audio Settings")]
        [SerializeField] private bool enable3DAudio = true;
        [SerializeField] private float spatialBlend = 1f;
        [SerializeField] private float dopplerLevel = 1f;
        [SerializeField] private float maxDistance = 50f;
        [SerializeField] private AnimationCurve distanceCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        
        [Header("Performance Settings")]
        [SerializeField] private int maxAudioSources = 32;
        [SerializeField] private float audioLODDistance = 20f;
        [SerializeField] private bool enableAudioCulling = true;
        [SerializeField] private float cullCheckInterval = 0.5f;
        
        // Audio source pools
        private Queue<AudioSource> musicSourcePool = new Queue<AudioSource>();
        private Queue<AudioSource> sfxSourcePool = new Queue<AudioSource>();
        private List<AudioSource> activeAudioSources = new List<AudioSource>();
        
        // Current playing audio
        private AudioSource currentMusicSource;
        private AudioSource previousMusicSource;
        private Coroutine musicFadeCoroutine;
        
        // Adaptive music system
        private int currentMusicIntensity = 0;
        private float lastCrowdSize = 0f;
        private float musicIntensityTimer = 0f;
        
        // Audio settings
        private float masterVolume = 1f;
        private float musicVolume = 0.8f;
        private float sfxVolume = 1f;
        private float voiceVolume = 1f;
        private float uiVolume = 0.9f;
        
        // Events
        public event System.Action<string> OnMusicChanged;
        public event System.Action<float> OnVolumeChanged;
        
        private void Start()
        {
            InitializeAudioSystem();
            StartCoroutine(AdaptiveMusicLoop());
            
            if (enableAudioCulling)
            {
                InvokeRepeating(nameof(CullDistantAudio), cullCheckInterval, cullCheckInterval);
            }
        }
        
        private void InitializeAudioSystem()
        {
            // Create audio source pools
            CreateAudioSourcePool();
            
            // Set initial mixer volumes
            UpdateMixerVolumes();
            
            // Play menu music
            PlayMusic(menuMusic, true);
            
            // Subscribe to game events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
            }
        }
        
        private void CreateAudioSourcePool()
        {
            // Create music sources
            for (int i = 0; i < 2; i++)
            {
                GameObject musicSourceGO = new GameObject($"MusicSource_{i}");
                musicSourceGO.transform.SetParent(transform);
                AudioSource musicSource = musicSourceGO.AddComponent<AudioSource>();
                musicSource.outputAudioMixerGroup = musicGroup;
                musicSource.loop = false;
                musicSource.playOnAwake = false;
                musicSourcePool.Enqueue(musicSource);
            }
            
            // Create SFX sources
            for (int i = 0; i < maxAudioSources - 2; i++)
            {
                GameObject sfxSourceGO = new GameObject($"SFXSource_{i}");
                sfxSourceGO.transform.SetParent(transform);
                AudioSource sfxSource = sfxSourceGO.AddComponent<AudioSource>();
                sfxSource.outputAudioMixerGroup = sfxGroup;
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
                
                if (enable3DAudio)
                {
                    sfxSource.spatialBlend = spatialBlend;
                    sfxSource.dopplerLevel = dopplerLevel;
                    sfxSource.maxDistance = maxDistance;
                    sfxSource.rolloffMode = AudioRolloffMode.Custom;
                    sfxSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, distanceCurve);
                }
                
                sfxSourcePool.Enqueue(sfxSource);
            }
        }
        
        public void PlayMusic(AudioClip clip, bool loop = true, bool fadeIn = true)
        {
            if (clip == null) return;
            
            if (musicFadeCoroutine != null)
            {
                StopCoroutine(musicFadeCoroutine);
            }
            
            musicFadeCoroutine = StartCoroutine(TransitionMusic(clip, loop, fadeIn));
        }
        
        private IEnumerator TransitionMusic(AudioClip newClip, bool loop, bool fadeIn)
        {
            AudioSource newMusicSource = GetMusicSource();
            if (newMusicSource == null) yield break;
            
            newMusicSource.clip = newClip;
            newMusicSource.loop = loop;
            newMusicSource.volume = 0f;
            newMusicSource.Play();
            
            // Fade out current music
            if (currentMusicSource != null && currentMusicSource.isPlaying)
            {
                previousMusicSource = currentMusicSource;
                float fadeOutTime = fadeIn ? musicFadeTime * 0.5f : 0f;
                
                yield return StartCoroutine(FadeAudioSource(previousMusicSource, 0f, fadeOutTime));
                
                if (previousMusicSource != null)
                {
                    previousMusicSource.Stop();
                    ReturnMusicSource(previousMusicSource);
                }
            }
            
            // Fade in new music
            currentMusicSource = newMusicSource;
            if (fadeIn)
            {
                yield return StartCoroutine(FadeAudioSource(currentMusicSource, musicVolume, musicFadeTime * 0.5f));
            }
            else
            {
                currentMusicSource.volume = musicVolume;
            }
            
            OnMusicChanged?.Invoke(newClip.name);
        }
        
        private IEnumerator FadeAudioSource(AudioSource source, float targetVolume, float fadeTime)
        {
            if (source == null || fadeTime <= 0f)
            {
                if (source != null) source.volume = targetVolume;
                yield break;
            }
            
            float startVolume = source.volume;
            float elapsed = 0f;
            
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / fadeTime;
                source.volume = Mathf.Lerp(startVolume, targetVolume, progress);
                yield return null;
            }
            
            source.volume = targetVolume;
        }
        
        public void PlaySFX(AudioClip clip, Vector3 position = default, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) return;
            
            AudioSource sfxSource = GetSFXSource();
            if (sfxSource == null) return;
            
            sfxSource.transform.position = position;
            sfxSource.clip = clip;
            sfxSource.volume = volume * sfxVolume;
            sfxSource.pitch = pitch;
            sfxSource.Play();
            
            StartCoroutine(ReturnSFXSourceAfterPlay(sfxSource, clip.length / pitch));
        }
        
        public void PlayRandomSFX(AudioClip[] clips, Vector3 position = default, float volume = 1f, float pitchVariation = 0.1f)
        {
            if (clips == null || clips.Length == 0) return;
            
            AudioClip randomClip = clips[Random.Range(0, clips.Length)];
            float randomPitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            
            PlaySFX(randomClip, position, volume, randomPitch);
        }
        
        public void PlayCrowdMovementSound(Vector3 position, float intensity = 1f)
        {
            PlayRandomSFX(crowdMovementSounds, position, intensity * 0.3f, 0.2f);
        }
        
        public void PlayGateActivationSound(Vector3 position, float multiplier = 1f)
        {
            float intensity = Mathf.Clamp(multiplier / 5f, 0.5f, 2f);
            PlayRandomSFX(gateActivationSounds, position, intensity, 0.15f);
        }
        
        public void PlayCrowdMultiplicationSound(Vector3 position, float multiplier)
        {
            AudioClip soundClip = null;
            
            if (multiplier >= 5f)
            {
                soundClip = multiplicationSounds.Length > 2 ? multiplicationSounds[2] : multiplicationSounds[0];
            }
            else if (multiplier >= 2f)
            {
                soundClip = multiplicationSounds.Length > 1 ? multiplicationSounds[1] : multiplicationSounds[0];
            }
            else
            {
                soundClip = multiplicationSounds[0];
            }
            
            float volume = Mathf.Clamp(multiplier / 3f, 0.7f, 1.5f);
            PlaySFX(soundClip, position, volume);
        }
        
        public void PlayUISound(string soundType)
        {
            AudioClip clip = null;
            
            switch (soundType.ToLower())
            {
                case "click":
                case "button":
                    PlayRandomSFX(uiClickSounds, Vector3.zero, uiVolume);
                    break;
                case "victory":
                    PlayRandomSFX(victoryStingers, Vector3.zero, uiVolume);
                    break;
                case "defeat":
                    PlayRandomSFX(defeatSounds, Vector3.zero, uiVolume);
                    break;
            }
        }
        
        private IEnumerator AdaptiveMusicLoop()
        {
            while (enableAdaptiveMusic)
            {
                yield return new WaitForSeconds(1f);
                
                if (GameManager.Instance?.CurrentGameState == GameState.Playing)
                {
                    UpdateAdaptiveMusic();
                }
            }
        }
        
        private void UpdateAdaptiveMusic()
        {
            var crowdController = FindObjectOfType<CrowdController>();
            if (crowdController == null) return;
            
            float currentCrowdSize = crowdController.GetCrowdSize();
            float crowdDelta = currentCrowdSize - lastCrowdSize;
            lastCrowdSize = currentCrowdSize;
            
            // Determine music intensity based on crowd size and growth
            int targetIntensity = 0;
            
            if (currentCrowdSize >= 500)
            {
                targetIntensity = 3; // Epic level
            }
            else if (currentCrowdSize >= 200)
            {
                targetIntensity = 2; // High intensity
            }
            else if (currentCrowdSize >= 50)
            {
                targetIntensity = 1; // Medium intensity
            }
            
            // Boost intensity if crowd is growing rapidly
            if (crowdDelta > 20f)
            {
                targetIntensity = Mathf.Min(targetIntensity + 1, 3);
            }
            
            // Update music if intensity changed
            if (targetIntensity != currentMusicIntensity)
            {
                currentMusicIntensity = targetIntensity;
                
                if (backgroundMusic.Length > currentMusicIntensity)
                {
                    PlayMusic(backgroundMusic[currentMusicIntensity], true, true);
                }
            }
        }
        
        private void OnGameStateChanged(GameState newState)
        {
            switch (newState)
            {
                case GameState.MainMenu:
                    PlayMusic(menuMusic, true);
                    currentMusicIntensity = 0;
                    break;
                    
                case GameState.Playing:
                    if (gameplayMusic != null)
                    {
                        PlayMusic(gameplayMusic, true);
                    }
                    break;
                    
                case GameState.LevelComplete:
                    if (victoryMusic != null)
                    {
                        PlayMusic(victoryMusic, false);
                    }
                    PlayUISound("victory");
                    break;
                    
                case GameState.GameOver:
                    PlayUISound("defeat");
                    break;
            }
        }
        
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            UpdateMixerVolumes();
            OnVolumeChanged?.Invoke(masterVolume);
        }
        
        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            UpdateMixerVolumes();
        }
        
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            UpdateMixerVolumes();
        }
        
        public void SetVoiceVolume(float volume)
        {
            voiceVolume = Mathf.Clamp01(volume);
            UpdateMixerVolumes();
        }
        
        public void SetUIVolume(float volume)
        {
            uiVolume = Mathf.Clamp01(volume);
            UpdateMixerVolumes();
        }
        
        private void UpdateMixerVolumes()
        {
            if (mainMixer == null) return;
            
            mainMixer.SetFloat("MasterVolume", VolumeToDecibels(masterVolume));
            mainMixer.SetFloat("MusicVolume", VolumeToDecibels(musicVolume));
            mainMixer.SetFloat("SFXVolume", VolumeToDecibels(sfxVolume));
            mainMixer.SetFloat("VoiceVolume", VolumeToDecibels(voiceVolume));
            mainMixer.SetFloat("UIVolume", VolumeToDecibels(uiVolume));
        }
        
        private float VolumeToDecibels(float volume)
        {
            return volume > 0.0001f ? Mathf.Log10(volume) * 20f : -80f;
        }
        
        private void CullDistantAudio()
        {
            if (!enableAudioCulling || Camera.main == null) return;
            
            Vector3 listenerPosition = Camera.main.transform.position;
            
            for (int i = activeAudioSources.Count - 1; i >= 0; i--)
            {
                AudioSource source = activeAudioSources[i];
                if (source == null || !source.isPlaying)
                {
                    activeAudioSources.RemoveAt(i);
                    continue;
                }
                
                float distance = Vector3.Distance(source.transform.position, listenerPosition);
                if (distance > audioLODDistance)
                {
                    source.volume *= 0.5f; // Reduce volume for distant sounds
                }
            }
        }
        
        private AudioSource GetMusicSource()
        {
            if (musicSourcePool.Count > 0)
            {
                return musicSourcePool.Dequeue();
            }
            return null;
        }
        
        private void ReturnMusicSource(AudioSource source)
        {
            if (source != null)
            {
                source.Stop();
                source.clip = null;
                musicSourcePool.Enqueue(source);
            }
        }
        
        private AudioSource GetSFXSource()
        {
            if (sfxSourcePool.Count > 0)
            {
                AudioSource source = sfxSourcePool.Dequeue();
                activeAudioSources.Add(source);
                return source;
            }
            return null;
        }
        
        private IEnumerator ReturnSFXSourceAfterPlay(AudioSource source, float delay)
        {
            yield return new WaitForSeconds(delay + 0.1f);
            
            if (source != null)
            {
                source.Stop();
                source.clip = null;
                activeAudioSources.Remove(source);
                sfxSourcePool.Enqueue(source);
            }
        }
        
        public void StopAllAudio()
        {
            if (currentMusicSource != null)
            {
                currentMusicSource.Stop();
            }
            
            foreach (var source in activeAudioSources)
            {
                if (source != null)
                {
                    source.Stop();
                }
            }
        }
        
        public void PauseAllAudio()
        {
            if (currentMusicSource != null)
            {
                currentMusicSource.Pause();
            }
            
            foreach (var source in activeAudioSources)
            {
                if (source != null && source.isPlaying)
                {
                    source.Pause();
                }
            }
        }
        
        public void ResumeAllAudio()
        {
            if (currentMusicSource != null)
            {
                currentMusicSource.UnPause();
            }
            
            foreach (var source in activeAudioSources)
            {
                if (source != null)
                {
                    source.UnPause();
                }
            }
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                PauseAllAudio();
            }
            else
            {
                ResumeAllAudio();
            }
        }
        
        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
            }
        }
    }
}
