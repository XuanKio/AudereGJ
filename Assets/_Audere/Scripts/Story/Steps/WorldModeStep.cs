using System.Collections;
using Audere.World;
using UnityEngine;

namespace Audere.Story.Steps
{
    public sealed class WorldModeStep : StoryStep
    {
        [SerializeField] private WorldModeController worldModeController;
        [SerializeField] private WorldGameplayMode targetMode;
        [SerializeField] private bool waitUntilTransitionFinished = true;

        public WorldModeController WorldModeController => worldModeController;
        public WorldGameplayMode TargetMode => targetMode;
        public bool WaitUntilTransitionFinished => waitUntilTransitionFinished;

        protected override IEnumerator Execute()
        {
            WorldModeController controller = worldModeController;
            if (!ValidateController(controller))
                yield break;

            while (controller.IsTransitioning)
            {
                if (!ValidateController(controller))
                    yield break;

                yield return null;
            }

            if (controller.CurrentMode == targetMode)
            {
                CompleteStep();
                yield break;
            }

            controller.SwitchTo(targetMode);

            if (!waitUntilTransitionFinished)
            {
                if (controller.CurrentMode == targetMode || controller.IsTransitioning)
                    CompleteStep();
                else
                    FailModeChange(controller);

                yield break;
            }

            while (controller.IsTransitioning)
            {
                if (!ValidateController(controller))
                    yield break;

                yield return null;
            }

            if (controller.CurrentMode != targetMode)
            {
                FailModeChange(controller);
                yield break;
            }

            CompleteStep();
        }

        private bool ValidateController(WorldModeController controller)
        {
            if (controller == null)
            {
                Debug.LogError("[WorldModeStep] Assign a WorldModeController reference.", this);
                FailStep();
                return false;
            }

            if (!controller.isActiveAndEnabled)
            {
                Debug.LogError(
                    $"[WorldModeStep] WorldModeController '{controller.gameObject.name}' is disabled or inactive.",
                    this);
                FailStep();
                return false;
            }

            return true;
        }

        private void FailModeChange(WorldModeController controller)
        {
            Debug.LogError(
                $"[WorldModeStep] WorldModeController '{controller.gameObject.name}' did not switch " +
                $"to '{targetMode}'. Current mode is '{controller.CurrentMode}'.",
                this);
            FailStep();
        }
    }
}
