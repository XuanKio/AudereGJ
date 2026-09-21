using UnityEngine;
using UnityEngine.UI;

namespace Audere.Combat
{
    [CreateAssetMenu(menuName = "Audere/Combat/Moves/Timor Pressure Waves")]
    public sealed class TimorPressureWaveMove : CombatMoveDefinition
    {
        [SerializeField] private CombatBulletView projectile;
        [SerializeField, Min(.3f)] private float waveInterval = .48f;
        [SerializeField, Min(.25f)] private float warningDuration = .38f;
        [SerializeField, Range(100f, 320f)] private float speed = 210f;
        [SerializeField, Range(110f, 200f)] private float gapWidth = 124f;
        [SerializeField, Range(2, 5)] private int wavesPerPhrase = 4;
        [SerializeField, Min(.2f)] private float phraseRest = .55f;
        [SerializeField, Range(.55f, 1f)] private float minimumWidth = .64f;
        [SerializeField, Range(0f, 1f)] private float sway = .78f;
        [SerializeField] private bool horizontalVolley;
        [SerializeField] private bool supported;
        [SerializeField] private string[] encouragement;
        public float GapWidth => gapWidth + (supported ? 36f : 0f);
        public float WaveInterval => waveInterval;
        public bool HorizontalVolley => horizontalVolley;
        public static float GapCenter(int wave, float width) => Mathf.Sin(wave * .8f) * width * .18f;

        // All live rows share one moving opening. Different-age waves never close each other's route.
        public Vector2 SafePointAt(Rect field, float age)
        {
            float axis = horizontalVolley ? field.height : field.width;
            float amplitude = Mathf.Max(0f, axis * .5f - GapWidth * .5f - 52f) * .92f;
            float offset = Mathf.Sin(age * .95f) * amplitude;
            return field.center + (horizontalVolley ? Vector2.up : Vector2.right) * offset;
        }
        public float WidthAt(float age)
        {
            float envelope = Mathf.SmoothStep(0f, 1f, Mathf.Min(age / .7f, (Duration - age) / .7f));
            float pulse = .5f + .5f * Mathf.Cos(age * Mathf.PI * 2f / (waveInterval * 4f));
            return Mathf.Lerp(1f, Mathf.Lerp(minimumWidth, Mathf.Min(1f, minimumWidth + .18f), pulse), envelope);
        }
        public override bool Validate(out string error)
        {
            if (!base.Validate(out error)) return false;
            if (projectile == null || waveInterval < .3f || warningDuration < .25f || gapWidth < 110f ||
                speed < 100f || speed > 320f || minimumWidth < .55f || minimumWidth > 1f ||
                wavesPerPhrase < 2 || wavesPerPhrase > 5 || phraseRest < .2f || Duration < 5f)
            { error = "Rhythmic pressure requires a continuous Heart gap, bounded squeeze and timed phrases."; return false; }
            return true;
        }
        public override ICombatMoveExecution CreateExecution(CombatMoveExecutionContext ctx) => new Execution(this, ctx);
        private sealed class Execution : ICombatMoveExecution
        {
            private readonly TimorPressureWaveMove data;
            private readonly CombatMoveExecutionContext ctx;
            private readonly CombatMoveStage stage;
            private readonly TMPro.TextMeshProUGUI supportText;
            private readonly Image[] beatMarks = new Image[2];
            private float elapsed, nextWave = .45f, lastBeat = -10f;
            private int wave;
            private bool cancelled;
            public Execution(TimorPressureWaveMove data, CombatMoveExecutionContext ctx)
            {
                this.data = data; this.ctx = ctx;
                stage = new CombatMoveStage(ctx.Board, Cancel);
                var root = stage.Root("Timor rhythm marks");
                root.SetAsFirstSibling();
                for (int i = 0; i < 2; i++)
                    beatMarks[i] = CombatMoveStage.Picture(root, "Phrase edge pulse", null, Vector2.one,
                        new Color(.85f, .82f, .9f, 0f));
                if (data.supported && data.encouragement != null && data.encouragement.Length > 0)
                    supportText = CombatMoveStage.Text(stage.Root("Support voices"), data.encouragement[0],
                        ctx.Board.TeacherQuestionFont, new Vector2(ctx.Board.PlayArea.rect.width - 32f, 56f),
                        19f, new Color(.95f, .86f, .6f, 1f));
                ctx.Board.SetMechanicHint(data.supported ? "Mọi người vẫn ở bên mình" : "Đi theo khoảng trống");
            }
            public bool IsComplete => cancelled || elapsed >= data.Duration;
            public void Tick(float dt)
            {
                if (cancelled || dt <= 0f) return;
                float previous = elapsed;
                elapsed = Mathf.Min(data.Duration, elapsed + dt);
                float envelope = Mathf.SmoothStep(0f, 1f, Mathf.Min(elapsed / .7f, (data.Duration - elapsed) / .7f));
                ctx.Board.SetBattleBoxHorizontalLayout(data.WidthAt(elapsed),
                    Mathf.Sin(elapsed * 1.05f) * data.sway * envelope);
                Rect field = ctx.Board.PlayArea.rect;
                if (supportText != null)
                {
                    supportText.text = data.encouragement[Mathf.Min(data.encouragement.Length - 1, (int)(elapsed / 3f))];
                    supportText.rectTransform.sizeDelta = new Vector2(field.width - 24f, 56f);
                    supportText.rectTransform.anchoredPosition = new Vector2(0, field.yMax - 34f);
                }
                float travel = (data.horizontalVolley ? field.width : field.height) + 56f;
                // Let the last rows leave before restoring the field; no empty seconds between phrases.
                float lastSpawn = data.Duration - travel / data.speed - data.warningDuration - .15f;
                while (nextWave <= elapsed && nextWave < lastSpawn)
                {
                    Emit(nextWave, Mathf.Max(0f, nextWave + data.warningDuration - previous), field);
                    lastBeat = nextWave; wave++;
                    nextWave += data.waveInterval + (wave % data.wavesPerPhrase == 0 ? data.phraseRest : 0f);
                }
                float alpha = Mathf.Clamp01(1f - (elapsed - lastBeat) / .32f) * .34f;
                Vector2 safe = data.SafePointAt(field, elapsed);
                for (int i = 0; i < 2; i++)
                {
                    float sign = i == 0 ? -1f : 1f;
                    var rect = beatMarks[i].rectTransform;
                    rect.sizeDelta = data.horizontalVolley ? new Vector2(4f, 24f) : new Vector2(24f, 4f);
                    rect.anchoredPosition = data.horizontalVolley
                        ? new Vector2(field.xMax - 5f, safe.y + sign * data.GapWidth * .5f)
                        : new Vector2(safe.x + sign * data.GapWidth * .5f, field.yMax - 5f);
                    beatMarks[i].color = new Color(.85f, .82f, .9f, alpha);
                }
            }
            private void Emit(float born, float delay, Rect field)
            {
                bool reverse = wave / data.wavesPerPhrase % 2 != 0;
                float halfAxis = (data.horizontalVolley ? field.height : field.width) * .5f;
                int columns = Mathf.CeilToInt(halfAxis / 28f) + 3;
                for (int side = -1; side <= 1; side += 2)
                    for (int column = 0; column < columns; column++)
                    {
                        float offset = side * (data.GapWidth * .5f + 10f + column * 28f);
                        var motion = new RowMotion(this, born + data.warningDuration, offset, reverse);
                        Vector2 origin = motion.Position(0f);
                        var bullet = stage.Bullet(ctx, data.projectile, origin, Vector2.zero, delay);
                        bullet?.ConfigurePathMotion(motion);
                    }
            }
            private sealed class RowMotion : ICombatProjectileMotion
            {
                private readonly Execution owner;
                private readonly float activeAt, offset;
                private readonly bool reverse;
                private bool cancelled;
                public RowMotion(Execution owner, float activeAt, float offset, bool reverse)
                { this.owner = owner; this.activeAt = activeAt; this.offset = offset; this.reverse = reverse; }
                public Vector2 Position(float age)
                {
                    Rect r = owner.ctx.Board.PlayArea.rect;
                    Vector2 safe = owner.data.SafePointAt(r, owner.elapsed);
                    float direction = reverse ? 1f : -1f;
                    return owner.data.horizontalVolley
                        ? new Vector2((reverse ? r.xMin + 16f : r.xMax - 16f) + direction * owner.data.speed * age, safe.y + offset)
                        : new Vector2(safe.x + offset, (reverse ? r.yMin + 16f : r.yMax - 16f) + direction * owner.data.speed * age);
                }
                public bool Tick(RectTransform target, float dt)
                {
                    if (cancelled || owner.cancelled || target == null) return false;
                    float age = Mathf.Max(0f, owner.elapsed - activeAt);
                    target.anchoredPosition = Position(age);
                    Rect r = owner.ctx.Board.PlayArea.rect;
                    float axis = owner.data.horizontalVolley ? r.width : r.height;
                    return age * owner.data.speed < axis + 48f;
                }
                public void Cancel() => cancelled = true;
            }
            public void Cancel()
            {
                if (cancelled) return;
                cancelled = true; stage?.Dispose(); ctx.Board.ResetBattleBoxLayout(); ctx.Board.SetMechanicHint(null);
            }
        }
    }
}
