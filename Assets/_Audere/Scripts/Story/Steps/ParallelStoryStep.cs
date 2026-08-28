using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Audere.Story.Steps
{
    /// <summary>Runs explicitly authored child events together and joins them before advancing.</summary>
    public sealed class ParallelStoryStep : StoryStep
    {
        [SerializeField] private StoryEvent[] branches = new StoryEvent[0];
        private readonly List<StoryEvent> ownedBranches = new List<StoryEvent>();
        private int sessionVersion;
        public IReadOnlyList<StoryEvent> Branches => branches;

        protected override IEnumerator Execute()
        {
            int session = ++sessionVersion;
            var unique = new HashSet<StoryEvent>();
            if (branches == null || branches.Length == 0)
            {
                Debug.LogError("[ParallelStoryStep] Assign child StoryEvents.", this);
                FailStep();
                yield break;
            }
            foreach (StoryEvent branch in branches)
            {
                if (branch == null || branch.transform.parent != transform ||
                    !unique.Add(branch) || !branch.isActiveAndEnabled || branch.IsPlaying ||
                    branch.AutoPlayNextEvent)
                {
                    Debug.LogError("[ParallelStoryStep] Branches must be unique, idle, active direct child events without auto-next.", this);
                    FailStep();
                    yield break;
                }
            }

            int remaining = branches.Length;
            StoryEventResult result = StoryEventResult.Completed;
            ownedBranches.Clear();
            foreach (StoryEvent branch in branches)
            {
                ownedBranches.Add(branch);
                bool ended = false;
                if (!branch.Play(value =>
                    {
                        if (ended || session != sessionVersion || !IsRunning) return;
                        ended = true;
                        remaining--;
                        if (value != StoryEventResult.Completed) result = value;
                    }))
                    result = StoryEventResult.Failed;
                if (result != StoryEventResult.Completed) break;
            }
            while (session == sessionVersion && IsRunning && remaining > 0 &&
                   result == StoryEventResult.Completed)
                yield return null;

            if (session != sessionVersion || !IsRunning) yield break;
            if (result != StoryEventResult.Completed)
            {
                StopOwnedBranches();
                if (result == StoryEventResult.Cancelled) Cancel();
                else FailStep();
                yield break;
            }
            ownedBranches.Clear();
            CompleteStep();
        }

        protected override void OnCancelled() => StopOwnedBranches();

        private void StopOwnedBranches()
        {
            sessionVersion++;
            foreach (StoryEvent branch in ownedBranches)
                if (branch != null) branch.Cancel();
            ownedBranches.Clear();
        }
    }
}
