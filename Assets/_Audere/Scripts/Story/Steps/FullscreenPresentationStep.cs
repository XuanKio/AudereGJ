using System;
using System.Collections;
using Audere.World;
using UnityEngine;

namespace Audere.Story.Steps
{
    /// <summary>Shared fullscreen profile, optionally swapping authored scenery under its cover.</summary>
    public sealed class FullscreenPresentationStep : StoryStep
    {
        [SerializeField] private FullscreenTransitionController controller;
        [SerializeField] private FullscreenTransitionProfile profile;
        [SerializeField] private Renderer focusRenderer;
        [SerializeField] private GameObject[] enableAtSwap = Array.Empty<GameObject>();
        [SerializeField] private GameObject[] disableAtSwap = Array.Empty<GameObject>();
        private bool[] enabledBefore, disabledBefore;

        protected override IEnumerator Execute()
        {
            bool ended = false, succeeded = false;
            enabledBefore = Capture(enableAtSwap);
            disabledBefore = Capture(disableAtSwap);
            Action swap = enableAtSwap.Length + disableAtSwap.Length > 0 ? Swap : (Action)null;
            if (controller == null || controller.IsTransitioning ||
                !controller.PlayPresentation(profile, focusRenderer,
                    ok => { ended = true; succeeded = ok; }, swap))
            { FailStep(); yield break; }
            while (!ended) yield return null;
            if (succeeded) CompleteStep(); else Cancel();
        }

        private void Swap()
        {
            foreach (var target in disableAtSwap) if (target != null) target.SetActive(false);
            foreach (var target in enableAtSwap) if (target != null) target.SetActive(true);
        }

        private static bool[] Capture(GameObject[] targets)
        {
            var result = new bool[targets.Length];
            for (int i = 0; i < targets.Length; i++) result[i] = targets[i] != null && targets[i].activeSelf;
            return result;
        }

        private static void Restore(GameObject[] targets, bool[] states)
        {
            if (states == null) return;
            for (int i = 0; i < targets.Length && i < states.Length; i++)
                if (targets[i] != null) targets[i].SetActive(states[i]);
        }

        protected override void OnCancelled()
        {
            controller?.CancelTransition();
            Restore(disableAtSwap, disabledBefore);
            Restore(enableAtSwap, enabledBefore);
        }
    }
}
