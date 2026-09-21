#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Audere.Combat.Editor.Tests
{
    public sealed class TeacherSpiralSketchTests
    {
        private CombatBoardView board;
        private TeacherSpiralSketchMove move;
        [SetUp] public void Setup()
        {
            board=Object.Instantiate(AssetDatabase.LoadAssetAtPath<CombatBoardView>("Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab"));
            board.gameObject.SetActive(true);board.PrepareEncounter("Spiral QA");board.ResetPlayer();
            move=AssetDatabase.LoadAssetAtPath<TeacherSpiralSketchMove>("Assets/_Audere/Data/Combat/Teacher/Moves/Move_TeacherSpiralSketch.asset");
        }
        [TearDown] public void Cleanup(){if(board!=null)Object.DestroyImmediate(board.gameObject);}
        private ICombatMoveExecution Create()=>move.CreateExecution(new CombatMoveExecutionContext(board,null,new SystemCombatRandom(7),721,2));

        [Test] public void ProductionSpiralRunsThreeRotationsBeforeItFinishes()
        {
            Assert.IsTrue(move.Validate(out string error),error);
            Assert.AreEqual(3,move.SweepRotations);
            Assert.AreEqual(2.5f,move.SweepDuration,.001f);
            Assert.LessOrEqual(move.ActivationTime,1.21f);
            Assert.AreEqual(-2.4f*Mathf.PI,move.RotationAt(1f),.001f);
            Assert.AreEqual(-6f*Mathf.PI,move.RotationAt(move.SweepDuration),.001f);
            var execution=Create();
            execution.Tick(move.ActivationTime+.01f);board.TickBullets(move.ActivationTime+.01f,.55f);
            for(int i=0;i<124;i++){execution.Tick(.02f);board.TickBullets(.02f,.55f);}
            Assert.IsFalse(execution.IsComplete);
            Assert.AreEqual(move.SegmentCount,board.GetComponentsInChildren<CombatBulletView>(true).Count(b=>b.gameObject.activeSelf));
            execution.Tick(.5f);board.TickBullets(.5f,.55f);
            Assert.IsTrue(execution.IsComplete);
            Assert.IsFalse(board.GetComponentsInChildren<CombatBulletView>(true).Any(b=>b.gameObject.activeSelf));
        }

        [Test] public void DrawingIsHarmless_ThenCenterCampingTakesHitsWithoutErasingTheSpiral()
        {
            var execution=Create();
            float elapsed=0;int hits=0;
            while(elapsed<move.ActivationTime-.03f)
            {
                execution.Tick(.02f);Assert.AreEqual(0,board.TickBullets(.02f,.2f));elapsed+=.02f;
            }
            Assert.IsNotNull(board.GetComponentInChildren<CombatChalkSketchGraphic>());
            for(int frame=0;frame<90;frame++)
            {
                board.TickHeartFeedback(.02f);execution.Tick(.02f);hits+=board.TickBullets(.02f,.2f);
            }
            Assert.Greater(hits,2,"The center cannot become a permanent pocket by destroying the first stroke.");
            Assert.AreEqual(move.SegmentCount,board.GetComponentsInChildren<CombatBulletView>(true).Count(b=>b.gameObject.activeSelf));
            execution.Cancel();
            Assert.IsFalse(board.GetComponentsInChildren<CombatBulletView>(true).Any(b=>b.gameObject.activeSelf));
        }

        [Test] public void AContinuousRouteBetweenCoilsFitsTheHeartForTheWholeSweep()
        {
            var execution=Create();float elapsed=0;int hits=0;
            Rect field=board.PlayArea.rect;
            // At angle PI/2 the neighboring windings are radius k/4 and k*1.25.
            // Their midpoint provides an independently sampled mouse route.
            float pitch=move.OuterRadius/move.Turns;
            float radius=pitch*.75f;
            for(int frame=0;frame<Mathf.CeilToInt(move.Duration/.02f);frame++)
            {
                elapsed+=.02f;
                float age=Mathf.Max(0,elapsed-move.ActivationTime);
                float angle=Mathf.PI*.5f+move.RotationAt(age);
                Vector2 point=field.center+Vector2.Scale(new Vector2(field.width*.5f+24f,field.height*.5f+24f),
                    new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius);
                Assert.IsTrue(field.Contains(point));
                board.CatchCursor.position=board.PlayArea.TransformPoint(point);
                board.TickHeartFeedback(.02f);execution.Tick(.02f);hits+=board.TickBullets(.02f,.2f);
            }
            Assert.AreEqual(0,hits,"A real Heart collider must be able to follow the coil opening continuously.");
            execution.Cancel();
        }

        [Test] public void PausedSpiralDoesNotAdvance_CancelAndPoolReuseRestoreOrdinaryContact()
        {
            var execution=Create();
            board.CatchCursor.anchoredPosition=new Vector2(220,155);
            execution.Tick(move.ActivationTime+.1f);board.TickBullets(move.ActivationTime+.1f,1f);
            var strokes=board.GetComponentsInChildren<CombatBulletView>(true).Where(b=>b.gameObject.activeSelf).ToArray();
            var positions=strokes.Select(b=>b.RectTransform.anchoredPosition).ToArray();
            execution.Tick(0);board.TickBullets(0,1);
            CollectionAssert.AreEqual(positions,strokes.Select(b=>b.RectTransform.anchoredPosition).ToArray());
            var source=strokes[0].SourcePrefab;
            execution.Cancel();execution.Cancel();
            Assert.IsEmpty(board.GetComponentsInChildren<CombatChalkSketchGraphic>(true));
            var ordinary=board.SpawnEnemyBullet(source,Vector2.zero,Vector2.zero,722,1);
            Assert.IsFalse(ordinary.ReturnOnPlayerHit);
            Assert.AreEqual(source.GetComponent<RectTransform>().sizeDelta,ordinary.RectTransform.sizeDelta,
                "Variable-length chalk strips must restore their authored footprint on the next lease.");
            board.CatchCursor.anchoredPosition=Vector2.zero;
            board.TickBullets(.02f,1);
            Assert.IsTrue(ordinary.gameObject.activeSelf,"A reused ordinary projectile must also persist on Heart contact.");
        }

        [Test] public void CancellingWhileChalkIsDrawingLeavesNoInvisibleHazards()
        {
            var execution=Create();execution.Tick(.4f);execution.Cancel();
            Assert.AreEqual(0,board.TickBullets(3f,1f));
            Assert.IsEmpty(board.GetComponentsInChildren<CombatChalkSketchGraphic>(true));
            Assert.IsFalse(board.GetComponentsInChildren<CombatBulletView>(true).Any(b=>b.gameObject.activeSelf));
        }
    }
}
#endif
