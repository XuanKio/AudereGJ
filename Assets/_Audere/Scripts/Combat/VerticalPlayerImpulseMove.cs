using System;
using UnityEngine;

namespace Audere.Combat
{
    [CreateAssetMenu(menuName="Audere/Combat/Moves/Vertical Player Impulse")]
    public sealed class VerticalPlayerImpulseMove:CombatMoveDefinition
    {
        [SerializeField,Min(.35f)] private float warningDuration=.9f;
        [SerializeField,Min(.1f)] private float impulseDuration=.65f;
        [SerializeField,Min(.3f)] private float recoveryDuration=1.6f;
        [SerializeField,Min(1)] private float speed=650f;
        public override bool Validate(out string error)
        {
            if(!base.Validate(out error))return false;
            if(warningDuration<.35f||impulseDuration<=0||recoveryDuration<.3f||speed<=0)
            {error="Vertical impulse needs a readable warning, finite pull and recovery.";return false;}return true;
        }
        public override ICombatMoveExecution CreateExecution(CombatMoveExecutionContext c)
        {if(!Validate(out var e))throw new InvalidOperationException(e);return new Execution(this,c);}
        private sealed class Execution:ICombatMoveExecution
        {
            private readonly VerticalPlayerImpulseMove d;private readonly CombatMoveExecutionContext c;
            private float elapsed;private bool cancelled;
            public Execution(VerticalPlayerImpulseMove d,CombatMoveExecutionContext c){this.d=d;this.c=c;}
            public bool IsComplete=>cancelled||elapsed>=d.Duration;
            public void Tick(float dt)
            {
                if(IsComplete||c.Board==null)return;
                elapsed+=Mathf.Max(0,dt);if(IsComplete){Release();return;}
                float cycle=d.warningDuration+d.impulseDuration+d.recoveryDuration;
                int index=Mathf.FloorToInt(elapsed/cycle);float t=elapsed-index*cycle;
                bool up=index%2==0;
                if(t<d.warningDuration)
                {c.Board.ReleaseVerticalPlayerControl(this);c.Board.SetMechanicHint(up?"Sắp bị hất lên — chuẩn bị né ngang":"Sắp bị kéo xuống — chuẩn bị né ngang");}
                else if(t<d.warningDuration+d.impulseDuration)
                {c.Board.SetMechanicHint("Vẫn có thể né sang trái / phải");c.Board.SetVerticalPlayerControl(this,up?.78f:.22f,d.speed,dt);}
                else Release();
            }
            private void Release(){c.Board?.ReleaseVerticalPlayerControl(this);c.Board?.SetMechanicHint(null);}
            public void Cancel(){if(cancelled)return;cancelled=true;Release();}
        }
    }
}
