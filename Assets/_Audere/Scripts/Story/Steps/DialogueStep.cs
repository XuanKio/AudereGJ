using System.Collections;
using Audere.Dialogue;
using UnityEngine;

namespace Audere.Story.Steps
{
    public sealed class DialogueStep : StoryStep
    {
        [SerializeField] private DialogueData dialogueData;
        [SerializeField] private DialogueController dialogueController;

        private DialogueController activeController;
        private int sessionVersion;
        private bool ownsDialogue;

        public DialogueData DialogueData => dialogueData;
        public DialogueController DialogueController => dialogueController;

        protected override IEnumerator Execute()
        {
            int session = ++sessionVersion;

            if (dialogueData == null)
            {
                Debug.LogWarning("[DialogueStep] Assign Dialogue Data before playing this step.", this);
                FailStep();
                yield break;
            }

            DialogueController controller = ResolveController();
            if (controller == null)
            {
                Debug.LogError(
                    "[DialogueStep] No DialogueController is assigned and GameplayUIRoot.Instance.Dialogue " +
                    "is not available.",
                    this);
                FailStep();
                yield break;
            }

            if (!controller.isActiveAndEnabled)
            {
                Debug.LogError(
                    $"[DialogueStep] DialogueController '{controller.gameObject.name}' is disabled or inactive.",
                    this);
                FailStep();
                yield break;
            }

            if (controller.IsPlaying)
            {
                Debug.LogWarning(
                    $"[DialogueStep] DialogueController '{controller.gameObject.name}' is already playing " +
                    "another dialogue. The active dialogue was not replaced.",
                    this);
                FailStep();
                yield break;
            }

            bool started = controller.Play(
                dialogueData,
                result => HandleDialogueEnded(session, controller, result),
                false);

            if (!started)
            {
                Debug.LogError(
                    $"[DialogueStep] DialogueController could not start '{dialogueData.name}'.",
                    this);
                FailStep();
                yield break;
            }

            activeController = controller;
            ownsDialogue = true;

            while (IsRunning)
                yield return null;
        }

        protected override void OnCancelled()
        {
            sessionVersion++;

            DialogueController controller = activeController;
            bool shouldClose = ownsDialogue && controller != null && controller.IsPlaying;
            ownsDialogue = false;
            activeController = null;

            if (shouldClose)
                controller.ForceClose();
        }

        private DialogueController ResolveController()
        {
            // Scene-authored steps keep a direct reference for scene-first authoring, but the
            // GameplayUIRoot itself persists between scenes. During a Single-mode scene load,
            // the incoming scene's duplicate UI root is disabled before it is destroyed, so a
            // direct reference can briefly point at that inactive duplicate. Prefer the authored
            // controller while it is usable, then fall back to the persistent shared controller.
            if (dialogueController != null && dialogueController.isActiveAndEnabled)
                return dialogueController;

            GameplayUIRoot root = GameplayUIRoot.Instance;
            DialogueController sharedController = root != null ? root.Dialogue : null;
            if (sharedController != null && sharedController.isActiveAndEnabled)
                return sharedController;

            // Return the best available reference so Execute can report the existing, precise
            // disabled/inactive error when neither controller is currently usable.
            return dialogueController != null ? dialogueController : sharedController;
        }

        private void HandleDialogueEnded(
            int session,
            DialogueController source,
            DialogueResult result)
        {
            if (session != sessionVersion ||
                !ownsDialogue ||
                activeController != source ||
                !IsRunning)
                return;

            ownsDialogue = false;
            activeController = null;

            if (result == DialogueResult.Completed)
                CompleteStep();
            else
                Cancel();
        }
    }
}
