using System.Collections;
using UnityEngine;

namespace Audere.Story.Steps
{
    public sealed class SpriteGroupFadeStep : StoryStep
    {
        [Header("Direct References")]
        [SerializeField] private SpriteRenderer[] renderers;
        [SerializeField] private float[] authoredAlphas;

        [Header("Visibility")]
        [SerializeField, Range(0f, 1f)] private float targetVisibility;
        [SerializeField, Min(0f)] private float duration = .28f;
        [SerializeField] private bool useUnscaledTime = true;

        private Color[] startColors;

        public SpriteRenderer[] Renderers => renderers;
        public float TargetVisibility => targetVisibility;

        protected override IEnumerator Execute()
        {
            if (renderers == null || renderers.Length == 0)
            {
                Debug.LogError("[SpriteGroupFadeStep] At least one direct SpriteRenderer reference is required.", this);
                FailStep();
                yield break;
            }

            startColors = new Color[renderers.Length];
            Color[] targetColors = new Color[renderers.Length];
            for (int index = 0; index < renderers.Length; index++)
            {
                SpriteRenderer renderer = renderers[index];
                if (renderer == null)
                {
                    Debug.LogError($"[SpriteGroupFadeStep] Renderer {index} is missing.", this);
                    RestoreStartColors();
                    FailStep();
                    yield break;
                }

                startColors[index] = renderer.color;
                float authoredAlpha = authoredAlphas != null && index < authoredAlphas.Length
                    ? authoredAlphas[index]
                    : renderer.color.a;
                Color target = renderer.color;
                target.a = authoredAlpha * targetVisibility;
                targetColors[index] = target;
            }

            if (duration <= Mathf.Epsilon)
            {
                ApplyColors(targetColors);
                CompleteStep();
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float eased = progress * progress * (3f - 2f * progress);
                for (int index = 0; index < renderers.Length; index++)
                    renderers[index].color = Color.LerpUnclamped(startColors[index], targetColors[index], eased);
                yield return null;
            }

            ApplyColors(targetColors);
            CompleteStep();
        }

        protected override void OnCancelled()
        {
            RestoreStartColors();
        }

        private void ApplyColors(Color[] colors)
        {
            for (int index = 0; index < renderers.Length; index++)
                if (renderers[index] != null)
                    renderers[index].color = colors[index];
        }

        private void RestoreStartColors()
        {
            if (startColors == null || renderers == null)
                return;
            ApplyColors(startColors);
        }
    }
}
