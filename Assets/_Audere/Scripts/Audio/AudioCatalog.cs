using System.Collections.Generic;
using UnityEngine;

namespace Audere.Audio
{
    /// <summary>
    /// The single shared mapping of <see cref="AudioId"/> -> <see cref="AudioEntry"/>, stored
    /// as a ScriptableObject .asset so it lives independently of any scene/GameObject. Swapping
    /// a sound = edit this asset in the Inspector; no gameplay code changes. Unity keeps the
    /// clip references by GUID/fileID (in the .meta), not by file name.
    /// </summary>
    [CreateAssetMenu(
        fileName = "AudioCatalog",
        menuName = "Audere/Audio/Audio Catalog")]
    public class AudioCatalog : ScriptableObject
    {
        [SerializeField]
        private List<AudioEntry> entries = new();

        private Dictionary<AudioId, AudioEntry> lookup;

        private void OnEnable()
        {
            BuildLookup();
        }

        private void BuildLookup()
        {
            lookup = new Dictionary<AudioId, AudioEntry>();

            foreach (AudioEntry entry in entries)
            {
                if (entry == null)
                    continue;

                lookup[entry.id] = entry;
            }
        }

        public bool TryGet(
            AudioId id,
            out AudioEntry entry)
        {
            if (lookup == null)
                BuildLookup();

            return lookup.TryGetValue(id, out entry);
        }
    }
}
