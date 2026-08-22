using System.Collections;
using UnityEngine;

namespace Audere.Story.Steps
{
    public sealed class MoveActorStep : StoryStep
    {
        [Header("Movement")]
        [SerializeField] private Transform actor;
        [SerializeField] private Transform targetTransform;
        [SerializeField, Min(0f)] private float duration = 0.5f;
        [SerializeField] private bool useUnscaledTime = true;
        [SerializeField] private bool snapOnComplete = true;

        public Transform Actor => actor;
        public Transform TargetTransform => targetTransform;
        public float Duration => duration;
        public bool UseUnscaledTime => useUnscaledTime;
        public bool SnapOnComplete => snapOnComplete;

        protected override IEnumerator Execute()
        {
            if (!TryGetMovementReferences(out Transform movingActor, out Transform target))
                yield break;

            if (duration <= 0f)
            {
                movingActor.position = target.position;
                CompleteStep();
                yield break;
            }

            Vector3 startPosition = movingActor.position;
            float startTime = CurrentTime;

            while (CurrentTime - startTime < duration)
            {
                if (movingActor == null || target == null)
                {
                    Debug.LogError(
                        "[MoveActorStep] Actor or Target Transform was destroyed while the step was running.",
                        this);
                    FailStep();
                    yield break;
                }

                float progress = Mathf.Clamp01((CurrentTime - startTime) / duration);
                movingActor.position = Vector3.Lerp(startPosition, target.position, progress);
                yield return null;
            }

            if (snapOnComplete)
            {
                if (movingActor == null || target == null)
                {
                    Debug.LogError(
                        "[MoveActorStep] Actor or Target Transform was destroyed before the movement completed.",
                        this);
                    FailStep();
                    yield break;
                }

                movingActor.position = target.position;
            }

            CompleteStep();
        }

        private float CurrentTime => useUnscaledTime ? Time.unscaledTime : Time.time;

        private bool TryGetMovementReferences(out Transform movingActor, out Transform target)
        {
            movingActor = actor;
            target = targetTransform;

            if (movingActor == null)
            {
                Debug.LogError("[MoveActorStep] Actor reference is required.", this);
                FailStep();
                return false;
            }

            if (target == null)
            {
                Debug.LogError("[MoveActorStep] Target Transform reference is required.", this);
                FailStep();
                return false;
            }

            return true;
        }

        private void OnDrawGizmosSelected()
        {
            if (actor == null || targetTransform == null)
                return;

            Vector3 start = actor.position;
            Vector3 end = targetTransform.position;
            Vector3 direction = end - start;

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(start, end);

            if (direction.sqrMagnitude <= Mathf.Epsilon)
                return;

            float arrowSize = Mathf.Clamp(direction.magnitude * 0.15f, 0.15f, 0.6f);
            Vector3 backward = -direction.normalized;
            Vector3 side = Vector3.Cross(direction.normalized, Vector3.forward);
            if (side.sqrMagnitude <= Mathf.Epsilon)
                side = Vector3.Cross(direction.normalized, Vector3.up);
            side.Normalize();

            Gizmos.DrawLine(end, end + (backward + side * 0.5f).normalized * arrowSize);
            Gizmos.DrawLine(end, end + (backward - side * 0.5f).normalized * arrowSize);
        }
    }
}
