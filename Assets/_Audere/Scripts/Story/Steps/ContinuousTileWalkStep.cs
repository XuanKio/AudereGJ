using System.Collections;
using Audere.Audio;
using Audere.Story.Presentation;
using UnityEngine;

namespace Audere.Story.Steps
{
    public sealed class ContinuousTileWalkStep : StoryStep
    {
        [SerializeField] private Transform actor;
        [SerializeField] private SpriteRenderer actorRenderer;
        [SerializeField] private Transform groundedShadow;
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private TileRippleField rippleField;
        [SerializeField] private TileRippleWalkProfile profile;
        private Vector3[] path;
        private float[] distances;
        private Vector3 baseScale, ground, shadowOffset, shadowScale;
        private Quaternion shadowRotation;
        private int nextFootstep;
        private bool motionPrepared;
        public Transform Actor => actor;
        public Transform GroundedShadow => groundedShadow;
        public TileRippleField RippleField => rippleField;
        public TileRippleWalkProfile Profile => profile;
        public Transform[] Waypoints => waypoints;

        protected override IEnumerator Execute()
        {
            if (!BeginMotion())
            {
                Debug.LogError("[ContinuousTileWalkStep] Assign actor, shadow, path, ripple field and shared profile.", this);
                FailStep(); yield break;
            }
            float elapsed = 0f;
            while (elapsed < profile.Duration)
            {
                if (actor == null || groundedShadow == null || rippleField == null)
                { OnCancelled(); FailStep(); yield break; }
                elapsed = Mathf.Min(profile.Duration, elapsed + Time.unscaledDeltaTime);
                RenderMotion(elapsed);
                yield return null;
            }
            RestoreGroundPose();
            rippleField.Finish();
            motionPrepared = false;
            CompleteStep();
        }
        private bool BeginMotion()
        {
            if (actor == null || actorRenderer == null || groundedShadow == null ||
                profile == null || rippleField == null || rippleField.Profile != profile ||
                waypoints == null || waypoints.Length == 0) return false;
            foreach (var point in waypoints) if (point == null) return false;
            if (!rippleField.Prepare()) return false;
            path = new Vector3[waypoints.Length + 1]; distances = new float[path.Length];
            path[0] = actor.position;
            for (int i = 1; i < path.Length; i++)
            { path[i] = waypoints[i - 1].position; distances[i] = distances[i - 1] + Vector3.Distance(path[i - 1], path[i]); }
            baseScale = actor.localScale; ground = path[0];
            shadowOffset = groundedShadow.position - actor.position;
            shadowRotation = groundedShadow.rotation; shadowScale = groundedShadow.lossyScale;
            nextFootstep = 1; motionPrepared = true;
            actorRenderer.flipX = path[path.Length - 1].x > path[0].x;
            RenderMotion(0f);
            return true;
        }
        private void RenderMotion(float elapsed)
        {
            float distance = profile.DistanceProgress(elapsed) * distances[distances.Length - 1];
            int segment = 1;
            while (segment < path.Length - 1 && distances[segment] < distance) segment++;
            float t = Mathf.InverseLerp(distances[segment - 1], distances[segment], distance);
            ground = Vector3.Lerp(path[segment - 1], path[segment], t);
            // Horizontal travel never eases to zero at a tile boundary.
            float lift = Mathf.Sin(t * Mathf.PI) * profile.StrideLift;
            actor.position = ground + Vector3.up * lift;
            actor.localScale = baseScale;
            KeepShadowGrounded();
            while (nextFootstep < path.Length && distance >= distances[nextFootstep])
            {
                AudioService.Instance?.Play(AudioId.Actor_Step);
                nextFootstep++;
            }
            rippleField.Tick(elapsed, distance);
        }
        private void KeepShadowGrounded()
        {
            if (groundedShadow == null) return;
            groundedShadow.position = ground + shadowOffset;
            groundedShadow.rotation = shadowRotation;
            Vector3 parentScale = groundedShadow.parent != null ? groundedShadow.parent.lossyScale : Vector3.one;
            groundedShadow.localScale = new Vector3(Divide(shadowScale.x, parentScale.x),
                Divide(shadowScale.y, parentScale.y), Divide(shadowScale.z, parentScale.z));
        }
        private static float Divide(float value, float divisor) => Mathf.Abs(divisor) > .000001f ? value / divisor : value;
        private void RestoreGroundPose()
        {
            if (!motionPrepared) return;
            if (actor != null) { actor.position = ground; actor.localScale = baseScale; }
            KeepShadowGrounded();
        }
        protected override void OnCancelled()
        {
            RestoreGroundPose(); motionPrepared = false;
            if (rippleField != null) rippleField.Restore();
        }
    }
}
