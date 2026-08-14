using System;
using System.Collections;
using UnityEngine;

namespace Audere.Puzzle.Tweening
{
    /// <summary>
    /// Small dependency-free gameplay tween used for short, deterministic effects.
    /// Keeps presentation code declarative without adding a project-wide package.
    /// </summary>
    public sealed class GameplayTween
    {
        private readonly float duration;
        private Action<float> update;

        public GameplayTween(float seconds)
        {
            duration = Mathf.Max(.001f, seconds);
        }

        public GameplayTween OnUpdate(Action<float> callback)
        {
            update = callback;
            return this;
        }

        public IEnumerator Play()
        {
            float elapsed = 0f;
            update?.Invoke(0f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                update?.Invoke(Mathf.Clamp01(elapsed / duration));
                yield return null;
            }

            update?.Invoke(1f);
        }

        public static float EaseOutCubic(float value)
        {
            float inverse = 1f - Mathf.Clamp01(value);
            return 1f - inverse * inverse * inverse;
        }

        public static float EaseInCubic(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * value;
        }

        public static float EaseInOutCubic(float value)
        {
            value = Mathf.Clamp01(value);
            return value < .5f
                ? 4f * value * value * value
                : 1f - Mathf.Pow(-2f * value + 2f, 3f) * .5f;
        }
    }
}
