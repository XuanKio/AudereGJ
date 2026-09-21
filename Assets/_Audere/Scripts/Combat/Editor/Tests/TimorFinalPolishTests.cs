#if UNITY_EDITOR
using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Audere.Combat.Editor.Tests
{
    public sealed class TimorFinalPolishTests
    {
        private CombatBoardView board;
        private ICombatMoveExecution execution;
        private readonly string[] names={"TimorDiceBreak","TimorClockwiseClones","TimorWordCorridor","TimorPressureWaves","TimorFootsteps","TimorSupportedWaves","TimorReleaseChoice"};
        private sealed class RandomSource:ICombatRandom
        {
            private readonly System.Random rng=new System.Random(42);
            public float Value01()=>(float)rng.NextDouble();
            public float Range(float min,float max)=>Mathf.Lerp(min,max,Value01());
        }
        [SetUp]public void Setup()
        {
            board=Object.Instantiate(AssetDatabase.LoadAssetAtPath<CombatBoardView>("Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab"));
            board.gameObject.SetActive(true);board.PrepareEncounter("Timor QA");board.ResetPlayer();Canvas.ForceUpdateCanvases();
        }
        [TearDown]public void Cleanup()
        {
            execution?.Cancel();execution=null;
            if(board!=null)Object.DestroyImmediate(board.gameObject);
        }
        private CombatMoveDefinition Move(string name)=>AssetDatabase.LoadAssetAtPath<CombatMoveDefinition>(TimorFinalPolishAuthoring.Moves+"Move_"+name+".asset");
        private void Start(string name)
        {
            execution=Move(name).CreateExecution(new CombatMoveExecutionContext(board,null,new RandomSource(),123,7));
        }
        private void Tick(float seconds)
        {
            for(float t=0;t<seconds;t+=1f/60f){execution.Tick(1f/60f);board.TickHeartFeedback(1f/60f);board.TickBullets(1f/60f,.85f);}
        }
        [Test]public void AuthoredEncounter_HasThreeDistinctPhasesAndCompletionGates()
        {
            var enemy=AssetDatabase.LoadAssetAtPath<CombatEnemyDefinition>(TimorFinalPolishAuthoring.Root+"Enemy_TimorReturn.asset");
            Assert.IsTrue(enemy.Validate(out string error),error);Assert.AreEqual(3,enemy.PhaseCount);
            Assert.AreEqual(3,enemy.GetPhase(0).MoveSet.Count);Assert.AreEqual(3,enemy.GetPhase(1).MoveSet.Count);
            Assert.AreEqual(4,enemy.GetPhase(2).MoveSet.Count);
            var gate=enemy.GetPhase(2).DialogueCues.Single(c=>c.CueId=="timor-final-choice");
            Assert.AreEqual(CombatDialogueCueTrigger.MoveCompleted,gate.Trigger);
            Assert.AreSame(Move("TimorSupportedWaves"),gate.TriggerMove);Assert.IsTrue(gate.RequiredBeforeVictory);
            foreach(var name in names)Assert.IsTrue(Move(name).Validate(out error),name+": "+error);
        }
        [Test]public void AllNewMoves_PauseAndCancelRestoreBoardAndClearOwnedHazards()
        {
            Vector2 size=board.PlayArea.rect.size;
            foreach(var name in names)
            {
                Start(name);Tick(3f);
                var positions=board.GetComponentsInChildren<RectTransform>().ToDictionary(x=>x,x=>x.anchoredPosition);
                for(int i=0;i<30;i++)execution.Tick(0);
                foreach(var pair in positions)if(pair.Key!=null)Assert.AreEqual(pair.Value,pair.Key.anchoredPosition,name+" advanced while paused");
                execution.Cancel();execution.Cancel();
                Assert.AreEqual(size.x,board.PlayArea.rect.width,.01f,name);Assert.AreEqual(size.y,board.PlayArea.rect.height,.01f,name);
                Assert.IsFalse(board.GetComponentsInChildren<CombatBulletView>().Any(x=>x.CollisionActive),name);
                Assert.IsFalse(board.GetComponentsInChildren<CombatDieView>().Any(x=>x.CanInteract),name);
                Assert.IsFalse(board.GetComponentsInChildren<RectTransform>().Any(x=>x.name.StartsWith("Synchronized battle box")),name);
                execution=null;
            }
        }
        [Test]public void BoardDisable_CancelsExclusiveChoicesAndEchoRootsImmediately()
        {
            foreach(var name in new[]{"TimorDiceBreak","TimorWordCorridor","TimorPressureWaves"})
            {
                board.gameObject.SetActive(true);Start(name);Tick(2.5f);board.gameObject.SetActive(false);
                // Plain MonoBehaviours do not receive EditMode lifecycle messages automatically.
                typeof(CombatBoardView).GetMethod("OnDisable",System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance).Invoke(board,null);
                Assert.IsTrue(execution.IsComplete,name);board.gameObject.SetActive(true);
                Assert.IsFalse(board.GetComponentsInChildren<CombatDieView>().Any(x=>x.CanInteract),name);
                Assert.IsFalse(board.GetComponentsInChildren<CombatBulletView>().Any(x=>x.CollisionActive),name);
                execution=null;
            }
        }
        [Test]public void DiceBreak_CatchBeforeStrikeWinsAndOnlyOneReplacementIsSpawned()
        {
            Start("TimorDiceBreak");var dice=board.GetComponentsInChildren<CombatDieView>().Where(x=>x.CanInteract).ToArray();
            Assert.AreEqual(3,dice.Length);board.CatchCursor.position=dice[0].RectTransform.position;
            Tick(.6f);((ICombatMoveInputHandler)execution).HandleInput(true,false);
            Assert.AreEqual(CombatDiceConstants.AttackDamage,((ICombatMoveDamageReward)execution).ConsumePendingDamage());
            Assert.AreEqual(0,((ICombatMoveDamageReward)execution).ConsumePendingDamage());
            Tick(3f);Assert.AreEqual(3,board.GetComponentsInChildren<CombatDieView>().Count(x=>x.CanInteract));
            Tick(1f);Assert.AreEqual(3,board.GetComponentsInChildren<CombatDieView>().Count(x=>x.CanInteract));
        }
        [Test]public void DiceBreak_BreaksAtMostOneDieAndNeverCreatesDamagingShards()
        {
            Start("TimorDiceBreak");Tick(2f);
            Assert.AreEqual(2,board.GetComponentsInChildren<CombatDieView>().Count(x=>x.CanInteract));
            Assert.IsFalse(board.GetComponentsInChildren<CombatBulletView>().Any(x=>x.CollisionActive));
            Tick(1.8f);Assert.AreEqual(3,board.GetComponentsInChildren<CombatDieView>().Count(x=>x.CanInteract));
        }
        [Test]public void CorridorAttack_UsesSharedDamageAndMissedDiceRemainCatchable()
        {
            Start("TimorWordCorridor");Tick(7f);
            var die=board.GetComponentsInChildren<CombatDieView>().First(x=>x.CanInteract);
            board.CatchCursor.position=die.RectTransform.position;
            ((ICombatMoveInputHandler)execution).HandleInput(true,false);
            Assert.AreEqual(CombatDiceConstants.AttackDamage,((ICombatMoveDamageReward)execution).ConsumePendingDamage());
            Assert.AreEqual(0,((ICombatMoveDamageReward)execution).ConsumePendingDamage());
        }
        [Test]public void OrbitVolley_HasClockwiseThenCounterclockwiseOrder()
        {
            CollectionAssert.AreEqual(new[]{0,1,2,3,3,2,1,0},Enumerable.Range(0,8).Select(OrbitingCloneVolleyMove.ShooterIndex));
        }
        [Test]public void Clones_RemainOutsideField_AndPressureUsesOnlyOneBox()
        {
            Start("TimorClockwiseClones");Tick(3f);
            var copies=board.GetComponentsInChildren<UnityEngine.UI.Image>().Where(x=>x.name.StartsWith("Timor echo ")&&!x.name.Contains("rim")).ToArray();
            Assert.AreEqual(4,copies.Length);
            foreach(var image in copies)
            {
                var size=image.rectTransform.rect.size;
                Assert.IsFalse(board.PlayArea.rect.Overlaps(new Rect(image.rectTransform.anchoredPosition-size*.5f,size)));
                Assert.AreEqual(Color.white,image.color);
                Assert.Greater(size.x,600f,"Use the original Timor size, not a tiny icon.");
            }
            Assert.Greater(board.GetComponentsInChildren<RectTransform>().Count(x=>x.name=="Mount Monochrome Echo (runtime)"),0);
            Assert.IsTrue(board.GetComponentsInChildren<CombatBulletView>().Any(x=>x.transform.parent.name=="Exterior Projectile Root"));
            execution.Cancel();Start("TimorPressureWaves");Tick(3f);
            Assert.AreEqual(0,board.GetComponentsInChildren<RectTransform>().Count(x=>x.name.StartsWith("Synchronized battle box")));
        }
        [Test]public void RandomCorridorSequences_KeepAReachableGapAcrossTenThousandWaves()
        {
            var rng=new System.Random(781);int previous=1;
            Rect r=new Rect(-290,-172,580,344);
            // Conservative 180 units/s, 0.8s reaction, full 22-unit heart plus 32-unit word bar.
            for(int i=0;i<10000;i++)
            {
                int next=ScrollingWordCorridorMove.NextLane(previous,(float)rng.NextDouble());
                float distance=Mathf.Abs(ScrollingWordCorridorMove.LaneY(r,next)-ScrollingWordCorridorMove.LaneY(r,previous));
                Assert.LessOrEqual(distance/180f+.8f,3.2f);
                Assert.Greater(ScrollingWordCorridorMove.LaneY(r,2)-ScrollingWordCorridorMove.LaneY(r,1)-32f,22f*2.5f);
                previous=next;
            }
        }
        [Test]public void LongCorridor_SidesLeaveViewport_AndCancelRestoresWidth()
        {
            float width=board.PlayArea.rect.width;
            Start("TimorWordCorridor");Tick(3f);
            Assert.Less(board.PlayArea.rect.xMin,board.CorridorVisibleRect.xMin-50f);
            Assert.Greater(board.PlayArea.rect.xMax,board.CorridorVisibleRect.xMax+50f);
            Assert.Greater(board.PlayArea.rect.width,width);
            Assert.IsFalse(board.HasForcedPlayerControl,"The moving scenery must not lock dodge input.");
            Assert.IsTrue(board.IsCorridorRunnerActive,"Horizontal travel is automatic; mouse Y remains dodgeable.");
            execution.Cancel();Assert.AreEqual(width,board.PlayArea.rect.width,.01f);
            Assert.IsFalse(board.IsCorridorRunnerActive);
        }
        [Test]public void PressureWaveGaps_OverlapAndLeaveRoomInsideCursorBounds()
        {
            float width=580f;float previous=0;
            for(int wave=0;wave<200;wave++)
            {
                float center=TimorPressureWaveMove.GapCenter(wave,width);
                Assert.Less(Mathf.Abs(center-previous),138f-22f);
                Assert.Less(Mathf.Abs(center)+50f,width*.5f);
                previous=center;
            }
        }
        [Test]public void FinalPeak_CannotBeSkippedByBurstDamage_AndCompletionReleasesVictory()
        {
            var enemy=AssetDatabase.LoadAssetAtPath<CombatEnemyDefinition>(TimorFinalPolishAuthoring.Root+"Enemy_TimorReturn.asset");
            var runtime=new CombatEnemyRuntime(enemy,board,new RandomSource(),456);
            try
            {
                runtime.Start(2);
                foreach(var cue in runtime.CurrentPhase.DialogueCues)
                    if(cue.CueId!="timor-final-choice")runtime.MarkCueResolved(cue);
                runtime.ApplyDamage(10000,out _);Assert.AreEqual(1,runtime.CurrentHealth);
                var final=runtime.CurrentPhase.DialogueCues.Single(c=>c.CueId=="timor-final-choice");
                int version=runtime.MoveCompletionVersion;bool completed=false;
                for(int frame=0;frame<60*70;frame++)
                {
                    runtime.Tick(1f/60f);
                    var counter=board.GetComponentsInChildren<CombatDieView>().FirstOrDefault(d=>d.CanInteract);
                    if(counter!=null){board.CatchCursor.position=counter.RectTransform.position;runtime.HandleMoveInput(true,false);runtime.ConsumeMoveDamageReward();}
                    Assert.AreEqual(CombatEnemyRuntimeState.Playing,runtime.State);
                    if(runtime.MoveCompletionVersion!=version)
                    {
                        version=runtime.MoveCompletionVersion;
                        if(runtime.LastCompletedMove==final.TriggerMove){completed=true;break;}
                    }
                }
                Assert.IsTrue(completed,"Peak must be reachable within the phase TIME budget.");
                runtime.MarkCueResolved(final);runtime.Tick(.02f);
                Assert.AreEqual(CombatEnemyRuntimeState.Completed,runtime.State);
            }
            finally{runtime.Cancel();}
        }
    }
}
#endif
