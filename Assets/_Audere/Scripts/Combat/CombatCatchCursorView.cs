using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Audere.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class CombatCatchCursorView : MonoBehaviour
    {
        [SerializeField] private Image fill;
        [SerializeField] private RectTransform borderRoot;
        [SerializeField] private CanvasGroup blockedX;
        [SerializeField] private Color readyColor = new Color(.95f, .92f, 1f, .96f);
        [SerializeField] private Color stunnedColor = new Color(.48f, .31f, .46f, .96f);
        [SerializeField] private Color readyFill = new Color(1f, 1f, 1f, .015f);
        [SerializeField] private Color stunnedFill = new Color(.34f, .22f, .34f, .10f);
        [SerializeField, Min(.01f)] private float colorTransitionDuration = .08f;
        [SerializeField, Min(.05f)] private float blockedFeedbackDuration = .44f;
        [SerializeField, Range(90f, 540f)] private float blockedSpinDegrees = 320f;
        [SerializeField, Range(.01f, .5f)] private float blockedStartScale = .06f;
        [Header("Reroll Feedback")]
        [SerializeField, Min(.05f)] private float rerollFeedbackDuration = .22f;
        [SerializeField, Range(1f, 90f)] private float rerollClockwiseDegrees = 22f;
        [SerializeField, Range(0f, .25f)] private float rerollScalePunch = .05f;

        private Graphic[] borderParts;
        private Coroutine colorRoutine;
        private Coroutine blockedRoutine;
        private Coroutine rerollRoutine;
        private Quaternion borderAuthoredRotation = Quaternion.identity;
        private Vector3 borderAuthoredScale = Vector3.one;
        private bool borderPoseCaptured;

        public bool IsStunned { get; private set; }

        private void Awake()
        {
            ResolveReferences();
            ApplyState(IsStunned ? 1f : 0f);
            ResetBlockedFeedback();
        }

        public void Configure(Image cursorFill, RectTransform cursorBorderRoot, CanvasGroup blockedFeedback)
        {
            fill = cursorFill;
            borderRoot = cursorBorderRoot;
            blockedX = blockedFeedback;
            ResolveReferences();
            ApplyState(IsStunned ? 1f : 0f);
        }

        public void SetStunned(bool stunned, bool immediate = false)
        {
            ResolveReferences();
            if (IsStunned == stunned && !immediate)
                return;

            IsStunned = stunned;
            if (colorRoutine != null)
                StopCoroutine(colorRoutine);

            if (!isActiveAndEnabled || immediate)
            {
                ApplyState(stunned ? 1f : 0f);
                colorRoutine = null;
                return;
            }

            colorRoutine = StartCoroutine(AnimateState(stunned));
        }

        public void PlayBlockedFeedback()
        {
            ResolveReferences();
            if (blockedX == null || !isActiveAndEnabled)
                return;

            if (blockedRoutine != null)
                StopCoroutine(blockedRoutine);
            blockedRoutine = StartCoroutine(AnimateBlockedX());
        }

        public void PlayRerollFeedback()
        {
            ResolveReferences();
            if (borderRoot == null || !isActiveAndEnabled)
                return;

            if (rerollRoutine != null)
                StopCoroutine(rerollRoutine);
            ResetBorderPose();
            rerollRoutine = StartCoroutine(AnimateReroll());
        }

        private IEnumerator AnimateState(bool stunned)
        {
            Color startBorder = borderParts != null && borderParts.Length > 0 && borderParts[0] != null
                ? borderParts[0].color
                : readyColor;
            Color startFill = fill != null ? fill.color : readyFill;
            Color targetBorder = stunned ? stunnedColor : readyColor;
            Color targetFill = stunned ? stunnedFill : readyFill;
            float elapsed = 0f;

            while (elapsed < colorTransitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / colorTransitionDuration);
                float eased = t * t * (3f - 2f * t);
                SetBorderColor(Color.Lerp(startBorder, targetBorder, eased));
                if (fill != null)
                    fill.color = Color.Lerp(startFill, targetFill, eased);
                yield return null;
            }

            ApplyState(stunned ? 1f : 0f);
            colorRoutine = null;
        }

        private IEnumerator AnimateBlockedX()
        {
            blockedX.gameObject.SetActive(true);
            blockedX.alpha = 0f;
            blockedX.transform.localScale = Vector3.one * blockedStartScale;
            blockedX.transform.localRotation = Quaternion.Euler(0f, 0f, -blockedSpinDegrees);
            float elapsed = 0f;

            while (elapsed < blockedFeedbackDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / blockedFeedbackDuration);
                float growT = Mathf.Clamp01(t / .62f);
                float grow = EaseOutBack(growT);
                float settle = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.62f, .80f, t));
                float scale = Mathf.LerpUnclamped(blockedStartScale, 1f, grow);
                scale = Mathf.Lerp(scale, 1f, settle);

                float rotationT = 1f - Mathf.Pow(1f - growT, 4f);
                float rotation = Mathf.LerpUnclamped(-blockedSpinDegrees, 0f, rotationT);
                rotation = Mathf.Lerp(rotation, 0f, settle);

                float fadeIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / .14f));
                float fadeOut = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.72f, 1f, t));
                blockedX.transform.localScale = Vector3.one * scale;
                blockedX.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
                blockedX.alpha = fadeIn * fadeOut;
                yield return null;
            }

            ResetBlockedFeedback();
            blockedRoutine = null;
        }

        private IEnumerator AnimateReroll()
        {
            float elapsed = 0f;
            while (elapsed < rerollFeedbackDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / rerollFeedbackDuration);
                float turnAmount;
                if (t < .42f)
                {
                    float kickT = t / .42f;
                    turnAmount = 1f - Mathf.Pow(1f - kickT, 3f);
                }
                else
                {
                    float settleT = Mathf.InverseLerp(.42f, 1f, t);
                    turnAmount = 1f - Mathf.SmoothStep(0f, 1f, settleT);
                }

                float scalePulse = 1f + Mathf.Sin(t * Mathf.PI) * rerollScalePunch;
                borderRoot.localRotation = borderAuthoredRotation *
                    Quaternion.Euler(0f, 0f, -rerollClockwiseDegrees * turnAmount);
                borderRoot.localScale = borderAuthoredScale * scalePulse;
                yield return null;
            }

            ResetBorderPose();
            rerollRoutine = null;
        }

        private static float EaseOutBack(float t)
        {
            const float overshoot = 1.45f;
            float shifted = t - 1f;
            return 1f + (overshoot + 1f) * shifted * shifted * shifted +
                overshoot * shifted * shifted;
        }

        private void ResetBlockedFeedback()
        {
            if (blockedX == null) return;
            blockedX.alpha = 0f;
            blockedX.transform.localScale = Vector3.one;
            blockedX.transform.localRotation = Quaternion.identity;
            blockedX.gameObject.SetActive(false);
        }

        private void ApplyState(float stunnedAmount)
        {
            SetBorderColor(Color.Lerp(readyColor, stunnedColor, stunnedAmount));
            if (fill != null)
                fill.color = Color.Lerp(readyFill, stunnedFill, stunnedAmount);
        }

        private void SetBorderColor(Color color)
        {
            if (borderParts == null)
                return;
            for (int i = 0; i < borderParts.Length; i++)
            {
                if (borderParts[i] != null)
                    borderParts[i].color = color;
            }
        }

        private void OnDisable()
        {
            blockedRoutine = null;
            rerollRoutine = null;
            ResetBorderPose();
            ResetBlockedFeedback();
        }

        private void ResetBorderPose()
        {
            if (borderRoot == null || !borderPoseCaptured)
                return;
            borderRoot.localRotation = borderAuthoredRotation;
            borderRoot.localScale = borderAuthoredScale;
        }

        private void ResolveReferences()
        {
            if (fill == null)
                fill = GetComponent<Image>();
            if (borderRoot == null)
            {
                Transform child = transform.Find("Cursor Border");
                if (child is RectTransform rect)
                    borderRoot = rect;
            }
            if (blockedX == null)
                blockedX = transform.Find("Blocked X")?.GetComponent<CanvasGroup>();

            if (borderRoot != null && !borderPoseCaptured)
            {
                borderAuthoredRotation = borderRoot.localRotation;
                borderAuthoredScale = borderRoot.localScale;
                borderPoseCaptured = true;
            }

            borderParts = borderRoot != null
                ? borderRoot.GetComponentsInChildren<Graphic>(true)
                : null;
        }
    }
}
