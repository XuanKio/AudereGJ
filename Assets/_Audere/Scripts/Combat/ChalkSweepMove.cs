using System;
using UnityEngine;

namespace Audere.Combat
{
    [CreateAssetMenu(menuName = "Audere/Combat/Moves/Chalk Sweep")]
    public sealed class ChalkSweepMove : CombatMoveDefinition
    {
        [SerializeField] private CombatBulletView projectilePrefab;
        [SerializeField, Min(.2f)] private float telegraph = .65f;
        [SerializeField, Min(.5f)] private float flightDuration = 3f;
        [SerializeField, Min(.3f)] private float interval = 1.7f;
        [SerializeField] private float turns = 1.5f;
        [SerializeField] private CombatProjectileTrailSettings stunTrail = new CombatProjectileTrailSettings();
        public override bool Validate(out string error)
        {
            if(!base.Validate(out error))return false;
            if(projectilePrefab==null||telegraph<=0||flightDuration<=0||interval<=0)
            {error="Chalk sweep requires projectile and positive timings.";return false;}
            return stunTrail.Validate(out error);
        }
        public override ICombatMoveExecution CreateExecution(CombatMoveExecutionContext c)
        { if(!Validate(out var e))throw new InvalidOperationException(e);return new Execution(this,c); }
        private sealed class Execution:ICombatMoveExecution
        {
            private readonly ChalkSweepMove d;private readonly CombatMoveExecutionContext c;
            private float elapsed,next;private int wave;private bool cancelled;
            public Execution(ChalkSweepMove d,CombatMoveExecutionContext c){this.d=d;this.c=c;}
            public bool IsComplete=>cancelled||elapsed>=d.Duration;
            public void Tick(float dt)
            {
                if(IsComplete||c.Board==null||c.Board.PlayArea==null)return;
                elapsed+=Mathf.Max(0,dt);if(elapsed<next||elapsed>=d.Duration)return;
                next=elapsed+d.interval;
                Rect r=c.Board.PlayArea.rect;bool right=(wave++%2)==0;
                float y=Mathf.Lerp(r.yMin,r.yMax,right?.25f:.75f);
                Vector2 start=new Vector2(right?r.xMin-25:r.xMax+25,y);
                Vector2 end=new Vector2(right?r.xMax+90:r.xMin-90,y);
                float direction=right?1:-1;
                var b=c.Board.SpawnEnemyBullet(d.projectilePrefab,start,Vector2.zero,c.SessionVersion,c.PhaseVersion,d.telegraph);
                b?.ConfigurePathMotion(d.stunTrail.Wrap(new ParametricProjectileMotion(d.flightDuration,t=>Vector2.Lerp(start,end,t),t=>t*360*d.turns*direction),c,this));
            }
            public void Cancel(){cancelled=true;c.Board?.ClearStunTrails(c.SessionVersion,c.PhaseVersion,this);}
        }
    }
}
