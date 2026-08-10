using System;
using UnityEngine;

namespace Audere.Audio
{
    /// <summary>
    /// One row in the <see cref="AudioCatalog"/>: a stable <see cref="AudioId"/> mapped to a
    /// concrete <see cref="AudioClip"/> plus a per-clip volume. Gameplay never sees the clip
    /// or its file name — only the id.
    /// </summary>
    [Serializable]
    public class AudioEntry
    {
        public AudioId id;

        public AudioClip clip;

        [Range(0f, 1f)]
        public float volume = 1f;
    }
}
