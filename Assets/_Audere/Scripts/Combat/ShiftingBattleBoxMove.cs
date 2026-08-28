using System;
using UnityEngine;

namespace Audere.Combat
{
    [Serializable]
    public struct BattleBoxHorizontalPose
    {
        [SerializeField, Range(.3f, 1f)] private float widthFraction;
        [SerializeField, Range(-1f, 1f)] private float normalizedX;

        public BattleBoxHorizontalPose(float widthFraction, float normalizedX)
        {
            this.widthFraction = widthFraction;
            this.normalizedX = normalizedX;
        }

        public float WidthFraction => widthFraction;
        public float NormalizedX => normalizedX;
    }

    [CreateAssetMenu(
        menuName = "Audere/Combat/Moves/Shifting Battle Box",
        fileName = "Move_ShiftingBattleBox")]
    public sealed class ShiftingBattleBoxMove : CombatMoveDefinition
    {
        [Tooltip("Each pose changes only Dice Field width and horizontal position inside Frame.")]
        [SerializeField] private BattleBoxHorizontalPose[] poses =
        {
            new BattleBoxHorizontalPose(.72f, -.7f),
            new BattleBoxHorizontalPose(.58f, .75f),
            new BattleBoxHorizontalPose(.46f, 0f),
        };
        [SerializeField, Min(.05f)] private float telegraphDuration = .35f;
        [SerializeField, Min(.05f)] private float squeezeDuration = .42f;
        [SerializeField, Min(0f)] private float holdDuration = .50f;
        [SerializeField, Min(.05f)] private float returnDuration = .46f;
        [SerializeField, Range(0f, .12f)] private float telegraphWidthPulse = .035f;

        public BattleBoxHorizontalPose[] Poses => poses;
        public float TelegraphDuration => telegraphDuration;
        public float SqueezeDuration => squeezeDuration;
        public float HoldDuration => holdDuration;
        public float ReturnDuration => returnDuration;
        public float TelegraphWidthPulse => telegraphWidthPulse;

        public override bool Validate(out string error)
        {
            if (!base.Validate(out error))
                return false;
            if (poses == null || poses.Length == 0)
            {
                error = $"Move '{name}' requires at least one horizontal battle-box pose.";
                return false;
            }
            for (int i = 0; i < poses.Length; i++)
            {
                BattleBoxHorizontalPose pose = poses[i];
                if (pose.WidthFraction < .3f || pose.WidthFraction > 1f)
                {
                    error = $"Move '{name}' pose {i} requires Width Fraction from 0.3 to 1.";
                    return false;
                }
                if (pose.NormalizedX < -1f || pose.NormalizedX > 1f)
                {
                    error = $"Move '{name}' pose {i} requires normalized X from -1 to 1.";
                    return false;
                }
                if (pose.WidthFraction >= .999f && Mathf.Abs(pose.NormalizedX) <= .001f)
                {
                    error = $"Move '{name}' pose {i} must shrink or horizontally reposition Dice Field.";
                    return false;
                }
            }
            if (telegraphDuration <= 0f || squeezeDuration <= 0f || returnDuration <= 0f)
            {
                error = $"Move '{name}' requires positive telegraph, squeeze and return durations.";
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
            private readonly ShiftingBattleBoxMove data;
            private readonly CombatMoveExecutionContext context;
            private float elapsed;
            private bool cancelled;
            private bool restored;

            public Execution(ShiftingBattleBoxMove data, CombatMoveExecutionContext context)
            {
                this.data = data;
                this.context = context;
                context.Board?.ResetBattleBoxLayout();
            }

            public bool IsComplete => cancelled || elapsed >= data.Duration;

            public void Tick(float activeDeltaTime)
            {
                if (cancelled || context.Board == null || context.Board.PlayArea == null)
                    return;

                elapsed = Mathf.Min(data.Duration, elapsed + Mathf.Max(0f, activeDeltaTime));
                if (elapsed >= data.Duration)
                {
                    Restore();
                    return;
                }

                float cycleDuration = data.TelegraphDuration + data.SqueezeDuration +
                                      data.HoldDuration + data.ReturnDuration;
                float sequenceDuration = cycleDuration * data.Poses.Length;
                if (elapsed >= sequenceDuration)
                {
                    Restore();
                    return;
                }

                int cycleIndex = Mathf.FloorToInt(elapsed / cycleDuration);
                float cycleTime = elapsed - cycleIndex * cycleDuration;
                BattleBoxHorizontalPose pose = data.Poses[cycleIndex];
                float widthFraction;
                float normalizedX;

                if (cycleTime < data.TelegraphDuration)
                {
                    float t = Mathf.Clamp01(cycleTime / data.TelegraphDuration);
                    widthFraction = 1f - Mathf.Sin(t * Mathf.PI) * data.TelegraphWidthPulse;
                    normalizedX = 0f;
                }
                else if (cycleTime < data.TelegraphDuration + data.SqueezeDuration)
                {
                    float t = Mathf.InverseLerp(
                        data.TelegraphDuration,
                        data.TelegraphDuration + data.SqueezeDuration,
                        cycleTime);
                    t = SmoothStep(t);
                    widthFraction = Mathf.LerpUnclamped(1f, pose.WidthFraction, t);
                    normalizedX = Mathf.LerpUnclamped(0f, pose.NormalizedX, t);
                }
                else if (cycleTime < data.TelegraphDuration + data.SqueezeDuration + data.HoldDuration)
                {
                    widthFraction = pose.WidthFraction;
                    normalizedX = pose.NormalizedX;
                }
                else
                {
                    float returnStart = data.TelegraphDuration + data.SqueezeDuration + data.HoldDuration;
                    float t = Mathf.InverseLerp(returnStart, returnStart + data.ReturnDuration, cycleTime);
                    t = SmoothStep(t);
                    widthFraction = Mathf.LerpUnclamped(pose.WidthFraction, 1f, t);
                    normalizedX = Mathf.LerpUnclamped(pose.NormalizedX, 0f, t);
                }

                context.Board.SetBattleBoxHorizontalLayout(widthFraction, normalizedX);
            }

            public void Cancel()
            {
                if (cancelled)
                    return;
                cancelled = true;
                Restore();
            }

            private void Restore()
            {
                if (restored)
                    return;
                restored = true;
                context.Board?.ResetBattleBoxLayout();
            }

            private static float SmoothStep(float value)
            {
                value = Mathf.Clamp01(value);
                return value * value * (3f - 2f * value);
            }
        }

        private void OnValidate()
        {
            if (!Validate(out string error))
                Debug.LogError($"[ShiftingBattleBoxMove] {error}", this);
        }
    }
}
