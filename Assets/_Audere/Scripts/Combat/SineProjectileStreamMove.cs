using System;
using UnityEngine;

namespace Audere.Combat
{
    [CreateAssetMenu(menuName="Audere/Combat/Moves/Sine Projectile Stream")]
    public sealed class SineProjectileStreamMove:CombatMoveDefinition
    {
        [SerializeField] private CombatBulletView projectilePrefab;
        [SerializeField,Min(.1f)] private float interval=.18f;
        [SerializeField,Min(.2f)] private float telegraph=.4f;
        [SerializeField,Min(.5f)] private float flightDuration=3.2f;
        [SerializeField,Range(.02f,.25f)] private float amplitudeFraction=.12f;
        public override bool Validate(out string error)
        {
            if(!base.Validate(out error))return false;
            if(projectilePrefab==null||interval<.1f||telegraph<=0||flightDuration<=0||amplitudeFraction<=0||amplitudeFraction>.25f)
            {error="Sine stream requires prefab, safe amplitude and positive cadence.";return false;}return true;
        }
        public static Vector2 Evaluate(Rect r,float t,float center,float phase,float amplitude)
            =>new Vector2(r.xMin+r.width*(center+Mathf.Sin(t*Mathf.PI*2+phase)*amplitude),Mathf.Lerp(r.yMax+25,r.yMin-30,t));
        public override ICombatMoveExecution CreateExecution(CombatMoveExecutionContext c)
        {if(!Validate(out var e))throw new InvalidOperationException(e);return new Execution(this,c);}
        private sealed class Execution:ICombatMoveExecution
        {
            private readonly SineProjectileStreamMove d;private readonly CombatMoveExecutionContext c;
            private float elapsed,next;private int shot;private bool cancelled;
            public Execution(SineProjectileStreamMove d,CombatMoveExecutionContext c){this.d=d;this.c=c;}
            public bool IsComplete=>cancelled||elapsed>=d.Duration;
            public void Tick(float dt)
            {
                if(IsComplete||c.Board==null||c.Board.PlayArea==null)return;
                elapsed+=Mathf.Max(0,dt);if(elapsed<next||elapsed>=d.Duration)return;
                next=elapsed+d.interval;Rect r=c.Board.PlayArea.rect;
                float phase=-shot*d.interval/d.flightDuration*Mathf.PI*2;
                float center=(shot++/12)%2==0?.28f:.72f;
                var b=c.Board.SpawnEnemyBullet(d.projectilePrefab,Evaluate(r,0,center,phase,d.amplitudeFraction),Vector2.zero,c.SessionVersion,c.PhaseVersion,d.telegraph);
                b?.ConfigurePathMotion(new ParametricProjectileMotion(d.flightDuration,t=>Evaluate(r,t,center,phase,d.amplitudeFraction),t=>0));
            }
            public void Cancel(){cancelled=true;}
        }
    }
}
