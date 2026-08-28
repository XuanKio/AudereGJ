using UnityEngine;
using Audere.Core;

namespace Audere.Audio
{
    /// <summary>
    /// Global audio service. Gameplay calls <c>AudioService.Instance.Play(AudioId.X)</c> —
    /// it never references a clip, file name, or path. The <see cref="AudioCatalog"/> resolves
    /// the id to a clip + volume, then it is fired via <c>AudioSource.PlayOneShot</c>.
    ///
    /// Lives under the persistent Bootstrap services root and is initialized by the
    /// <see cref="Bootstrapper"/> via <see cref="IGameService"/>.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public sealed class AudioService : MonoBehaviour, IGameService
    {
        public const string MusicVolumePrefKey = "Audere.Audio.MusicVolume";
        public const string SfxVolumePrefKey = "Audere.Audio.SfxVolume";

        public static AudioService Instance { get; private set; }

        [Tooltip("Shared AudioId -> AudioClip mapping asset. Assign the AudioCatalog.asset here.")]
        [SerializeField] private AudioCatalog catalog;

        [Tooltip("Dedicated 2D looping source; never shared with SFX.")]
        [SerializeField] private AudioSource musicSource;

        [Header("Shared BGM")]
        [SerializeField] private AudioId explorationMusic = AudioId.Music_Exploration;
        [SerializeField] private AudioId combatMusic = AudioId.Music_Combat;
        [SerializeField, Min(.01f)] private float musicReturnDuration = .35f;
        [SerializeField, Min(.01f)] private float musicSwitchFadeDuration = .18f;

        [Tooltip("Optional. Auto-created as a 2D source (spatialBlend 0) if left empty.")]
        [SerializeField] private AudioSource sfxSource;

        public float MusicVolume { get; private set; } = 0.8f;
        public float SfxVolume { get; private set; } = 0.8f;

        private readonly MusicPresentationState musicPresentation = new MusicPresentationState();
        private bool initialized;
        private float musicGain;
        private float clipVolume = 1f;

        public AudioSource MusicSource => musicSource;
        public AudioId CurrentMusicId { get; private set; } = AudioId.None;
        public float MusicPresentationGain => musicGain;

        public void Initialize()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[AudioService] A second instance was initialized. Ignoring.");
                return;
            }

            Instance = this;
            if (initialized) return;
            initialized = true;

            if (musicSource == null || musicSource == sfxSource)
            {
                var go = new GameObject("MusicSource");
                go.transform.SetParent(transform, worldPositionStays: false);
                musicSource = go.AddComponent<AudioSource>();
            }
            musicSource.Stop();
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;
            musicSource.loop = true;
            musicGain = 0f;

            if (sfxSource == null)
            {
                var go = new GameObject("SfxSource");
                go.transform.SetParent(transform, worldPositionStays: false);
                sfxSource = go.AddComponent<AudioSource>();
            }

            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;

            MusicVolume = PlayerPrefs.GetFloat(MusicVolumePrefKey, 0.8f);
            SfxVolume = PlayerPrefs.GetFloat(SfxVolumePrefKey, 0.8f);
            ApplyVolumes();

            if (catalog == null)
                Debug.LogWarning("[AudioService] No AudioCatalog assigned. Play(...) will no-op.");
        }

        public void SetMusicVolume(float value)
        {
            MusicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MusicVolumePrefKey, MusicVolume);

            ApplyVolumes();
        }

        public void SetSfxVolume(float value)
        {
            SfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SfxVolumePrefKey, SfxVolume);

            if (sfxSource != null)
                sfxSource.volume = SfxVolume;
        }

        /// <summary>Play a one-shot SFX by its stable <see cref="AudioId"/>.</summary>
        public void Play(AudioId id)
        {
            if (catalog == null)
            {
                Debug.LogWarning($"[AudioService] No catalog assigned. Cannot play {id}.");
                return;
            }

            if (!catalog.TryGet(id, out AudioEntry entry))
            {
                Debug.LogWarning($"[AudioService] Audio not found: {id}");
                return;
            }

            if (entry.clip == null)
                return;

            sfxSource.PlayOneShot(entry.clip, entry.volume);
        }

        public bool TryResolveSfx(AudioId id, out AudioClip clip, out float volume)
        {
            clip = null;
            volume = 0f;

            if (catalog == null ||
                !catalog.TryGet(id, out AudioEntry entry) ||
                entry.clip == null)
            {
                return false;
            }

            clip = entry.clip;
            volume = entry.volume * SfxVolume;
            return true;
        }

        /// <summary>Track the cover itself, not the step: black holds between steps remain silent.</summary>
        public void TrackScreenFade(CanvasGroup cover) => musicPresentation.TrackScreenFade(cover);

        public void SetCombatMusicOwner(Object owner, bool active, AudioId? track = null, int priority = 0) => musicPresentation.SetCombat(owner, active, track, priority);
        public void SetMusicDuck(Object owner, float gain) => musicPresentation.SetDuck(owner, gain);
        public void ReleaseMusicOwner(Object owner) => musicPresentation.Release(owner);

        private void LateUpdate()
        {
            TickMusic(Time.unscaledDeltaTime);
        }

        private void TickMusic(float deltaTime)
        {
            if (!initialized || Instance != this || musicSource == null) return;

            AudioId desiredId = musicPresentation.IsCombat ? musicPresentation.ResolveCombatTrack(combatMusic) : explorationMusic;
            AudioClip desiredClip = null;
            float desiredVolume = 1f;
            if (catalog != null && catalog.TryGet(desiredId, out AudioEntry entry) && entry != null)
            {
                desiredClip = entry.clip;
                desiredVolume = entry.volume;
            }

            float targetGain = musicPresentation.Gain;
            bool switchingClip = musicSource.clip != desiredClip;
            if (switchingClip)
            {
                musicGain = Mathf.Min(targetGain, Mathf.MoveTowards(
                    musicGain, 0f, deltaTime / Mathf.Max(.01f, musicSwitchFadeDuration)));
                if (musicGain <= 0f)
                {
                    musicSource.Stop();
                    musicSource.clip = desiredClip;
                    CurrentMusicId = desiredId;
                    clipVolume = desiredVolume;
                    musicSource.volume = 0f;
                    // Empty combat slot means intentional silence; never fall back to exploration.
                    if (desiredClip != null) musicSource.Play();
                }
            }
            else
            {
                CurrentMusicId = desiredId;
                clipVolume = desiredVolume;
                // Decreases follow the actual cover/transition envelope without a lag at black.
                // Recovery is smoothed, including after cancel or an instant mode change.
                musicGain = desiredClip == null ? 0f : targetGain < musicGain ? targetGain : Mathf.MoveTowards(
                    musicGain, targetGain, deltaTime / Mathf.Max(.01f, musicReturnDuration));
            }
            ApplyVolumes();
        }

        private void OnDisable()
        {
            if (Instance != this) return;
            musicPresentation.Clear();
            musicGain = 0f;
            if (musicSource != null) musicSource.Stop();
        }

        private void OnEnable()
        {
            if (initialized && Instance == this && musicSource != null)
            {
                musicSource.volume = 0f;
                if (musicSource.clip != null) musicSource.Play();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            musicPresentation.Clear();
        }

        private void ApplyVolumes()
        {
            if (musicSource != null)
                musicSource.volume = MusicVolume * clipVolume * musicGain;

            if (sfxSource != null)
                sfxSource.volume = SfxVolume;
        }
    }
}
