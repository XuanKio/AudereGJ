using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Audere.Combat
{
    public enum TeacherSubjectPattern { Arithmetic, Geometry, Literature }

    [CreateAssetMenu(menuName = "Audere/Combat/Moves/Subject Pattern")]
    public sealed class TeacherSubjectMove : CombatMoveDefinition
    {
        [SerializeField] private TeacherSubjectPattern pattern;
        [SerializeField] private CombatBulletView glyphPrefab;
        [SerializeField] private CombatBulletView chalkPrefab;
        [SerializeField, Min(1.5f)] private float beatDuration = 2.55f;
        [SerializeField, Min(.6f)] private float warningDuration = 1.05f;
        [SerializeField, Min(60f)] private float projectileSpeed = 150f;
        [SerializeField, Min(.5f)] private float geometryFlightDuration = 2.15f;

        public TeacherSubjectPattern Pattern => pattern;
        public float WarningDuration => warningDuration;

        public override bool Validate(out string error)
        {
            if (!base.Validate(out error)) return false;
            if (glyphPrefab == null || chalkPrefab == null || warningDuration < .6f ||
                beatDuration <= warningDuration + .35f || Duration < beatDuration * 2f ||
                projectileSpeed < 60f || geometryFlightDuration < .5f)
            {
                error = "Subject pattern needs both prefabs and enough warning, flight, and recovery time.";
                return false;
            }
            error = null;
            return true;
        }

        public override ICombatMoveExecution CreateExecution(CombatMoveExecutionContext context)
        {
            if (!Validate(out string error)) throw new InvalidOperationException(error);
            return new Execution(this, context);
        }

        private sealed class Execution : ICombatMoveExecution
        {
            private readonly TeacherSubjectMove data;
            private readonly CombatMoveExecutionContext context;
            private readonly List<(CombatBulletView bullet, int lease)> bullets = new List<(CombatBulletView, int)>();
            private float elapsed;
            private int beat = -1;
            private bool fired, cancelled;

            public Execution(TeacherSubjectMove data, CombatMoveExecutionContext context)
            {
                this.data = data;
                this.context = context;
            }

            public bool IsComplete => cancelled || elapsed >= data.Duration;

            public void Tick(float deltaTime)
            {
                if (IsComplete || deltaTime <= 0f) return;
                if (context.Board == null || !context.Board.isActiveAndEnabled) { Cancel(); return; }
                elapsed += deltaTime;
                if (elapsed >= data.Duration) { Cancel(); return; }
                int nextBeat = Mathf.FloorToInt(elapsed / data.beatDuration);
                if (nextBeat >= 3) { context.Board.HideAttackWarning(this); return; }
                if (nextBeat != beat) { beat = nextBeat; fired = false; }
                float age = elapsed - beat * data.beatDuration;
                if (age < data.warningDuration)
                {
                    context.Board.ShowAttackWarning(this, WarningPosition(beat), age);
                    return;
                }
                context.Board.HideAttackWarning(this);
                if (fired) return;
                fired = true;
                switch (data.pattern)
                {
                    case TeacherSubjectPattern.Arithmetic: EmitArithmetic(beat); break;
                    case TeacherSubjectPattern.Geometry: EmitGeometry(beat); break;
                    case TeacherSubjectPattern.Literature: EmitLiterature(beat); break;
                }
            }

            private Vector2 WarningPosition(int index)
            {
                Rect r = context.Board.PlayArea.rect;
                if (data.pattern == TeacherSubjectPattern.Literature)
                    return new Vector2(index % 2 == 0 ? r.xMin + 34f : r.xMax - 34f, r.center.y);
                return data.pattern == TeacherSubjectPattern.Geometry
                    ? r.center : new Vector2(r.center.x, r.yMax - 35f);
            }

            private void EmitArithmetic(int index)
            {
                Rect r = context.Board.PlayArea.rect;
                string[] symbols = { "1", "+", "2", "=", "3" };
                int openLane = (index * 2 + 1) % symbols.Length;
                for (int lane = 0; lane < symbols.Length; lane++)
                {
                    if (lane == openLane) continue;
                    float x = Mathf.Lerp(r.xMin + 45f, r.xMax - 45f, (lane + .5f) / symbols.Length);
                    SpawnGlyph(symbols[lane], new Vector2(x, r.yMax + 18f), Vector2.down * data.projectileSpeed, 38f);
                }
            }

            private void EmitLiterature(int index)
            {
                Rect r = context.Board.PlayArea.rect;
                string[] words = { "đọc", "viết", "nghe" };
                bool fromLeft = index % 2 == 0;
                int openRow = (index + 1) % words.Length;
                for (int row = 0; row < words.Length; row++)
                {
                    if (row == openRow) continue;
                    float y = Mathf.Lerp(r.yMin + 48f, r.yMax - 48f, (row + .5f) / words.Length);
                    float x = fromLeft ? r.xMin - 42f : r.xMax + 42f;
                    SpawnGlyph(words[row], new Vector2(x, y),
                        (fromLeft ? Vector2.right : Vector2.left) * data.projectileSpeed, 72f);
                }
            }

            private void SpawnGlyph(string glyph, Vector2 position, Vector2 velocity, float width)
            {
                var bullet = context.Board.SpawnEnemyBullet(data.glyphPrefab, position, velocity,
                    context.SessionVersion, context.PhaseVersion);
                if (bullet == null) return;
                bullet.RectTransform.sizeDelta = new Vector2(width, 34f);
                var text = bullet.GetComponentInChildren<TextMeshProUGUI>(true);
                if (text != null) text.text = glyph;
                bullets.Add((bullet, bullet.PoolLeaseVersion));
            }

            private void EmitGeometry(int index)
            {
                Rect r = context.Board.PlayArea.rect;
                float radius = Mathf.Max(r.width, r.height) * .63f + 75f;
                for (int side = 0; side < 3; side++)
                {
                    float degrees = 90f + side * 120f + index * 35f;
                    Vector2 outward = new Vector2(Mathf.Cos(degrees * Mathf.Deg2Rad),
                        Mathf.Sin(degrees * Mathf.Deg2Rad));
                    Vector2 start = r.center + outward * radius;
                    Vector2 end = r.center - outward * radius;
                    float facing = Mathf.Atan2(-outward.y, -outward.x) * Mathf.Rad2Deg;
                    var bullet = context.Board.SpawnExteriorEnemyBullet(data.chalkPrefab, start,
                        context.SessionVersion, context.PhaseVersion, 0f);
                    if (bullet == null) continue;
                    bullet.ConfigurePathMotion(new ParametricProjectileMotion(data.geometryFlightDuration,
                        t => Vector2.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t)), t => facing));
                    bullets.Add((bullet, bullet.PoolLeaseVersion));
                }
            }

            public void Cancel()
            {
                if (cancelled) return;
                cancelled = true;
                if (context.Board == null) return;
                context.Board.HideAttackWarning(this);
                foreach (var lease in bullets)
                    context.Board.ReturnEnemyBullet(lease.bullet, lease.lease);
                bullets.Clear();
            }
        }
    }
}
