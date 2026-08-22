using System;
using System.Collections;
using UnityEngine;

namespace Audere.Story
{
    public enum StoryStepState
    {
        Idle,
        Running,
        Completed,
        Cancelled,
        Failed,
    }

    [DisallowMultipleComponent]
    public abstract class StoryStep : MonoBehaviour
    {
        private Coroutine executionRoutine;
        private Action<StoryStepState> activeCompletion;

        public StoryStepState CurrentState { get; private set; } = StoryStepState.Idle;
        public bool IsRunning => CurrentState == StoryStepState.Running;

        private void OnDisable()
        {
            Cancel();
        }

        public bool Play(Action<StoryStepState> onEnded = null)
        {
            if (IsRunning)
            {
                Debug.LogWarning("[StoryStep] This step is already running.", this);
                return false;
            }

            if (!isActiveAndEnabled)
            {
                Debug.LogWarning("[StoryStep] Enable the step before calling Play.", this);
                CurrentState = StoryStepState.Failed;
                return false;
            }

            activeCompletion = null;
            activeCompletion = onEnded;
            CurrentState = StoryStepState.Running;
            executionRoutine = StartCoroutine(RunExecution());
            return true;
        }

        public void Cancel()
        {
            if (!IsRunning)
                return;

            if (executionRoutine != null)
                StopCoroutine(executionRoutine);
            executionRoutine = null;

            Action<StoryStepState> completion = PrepareCompletion(StoryStepState.Cancelled);
            OnCancelled();
            completion?.Invoke(StoryStepState.Cancelled);
        }

        protected abstract IEnumerator Execute();

        protected virtual void OnCancelled() { }

        protected void CompleteStep()
        {
            EndExecution(StoryStepState.Completed);
        }

        protected void FailStep()
        {
            EndExecution(StoryStepState.Failed);
        }

        private IEnumerator RunExecution()
        {
            IEnumerator execution;
            try
            {
                execution = Execute();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                EndExecution(StoryStepState.Failed);
                yield break;
            }

            if (execution == null)
            {
                EndExecution(StoryStepState.Completed);
                yield break;
            }

            while (IsRunning)
            {
                bool hasNext;
                object yieldedValue = null;
                try
                {
                    hasNext = execution.MoveNext();
                    if (hasNext)
                        yieldedValue = execution.Current;
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                    EndExecution(StoryStepState.Failed);
                    yield break;
                }

                if (!hasNext)
                    break;

                yield return yieldedValue;
            }

            if (IsRunning)
                EndExecution(StoryStepState.Completed);
        }

        private void EndExecution(StoryStepState result)
        {
            if (!IsRunning)
                return;

            executionRoutine = null;
            Action<StoryStepState> completion = PrepareCompletion(result);
            completion?.Invoke(result);
        }

        private Action<StoryStepState> PrepareCompletion(StoryStepState result)
        {
            CurrentState = result;
            Action<StoryStepState> completion = activeCompletion;
            activeCompletion = null;
            return completion;
        }
    }
}
