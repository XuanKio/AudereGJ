using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Audere.Story
{
    public enum StoryEventResult
    {
        Completed,
        Cancelled,
        Failed,
    }

    [DisallowMultipleComponent]
    public sealed class StoryEvent : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string eventId;

        [Header("Chaining")]
        [SerializeField] private bool autoPlayNextEvent;
        [SerializeField] private StoryEvent nextEvent;

        private readonly List<StoryStep> orderedSteps = new List<StoryStep>();
        private Coroutine playbackRoutine;
        private StoryStep currentStep;
        private Action<StoryEventResult> activeCompletion;
        private bool isPlaying;

        public string EventId => eventId;
        public bool AutoPlayNextEvent => autoPlayNextEvent;
        public StoryEvent NextEvent => nextEvent;
        public bool IsPlaying => isPlaying;
        public StoryStep CurrentStep => currentStep;

        private void OnDisable()
        {
            Cancel();
        }

        public bool Play(Action<StoryEventResult> onEnded = null)
        {
            if (isPlaying)
            {
                Debug.LogWarning($"[StoryEvent] '{eventId}' is already running.", this);
                return false;
            }

            if (!isActiveAndEnabled)
            {
                Debug.LogWarning($"[StoryEvent] Enable '{eventId}' before calling Play.", this);
                return false;
            }

            if (!CollectDirectChildSteps())
                return false;

            activeCompletion = null;
            activeCompletion = onEnded;
            isPlaying = true;
            playbackRoutine = StartCoroutine(PlayStepsInOrder());
            return true;
        }

        public void Cancel()
        {
            if (!isPlaying)
                return;

            if (playbackRoutine != null)
                StopCoroutine(playbackRoutine);
            playbackRoutine = null;

            StoryStep stepToCancel = currentStep;
            currentStep = null;
            stepToCancel?.Cancel();
            Finish(StoryEventResult.Cancelled);
        }

        private bool CollectDirectChildSteps()
        {
            orderedSteps.Clear();

            for (int siblingIndex = 0; siblingIndex < transform.childCount; siblingIndex++)
            {
                Transform child = transform.GetChild(siblingIndex);
                if (!child.gameObject.activeSelf)
                    continue;

                StoryStep[] steps = child.GetComponents<StoryStep>();
                if (steps.Length != 1)
                {
                    Debug.LogError(
                        $"[StoryEvent] Direct child '{child.name}' under '{eventId}' must have exactly one " +
                        $"StoryStep, but found {steps.Length}. Nested steps are not collected.",
                        child.gameObject);
                    orderedSteps.Clear();
                    return false;
                }

                orderedSteps.Add(steps[0]);
            }

            return true;
        }

        private IEnumerator PlayStepsInOrder()
        {
            for (int index = 0; index < orderedSteps.Count; index++)
            {
                currentStep = orderedSteps[index];
                bool stepEnded = false;
                StoryStepState stepResult = StoryStepState.Failed;

                bool started = currentStep.Play(result =>
                {
                    stepResult = result;
                    stepEnded = true;
                });

                if (!started)
                {
                    Finish(StoryEventResult.Failed);
                    yield break;
                }

                while (isPlaying && !stepEnded)
                    yield return null;

                if (!isPlaying)
                    yield break;

                currentStep = null;
                switch (stepResult)
                {
                    case StoryStepState.Completed:
                        continue;
                    case StoryStepState.Cancelled:
                        Finish(StoryEventResult.Cancelled);
                        yield break;
                    default:
                        Finish(StoryEventResult.Failed);
                        yield break;
                }
            }

            currentStep = null;
            Finish(StoryEventResult.Completed);
        }

        private void Finish(StoryEventResult result)
        {
            if (!isPlaying)
                return;

            isPlaying = false;
            playbackRoutine = null;
            currentStep = null;
            orderedSteps.Clear();

            Action<StoryEventResult> completion = activeCompletion;
            activeCompletion = null;
            completion?.Invoke(result);
        }
    }
}
