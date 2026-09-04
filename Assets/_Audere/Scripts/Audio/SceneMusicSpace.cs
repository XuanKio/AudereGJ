using UnityEngine;

namespace Audere.Audio
{
    /// <summary>Scene-owned space in the existing score; never changes saved volume or SFX.</summary>
    public sealed class SceneMusicSpace : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float musicGain = .34f;
        [SerializeField, Range(0f, 1f)] private float quietGain = .035f;
        [SerializeField, Min(1f)] private float phraseDuration = 18f;
        [SerializeField, Min(0f)] private float quietDuration = 8f;
        [SerializeField, Min(.1f)] private float fadeDuration = 3f;
        private float elapsed;
        private AudioService owner;

        public float GainAt(float seconds)
        {
            float fade = Mathf.Max(.1f, fadeDuration);
            float phrase = Mathf.Max(1f, phraseDuration);
            float quiet = Mathf.Max(0f, quietDuration);
            float t = Mathf.Repeat(Mathf.Max(0f, seconds), phrase + quiet + fade * 2f);
            if (t < phrase) return musicGain;
            if (t < phrase + fade) return Mathf.Lerp(musicGain, quietGain, Mathf.SmoothStep(0f, 1f, (t - phrase) / fade));
            if (t < phrase + fade + quiet) return quietGain;
            return Mathf.Lerp(quietGain, musicGain, Mathf.SmoothStep(0f, 1f, (t - phrase - fade - quiet) / fade));
        }

        private void OnEnable() { elapsed = 0f; }
        private void Update()
        {
            var audio = AudioService.Instance;
            if (owner != audio) { if (owner != null) owner.ReleaseMusicOwner(this); owner = audio; }
            if (owner != null) owner.SetMusicDuck(this, GainAt(elapsed));
            elapsed += Time.unscaledDeltaTime;
        }
        private void OnDisable() { if (owner != null) owner.ReleaseMusicOwner(this); owner = null; }
    }
}
