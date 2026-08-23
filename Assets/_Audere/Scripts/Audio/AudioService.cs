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
    public sealed class AudioService : MonoBehaviour, IGameService
    {
        public const string MusicVolumePrefKey = "Audere.Audio.MusicVolume";
        public const string SfxVolumePrefKey = "Audere.Audio.SfxVolume";

        public static AudioService Instance { get; private set; }

        [Tooltip("Shared AudioId -> AudioClip mapping asset. Assign the AudioCatalog.asset here.")]
        [SerializeField] private AudioCatalog catalog;

        [Tooltip("Optional music source. Its saved volume is applied automatically when assigned.")]
        [SerializeField] private AudioSource musicSource;

        [Tooltip("Optional. Auto-created as a 2D source (spatialBlend 0) if left empty.")]
        [SerializeField] private AudioSource sfxSource;

        public float MusicVolume { get; private set; } = 0.8f;
        public float SfxVolume { get; private set; } = 0.8f;

        public void Initialize()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[AudioService] A second instance was initialized. Ignoring.");
                return;
            }

            Instance = this;

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

            if (musicSource != null)
                musicSource.volume = MusicVolume;
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

        private void ApplyVolumes()
        {
            if (musicSource != null)
                musicSource.volume = MusicVolume;

            if (sfxSource != null)
                sfxSource.volume = SfxVolume;
        }
    }
}

