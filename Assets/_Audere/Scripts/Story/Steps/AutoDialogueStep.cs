using System.Collections;
using Audere.Dialogue;
using UnityEngine;

namespace Audere.Story.Steps
{
    /// <summary>Non-interactive speech for an authored parallel performance; uses the existing DialogueUI.</summary>
    public sealed class AutoDialogueStep : StoryStep
    {
        [SerializeField] private DialogueData dialogueData;
        [SerializeField, Min(.1f)] private float minimumLineDuration = 1.5f;
        [SerializeField, Min(1f)] private float charactersPerSecond = 18f;
        private DialogueController controller;
        private int version;
        protected override IEnumerator Execute()
        {
            int session = ++version;
            controller = GameplayUIRoot.Instance != null ? GameplayUIRoot.Instance.Dialogue : null;
            if (controller == null || controller.IsPlaying || dialogueData == null) { FailStep(); yield break; }
            bool done = false;
            DialogueResult result = DialogueResult.Cancelled;
            if (!controller.PlayAuto(dialogueData, r => { if (version == session) { result = r; done = true; } },
                minimumLineDuration, charactersPerSecond, .2f, false)) { controller = null; FailStep(); yield break; }
            while (!done) yield return null;
            controller = null;
            if (result == DialogueResult.Completed) CompleteStep(); else Cancel();
        }
        protected override void OnCancelled()
        {
            version++;
            var old = controller; controller = null;
            if (old != null && old.IsPlaying) old.ForceClose();
        }
    }
}
