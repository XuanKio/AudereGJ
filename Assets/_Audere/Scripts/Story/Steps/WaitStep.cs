using System.Collections;
using UnityEngine;

namespace Audere.Story.Steps
{
    public sealed class WaitStep : StoryStep
    {
        [SerializeField, Min(0f)] private float duration;
        [SerializeField] private bool useUnscaledTime = true;

        public float Duration => duration;
        public bool UseUnscaledTime => useUnscaledTime;

        protected override IEnumerator Execute()
        {
            if (duration <= 0f)
            {
                CompleteStep();
                yield break;
            }

            if (useUnscaledTime)
                yield return new WaitForSecondsRealtime(duration);
            else
                yield return new WaitForSeconds(duration);

            CompleteStep();
        }
    }
}
