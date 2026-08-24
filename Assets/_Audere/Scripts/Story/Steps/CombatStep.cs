using System.Collections;
using Audere.Combat;
using Audere.Dialogue;
using UnityEngine;

namespace Audere.Story.Steps
{
    public enum CombatResultBehaviour
    {
        Complete,
        Fail,
        Retry,
        Cancel,
    }

    public sealed class CombatStep : StoryStep
    {
        [Header("Combat")]
        [SerializeField] private CombatController combatController;
        [SerializeField] private CombatEncounterData combatEncounterData;

        [Header("Result Behaviour")]
        [SerializeField] private CombatResultBehaviour victoryBehaviour = CombatResultBehaviour.Complete;
        [SerializeField] private CombatResultBehaviour defeatBehaviour = CombatResultBehaviour.Retry;
        [SerializeField] private CombatResultBehaviour specialBehaviour = CombatResultBehaviour.Complete;

        private CombatController activeController;
        private CombatRetryView activeRetryView;
        private int executionVersion;
        private int attemptVersion;
        private bool ownsCombatSession;
        private bool isWaitingForRetry;

        public CombatController CombatController => combatController;
        public CombatEncounterData CombatEncounterData => combatEncounterData;
        public CombatResultBehaviour VictoryBehaviour => victoryBehaviour;
        public CombatResultBehaviour DefeatBehaviour => defeatBehaviour;
        public CombatResultBehaviour SpecialBehaviour => specialBehaviour;
        public bool IsWaitingForRetry => isWaitingForRetry;

        protected override IEnumerator Execute()
        {
            int execution = ++executionVersion;

            if (combatController == null)
            {
                Debug.LogError($"[CombatStep] '{name}' requires a CombatController reference.", this);
                FailStep();
                yield break;
            }

            if (combatEncounterData == null)
            {
                Debug.LogError($"[CombatStep] '{name}' requires Combat Encounter Data.", this);
                FailStep();
                yield break;
            }

            if (!combatController.isActiveAndEnabled)
            {
                Debug.LogError(
                    $"[CombatStep] '{name}' cannot play '{combatController.name}' because its controller is not active and enabled.",
                    this);
                FailStep();
                yield break;
            }

            if (combatController.IsPlaying)
            {
                Debug.LogWarning(
                    $"[CombatStep] '{name}' cannot start '{combatController.name}' because that combat is already playing.",
                    this);
                FailStep();
                yield break;
            }

            activeController = combatController;
            activeRetryView = GameplayUIRoot.Instance != null
                ? GameplayUIRoot.Instance.CombatRetry
                : null;

            if (UsesRetryBehaviour() && activeRetryView == null)
            {
                Debug.LogError(
                    $"[CombatStep] '{name}' uses Retry but '{combatController.name}' has no CombatRetryView.",
                    this);
                ClearOwnership();
                FailStep();
                yield break;
            }

            StartAttempt(execution, combatController);

            while (IsRunning)
                yield return null;
        }

        protected override void OnCancelled()
        {
            executionVersion++;
            attemptVersion++;

            CombatController controller = activeController;
            bool shouldCancel = ownsCombatSession && controller != null && controller.IsPlaying;

            activeRetryView?.ForceHide();
            ClearOwnership();

            if (shouldCancel)
                controller.Cancel();
        }

        private void StartAttempt(int execution, CombatController controller)
        {
            if (execution != executionVersion || !IsRunning || activeController != controller)
                return;

            if (!controller.isActiveAndEnabled)
            {
                Debug.LogError(
                    $"[CombatStep] CombatController '{controller.name}' became disabled before retry.",
                    this);
                FinishWith(CombatResultBehaviour.Fail);
                return;
            }

            if (controller.IsPlaying)
            {
                Debug.LogWarning(
                    $"[CombatStep] '{name}' will not replace another active combat session on '{controller.name}'.",
                    this);
                FinishWith(CombatResultBehaviour.Fail);
                return;
            }

            activeRetryView?.ForceHide();
            isWaitingForRetry = false;
            controller.ResetEncounter();

            int attempt = ++attemptVersion;
            ownsCombatSession = true;

            bool started = controller.Play(
                combatEncounterData,
                result => HandleCombatEnded(execution, attempt, controller, result));

            if (started)
                return;

            if (execution == executionVersion &&
                attempt == attemptVersion &&
                activeController == controller &&
                IsRunning)
            {
                ownsCombatSession = false;
                Debug.LogError(
                    $"[CombatStep] '{name}' could not start encounter '{combatEncounterData.name}'.",
                    this);
                FinishWith(CombatResultBehaviour.Fail);
            }
        }

        private void HandleCombatEnded(
            int execution,
            int attempt,
            CombatController controller,
            CombatResult result)
        {
            if (execution != executionVersion ||
                attempt != attemptVersion ||
                !ownsCombatSession ||
                activeController != controller ||
                !IsRunning)
            {
                return;
            }

            ownsCombatSession = false;

            switch (result)
            {
                case CombatResult.Victory:
                    ApplyBehaviour(execution, controller, victoryBehaviour);
                    break;

                case CombatResult.Defeat:
                    ApplyBehaviour(execution, controller, defeatBehaviour);
                    break;

                case CombatResult.Special:
                    ApplyBehaviour(execution, controller, specialBehaviour);
                    break;

                case CombatResult.Cancelled:
                    FinishWith(CombatResultBehaviour.Cancel);
                    break;

                default:
                    Debug.LogError($"[CombatStep] '{name}' received unsupported result '{result}'.", this);
                    FinishWith(CombatResultBehaviour.Fail);
                    break;
            }
        }

        private void ApplyBehaviour(
            int execution,
            CombatController controller,
            CombatResultBehaviour behaviour)
        {
            if (behaviour != CombatResultBehaviour.Retry)
            {
                FinishWith(behaviour);
                return;
            }

            if (activeRetryView == null)
            {
                Debug.LogError($"[CombatStep] '{name}' cannot retry without a CombatRetryView.", this);
                FinishWith(CombatResultBehaviour.Fail);
                return;
            }

            isWaitingForRetry = true;
            bool shown = activeRetryView.Show(
                this,
                () => HandleRetryRequested(execution, controller));

            if (shown)
                return;

            isWaitingForRetry = false;
            FinishWith(CombatResultBehaviour.Fail);
        }

        private void HandleRetryRequested(int execution, CombatController controller)
        {
            if (execution != executionVersion ||
                !isWaitingForRetry ||
                activeController != controller ||
                !IsRunning)
            {
                return;
            }

            isWaitingForRetry = false;
            StartAttempt(execution, controller);
        }

        private void FinishWith(CombatResultBehaviour behaviour)
        {
            activeRetryView?.ForceHide();
            ClearOwnership();

            switch (behaviour)
            {
                case CombatResultBehaviour.Complete:
                    CompleteStep();
                    break;

                case CombatResultBehaviour.Cancel:
                    Cancel();
                    break;

                case CombatResultBehaviour.Fail:
                    FailStep();
                    break;

                default:
                    Debug.LogError(
                        $"[CombatStep] '{name}' cannot finish directly with behaviour '{behaviour}'.",
                        this);
                    FailStep();
                    break;
            }
        }

        private bool UsesRetryBehaviour()
        {
            return victoryBehaviour == CombatResultBehaviour.Retry ||
                   defeatBehaviour == CombatResultBehaviour.Retry ||
                   specialBehaviour == CombatResultBehaviour.Retry;
        }

        private void ClearOwnership()
        {
            ownsCombatSession = false;
            isWaitingForRetry = false;
            activeController = null;
            activeRetryView = null;
        }
    }
}
