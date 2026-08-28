using System;
using UnityEngine;

namespace Audere.Combat
{
    [CreateAssetMenu(menuName = "Audere/Combat/Moves/Chalk Fence")]
    public sealed class ChalkFenceMove : CombatMoveDefinition
    {
        [SerializeField] private CombatBulletView projectilePrefab;
        [SerializeField, Range(3, 12)] private int columns = 7;
        [SerializeField, Range(.15f, .45f)] private float reachFraction = .24f;
        [SerializeField, Min(.4f)] private float waveInterval = 2.6f;
        [SerializeField, Min(.2f)] private float telegraph = .65f;
        [SerializeField, Min(.3f)] private float flightDuration = 1.8f;
        public override bool Validate(out string error)
        {
            if (!base.Validate(out error)) return false;
            if (projectilePrefab == null || columns < 3 || reachFraction < .15f || reachFraction > .45f || waveInterval <= 0 || telegraph <= 0 || flightDuration <= 0)
            { error = "Chalk fence requires prefab, >=3 columns, safe reach and positive timings."; return false; }
            return true;
        }
        public static float Reach(float t) => Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
        public override ICombatMoveExecution CreateExecution(CombatMoveExecutionContext c)
        { if (!Validate(out var error)) throw new InvalidOperationException(error); return new Execution(this, c); }
        private sealed class Execution : ICombatMoveExecution
        {
            private readonly ChalkFenceMove d; private readonly CombatMoveExecutionContext c;
            private float elapsed, next; private int wave; private bool cancelled;
            public Execution(ChalkFenceMove d, CombatMoveExecutionContext c) { this.d=d; this.c=c; }
            public bool IsComplete => cancelled || elapsed >= d.Duration;
            public void Tick(float dt)
            {
                if (IsComplete || c.Board == null || c.Board.PlayArea == null) return;
                elapsed += Mathf.Max(0, dt);
                if (elapsed < next || elapsed >= d.Duration) return;
                next = elapsed + d.waveInterval;
                Rect r=c.Board.PlayArea.rect;
                // Alternate a clear lane; each paired fence also leaves a broad central corridor.
                int gap = 1 + wave++ % (d.columns - 2);
                for(int i=0;i<d.columns;i++)
                {
                    if(i==gap) continue;
                    float x=Mathf.Lerp(r.xMin+20,r.xMax-20,(i+.5f)/d.columns);
                    for(int side=-1;side<=1;side+=2)
                    {
                        float y=side<0?r.yMin-25:r.yMax+25;
                        Vector2 start=new Vector2(x,y);
                        float depth=-side*r.height*d.reachFraction;
                        var b=c.Board.SpawnEnemyBullet(d.projectilePrefab,start,Vector2.zero,c.SessionVersion,c.PhaseVersion,d.telegraph);
                        b?.ConfigurePathMotion(new ParametricProjectileMotion(d.flightDuration,
                            t=>start+Vector2.up*(depth*Reach(t)), t=>90f));
                    }
                }
            }
            public void Cancel() { cancelled=true; }
        }
    }
}
