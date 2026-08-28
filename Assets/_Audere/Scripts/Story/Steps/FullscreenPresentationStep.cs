using System.Collections;
using Audere.World;
using UnityEngine;

namespace Audere.Story.Steps
{
    /// <summary>Shared fullscreen profile without a world-mode swap (e.g. a brief dizzy spell).</summary>
    public sealed class FullscreenPresentationStep : StoryStep
    {
        [SerializeField] private FullscreenTransitionController controller;
        [SerializeField] private FullscreenTransitionProfile profile;
        [SerializeField] private Renderer focusRenderer;
        protected override IEnumerator Execute()
        {
            bool ended = false, succeeded = false;
            if (controller == null || controller.IsTransitioning ||
                !controller.PlayPresentation(profile, focusRenderer, ok => { ended = true; succeeded = ok; }))
            { FailStep(); yield break; }
            while (!ended) yield return null;
            if (succeeded) CompleteStep(); else Cancel();
        }
        protected override void OnCancelled() => controller?.CancelTransition();
    }
}
