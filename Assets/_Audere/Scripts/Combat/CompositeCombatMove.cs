using System;
using UnityEngine;

namespace Audere.Combat
{
    [CreateAssetMenu(
        menuName = "Audere/Combat/Moves/Composite",
        fileName = "Move_Composite")]
    public sealed class CompositeCombatMove : CombatMoveDefinition
    {
        [SerializeField] private CombatMoveDefinition[] children;
        [SerializeField] private float[] childStartDelays;

        public CombatMoveDefinition[] Children => children;
        public float[] ChildStartDelays => childStartDelays;

        public override bool Validate(out string error)
        {
            if (!base.Validate(out error))
                return false;
            if (children == null || children.Length < 2)
            {
                error = $"Composite move '{name}' requires at least two child moves.";
                return false;
            }
            for (int i = 0; i < children.Length; i++)
            {
                CombatMoveDefinition child = children[i];
                if (child == null)
                {
                    error = $"Composite move '{name}' has a null child at index {i}.";
                    return false;
                }
                if (ReferenceEquals(child, this))
                {
                    error = $"Composite move '{name}' cannot contain itself.";
                    return false;
                }
                if (!child.Validate(out string childError))
                {
                    error = $"Composite move '{name}' child {i} is invalid: {childError}";
                    return false;
                }
            }
            if (childStartDelays != null && childStartDelays.Length > 0 &&
                childStartDelays.Length != children.Length)
            {
                error = $"Composite move '{name}' needs one start delay per child.";
                return false;
            }
            if (childStartDelays != null)
                for (int i = 0; i < childStartDelays.Length; i++)
                    if (childStartDelays[i] < 0f || childStartDelays[i] >= Duration)
                    {
                        error = $"Composite move '{name}' child {i} has an invalid start delay.";
                        return false;
                    }
            error = null;
            return true;
        }

        public override ICombatMoveExecution CreateExecution(CombatMoveExecutionContext context)
        {
            if (!Validate(out string error))
                throw new InvalidOperationException(error);
            return new Execution(this, context);
        }

        private sealed class Execution : ICombatMoveExecution
        {
            private readonly CompositeCombatMove data;
            private readonly CombatMoveExecutionContext context;
            private readonly ICombatMoveExecution[] children;
            private float elapsed;
            private bool cancelled;
            private bool childrenCancelled;

            public Execution(CompositeCombatMove data, CombatMoveExecutionContext context)
            {
                this.data = data;
                this.context = context;
                children = new ICombatMoveExecution[data.Children.Length];
            }

            public bool IsComplete => cancelled || elapsed >= data.Duration;

            public void Tick(float activeDeltaTime)
            {
                if (cancelled)
                    return;
                float previous = elapsed;
                elapsed = Mathf.Min(data.Duration, elapsed + Mathf.Max(0f, activeDeltaTime));
                for (int i = 0; i < children.Length; i++)
                {
                    float delay = data.ChildStartDelays != null && i < data.ChildStartDelays.Length
                        ? data.ChildStartDelays[i] : 0f;
                    float childDelta = Mathf.Max(0f, elapsed - Mathf.Max(previous, delay));
                    if (childDelta <= 0f) continue;
                    if (children[i] == null) children[i] = data.Children[i].CreateExecution(context);
                    ICombatMoveExecution child = children[i];
                    if (child != null && !child.IsComplete)
                        child.Tick(childDelta);
                }
                if (elapsed >= data.Duration)
                    CancelChildren();
            }

            public void Cancel()
            {
                if (cancelled)
                    return;
                cancelled = true;
                CancelChildren();
            }

            private void CancelChildren()
            {
                if (childrenCancelled)
                    return;
                childrenCancelled = true;
                for (int i = 0; i < children.Length; i++)
                    children[i]?.Cancel();
            }
        }

        private void OnValidate()
        {
            if (!Validate(out string error))
                Debug.LogError($"[CompositeCombatMove] {error}", this);
        }
    }
}
