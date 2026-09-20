using System.Collections;
using Audere.Audio;
using Audere.Story.Presentation;
using UnityEngine;

namespace Audere.Story.Steps
{
    /// <summary>Automatic dream locomotion over authored anchors, without a puzzle input session.</summary>
    public sealed class DreamWalkStep : StoryStep
    {
        [SerializeField] private Transform actor;
        [SerializeField] private SpriteRenderer actorRenderer;
        [SerializeField] private Transform groundedShadow;
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private DreamAtmosphereView atmosphere;
        [SerializeField, Min(.05f)] private float firstStrideDuration = .28f;
        [SerializeField, Min(.05f)] private float lastStrideDuration = .95f;
        [SerializeField, Min(0f)] private float strideLift = .035f;
        private Vector3 ground, shadowOffset;
        private Quaternion shadowRotation;
        private bool prepared;
        public Transform Actor => actor;
        public Transform[] Waypoints => waypoints;
        public float Progress { get; private set; }

        protected override IEnumerator Execute()
        {
            if (actor == null || actorRenderer == null || groundedShadow == null || atmosphere == null ||
                waypoints == null || waypoints.Length == 0)
            { FailStep(); yield break; }
            foreach (var point in waypoints) if (point == null) { FailStep(); yield break; }
            ground = actor.position;
            shadowOffset = groundedShadow.position - ground;
            shadowRotation = groundedShadow.rotation;
            prepared = true;
            Progress = 0f;
            for (int i = 0; i < waypoints.Length; i++)
            {
                Vector3 start = ground;
                Vector3 end = waypoints[i].position;
                actorRenderer.flipX = end.x > start.x;
                float duration = Mathf.Lerp(firstStrideDuration, lastStrideDuration,
                    Mathf.Pow((float)i / Mathf.Max(1, waypoints.Length - 1), 1.4f));
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed = Mathf.Min(duration, elapsed + Time.unscaledDeltaTime);
                    float t = elapsed / duration;
                    ground = Vector3.Lerp(start, end, t);
                    actor.position = ground + Vector3.up * (Mathf.Sin(t * Mathf.PI) * strideLift);
                    groundedShadow.SetPositionAndRotation(ground + shadowOffset, shadowRotation);
                    Progress = (i + t) / waypoints.Length;
                    atmosphere.SetPressure(Progress);
                    yield return null;
                }
                AudioService.Instance?.Play(AudioId.Actor_Step);
            }
            RestoreGround();
            prepared = false;
        }

        private void RestoreGround()
        {
            if (!prepared) return;
            if (actor != null) actor.position = ground;
            if (groundedShadow != null) groundedShadow.SetPositionAndRotation(ground + shadowOffset, shadowRotation);
        }
        protected override void OnCancelled()
        {
            RestoreGround();
            prepared = false;
            atmosphere?.StopAndRestore();
        }
    }
}
