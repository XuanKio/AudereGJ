using System.Collections;
using UnityEngine;

namespace Audere.Story
{
    public sealed class DebugStoryStep : StoryStep
    {
        [SerializeField, TextArea] private string message;
        [SerializeField, Min(0f)] private float unscaledDelay;

        public string Message => message;
        public float UnscaledDelay => unscaledDelay;

        protected override IEnumerator Execute()
        {
            Debug.Log($"[DebugStoryStep] Start: {message}", this);

            float elapsed = 0f;
            while (elapsed < unscaledDelay)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            Debug.Log($"[DebugStoryStep] Complete: {message}", this);
        }
    }
}
