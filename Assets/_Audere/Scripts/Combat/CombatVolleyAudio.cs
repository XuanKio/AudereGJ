using Audere.Audio;
using UnityEngine;

namespace Audere.Combat
{
    /// <summary>Two board-owned voices. Coalesces volleys and never queues a delayed catch-up sound.</summary>
    public sealed class CombatVolleyAudio
    {
        private readonly Transform owner;
        private AudioSource bulletSource, laserSource;
        private double activeTime, nextBullet, nextLaser;
        private bool paused;
        private int ownerSession, ownerPhase;

        public CombatVolleyAudio(Transform owner) { this.owner = owner; }

        public void Advance(float deltaTime)
        {
            if (paused || deltaTime <= 0f) return;
            activeTime += deltaTime;
            var audio = AudioService.Instance;
            if (audio == null) return;
            if (bulletSource != null && audio.TryResolveSfx(AudioId.Enemy_BulletVolley, out _, out float bulletVolume))
                bulletSource.volume = bulletVolume;
            if (laserSource != null && audio.TryResolveSfx(AudioId.Enemy_LaserVolley, out _, out float laserVolume))
                laserSource.volume = laserVolume;
        }

        public bool PlayBullet(float minimumInterval, int session = 0, int phase = 0) =>
            TryPlay(AudioId.Enemy_BulletVolley, minimumInterval, session, phase, ref nextBullet, ref bulletSource);
        public bool PlayLaser(float minimumInterval, int session = 0, int phase = 0) =>
            TryPlay(AudioId.Enemy_LaserVolley, minimumInterval, session, phase, ref nextLaser, ref laserSource);

        private bool TryPlay(AudioId id, float interval, int session, int phase, ref double next, ref AudioSource source)
        {
            if (paused || owner == null || !owner.gameObject.activeInHierarchy) return false;
            if (ownerSession != session || ownerPhase != phase)
            {
                Reset();
                ownerSession = session; ownerPhase = phase;
            }
            if (activeTime + .000001 < next)
                return false;
            var audio = AudioService.Instance;
            if (audio == null || !audio.TryResolveSfx(id, out AudioClip clip, out float volume)) return false;
            if (source == null)
            {
                var go = new GameObject(id == AudioId.Enemy_BulletVolley ? "Bullet Volley Audio" : "Laser Volley Audio");
                go.transform.SetParent(owner, false);
                source = go.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
            }
            // One voice per kind: a new beat replaces its old tail, never stacks N copies.
            source.clip = clip;
            source.volume = volume;
            source.Play();
            next = activeTime + Mathf.Max(.01f, interval);
            return true;
        }

        public void SetPaused(bool value)
        {
            if (paused == value) return;
            paused = value;
            if (value) { if (bulletSource != null) bulletSource.Pause(); if (laserSource != null) laserSource.Pause(); }
            else { if (bulletSource != null) bulletSource.UnPause(); if (laserSource != null) laserSource.UnPause(); }
        }

        public void Reset(int session, int phase = -1)
        {
            if (ownerSession == session && (phase < 0 || ownerPhase == phase)) Reset();
        }

        public void Reset()
        {
            if (bulletSource != null) bulletSource.Stop();
            if (laserSource != null) laserSource.Stop();
            activeTime = nextBullet = nextLaser = 0d;
            paused = false;
            ownerSession = ownerPhase = 0;
        }
    }
}
