using System;
using UnityEngine;

namespace Audere.Combat
{
    [Serializable]
    public struct StunZonePulse
    {
        [SerializeField] private Vector2 normalizedCenter;
        [SerializeField] private Vector2 normalizedSize;
        [SerializeField, Min(.05f)] private float telegraphDuration;
        [SerializeField, Min(.05f)] private float activeDuration;
        [SerializeField, Min(.05f)] private float fadeOutDuration;
        [SerializeField, Min(0f)] private float hiddenDuration;

        public StunZonePulse(
            Vector2 normalizedCenter,
            Vector2 normalizedSize,
            float telegraphDuration,
            float activeDuration,
            float fadeOutDuration,
            float hiddenDuration)
        {
            this.normalizedCenter = normalizedCenter;
            this.normalizedSize = normalizedSize;
            this.telegraphDuration = telegraphDuration;
            this.activeDuration = activeDuration;
            this.fadeOutDuration = fadeOutDuration;
            this.hiddenDuration = hiddenDuration;
        }

        public Vector2 NormalizedCenter => normalizedCenter;
        public Vector2 NormalizedSize => normalizedSize;
        public float TelegraphDuration => telegraphDuration;
        public float ActiveDuration => activeDuration;
        public float FadeOutDuration => fadeOutDuration;
        public float HiddenDuration => hiddenDuration;
        public float TotalDuration => telegraphDuration + activeDuration + fadeOutDuration + hiddenDuration;
    }

    [CreateAssetMenu(
        menuName = "Audere/Combat/Moves/Stun Zone Pressure",
        fileName = "Move_StunZonePressure")]
    public sealed class StunZonePressureMove : CombatMoveDefinition
    {
        [SerializeField] private StunZonePulse[] pulses =
        {
            new StunZonePulse(new Vector2(.25f, .5f), new Vector2(.22f, .9f), .35f, .75f, .2f, .25f),
            new StunZonePulse(new Vector2(.75f, .5f), new Vector2(.22f, .9f), .35f, .75f, .2f, .25f),
        };
        [SerializeField, Range(.05f, 1f)] private float telegraphAlpha = .42f;
        [SerializeField, Range(.05f, 1f)] private float activeAlpha = .82f;
        [SerializeField, Min(0)] private int zoneSlot;

        public StunZonePulse[] Pulses => pulses;
        public float TelegraphAlpha => telegraphAlpha;
        public float ActiveAlpha => activeAlpha;
        public int ZoneSlot => zoneSlot;

        public override bool Validate(out string error)
        {
            if (!base.Validate(out error))
                return false;
            if (pulses == null || pulses.Length == 0)
            {
                error = $"Move '{name}' requires at least one Stun Zone pulse.";
                return false;
            }
            for (int i = 0; i < pulses.Length; i++)
            {
                StunZonePulse pulse = pulses[i];
                if (pulse.NormalizedCenter.x < 0f || pulse.NormalizedCenter.x > 1f ||
                    pulse.NormalizedCenter.y < 0f || pulse.NormalizedCenter.y > 1f)
                {
                    error = $"Move '{name}' pulse {i} requires a normalized center inside 0..1.";
                    return false;
                }
                if (pulse.NormalizedSize.x <= 0f || pulse.NormalizedSize.x > 1f ||
                    pulse.NormalizedSize.y <= 0f || pulse.NormalizedSize.y > 1f)
                {
                    error = $"Move '{name}' pulse {i} requires a normalized size inside 0..1.";
                    return false;
                }
                if (pulse.TelegraphDuration <= 0f || pulse.ActiveDuration <= 0f ||
                    pulse.FadeOutDuration <= 0f || pulse.HiddenDuration < 0f)
                {
                    error = $"Move '{name}' pulse {i} has invalid timing.";
                    return false;
                }
            }
            if (activeAlpha < telegraphAlpha)
            {
                error = $"Move '{name}' Active Alpha must be at least Telegraph Alpha.";
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
            private readonly StunZonePressureMove data;
            private readonly CombatMoveExecutionContext context;
            private readonly float sequenceDuration;
            private float elapsed;
            private bool cancelled;
            private bool hidden;

            public Execution(StunZonePressureMove data, CombatMoveExecutionContext context)
            {
                this.data = data;
                this.context = context;
                for (int i = 0; i < data.Pulses.Length; i++)
                    sequenceDuration += data.Pulses[i].TotalDuration;
                context.Board?.HideStunZones();
            }

            public bool IsComplete => cancelled || elapsed >= data.Duration;

            public void Tick(float activeDeltaTime)
            {
                if (cancelled || context.Board == null)
                    return;

                elapsed = Mathf.Min(data.Duration, elapsed + Mathf.Max(0f, activeDeltaTime));
                if (elapsed >= data.Duration || sequenceDuration <= 0f)
                {
                    Hide();
                    return;
                }

                hidden = false;
                float cycleTime = elapsed % sequenceDuration;
                for (int i = 0; i < data.Pulses.Length; i++)
                {
                    StunZonePulse pulse = data.Pulses[i];
                    if (cycleTime <= pulse.TotalDuration)
                    {
                        ApplyPulse(pulse, cycleTime);
                        return;
                    }
                    cycleTime -= pulse.TotalDuration;
                }
                Hide();
            }

            public void Cancel()
            {
                if (cancelled)
                    return;
                cancelled = true;
                Hide();
            }

            private void ApplyPulse(StunZonePulse pulse, float time)
            {
                float telegraphEnd = pulse.TelegraphDuration;
                float activeEnd = telegraphEnd + pulse.ActiveDuration;
                float fadeEnd = activeEnd + pulse.FadeOutDuration;
                float alpha;
                bool blocking;

                if (time < telegraphEnd)
                {
                    float t = Smooth(time / pulse.TelegraphDuration);
                    alpha = Mathf.Lerp(0f, data.TelegraphAlpha, t);
                    blocking = false;
                }
                else if (time < activeEnd)
                {
                    float t = Smooth((time - telegraphEnd) / Mathf.Min(.12f, pulse.ActiveDuration));
                    alpha = Mathf.Lerp(data.TelegraphAlpha, data.ActiveAlpha, t);
                    blocking = true;
                }
                else if (time < fadeEnd)
                {
                    float t = Smooth((time - activeEnd) / pulse.FadeOutDuration);
                    alpha = Mathf.Lerp(data.ActiveAlpha, 0f, t);
                    blocking = false;
                }
                else
                {
                    Hide();
                    return;
                }

                context.Board.SetStunZonePresentation(
                    data.ZoneSlot,
                    pulse.NormalizedCenter,
                    pulse.NormalizedSize,
                    alpha,
                    blocking);
            }

            private void Hide()
            {
                if (hidden)
                    return;
                hidden = true;
                context.Board?.HideStunZones();
            }

            private static float Smooth(float value)
            {
                value = Mathf.Clamp01(value);
                return value * value * (3f - 2f * value);
            }
        }

        private void OnValidate()
        {
            if (!Validate(out string error))
                Debug.LogError($"[StunZonePressureMove] {error}", this);
        }
    }
}
