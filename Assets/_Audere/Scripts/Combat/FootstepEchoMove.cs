using UnityEngine;

namespace Audere.Combat
{
    [CreateAssetMenu(menuName="Audere/Combat/Moves/Footstep Echo")]
    public sealed class FootstepEchoMove : CombatMoveDefinition
    {
        [SerializeField] private CombatBulletView projectile;
        [SerializeField,Min(.8f)] private float sampleInterval=1.2f;
        [SerializeField,Min(.7f)] private float warningDuration=.95f;
        public override bool Validate(out string error)
        {
            if(!base.Validate(out error))return false;
            if(projectile==null || sampleInterval<.8f || warningDuration<.7f)
            {error="Footstep echo needs a projectile and time to leave each recorded position.";return false;}
            return true;
        }
        public override ICombatMoveExecution CreateExecution(CombatMoveExecutionContext ctx)=>new Execution(this,ctx);
        private sealed class Execution:ICombatMoveExecution
        {
            private readonly FootstepEchoMove data; private readonly CombatMoveExecutionContext ctx;
            private readonly CombatMoveStage stage; private float elapsed,next=.7f; private bool cancelled;
            public Execution(FootstepEchoMove data,CombatMoveExecutionContext ctx)
            {this.data=data;this.ctx=ctx;stage=new CombatMoveStage(ctx.Board,Cancel);}
            public bool IsComplete=>cancelled||elapsed>=data.Duration;
            public void Tick(float dt)
            {
                if(cancelled||dt<=0)return;elapsed+=dt;
                if(elapsed<next||elapsed>data.Duration-1.6f)return;
                Vector2 p=ctx.Board.PlayerPosition;
                var bullet=stage.Bullet(ctx,data.projectile,p,Vector2.zero,data.warningDuration);
                bullet?.ConfigurePathMotion(new ParametricProjectileMotion(.4f,t=>p,t=>t*90f));
                next=elapsed+data.sampleInterval;
            }
            public void Cancel(){if(cancelled)return;cancelled=true;stage?.Dispose();}
        }
    }
}
