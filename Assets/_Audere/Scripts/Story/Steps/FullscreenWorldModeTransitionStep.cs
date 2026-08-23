using System.Collections;
using Audere.World;
using UnityEngine;

namespace Audere.Story.Steps
{
    public sealed class FullscreenWorldModeTransitionStep : StoryStep
    {
        [Header("Direct References")]
        [SerializeField] private FullscreenTransitionController transitionController;
        [SerializeField] private WorldModeController worldModeController;
        [SerializeField] private FullscreenTransitionProfile transitionProfile;
        [SerializeField] private Renderer focusRenderer;

        [Header("Mode Contract")]
        [SerializeField] private WorldGameplayMode sourceMode = WorldGameplayMode.Story;
        [SerializeField] private WorldGameplayMode targetMode = WorldGameplayMode.Combat;

        private bool transitionEnded;
        private bool transitionSucceeded;

        public FullscreenTransitionController TransitionController => transitionController;
        public WorldModeController WorldModeController => worldModeController;
        public FullscreenTransitionProfile TransitionProfile => transitionProfile;
        public Renderer FocusRenderer => focusRenderer;
        public WorldGameplayMode SourceMode => sourceMode;
        public WorldGameplayMode TargetMode => targetMode;

        protected override IEnumerator Execute()
        {
            transitionEnded = false;
            transitionSucceeded = false;

            if (!ValidateReferences())
                yield break;

            if (worldModeController.CurrentMode != sourceMode)
            {
                Debug.LogError(
                    $"[FullscreenWorldModeTransitionStep] Expected '{sourceMode}' but current mode " +
                    $"is '{worldModeController.CurrentMode}'.",
                    this);
                FailStep();
                yield break;
            }

            bool started = transitionController.Play(
                transitionProfile,
                worldModeController,
                targetMode,
                focusRenderer,
                OnTransitionEnded);
            if (!started)
            {
                RestoreSourceMode();
                FailStep();
                yield break;
            }

            while (!transitionEnded)
                yield return null;

            if (!transitionSucceeded || worldModeController.CurrentMode != targetMode)
            {
                RestoreSourceMode();
                Debug.LogError(
                    "[FullscreenWorldModeTransitionStep] Transition ended without the target mode.",
                    this);
                FailStep();
                yield break;
            }

            CompleteStep();
        }

        protected override void OnCancelled()
        {
            if (transitionController != null)
                transitionController.CancelTransition();
            RestoreSourceMode();
        }

        private bool ValidateReferences()
        {
            if (transitionController == null || worldModeController == null || transitionProfile == null)
            {
                Debug.LogError(
                    "[FullscreenWorldModeTransitionStep] Assign profile and both controllers.",
                    this);
                FailStep();
                return false;
            }

            if (!transitionController.isActiveAndEnabled || !worldModeController.isActiveAndEnabled)
            {
                Debug.LogError(
                    "[FullscreenWorldModeTransitionStep] Both controllers must be active and enabled.",
                    this);
                FailStep();
                return false;
            }

            if (transitionProfile.UsesFocusRenderer && focusRenderer == null)
            {
                Debug.LogError(
                    "[FullscreenWorldModeTransitionStep] This profile requires a focus renderer.",
                    this);
                FailStep();
                return false;
            }

            return true;
        }

        private void OnTransitionEnded(bool succeeded)
        {
            transitionSucceeded = succeeded;
            transitionEnded = true;
        }

        private void RestoreSourceMode()
        {
            if (worldModeController != null)
                worldModeController.ApplyModeImmediate(sourceMode);
        }
    }
}
