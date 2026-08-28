using System.Collections;
using Audere.Audio;
using UnityEngine;

namespace Audere.Story.Steps
{
    public enum CharacterFacingMode
    {
        Preserve,
        FollowHorizontalTravel,
        FaceLeft,
        FaceRight,
    }

    public enum CharacterMotionMode
    {
        TravelToTarget,
        VerticalInPlace,
    }

    /// <summary>
    /// Scene-authored character motion with the same light hop and landing response
    /// used by the StepTile player. A zero-distance target becomes an in-place reaction.
    /// </summary>
    public sealed class CharacterMotionStep : StoryStep
    {
        [Header("Direct References")]
        [SerializeField] private Transform actor;
        [SerializeField] private Transform targetTransform;
        [SerializeField] private SpriteRenderer actorRenderer;
        [Tooltip("Grounded presentation that follows the actor across the floor but must not inherit hop height or squash.")]
        [SerializeField] private Transform groundedShadow;

        [Header("Motion")]
        [SerializeField] private CharacterMotionMode motionMode =
            CharacterMotionMode.TravelToTarget;
        [SerializeField, Min(.01f)] private float duration = .32f;
        [SerializeField, Min(0f)] private float arcHeight = .075f;
        [SerializeField, Range(0f, .2f)] private float travelStretch = .065f;
        [SerializeField, Min(0f)] private float landingDuration = .1f;
        [SerializeField, Range(0f, .25f)] private float landingSquash = .105f;
        [SerializeField, Range(0f, .2f)] private float landingWiden = .075f;
        [SerializeField] private bool useUnscaledTime = true;

        [Header("Facing")]
        [SerializeField] private CharacterFacingMode facingMode =
            CharacterFacingMode.FollowHorizontalTravel;
        [Tooltip("The current Player sprite is authored facing left before Flip X is applied.")]
        [SerializeField] private bool sourceSpriteFacesLeft = true;

        private Vector3 activeBaseScale;
        private Vector3 activeGroundPosition;
        private Vector3 shadowWorldOffset;
        private Quaternion shadowWorldRotation;
        private Vector3 shadowWorldScale;
        private bool shadowPoseCached;

        public Transform Actor => actor;
        public Transform TargetTransform => targetTransform;
        public SpriteRenderer ActorRenderer => actorRenderer;
        public Transform GroundedShadow => groundedShadow;
        public CharacterMotionMode MotionMode => motionMode;
        public float Duration => duration;
        public float ArcHeight => arcHeight;
        public CharacterFacingMode FacingMode => facingMode;

        protected override IEnumerator Execute()
        {
            if (actor == null || targetTransform == null)
            {
                Debug.LogError(
                    $"[CharacterMotionStep] '{name}' requires direct Actor and Target Transform references.",
                    this);
                FailStep();
                yield break;
            }

            Vector3 startPosition = actor.position;
            bool verticalInPlace = motionMode == CharacterMotionMode.VerticalInPlace;
            Vector3 targetPosition = verticalInPlace ? startPosition : targetTransform.position;
            Vector3 baseScale = actor.localScale;
            activeBaseScale = baseScale;
            activeGroundPosition = startPosition;
            CacheGroundedShadowPose(startPosition);
            float horizontalTravel = targetPosition.x - startPosition.x;
            ApplyFacing(horizontalTravel);

            if (duration <= Mathf.Epsilon)
            {
                actor.position = targetPosition;
                actor.localScale = baseScale;
                activeGroundPosition = targetPosition;
                KeepShadowGrounded(activeGroundPosition);
                ApplyFinalFacing(horizontalTravel);
                CompleteStep();
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (actor == null || targetTransform == null)
                {
                    RestoreGroundedPose();
                    Debug.LogError(
                        $"[CharacterMotionStep] '{name}' lost an Actor or Target while running.",
                        this);
                    FailStep();
                    yield break;
                }

                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float eased = SmootherStep(progress);
                float travelPulse = Mathf.Sin(progress * Mathf.PI);
                targetPosition = verticalInPlace ? startPosition : targetTransform.position;
                activeGroundPosition = Vector3.LerpUnclamped(startPosition, targetPosition, eased);
                actor.position = activeGroundPosition +
                    Vector3.up * (arcHeight * travelPulse);
                actor.localScale = new Vector3(
                    baseScale.x * (1f - travelStretch * travelPulse * .35f),
                    baseScale.y * (1f + travelStretch * travelPulse),
                    baseScale.z);
                KeepShadowGrounded(activeGroundPosition);
                yield return null;
            }

            activeGroundPosition = verticalInPlace ? startPosition : targetTransform.position;
            actor.position = activeGroundPosition;
            actor.localScale = baseScale;
            KeepShadowGrounded(activeGroundPosition);
            if (!verticalInPlace &&
                (activeGroundPosition - startPosition).sqrMagnitude > Mathf.Epsilon)
            {
                AudioService.Instance?.Play(AudioId.Actor_Step);
            }
            yield return PlayLanding(baseScale);
            ApplyFinalFacing(horizontalTravel);
            CompleteStep();
        }

        protected override void OnCancelled()
        {
            RestoreGroundedPose();
        }

        private void RestoreGroundedPose()
        {
            if (actor != null)
            {
                actor.localScale = activeBaseScale;
                actor.position = activeGroundPosition;
                KeepShadowGrounded(activeGroundPosition);
            }
        }

        private IEnumerator PlayLanding(Vector3 baseScale)
        {
            if (landingDuration <= Mathf.Epsilon)
                yield break;

            float elapsed = 0f;
            while (elapsed < landingDuration)
            {
                if (actor == null)
                    yield break;

                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / landingDuration);
                float impact = Mathf.Sin(progress * Mathf.PI);
                float rebound = Mathf.Sin(progress * Mathf.PI * 2f) *
                    (1f - progress) * .35f;
                actor.localScale = new Vector3(
                    baseScale.x * (1f + landingWiden * impact - rebound * .02f),
                    baseScale.y * (1f - landingSquash * impact + rebound * .04f),
                    baseScale.z);
                KeepShadowGrounded(activeGroundPosition);
                yield return null;
            }

            if (actor != null)
            {
                actor.localScale = baseScale;
                KeepShadowGrounded(activeGroundPosition);
            }
        }

        private void CacheGroundedShadowPose(Vector3 groundPosition)
        {
            if (groundedShadow == null)
                groundedShadow = FindGroundedShadow(actor);

            shadowPoseCached = groundedShadow != null;
            if (!shadowPoseCached)
                return;

            shadowWorldOffset = groundedShadow.position - groundPosition;
            shadowWorldRotation = groundedShadow.rotation;
            shadowWorldScale = groundedShadow.lossyScale;
        }

        private void KeepShadowGrounded(Vector3 groundPosition)
        {
            if (!shadowPoseCached || groundedShadow == null)
                return;

            groundedShadow.position = groundPosition + shadowWorldOffset;
            groundedShadow.rotation = shadowWorldRotation;

            Transform shadowParent = groundedShadow.parent;
            Vector3 parentScale = shadowParent != null
                ? shadowParent.lossyScale
                : Vector3.one;
            groundedShadow.localScale = new Vector3(
                DivideScale(shadowWorldScale.x, parentScale.x),
                DivideScale(shadowWorldScale.y, parentScale.y),
                DivideScale(shadowWorldScale.z, parentScale.z));
        }

        private static Transform FindGroundedShadow(Transform actorRoot)
        {
            if (actorRoot == null)
                return null;

            Transform[] descendants = actorRoot.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < descendants.Length; i++)
            {
                Transform candidate = descendants[i];
                if (candidate != actorRoot &&
                    candidate.name.StartsWith("shadow", System.StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
            }

            return null;
        }

        private static float DivideScale(float value, float divisor)
        {
            return Mathf.Abs(divisor) > Mathf.Epsilon ? value / divisor : value;
        }

        private void ApplyFacing(float horizontalTravel)
        {
            switch (facingMode)
            {
                case CharacterFacingMode.FollowHorizontalTravel:
                    if (Mathf.Abs(horizontalTravel) > Mathf.Epsilon)
                        SetFacing(horizontalTravel > 0f);
                    break;
                case CharacterFacingMode.FaceLeft:
                    SetFacing(false);
                    break;
                case CharacterFacingMode.FaceRight:
                    SetFacing(true);
                    break;
            }
        }

        private void ApplyFinalFacing(float horizontalTravel)
        {
            ApplyFacing(horizontalTravel);
        }

        private void SetFacing(bool faceRight)
        {
            if (actorRenderer == null)
                return;

            actorRenderer.flipX = sourceSpriteFacesLeft ? faceRight : !faceRight;
        }

        private static float SmootherStep(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * value * (value * (value * 6f - 15f) + 10f);
        }

        private void OnDrawGizmosSelected()
        {
            if (actor == null || targetTransform == null)
                return;

            Gizmos.color = new Color(.95f, .55f, .78f, 1f);
            Vector3 start = actor.position;
            Vector3 end = motionMode == CharacterMotionMode.VerticalInPlace
                ? start
                : targetTransform.position;
            Gizmos.DrawLine(start, end);
            Vector3 midpoint = Vector3.Lerp(start, end, .5f) + Vector3.up * arcHeight;
            Gizmos.DrawLine(start, midpoint);
            Gizmos.DrawLine(midpoint, end);
        }
    }
}
