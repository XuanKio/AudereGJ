using System.Collections;
using Audere.Story.Presentation;
using UnityEngine;

namespace Audere.Story.Steps
{
    public sealed class StoryIllustrationStep : StoryStep
    {
        [SerializeField] private StoryIllustrationOverlayView overlayView;

        public StoryIllustrationOverlayView OverlayView => overlayView;

        protected override IEnumerator Execute()
        {
            if (overlayView == null)
            {
                Debug.LogError("[StoryIllustrationStep] Overlay View reference is required.", this);
                FailStep();
                yield break;
            }

            bool dismissed = false;
            if (!overlayView.Show(this, () => dismissed = true))
            {
                FailStep();
                yield break;
            }

            while (!dismissed)
                yield return null;

            CompleteStep();
        }

        protected override void OnCancelled()
        {
            overlayView?.ForceHide(this);
        }
    }
}
