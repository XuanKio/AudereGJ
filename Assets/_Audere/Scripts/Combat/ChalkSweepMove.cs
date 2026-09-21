using System;
using System.Collections.Generic;
using UnityEngine;

namespace Audere.Combat
{
    [CreateAssetMenu(menuName = "Audere/Combat/Moves/Chalk Sweep")]
    public sealed class ChalkSweepMove : CombatMoveDefinition
    {
        [SerializeField] private CombatBulletView projectilePrefab;
        [SerializeField, Min(.2f)] private float telegraph = .65f;
        [SerializeField, Min(.5f)] private float flightDuration = 3f;
        [SerializeField, Min(.15f)] private float interval = 1.7f;
        [SerializeField] private float turns = 1.5f;
        [SerializeField] private bool clockwiseLunges;
        [SerializeField] private bool shorteningClockwise;
        [SerializeField, Range(.35f, .8f)] private float finalTravelFraction = .52f;
        [SerializeField] private CombatProjectileTrailSettings stunTrail = new CombatProjectileTrailSettings();
        public bool ClockwiseLunges => clockwiseLunges;
        public override bool Validate(out string error)
        {
            if(!base.Validate(out error))return false;
            if(projectilePrefab==null||telegraph<=0||flightDuration<=0||interval<=0||Duration<telegraph+flightDuration ||
                (shorteningClockwise && !clockwiseLunges))
            {error="Chalk sweep requires projectile and a complete telegraph/flight.";return false;}
            return stunTrail.Validate(out error);
        }
        public static Vector2 ClockwiseDirection(int shot)
        {
            float angle=(135f-shot*45f)*Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
        }
        public override ICombatMoveExecution CreateExecution(CombatMoveExecutionContext c)
        { if(!Validate(out var e))throw new InvalidOperationException(e);return new Execution(this,c); }
        private sealed class Execution:ICombatMoveExecution
        {
            private readonly ChalkSweepMove d;private readonly CombatMoveExecutionContext c;
            private readonly List<(CombatBulletView bullet,int lease)> bullets=new List<(CombatBulletView,int)>();
            private readonly List<(Vector2 point,float until)> warnings=new List<(Vector2,float)>();
            private float elapsed,next;private int wave;private bool cancelled;
            public Execution(ChalkSweepMove d,CombatMoveExecutionContext c){this.d=d;this.c=c;}
            public bool IsComplete=>cancelled||elapsed>=d.Duration;
            public void Tick(float dt)
            {
                if(IsComplete||dt<=0)return;
                if(c.Board==null||!c.Board.isActiveAndEnabled){Cancel();return;}
                elapsed+=dt;
                if(elapsed>=d.Duration){Cancel();return;}
                if(elapsed>=next && elapsed+d.telegraph+d.flightDuration<=d.Duration)
                {
                    next=elapsed+d.interval;
                    Spawn();
                }
                warnings.RemoveAll(w=>elapsed>=w.until);
                var points=new Vector2[warnings.Count];
                for(int i=0;i<points.Length;i++)points[i]=warnings[i].point;
                c.Board.ShowAttackWarnings(this,points,elapsed);
            }
            private void Spawn()
            {
                Rect r=c.Board.PlayArea.rect;
                Vector2 start,end,warning;float rotation,direction;
                if(d.clockwiseLunges)
                {
                    int shot=wave++;
                    Vector2 outward=d.shorteningClockwise && shot>=4
                        ? new Vector2(Mathf.Cos((112.5f-shot*45f)*Mathf.Deg2Rad),
                            Mathf.Sin((112.5f-shot*45f)*Mathf.Deg2Rad))
                        : ClockwiseDirection(shot);
                    float sx=Mathf.Abs(outward.x)<.001f?float.PositiveInfinity:r.width*.5f/Mathf.Abs(outward.x);
                    float sy=Mathf.Abs(outward.y)<.001f?float.PositiveInfinity:r.height*.5f/Mathf.Abs(outward.y);
                    float distance=Mathf.Min(sx,sy);
                    start=r.center+outward*(distance+d.projectilePrefab.GetComponent<RectTransform>().rect.width*.5f+18f);
                    float travelFraction=d.shorteningClockwise
                        ? Mathf.Lerp(1f,d.finalTravelFraction,Mathf.Clamp01((shot-3f)/5f)) : 1f;
                    end=r.center-outward*(distance+100f)*travelFraction;
                    warning=r.center+outward*(distance-30f);
                    rotation=Mathf.Atan2(-outward.y,-outward.x)*Mathf.Rad2Deg;direction=0;
                }
                else
                {
                    bool right=(wave++%2)==0;float y=Mathf.Lerp(r.yMin,r.yMax,right?.25f:.75f);
                    start=new Vector2(right?r.xMin-70:r.xMax+70,y);
                    end=new Vector2(right?r.xMax+90:r.xMin-90,y);
                    warning=new Vector2(right?r.xMin+30:r.xMax-30,y);
                    rotation=0;direction=right?1:-1;
                }
                var b=c.Board.SpawnExteriorEnemyBullet(d.projectilePrefab,start,c.SessionVersion,c.PhaseVersion,d.telegraph);
                if(b==null)return;
                b.ConfigurePathMotion(d.stunTrail.Wrap(new ParametricProjectileMotion(d.flightDuration,
                    t=>Vector2.Lerp(start,end,t),t=>rotation+t*360*d.turns*direction),c,this));
                b.FadeInDuringTelegraph();bullets.Add((b,b.PoolLeaseVersion));
                warnings.Add((warning,elapsed+d.telegraph));
            }
            public void Cancel()
            {
                if(cancelled)return;cancelled=true;
                c.Board?.HideAttackWarning(this);c.Board?.ClearStunTrails(c.SessionVersion,c.PhaseVersion,this);
                foreach(var b in bullets)c.Board?.ReturnEnemyBullet(b.bullet,b.lease);
                bullets.Clear();warnings.Clear();
            }
        }
    }
}
