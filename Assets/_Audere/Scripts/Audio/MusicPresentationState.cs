using System.Collections.Generic;
using UnityEngine;

namespace Audere.Audio
{
    /// <summary>Session-local, owner-scoped music suppression. Never modifies saved volume.</summary>
    public sealed class MusicPresentationState
    {
        private readonly HashSet<CanvasGroup> screenFades = new HashSet<CanvasGroup>();
        private readonly HashSet<Object> combatOwners = new HashSet<Object>();
        private struct TrackClaim { public AudioId Track; public int Priority; public long Order; }
        private readonly Dictionary<Object, TrackClaim> combatTracks = new Dictionary<Object, TrackClaim>();
        private long trackOrder;

        public AudioId ResolveCombatTrack(AudioId fallback)
        {
            AudioId selected = fallback;
            int priority = int.MinValue;
            long order = -1;
            staleOwners.Clear();
            foreach (var pair in combatTracks)
            {
                if (pair.Key == null) { staleOwners.Add(pair.Key); continue; }
                var claim = pair.Value;
                if (claim.Priority > priority || claim.Priority == priority && claim.Order > order)
                { selected = claim.Track; priority = claim.Priority; order = claim.Order; }
            }
            foreach (var owner in staleOwners) combatTracks.Remove(owner);
            return selected;
        }
        private readonly Dictionary<Object, float> ducks = new Dictionary<Object, float>();
        private readonly List<Object> staleOwners = new List<Object>();

        public void TrackScreenFade(CanvasGroup group)
        {
            if (group != null) screenFades.Add(group);
        }

        public void SetCombat(Object owner, bool active, AudioId? track = null, int priority = 0)
        {
            if (owner == null) return;
            if (!active) { combatOwners.Remove(owner); combatTracks.Remove(owner); return; }
            combatOwners.Add(owner);
            if (!track.HasValue) { combatTracks.Remove(owner); return; }
            if (combatTracks.TryGetValue(owner, out var current) &&
                current.Track == track.Value && current.Priority == priority) return;
            combatTracks[owner] = new TrackClaim { Track = track.Value, Priority = priority, Order = ++trackOrder };
        }

        public void SetDuck(Object owner, float gain)
        {
            if (owner != null) ducks[owner] = Mathf.Clamp01(gain);
        }

        public void Release(Object owner)
        {
            if (ReferenceEquals(owner, null)) return;
            combatOwners.Remove(owner);
            combatTracks.Remove(owner);
            ducks.Remove(owner);
        }

        public bool IsCombat
        {
            get
            {
                combatOwners.RemoveWhere(owner => owner == null);
                return combatOwners.Count > 0;
            }
        }

        public float Gain
        {
            get
            {
                screenFades.RemoveWhere(group => group == null);
                float gain = 1f;
                foreach (CanvasGroup group in screenFades)
                    if (group.isActiveAndEnabled)
                        gain = Mathf.Min(gain, 1f - Mathf.Clamp01(group.alpha));

                staleOwners.Clear();
                foreach (KeyValuePair<Object, float> duck in ducks)
                {
                    if (duck.Key == null) staleOwners.Add(duck.Key);
                    else gain = Mathf.Min(gain, duck.Value);
                }
                foreach (Object owner in staleOwners) ducks.Remove(owner);
                return gain;
            }
        }

        public void Clear()
        {
            screenFades.Clear();
            combatOwners.Clear();
            combatTracks.Clear();
            trackOrder = 0;
            ducks.Clear();
            staleOwners.Clear();
        }
    }
}
