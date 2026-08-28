using System.Collections;
using Audere.Puzzle;
using UnityEngine;

namespace Audere.Story.Steps
{
    public sealed class PuzzleStep : StoryStep
    {
        [Header("Puzzle")]
        [SerializeField] private PuzzleController puzzleController;
        [SerializeField] private GameObject puzzleRoot;
        [SerializeField] private bool resetBeforePlay = true;
        [SerializeField] private Transform startFromAnchor;
        [SerializeField] private PuzzleSequencePrepareStep normalizeOnCancel;

        private PuzzleController activeController;
        private int sessionVersion;
        private bool ownsPuzzleSession;

        public PuzzleController PuzzleController => puzzleController;
        public GameObject PuzzleRoot => puzzleRoot;
        public bool ResetBeforePlay => resetBeforePlay;

        protected override IEnumerator Execute()
        {
            int session = ++sessionVersion;

            if (puzzleController == null)
            {
                Debug.LogError($"[PuzzleStep] '{name}' requires a PuzzleController reference.", this);
                FailStep();
                yield break;
            }

            if (puzzleRoot != null && !puzzleRoot.activeSelf)
            {
                puzzleRoot.SetActive(true);
            }

            if (!puzzleController.isActiveAndEnabled)
            {
                Debug.LogError(
                    $"[PuzzleStep] '{name}' cannot play '{puzzleController.name}' because its controller is not active and enabled.",
                    this);
                FailStep();
                yield break;
            }

            if (puzzleController.IsPlaying)
            {
                Debug.LogWarning(
                    $"[PuzzleStep] '{name}' cannot start '{puzzleController.name}' because that puzzle is already playing.",
                    this);
                FailStep();
                yield break;
            }

            if (resetBeforePlay && startFromAnchor == null && !puzzleController.ResetPuzzle())
            {
                Debug.LogError($"[PuzzleStep] '{name}' could not reset puzzle '{puzzleController.name}'.", this);
                FailStep();
                yield break;
            }

            PuzzleController controller = puzzleController;
            activeController = controller;
            ownsPuzzleSession = true;

            bool started = startFromAnchor != null
                ? controller.PlayFromAnchor(
                    startFromAnchor,
                    result => HandlePuzzleEnded(session, controller, result))
                : controller.Play(result => HandlePuzzleEnded(session, controller, result));
            if (!started)
            {
                if (session == sessionVersion && activeController == controller)
                {
                    ownsPuzzleSession = false;
                    activeController = null;

                    if (IsRunning)
                    {
                        Debug.LogError($"[PuzzleStep] '{name}' could not start puzzle '{controller.name}'.", this);
                        FailStep();
                    }
                }

                yield break;
            }

            while (IsRunning)
            {
                yield return null;
            }
        }

        protected override void OnCancelled()
        {
            sessionVersion++;

            PuzzleController controller = activeController;
            bool shouldCancel = ownsPuzzleSession && controller != null && controller.IsPlaying;

            ownsPuzzleSession = false;
            activeController = null;

            if (shouldCancel)
            {
                controller.Cancel();
            }

            if (isActiveAndEnabled && normalizeOnCancel != null)
                normalizeOnCancel.NormalizeAfterCancel();
        }

        private void HandlePuzzleEnded(int session, PuzzleController controller, PuzzleResult result)
        {
            if (session != sessionVersion ||
                !ownsPuzzleSession ||
                activeController != controller ||
                !IsRunning)
            {
                return;
            }

            ownsPuzzleSession = false;
            activeController = null;

            switch (result)
            {
                case PuzzleResult.Completed:
                    CompleteStep();
                    break;

                case PuzzleResult.Cancelled:
                    Cancel();
                    break;

                case PuzzleResult.Failed:
                    FailStep();
                    break;

                default:
                    Debug.LogError($"[PuzzleStep] '{name}' received unsupported result '{result}'.", this);
                    FailStep();
                    break;
            }
        }
    }
}
