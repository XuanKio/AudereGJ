using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Audere.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(Image), typeof(CanvasGroup))]
    public sealed class CombatDieView : MonoBehaviour
    {
        [Header("Prefab Identity")]
        [SerializeField] private CombatSymbol prefabSymbol = CombatSymbol.Attack;

        [Header("Visuals")]
        [SerializeField] private Image background;
        [SerializeField] private Image shadowImage;
        [SerializeField] private RectTransform shadowRect;
        [SerializeField] private RectTransform frameRect;
        [SerializeField] private RectTransform faceRect;
        [SerializeField] private Image symbolIcon;
        [SerializeField] private TMP_Text symbolLabel;
        [FormerlySerializedAs("swordColor")]
        [SerializeField] private Color attackColor = new Color32(168, 59, 68, 255);
        [FormerlySerializedAs("shieldColor")]
        [SerializeField] private Color armorColor = new Color32(176, 171, 183, 255);
        [FormerlySerializedAs("dangerColor")]
        [SerializeField] private Color healColor = new Color32(216, 192, 151, 255);
        [SerializeField] private Color inactiveColor = new Color32(35, 33, 45, 255);
        [SerializeField] private Color activeIconColor = new Color32(168, 59, 68, 255);
        [SerializeField] private Color normalSymbolColor = new Color(.08f, .07f, .11f, 1f);

        [Header("2D Toss / Landing Reveal")]
        [SerializeField] private Vector2 launchDelayRange = new Vector2(0f, .12f);
        [SerializeField] private Vector2Int bounceCountRange = new Vector2Int(2, 3);
        [FormerlySerializedAs("fallHeight")]
        [SerializeField, Min(1f)] private float firstBounceHeight = 72f;
        [SerializeField, Range(.1f, .9f)] private float bounceHeightDecay = .48f;
        [FormerlySerializedAs("landingDuration")]
        [SerializeField, Min(.05f)] private float firstBounceDuration = .38f;
        [SerializeField, Range(.3f, 1f)] private float bounceDurationDecay = .78f;
        [SerializeField, Min(.05f)] private float landingSquashDuration = .07f;
        [SerializeField, Min(.1f)] private float tossTravelSpeedMultiplier = 1.2f;
        [SerializeField, Range(.4f, 1f)] private float shadowScaleAtApex = .72f;
        [SerializeField, Range(0f, .2f)] private float bodyScaleAtApex = .06f;

        [Header("Movement")]
        [SerializeField, Min(.05f)] private float speedMultiplier = 1f;
        [SerializeField] private bool rotateWhileMoving;
        [SerializeField] private Vector2 angularSpeedRange = new Vector2(-75f, 75f);

        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Vector2 velocity;
        private float angularVelocity;
        private bool captured;
        private bool isLanded;
        private bool tossMotionStarted;
        private Quaternion authoredRotation;
        private Vector3 authoredShadowScale;
        private Vector2 authoredFramePosition;
        private Vector2 authoredFacePosition;
        private Vector3 authoredFrameScale;
        private Vector3 authoredFaceScale;
        private RectTransform landedParent;
        private RectTransform airborneParent;

        public CombatSymbol Symbol { get; private set; }
        public CombatSymbol PrefabSymbol => prefabSymbol;
        public RectTransform RectTransform => rectTransform;
        public bool IsCaptured => captured;
        public bool CanInteract => isLanded && !captured;
        public bool IsInAirborneOverlay => airborneParent != null && rectTransform != null && rectTransform.parent == airborneParent;
        public Vector2 MoveVelocity => velocity;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (background == null) background = GetComponent<Image>();
            if (shadowImage == null) shadowImage = transform.Find("Shadow")?.GetComponent<Image>();
            if (shadowRect == null) shadowRect = transform.Find("Shadow") as RectTransform;
            if (frameRect == null) frameRect = transform.Find("Frame") as RectTransform;
            if (faceRect == null) faceRect = transform.Find("Face") as RectTransform;
            if (symbolIcon == null) symbolIcon = transform.Find("Face/Icon")?.GetComponent<Image>();
            if (symbolLabel == null) symbolLabel = GetComponentInChildren<TMP_Text>(true);
            authoredRotation = rectTransform.localRotation;
            authoredShadowScale = shadowRect != null ? shadowRect.localScale : Vector3.one;
            authoredFramePosition = frameRect != null ? frameRect.anchoredPosition : Vector2.zero;
            authoredFacePosition = faceRect != null ? faceRect.anchoredPosition : Vector2.zero;
            authoredFrameScale = frameRect != null ? frameRect.localScale : Vector3.one;
            authoredFaceScale = faceRect != null ? faceRect.localScale : Vector3.one;
        }

        public void ConfigureVisuals(CombatSymbol authoredSymbol, Image image, TMP_Text label)
        {
            prefabSymbol = authoredSymbol;
            background = image;
            symbolLabel = label;
        }

        public void ConfigurePresentationRoots(RectTransform landedRoot, RectTransform airborneRoot)
        {
            landedParent = landedRoot;
            airborneParent = airborneRoot;
        }

        public void Setup(CombatSymbol symbol, Vector2 startPosition, Vector2 moveVelocity)
        {
            if (rectTransform == null) Awake();
            StopAllCoroutines();
            MoveToPresentationRoot(landedParent);
            captured = false;
            isLanded = false;
            tossMotionStarted = false;
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = false;
            rectTransform.anchoredPosition = startPosition;
            rectTransform.localScale = Vector3.one;
            rectTransform.localRotation = authoredRotation;
            velocity = moveVelocity * speedMultiplier;
            angularVelocity = rotateWhileMoving
                ? UnityEngine.Random.Range(angularSpeedRange.x, angularSpeedRange.y)
                : 0f;
            SetSymbol(symbol);
            ApplyVisualOffset(0f);
            ApplyVisualScale(Vector3.one);
            ApplyShadowScale(1f);
            ApplyAirborneVisual();
            MoveToPresentationRoot(airborneParent);
            StartCoroutine(TossAnimation());
        }

        public void SetSymbol(CombatSymbol symbol)
        {
            Symbol = symbol;
            if (isLanded) ApplyLandedVisual();
            else ApplyAirborneVisual();

            if (symbolLabel != null)
            {
                symbolLabel.color = normalSymbolColor;
                symbolLabel.text = symbol switch
                {
                    CombatSymbol.Attack => "ATK",
                    CombatSymbol.Armor => "ARM",
                    _ => "HEAL",
                };
            }
        }

        public void TickMovement(Rect playRect, float deltaTime)
        {
            if (captured || rectTransform == null || (!isLanded && !tossMotionStarted))
                return;

            float travelMultiplier = isLanded ? 1f : tossTravelSpeedMultiplier;
            Vector2 position = rectTransform.anchoredPosition + velocity * travelMultiplier * deltaTime;
            rectTransform.anchoredPosition = position;
            ConstrainToBounds(playRect);

            if (rotateWhileMoving)
                rectTransform.Rotate(0f, 0f, angularVelocity * deltaTime);
        }

        public bool ResolveCollisionWith(CombatDieView other, float bounciness, float separationPadding)
        {
            if (other == null || other == this || captured || other.captured ||
                rectTransform == null || other.rectTransform == null ||
                !gameObject.activeInHierarchy || !other.gameObject.activeInHierarchy)
                return false;

            RectTransform collisionSpace = landedParent != null
                ? landedParent
                : rectTransform.parent as RectTransform;
            if (collisionSpace == null) return false;

            Vector2 center = GetCenterInSpace(rectTransform, collisionSpace);
            Vector2 otherCenter = GetCenterInSpace(other.rectTransform, collisionSpace);
            Vector2 halfExtents = GetHalfExtentsInSpace(rectTransform, collisionSpace);
            Vector2 otherHalfExtents = GetHalfExtentsInSpace(other.rectTransform, collisionSpace);
            Vector2 delta = otherCenter - center;
            float overlapX = halfExtents.x + otherHalfExtents.x - Mathf.Abs(delta.x);
            float overlapY = halfExtents.y + otherHalfExtents.y - Mathf.Abs(delta.y);
            if (overlapX <= 0f || overlapY <= 0f) return false;

            Vector2 normal;
            float penetration;
            if (overlapX < overlapY)
            {
                float direction = Mathf.Abs(delta.x) > .001f
                    ? Mathf.Sign(delta.x)
                    : GetInstanceID() < other.GetInstanceID() ? -1f : 1f;
                normal = Vector2.right * direction;
                penetration = overlapX;
            }
            else
            {
                float direction = Mathf.Abs(delta.y) > .001f
                    ? Mathf.Sign(delta.y)
                    : GetInstanceID() < other.GetInstanceID() ? -1f : 1f;
                normal = Vector2.up * direction;
                penetration = overlapY;
            }

            float correctionDistance = (penetration + Mathf.Max(0f, separationPadding)) * .5f;
            TranslateInSpace(rectTransform, collisionSpace, -normal * correctionDistance);
            TranslateInSpace(other.rectTransform, collisionSpace, normal * correctionDistance);

            Vector2 relativeVelocity = other.velocity - velocity;
            float velocityAlongNormal = Vector2.Dot(relativeVelocity, normal);
            if (velocityAlongNormal < 0f)
            {
                float restitution = Mathf.Clamp01(bounciness);
                float impulseMagnitude = -(1f + restitution) * velocityAlongNormal * .5f;
                Vector2 impulse = normal * impulseMagnitude;
                velocity -= impulse;
                other.velocity += impulse;
            }

            return true;
        }

        public void ConstrainToBounds(Rect playRect)
        {
            if (rectTransform == null) return;

            Vector2 position = rectTransform.anchoredPosition;
            Vector2 halfSize = rectTransform.rect.size * .5f;

            float minX = playRect.xMin + halfSize.x;
            float maxX = playRect.xMax - halfSize.x;
            float minY = playRect.yMin + halfSize.y;
            float maxY = playRect.yMax - halfSize.y;

            if (position.x < minX)
            {
                position.x = minX;
                if (velocity.x < 0f) velocity.x *= -1f;
            }
            else if (position.x > maxX)
            {
                position.x = maxX;
                if (velocity.x > 0f) velocity.x *= -1f;
            }

            if (position.y < minY)
            {
                position.y = minY;
                if (velocity.y < 0f) velocity.y *= -1f;
            }
            else if (position.y > maxY)
            {
                position.y = maxY;
                if (velocity.y > 0f) velocity.y *= -1f;
            }

            rectTransform.anchoredPosition = position;
        }

        public void Reroll(CombatSymbol nextSymbol)
        {
            if (!CanInteract)
                return;

            StopAllCoroutines();
            isLanded = false;
            tossMotionStarted = false;
            canvasGroup.blocksRaycasts = false;
            SetSymbol(nextSymbol);
            ApplyVisualOffset(0f);
            ApplyVisualScale(Vector3.one);
            ApplyShadowScale(1f);
            MoveToPresentationRoot(airborneParent);
            StartCoroutine(TossAnimation());
        }

        public void PlayCaptured()
        {
            if (!CanInteract)
                return;

            captured = true;
            canvasGroup.blocksRaycasts = false;
            StopAllCoroutines();
            StartCoroutine(CaptureAnimation());
        }

        public void ReturnToPool()
        {
            StopAllCoroutines();
            MoveToPresentationRoot(landedParent);

            captured = false;
            isLanded = false;
            tossMotionStarted = false;
            velocity = Vector2.zero;
            angularVelocity = 0f;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = false;
            }

            if (rectTransform != null)
            {
                rectTransform.localScale = Vector3.one;
                rectTransform.localRotation = authoredRotation;
            }
            ApplyVisualOffset(0f);
            ApplyVisualScale(Vector3.one);
            ApplyShadowScale(1f);

            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        private IEnumerator TossAnimation()
        {
            yield return null;

            float delay = UnityEngine.Random.Range(
                Mathf.Min(launchDelayRange.x, launchDelayRange.y),
                Mathf.Max(launchDelayRange.x, launchDelayRange.y));
            float elapsed = 0f;
            while (elapsed < delay)
            {
                elapsed += Mathf.Min(Time.unscaledDeltaTime, .05f);
                yield return null;
            }

            tossMotionStarted = true;
            int minimumBounces = Mathf.Max(1, Mathf.Min(bounceCountRange.x, bounceCountRange.y));
            int maximumBounces = Mathf.Max(minimumBounces, Mathf.Max(bounceCountRange.x, bounceCountRange.y));
            int bounceCount = UnityEngine.Random.Range(minimumBounces, maximumBounces + 1);

            for (int bounceIndex = 0; bounceIndex < bounceCount; bounceIndex++)
            {
                float height = firstBounceHeight * Mathf.Pow(bounceHeightDecay, bounceIndex);
                float duration = firstBounceDuration * Mathf.Pow(bounceDurationDecay, bounceIndex);
                elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Mathf.Min(Time.unscaledDeltaTime, .05f);
                    float t = Mathf.Clamp01(elapsed / duration);
                    float arc = 4f * t * (1f - t);
                    ApplyVisualOffset(height * arc);
                    ApplyVisualScale(Vector3.one * (1f + bodyScaleAtApex * arc));
                    ApplyShadowScale(Mathf.Lerp(1f, shadowScaleAtApex, arc));
                    yield return null;
                }

                ApplyVisualOffset(0f);
                ApplyShadowScale(1f);
                yield return PlayLandingSquash();
            }

            ApplyVisualOffset(0f);
            ApplyVisualScale(Vector3.one);
            ApplyShadowScale(1f);
            MoveToPresentationRoot(landedParent);
            ApplyLandedVisual();
            isLanded = true;
            tossMotionStarted = false;
            canvasGroup.blocksRaycasts = true;
        }

        private IEnumerator PlayLandingSquash()
        {
            float elapsed = 0f;
            Vector3 impactScale = new Vector3(1.08f, .88f, 1f);
            ApplyVisualScale(impactScale);
            while (elapsed < landingSquashDuration)
            {
                elapsed += Mathf.Min(Time.unscaledDeltaTime, .05f);
                float t = Mathf.Clamp01(elapsed / landingSquashDuration);
                float eased = 1f - Mathf.Pow(1f - t, 2f);
                ApplyVisualScale(Vector3.LerpUnclamped(impactScale, Vector3.one, eased));
                yield return null;
            }

            ApplyVisualScale(Vector3.one);
        }

        private void ApplyAirborneVisual()
        {
            SetShadowAlpha(1f);
            if (symbolIcon != null) symbolIcon.color = inactiveColor;
            if (background != null)
                background.color = symbolIcon != null ? Color.clear : inactiveColor;
        }

        private void ApplyLandedVisual()
        {
            Color symbolColor = Symbol switch
            {
                CombatSymbol.Attack => attackColor,
                CombatSymbol.Armor => armorColor,
                _ => healColor,
            };

            SetShadowAlpha(0f);
            if (symbolIcon != null) symbolIcon.color = activeIconColor;
            if (background != null)
                background.color = symbolIcon != null ? Color.clear : symbolColor;
        }

        private void SetShadowAlpha(float alpha)
        {
            if (shadowImage == null) return;
            Color color = inactiveColor;
            color.a = Mathf.Clamp01(alpha);
            shadowImage.color = color;
        }

        private void ApplyVisualOffset(float yOffset)
        {
            if (frameRect != null)
                frameRect.anchoredPosition = authoredFramePosition + Vector2.up * yOffset;
            if (faceRect != null)
                faceRect.anchoredPosition = authoredFacePosition + Vector2.up * yOffset;
        }

        private void ApplyVisualScale(Vector3 multiplier)
        {
            if (frameRect != null)
                frameRect.localScale = Vector3.Scale(authoredFrameScale, multiplier);
            if (faceRect != null)
                faceRect.localScale = Vector3.Scale(authoredFaceScale, multiplier);
        }

        private void ApplyShadowScale(float multiplier)
        {
            if (shadowRect != null)
                shadowRect.localScale = authoredShadowScale * multiplier;
        }

        private static Vector2 GetCenterInSpace(RectTransform target, RectTransform space)
        {
            Vector3 worldCenter = target.TransformPoint(target.rect.center);
            Vector3 localCenter = space.InverseTransformPoint(worldCenter);
            return new Vector2(localCenter.x, localCenter.y);
        }

        private static Vector2 GetHalfExtentsInSpace(RectTransform target, RectTransform space)
        {
            Vector2 halfSize = target.rect.size * .5f;
            Vector3 localX = space.InverseTransformVector(target.TransformVector(new Vector3(halfSize.x, 0f, 0f)));
            Vector3 localY = space.InverseTransformVector(target.TransformVector(new Vector3(0f, halfSize.y, 0f)));
            return new Vector2(
                Mathf.Abs(localX.x) + Mathf.Abs(localY.x),
                Mathf.Abs(localX.y) + Mathf.Abs(localY.y));
        }

        private static void TranslateInSpace(RectTransform target, RectTransform space, Vector2 displacement)
        {
            Vector3 worldDisplacement = space.TransformVector(new Vector3(displacement.x, displacement.y, 0f));
            target.position += worldDisplacement;
        }

        private void MoveToPresentationRoot(RectTransform targetRoot)
        {
            if (rectTransform == null || targetRoot == null || rectTransform.parent == targetRoot)
                return;

            Vector2 position = rectTransform.anchoredPosition;
            Vector3 scale = rectTransform.localScale;
            Quaternion rotation = rectTransform.localRotation;
            rectTransform.SetParent(targetRoot, false);
            rectTransform.anchoredPosition = position;
            rectTransform.localScale = scale;
            rectTransform.localRotation = rotation;
        }

        private IEnumerator PulseReroll()
        {
            const float duration = .16f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float pulse = 1f + Mathf.Sin(t * Mathf.PI) * .17f;
                rectTransform.localScale = Vector3.one * pulse;
                yield return null;
            }
            rectTransform.localScale = Vector3.one;
        }

        private IEnumerator CaptureAnimation()
        {
            const float duration = .20f;
            float elapsed = 0f;
            Vector3 startScale = rectTransform.localScale;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                rectTransform.localScale = Vector3.LerpUnclamped(startScale, Vector3.one * 1.28f, eased);
                canvasGroup.alpha = 1f - eased;
                yield return null;
            }
            ReturnToPool();
        }
    }
}
