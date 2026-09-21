using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Audere.Combat
{
    [CreateAssetMenu(menuName = "Audere/Combat/Moves/Chalk Spiral")]
    public sealed class TeacherSpiralSketchMove : CombatMoveDefinition, ICombatExclusiveDiceMove
    {
        [SerializeField] private CombatBulletView strokePrefab;
        [SerializeField] private Sprite chalkTip;
        [SerializeField] private Material chalkMaterial;
        [SerializeField, Min(.5f)] private float drawDuration = .8f;
        [SerializeField, Min(.3f)] private float warningDuration = .4f;
        [SerializeField, Min(1)] private int sweepRotations = 3;
        [SerializeField, Min(.1f)] private float secondsPerRotation = 2.5f / 3f;
        [SerializeField, Range(64,160)] private int segmentCount = 128;
        [SerializeField, Range(1.5f,2.5f)] private float turns = 2f;
        [SerializeField, Range(1.4f,1.7f)] private float outerRadius = 1.45f;
        [SerializeField, Range(5f,12f)] private float strokeWidth = 8f;
        public float ActivationTime => drawDuration+warningDuration;
        public float SweepDuration => sweepRotations * secondsPerRotation;
        public int SweepRotations => sweepRotations;
        public int SegmentCount => segmentCount;
        public float Turns => turns;
        public float OuterRadius => outerRadius;

        public override bool Validate(out string error)
        {
            if (!base.Validate(out error)) return false;
            if (strokePrefab == null || chalkTip == null || chalkMaterial == null || drawDuration < .5f ||
                warningDuration < .3f || sweepRotations < 1 || secondsPerRotation < .1f || segmentCount < 64 || turns < 1.5f ||
                turns > 2.5f || outerRadius < 1.4f || strokeWidth < 5f || Duration < ActivationTime+SweepDuration+.3f)
            { error = "Chalk spiral needs art, an open coil spacing and complete draw/warning/sweep/fade timing."; return false; }
            error = null;
            return true;
        }

        public float RotationAt(float activeAge) => -Mathf.Clamp(activeAge,0f,SweepDuration)*Mathf.PI*2f/secondsPerRotation;

        public Vector2 PointAt(Rect field, float progress, float activeAge)
        {
            float angle = progress*turns*Mathf.PI*2f+RotationAt(activeAge);
            float radius = progress*outerRadius;
            return field.center + Vector2.Scale(new Vector2(field.width*.5f+24f,field.height*.5f+24f),
                new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius);
        }

        public override ICombatMoveExecution CreateExecution(CombatMoveExecutionContext context)
        {
            if (!Validate(out string error)) throw new InvalidOperationException(error);
            return new Execution(this,context);
        }

        private sealed class Execution : ICombatMoveExecution
        {
            private readonly TeacherSpiralSketchMove data;
            private readonly CombatMoveExecutionContext context;
            private readonly List<(CombatBulletView bullet,int lease)> strokes = new List<(CombatBulletView,int)>();
            private CombatChalkSketchGraphic drawing;
            private Image chalk;
            private Rect field;
            private Vector2[] points;
            private float elapsed;
            private bool launched,cancelled;
            public Execution(TeacherSpiralSketchMove data,CombatMoveExecutionContext context)
            { this.data=data; this.context=context; }
            public bool IsComplete => cancelled || elapsed>=data.Duration;

            public void Tick(float dt)
            {
                if (IsComplete || dt<=0f) return;
                if (context.Board==null || !context.Board.isActiveAndEnabled) { Cancel(); return; }
                if (drawing==null) CreateDrawing();
                float previous=elapsed;
                elapsed=Mathf.Min(data.Duration,elapsed+dt);
                if (!launched && elapsed<data.ActivationTime)
                {
                    float progress=Mathf.Clamp01(elapsed/data.drawDuration)*data.segmentCount;
                    // The final point is NOT connected back to the first: this is an open spiral.
                    drawing.Draw(points,progress,data.strokeWidth);
                    drawing.color=new Color(1f,1f,1f,elapsed<data.drawDuration?.7f:.55f+.2f*Mathf.Sin(elapsed*24f));
                    int segment=Mathf.Min(data.segmentCount-1,Mathf.FloorToInt(progress));
                    chalk.rectTransform.anchoredPosition=Vector2.Lerp(points[segment],points[segment+1],progress-segment);
                    chalk.gameObject.SetActive(elapsed<data.drawDuration);
                }
                else if (!launched)
                {
                    launched=true;
                    drawing.gameObject.SetActive(false);
                    if (elapsed<data.ActivationTime+data.SweepDuration)
                        Launch(Mathf.Max(0f,data.ActivationTime-previous));
                }
                if (elapsed>=data.Duration) Cancel();
            }

            private void CreateDrawing()
            {
                field=context.Board.PlayArea.rect;
                points=new Vector2[data.segmentCount+1];
                for(int i=0;i<points.Length;i++)points[i]=data.PointAt(field,(float)i/data.segmentCount,0f);
                var go=new GameObject("Chalk spiral drawing (runtime)",typeof(RectTransform),typeof(CanvasRenderer),typeof(CombatChalkSketchGraphic));
                go.layer=context.Board.gameObject.layer;
                go.transform.SetParent(context.Board.MaskedProjectilePresentationRoot,false);
                go.transform.position=context.Board.PlayArea.position;
                drawing=go.GetComponent<CombatChalkSketchGraphic>();
                drawing.material=data.chalkMaterial; drawing.raycastTarget=false;
                drawing.rectTransform.sizeDelta=field.size;
                var tip=new GameObject("Spiral chalk tip",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
                tip.layer=go.layer; tip.transform.SetParent(go.transform,false);
                chalk=tip.GetComponent<Image>(); chalk.sprite=data.chalkTip; chalk.preserveAspect=true; chalk.raycastTarget=false;
                chalk.rectTransform.sizeDelta=new Vector2(40,14);
                chalk.rectTransform.localRotation=Quaternion.Euler(0,0,32);
            }

            private void Launch(float firstFrameDelay)
            {
                for(int i=0;i<data.segmentCount;i++)
                {
                    var bullet=context.Board.SpawnEnemyBullet(data.strokePrefab,Vector2.zero,Vector2.zero,
                        context.SessionVersion,context.PhaseVersion,firstFrameDelay);
                    if(bullet==null)continue;
                    bullet.SetReturnOnPlayerHit(false);
                    bullet.ConfigurePathMotion(new SpiralSegment(data,field,i));
                    strokes.Add((bullet,bullet.PoolLeaseVersion));
                }
            }

            public void Cancel()
            {
                if(cancelled)return;
                cancelled=true;
                foreach(var stroke in strokes)context.Board?.ReturnEnemyBullet(stroke.bullet,stroke.lease);
                strokes.Clear();
                if(drawing==null)return;
                drawing.gameObject.SetActive(false);
                if(Application.isPlaying)UnityEngine.Object.Destroy(drawing.gameObject);
                else UnityEngine.Object.DestroyImmediate(drawing.gameObject);
            }
        }

        private sealed class SpiralSegment : ICombatProjectileMotion
        {
            private readonly TeacherSpiralSketchMove data;
            private readonly Rect field;
            private readonly int segment;
            private float age;
            private bool cancelled;
            public SpiralSegment(TeacherSpiralSketchMove data,Rect field,int segment)
            { this.data=data; this.field=field; this.segment=segment; }
            public bool Tick(RectTransform target,float dt)
            {
                if(cancelled || target==null)return false;
                age=Mathf.Min(data.SweepDuration,age+Mathf.Max(0f,dt));
                Vector2 a=data.PointAt(field,(float)segment/data.segmentCount,age);
                Vector2 b=data.PointAt(field,(float)(segment+1)/data.segmentCount,age);
                target.anchoredPosition=(a+b)*.5f;
                target.sizeDelta=new Vector2(Vector2.Distance(a,b)+1f,data.strokeWidth);
                target.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg);
                return age<data.SweepDuration;
            }
            public void Cancel(){cancelled=true;}
        }
    }
}
