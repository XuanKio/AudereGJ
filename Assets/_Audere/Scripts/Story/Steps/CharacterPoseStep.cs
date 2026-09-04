using System.Collections;
using UnityEngine;

namespace Audere.Story.Steps
{
    // Scene anchors own standing/lying poses. Shadow remains on the authored floor anchor.
    public sealed class CharacterPoseStep : StoryStep
    {
        [SerializeField] private Transform actor;
        [SerializeField] private Transform targetPose;
        [SerializeField] private Transform groundedShadow;
        [SerializeField] private Transform shadowAnchor;
        [SerializeField, Min(0f)] private float duration = .45f;
        private Vector3 startPosition, shadowPosition, shadowScale;
        private Quaternion startRotation, shadowRotation;
        private bool captured;
        public Transform Actor => actor;
        public Transform TargetPose => targetPose;

        protected override IEnumerator Execute()
        {
            if (actor == null || targetPose == null || groundedShadow == null || shadowAnchor == null)
            { Debug.LogError("[CharacterPoseStep] Bind actor, pose and grounded shadow anchors.", this); FailStep(); yield break; }
            startPosition = actor.position; startRotation = actor.rotation;
            shadowPosition = groundedShadow.position; shadowRotation = groundedShadow.rotation; shadowScale = groundedShadow.lossyScale;
            captured = true;
            float elapsed = 0f;
            do
            {
                elapsed += Time.unscaledDeltaTime;
                float t = duration <= 0f ? 1f : Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                actor.SetPositionAndRotation(Vector3.Lerp(startPosition, targetPose.position, t), Quaternion.Slerp(startRotation, targetPose.rotation, t));
                GroundShadow(shadowAnchor.position, shadowAnchor.rotation);
                if (elapsed < duration) yield return null;
            } while (elapsed < duration);
            captured = false; CompleteStep();
        }
        private void GroundShadow(Vector3 position, Quaternion rotation)
        {
            groundedShadow.SetPositionAndRotation(position, rotation);
            Vector3 scale = groundedShadow.parent == null ? Vector3.one : groundedShadow.parent.lossyScale;
            groundedShadow.localScale = new Vector3(shadowScale.x / scale.x, shadowScale.y / scale.y, shadowScale.z / scale.z);
        }
        protected override void OnCancelled()
        {
            if (!captured || actor == null || groundedShadow == null) return;
            actor.SetPositionAndRotation(startPosition, startRotation);
            GroundShadow(shadowPosition, shadowRotation); captured = false;
        }
    }
}
