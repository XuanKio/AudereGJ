using UnityEngine;
using Audere.Core;

namespace Audere.Audio
{
    /// <summary>
    /// Global audio service. Gameplay calls <c>AudioService.Instance.Play(AudioId.X)</c> —
    /// it never references a clip, file name, or path. The <see cref="AudioCatalog"/> resolves
    /// the id to a clip + volume, then it is fired via <c>AudioSource.PlayOneShot</c> (Unity 6
    /// allows many overlapping one-shots on a single AudioSource).
    ///
    /// Lives under the persistent Bootstrap services root and is initialized by the
    /// <see cref="Bootstrapper"/> via <see cref="IGameService"/>.
    /// </summary>
    public sealed class AudioService : MonoBehaviour, IGameService
    {
        public static AudioService Instance { get; private set; }

        [Tooltip("Shared AudioId -> AudioClip mapping asset. Assign the AudioCatalog.asset here.")]
        [SerializeField] private AudioCatalog catalog;

        [Tooltip("Optional. Auto-created as a 2D source (spatialBlend 0) if left empty.")]
        [SerializeField] private AudioSource sfxSource;

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
            sfxSource.spatialBlend = 0f; // Audere is 2D: UI/SFX are position-independent.

            if (catalog == null)
                Debug.LogWarning("[AudioService] No AudioCatalog assigned. Play(...) will no-op.");
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
    }
}
