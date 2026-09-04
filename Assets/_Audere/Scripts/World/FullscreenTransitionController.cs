using System;
using System.Collections;
using Audere.Audio;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Audere.World
{
    [DisallowMultipleComponent]
    public sealed class FullscreenTransitionController : MonoBehaviour
    {
        private static readonly int FocusCenterId = Shader.PropertyToID("_FocusCenter");
        private static readonly int AspectRatioId = Shader.PropertyToID("_AspectRatio");

        [Header("Shared Renderer Feature")]
        [SerializeField] private Camera worldCamera;
        [SerializeField] private FullScreenPassRendererFeature rendererFeature;

        private GameObject shatterOverlay;
        private Texture2D frozenFrame;
        private ScreenShatterGraphic shatterView;
        private UnityEngine.UI.Image shatterCover;
        public ScreenShatterGraphic ShatterView => shatterView;

        private Material runtimeMaterial;
        private Material originalFeatureMaterial;
        private FullscreenTransitionProfile activeProfile;
        private Coroutine transitionRoutine;
        private Action<bool> activeCompletion;
        private int transitionVersion;

        public FullScreenPassRendererFeature RendererFeature => rendererFeature;
        public Camera WorldCamera => worldCamera;
        public FullscreenTransitionProfile ActiveProfile => activeProfile;
        public bool IsTransitioning { get; private set; }

        private void Awake()
        {
            ResetPresentation();
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
                CancelTransition();
        }

        private void OnDestroy()
        {
            transitionVersion++;
            if (transitionRoutine != null)
                StopCoroutine(transitionRoutine);
            transitionRoutine = null;
            activeCompletion = null;
            IsTransitioning = false;
            ResetPresentation();
        }

        public bool Play(
            FullscreenTransitionProfile profile,
            WorldModeController modeController,
            WorldGameplayMode targetMode,
            Renderer focusRenderer,
            Action<bool> onEnded)
        {
            return PlayInternal(profile, modeController, targetMode, focusRenderer, onEnded, true);
        }

        public bool PlayPresentation(FullscreenTransitionProfile profile, Renderer focusRenderer, Action<bool> onEnded, Action onSwap = null)
        {
            return PlayInternal(profile, null, default, focusRenderer, onEnded, false, onSwap);
        }

        private bool PlayInternal(FullscreenTransitionProfile profile, WorldModeController modeController,
            WorldGameplayMode targetMode, Renderer focusRenderer, Action<bool> onEnded, bool swapMode, Action onSwap = null)
        {
            if (!ValidateReferences(profile, modeController, focusRenderer, swapMode))
                return false;

            CancelTransition();
            if (!PrepareRuntimeMaterial(profile))
                return false;

            ApplyFocus(focusRenderer);

            int version = ++transitionVersion;
            activeCompletion = onEnded;
            IsTransitioning = true;
            if (swapMode) AudioService.Instance?.SetMusicDuck(this, 1f);

            profile.Apply(runtimeMaterial, 0f);
            rendererFeature.SetActive(true);
            transitionRoutine = StartCoroutine(
                RunTransition(version, profile, modeController, targetMode, swapMode, onSwap));
            return true;
        }

        public void CancelTransition()
        {
            if (!IsTransitioning && transitionRoutine == null)
            {
                ResetPresentation();
                return;
            }

            transitionVersion++;
            if (transitionRoutine != null)
                StopCoroutine(transitionRoutine);
            transitionRoutine = null;
            IsTransitioning = false;

            Action<bool> completion = activeCompletion;
            activeCompletion = null;
            ResetPresentation();
            completion?.Invoke(false);
        }

        private IEnumerator RunTransition(
            int version,
            FullscreenTransitionProfile profile,
            WorldModeController modeController,
            WorldGameplayMode targetMode, bool swapMode, Action onSwap)
        {
            float duration = profile.Duration;
            float swapTime = profile.ModeSwapTime;
            float elapsed = 0f;
            bool modeSwapped = false;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                if (swapMode) AudioService.Instance?.SetMusicDuck(this, EvaluateMusicGain(elapsed, swapTime));

                var shatter = profile.ScreenShatter;
                if (shatter != null && shatter.Enabled && frozenFrame == null && elapsed >= shatter.CaptureTime)
                {
                    // Render the source before enabling any snapshot overlay. Pausing the
                    // timeline here also guarantees capture before swap on a long frame.
                    elapsed = shatter.CaptureTime;
                    profile.Apply(runtimeMaterial, elapsed);
                    yield return new WaitForEndOfFrame();
                    if (version != transitionVersion) yield break;
                    frozenFrame = ScreenCapture.CaptureScreenshotAsTexture();
                    if (frozenFrame == null)
                    {
                        Debug.LogError("[FullscreenTransition] Could not capture the source frame.", this);
                        CancelTransition();
                        yield break;
                    }
                    frozenFrame.name = "Fullscreen Shatter Snapshot (Runtime)";
                    frozenFrame.hideFlags = HideFlags.HideAndDontSave;
                    CreateShatterOverlay(shatter);
                }

                if ((swapMode || onSwap != null) && !modeSwapped && elapsed >= swapTime)
                {
                    // Always render the profile's fully-covered swap state before revealing
                    // the target presentation, including after an unusually long frame.
                    ApplyPresentation(profile, swapTime);
                    if (swapMode) modeController.ApplyModeImmediate(targetMode);
                    onSwap?.Invoke();
                    if (version != transitionVersion) yield break;
                    modeSwapped = true;
                    yield return null;
                    if (version != transitionVersion)
                        yield break;
                }

                ApplyPresentation(profile, Mathf.Min(elapsed, duration));
                yield return null;
                if (version != transitionVersion)
                    yield break;
            }

            if (!modeSwapped)
            {
                if (swapMode) modeController.ApplyModeImmediate(targetMode);
                onSwap?.Invoke();
                if (version != transitionVersion) yield break;
            }

            profile.Apply(runtimeMaterial, duration);
            ResetPresentation();
            transitionRoutine = null;
            IsTransitioning = false;

            Action<bool> completion = activeCompletion;
            activeCompletion = null;
            completion?.Invoke(true);
        }

        private bool ValidateReferences(
            FullscreenTransitionProfile profile,
            WorldModeController modeController,
            Renderer focusRenderer, bool swapMode)
        {
            if ((swapMode && modeController == null) || profile == null || worldCamera == null || rendererFeature == null)
            {
                Debug.LogError(
                    "[FullscreenTransition] Assign profile, camera, mode controller, and renderer feature.",
                    this);
                return false;
            }

            if (!profile.Validate(out string profileError))
            {
                Debug.LogError($"[FullscreenTransition] {profileError}", profile);
                return false;
            }

            if (profile.UsesFocusRenderer && focusRenderer == null)
            {
                Debug.LogError(
                    $"[FullscreenTransition] Profile '{profile.name}' requires a focus renderer.",
                    this);
                return false;
            }

            if (!isActiveAndEnabled || (swapMode && !modeController.isActiveAndEnabled))
            {
                Debug.LogError(
                    "[FullscreenTransition] Both transition and world mode controllers must be active.",
                    this);
                return false;
            }

            return true;
        }

        private void ApplyFocus(Renderer focusRenderer)
        {
            Vector2 focus = new Vector2(.5f, .5f);
            if (activeProfile != null && activeProfile.UsesFocusRenderer && focusRenderer != null)
            {
                Vector3 viewport = worldCamera.WorldToViewportPoint(focusRenderer.bounds.center);
                focus = new Vector2(viewport.x, viewport.y);
            }

            runtimeMaterial.SetVector(FocusCenterId, focus);
            runtimeMaterial.SetFloat(
                AspectRatioId,
                worldCamera.pixelHeight > 0
                    ? (float)worldCamera.pixelWidth / worldCamera.pixelHeight
                    : 1f);
        }

        private bool PrepareRuntimeMaterial(FullscreenTransitionProfile profile)
        {
            ResetPresentation();
            if (profile == null || profile.Material == null || rendererFeature == null)
                return false;

            activeProfile = profile;
            originalFeatureMaterial = rendererFeature.passMaterial;
            runtimeMaterial = new Material(profile.Material)
            {
                name = profile.Material.name + " (Runtime)",
                hideFlags = HideFlags.HideAndDontSave,
            };
            rendererFeature.passMaterial = runtimeMaterial;
            rendererFeature.Create();
            return true;
        }

        private void CreateShatterOverlay(FullscreenShatterSettings settings)
        {
            shatterOverlay = new GameObject("Fullscreen Shatter (Runtime)", typeof(RectTransform), typeof(Canvas));
            shatterOverlay.hideFlags = HideFlags.HideAndDontSave;
            var canvas = shatterOverlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32760;
            var coverObject = new GameObject("Covered source", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            coverObject.transform.SetParent(shatterOverlay.transform, false);
            shatterCover = coverObject.GetComponent<UnityEngine.UI.Image>();
            Stretch(shatterCover.rectTransform);
            shatterCover.raycastTarget = false;
            shatterCover.color = new Color(.017f, .015f, .027f, 1f);
            var piecesObject = new GameObject("Frozen glass pieces", typeof(RectTransform), typeof(ScreenShatterGraphic));
            piecesObject.transform.SetParent(shatterOverlay.transform, false);
            shatterView = piecesObject.GetComponent<ScreenShatterGraphic>();
            Stretch(shatterView.rectTransform);
            shatterView.Initialize(frozenFrame, settings);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private void ApplyPresentation(FullscreenTransitionProfile profile, float time)
        {
            profile.Apply(runtimeMaterial, time);
            if (shatterView == null) return;
            // Swap the world under the still-intact frozen pane, then expose the target
            // through moving boundaries. Other profiles can retain a black covered swap.
            float cover = profile.ScreenShatter.RevealTargetBehindShards
                ? (time < profile.ModeSwapTime ? 1f : 0f)
                : (time <= profile.ModeSwapTime ? 1f : runtimeMaterial.GetFloat("_Cover"));
            runtimeMaterial.SetFloat("_Cover", cover);
            shatterCover.color = new Color(.017f, .015f, .027f, cover);
            shatterView.SetTime(time);
        }

        private void ResetPresentation()
        {
            if (shatterOverlay != null)
            {
                shatterOverlay.SetActive(false);
                if (Application.isPlaying) Destroy(shatterOverlay);
                else DestroyImmediate(shatterOverlay);
            }
            if (frozenFrame != null)
            {
                if (Application.isPlaying) Destroy(frozenFrame);
                else DestroyImmediate(frozenFrame);
            }
            shatterOverlay = null;
            shatterView = null;
            shatterCover = null;
            frozenFrame = null;
            AudioService.Instance?.ReleaseMusicOwner(this);
            if (rendererFeature != null)
            {
                rendererFeature.SetActive(false);
                if (runtimeMaterial != null)
                {
                    rendererFeature.passMaterial = originalFeatureMaterial;
                    rendererFeature.Create();
                }
            }

            if (runtimeMaterial != null)
            {
                if (Application.isPlaying)
                    Destroy(runtimeMaterial);
                else
                    DestroyImmediate(runtimeMaterial);
            }

            runtimeMaterial = null;
            originalFeatureMaterial = null;
            activeProfile = null;
        }

        // Fade down with the source, then hold silence until the entire shader has finished.
        public static float EvaluateMusicGain(float elapsed, float swapTime)
        {
            return 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / Mathf.Max(.01f, swapTime)));
        }
    }
}
