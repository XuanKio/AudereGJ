using System;
using System.Collections;
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
            if (!ValidateReferences(profile, modeController, focusRenderer))
                return false;

            CancelTransition();
            if (!PrepareRuntimeMaterial(profile))
                return false;

            ApplyFocus(focusRenderer);

            int version = ++transitionVersion;
            activeCompletion = onEnded;
            IsTransitioning = true;

            profile.Apply(runtimeMaterial, 0f);
            rendererFeature.SetActive(true);
            transitionRoutine = StartCoroutine(
                RunTransition(version, profile, modeController, targetMode));
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
            WorldGameplayMode targetMode)
        {
            float duration = profile.Duration;
            float swapTime = profile.ModeSwapTime;
            float elapsed = 0f;
            bool modeSwapped = false;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;

                if (!modeSwapped && elapsed >= swapTime)
                {
                    // Always render the profile's fully-covered swap state before revealing
                    // the target presentation, including after an unusually long frame.
                    profile.Apply(runtimeMaterial, swapTime);
                    modeController.ApplyModeImmediate(targetMode);
                    modeSwapped = true;
                    yield return null;
                    if (version != transitionVersion)
                        yield break;
                }

                profile.Apply(runtimeMaterial, Mathf.Min(elapsed, duration));
                yield return null;
                if (version != transitionVersion)
                    yield break;
            }

            if (!modeSwapped)
                modeController.ApplyModeImmediate(targetMode);

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
            Renderer focusRenderer)
        {
            if (modeController == null || profile == null || worldCamera == null || rendererFeature == null)
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

            if (!isActiveAndEnabled || !modeController.isActiveAndEnabled)
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

        private void ResetPresentation()
        {
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
    }
}
