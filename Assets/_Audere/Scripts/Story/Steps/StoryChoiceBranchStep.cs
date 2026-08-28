using System.Collections;
using System.Collections.Generic;
using Audere.Story.Presentation;
using UnityEngine;

namespace Audere.Story.Steps
{
    public sealed class StoryChoiceBranchStep : StoryStep
    {
        [SerializeField] private StoryChoiceView choiceView;
        [SerializeField] private string[] options;
        [SerializeField] private StoryEvent[] branches;

        private StoryEvent activeBranch;
        private int sessionVersion;

        public StoryChoiceView ChoiceView => choiceView;
        public IReadOnlyList<string> Options => options;
        public IReadOnlyList<StoryEvent> Branches => branches;

        protected override IEnumerator Execute()
        {
            int session = ++sessionVersion;
            if (choiceView == null || options == null || branches == null ||
                options.Length == 0 || options.Length != branches.Length)
            {
                Debug.LogError("[StoryChoiceBranchStep] Choice View and matching option/branch arrays are required.", this);
                FailStep();
                yield break;
            }

            int selectedIndex = -1;
            if (!choiceView.Show(this, options, index => selectedIndex = index))
            {
                FailStep();
                yield break;
            }

            while (IsRunning && session == sessionVersion && selectedIndex < 0)
                yield return null;
            if (!IsRunning || session != sessionVersion)
                yield break;
            if (selectedIndex >= branches.Length || branches[selectedIndex] == null)
            {
                Debug.LogError($"[StoryChoiceBranchStep] Branch {selectedIndex} is missing.", this);
                FailStep();
                yield break;
            }

            activeBranch = branches[selectedIndex];
            bool branchEnded = false;
            StoryEventResult branchResult = StoryEventResult.Failed;
            if (!activeBranch.Play(result =>
                {
                    branchResult = result;
                    branchEnded = true;
                }))
            {
                activeBranch = null;
                FailStep();
                yield break;
            }

            while (IsRunning && session == sessionVersion && !branchEnded)
                yield return null;
            activeBranch = null;
            if (!IsRunning || session != sessionVersion)
                yield break;

            if (branchResult == StoryEventResult.Completed)
                CompleteStep();
            else if (branchResult == StoryEventResult.Cancelled)
                Cancel();
            else
                FailStep();
        }

        protected override void OnCancelled()
        {
            sessionVersion++;
            choiceView?.ForceHide(this);
            activeBranch?.Cancel();
            activeBranch = null;
        }
    }
}
