#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Audere.Combat.Editor.Tests
{
    public sealed class EnemyMountDiveTests
    {
        private CombatBoardView board;
        private CombatEnemyActor actor;
        private EnemyMountDiveMove move;
        private Transform mount;
        private Vector3 home, scale;
        private Quaternion rotation;
        private Image frame;

        [SetUp]
        public void SetUp()
        {
            board=Object.Instantiate(AssetDatabase.LoadAssetAtPath<CombatBoardView>("Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab"));
            board.gameObject.SetActive(true);Canvas.ForceUpdateCanvases();
            board.PrepareEncounter("Mount dive test");
            var enemy=AssetDatabase.LoadAssetAtPath<CombatEnemyDefinition>("Assets/_Audere/Data/Combat/Crowd/Enemy_Crowd.asset");
            var mountTransform=(Transform)new SerializedObject(board).FindProperty("enemyMount").objectReferenceValue;
            actor=Object.Instantiate(enemy.ActorPrefab,mountTransform);
            board.BindAuthoredEnemyActor(actor);
            board.SpawnEnemyActor(enemy.ActorPrefab,901);
            board.GetComponentInChildren<CombatPlayerView>(true).ResetPlayer();
            mount=actor.transform.parent;home=mount.localPosition;scale=mount.localScale;rotation=mount.localRotation;
            frame=((RectTransform)new SerializedObject(board).FindProperty("battleBoxFrame").objectReferenceValue).GetComponent<Image>();
            move=AssetDatabase.LoadAssetAtPath<EnemyMountDiveMove>("Assets/_Audere/Data/Combat/Crowd/Move_MountDive_Alternating.asset");
        }

        [TearDown]
        public void TearDown(){if(board!=null)Object.DestroyImmediate(board.gameObject);}

        private ICombatMoveExecution StartMove() => move.CreateExecution(new CombatMoveExecutionContext(board,actor,new SystemCombatRandom(73),901,2));
        private void Tick(ICombatMoveExecution execution,float duration)
        {
            for(float t=0;t<duration;t+=.01f)
            {execution.Tick(.01f);board.TickBullets(.01f,.65f);}
        }
        private void AssertRestored()
        {
            Assert.IsFalse(board.IsMountDiveActive);Assert.AreEqual(0f,board.BoardSeparation);
            Assert.AreEqual(0,board.ActiveMountEchoes);Assert.IsTrue(frame.enabled);Assert.IsFalse(board.IsAttackWarningVisible);
            Assert.Less(Vector3.Distance(home,mount.localPosition),.001f);
            Assert.Less(Quaternion.Angle(rotation,mount.localRotation),.001f);
            Assert.AreEqual(scale,mount.localScale);
        }

        [TestCase(.2f)] [TestCase(.85f)] [TestCase(1.2f)] [TestCase(1.7f)] [TestCase(2.1f)]
        public void CancelDuringEveryStageRestoresAuthoredMountAndBoard(float at)
        {
            var execution=StartMove();Tick(execution,at);execution.Cancel();execution.Cancel();AssertRestored();
            Assert.AreEqual(0,board.TickBullets(.02f,.65f));
        }

        [Test]
        public void PauseFreezesMotionEchoAndSplitThenCompletionRestores()
        {
            var execution=StartMove();Tick(execution,.97f);
            Assert.Greater(board.ActiveMountEchoes,0);Assert.Greater(board.BoardSeparation,0f);
            Vector3 position=mount.localPosition;float gap=board.BoardSeparation;int echoes=board.ActiveMountEchoes;
            for(int i=0;i<50;i++)execution.Tick(0f);
            Assert.AreEqual(position,mount.localPosition);Assert.AreEqual(gap,board.BoardSeparation);Assert.AreEqual(echoes,board.ActiveMountEchoes);
            Tick(execution,move.Duration);Assert.IsTrue(execution.IsComplete);AssertRestored();
        }

        [TestCase(-300f,0)] [TestCase(0f,1)] [TestCase(300f,0)]
        public void BodySweepDamagesContactAndLeavesBothSidesSafe(float heartX,int expectedHits)
        {
            var owner=new object();Assert.IsTrue(board.BeginMountDive(owner,actor,move.RainbowEchoMaterial));
            board.CatchCursor.anchoredPosition=new Vector2(heartX,0f);
            board.AccumulateMountDiveBody(owner,new Vector2(0,400),new Vector2(0,-400),190,148);
            Assert.AreEqual(expectedHits,board.TickBullets(.016f,.65f));
            Assert.AreEqual(0,board.TickBullets(.016f,.65f),"The swept hit cannot persist into the return.");
            board.EndMountDive(owner);AssertRestored();
        }

        [Test]
        public void SplitProvidesTwoWideRegionsAndDiceBounceAtInnerEdges()
        {
            var owner=new object();board.BeginMountDive(owner,actor,move.RainbowEchoMaterial);
            float width=board.PlayArea.rect.width;board.SetMountDiveSplit(owner,-width*.22f,width*.11f,0,true);
            Rect left=board.GetDiceMovementBounds(new Vector2(-width*.4f,0));
            Rect right=board.GetDiceMovementBounds(new Vector2(width*.4f,0));
            Assert.Greater(left.width,width*.25f);Assert.Greater(right.width,width*.25f);
            Assert.Greater(right.xMin-left.xMax,width*.2f);
            var die=board.SpawnDie(CombatSymbol.Attack,100f);
            die.RectTransform.anchoredPosition=new Vector2(left.xMax,0);
            die.ConstrainToBounds(left);
            Assert.LessOrEqual(die.RectTransform.anchoredPosition.x+die.RectTransform.rect.width*.5f,left.xMax+.01f);
            board.EndMountDive(owner);AssertRestored();
        }

        [Test]
        public void SplitOuterEdgesUseHeartFootprintAndExpandThenRestoreMask()
        {
            var mask=board.PlayArea.GetComponent<RectMask2D>();Assert.IsNotNull(mask);
            Vector4 original=new Vector4(2,3,4,5);mask.padding=original;
            var owner=new object();board.BeginMountDive(owner,actor,move.RainbowEchoMaterial);
            var clamp=typeof(CombatBoardView).GetMethod("ClampCursorToBattleBox",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            var heart=board.GetComponentInChildren<CombatPlayerView>(true).RectTransform;
            var corners=new Vector3[4];Rect field=board.PlayArea.rect;
            foreach(float separation in new[]{20f,94f,35f})
            {
                board.SetMountDiveSplit(owner,0,separation,0,true);
                Assert.Less(mask.padding.x,original.x);Assert.Less(mask.padding.z,original.z);
                foreach(float side in new[]{-1f,1f})
                {
                    board.CatchCursor.anchoredPosition=(Vector2)clamp.Invoke(board,new object[]{new Vector2(side*9999,0)});
                    heart.GetWorldCorners(corners);
                    float min=corners.Min(p=>board.PlayArea.InverseTransformPoint(p).x);
                    float max=corners.Max(p=>board.PlayArea.InverseTransformPoint(p).x);
                    Assert.AreEqual(side<0?field.xMin-separation:field.xMax+separation,side<0?min:max,.01f,
                        "The visible Heart, rather than its larger catch circle, must reach the expanded outer edge.");
                }
            }
            board.EndMountDive(owner);Assert.AreEqual(original,mask.padding);AssertRestored();
        }

        [Test]
        public void ClearAndOldCancelCannotLeaveEffectsOrResetNextExecution()
        {
            var first=StartMove();Tick(first,.97f);board.ClearRuntimeBullets();AssertRestored();
            var second=StartMove();Tick(second,.97f);
            first.Cancel();Assert.IsTrue(board.IsMountDiveActive);Assert.Greater(board.BoardSeparation,0);
            second.Cancel();AssertRestored();
        }

        [Test]
        public void CrowdHasPressureAtTenAndQuietAtThreeWithRequiredStoryCue()
        {
            var enemy=AssetDatabase.LoadAssetAtPath<CombatEnemyDefinition>("Assets/_Audere/Data/Combat/Crowd/Enemy_Crowd.asset");
            Assert.IsTrue(enemy.Validate(out string error),error);
            Assert.AreEqual(CombatPhasePolicy.SharedHealthThresholds,enemy.PhasePolicy);
            Assert.AreEqual(10,enemy.GetPhase(0).SharedExitThreshold);
            Assert.AreEqual(3,enemy.PhaseCount);
            Assert.AreEqual(3,enemy.GetPhase(1).SharedExitThreshold);
            Assert.AreEqual(2,enemy.GetPhase(1).MoveSet.Count);
            Assert.IsFalse(enemy.GetPhase(1).MoveSet.Entries.Any(e=>e.Move is EnemyMountDiveMove));
            Assert.AreEqual(2,enemy.GetPhase(1).MoveSet.Entries.Count(e=>e.Move is ProcessionGateMove));
            Assert.IsInstanceOf<EnemyMountDiveMove>(enemy.GetPhase(1).DamageReactionMove);
            Assert.IsEmpty(enemy.GetPhase(1).DialogueCues);
            Assert.IsTrue(enemy.GetPhase(2).DialogueCues.Single().RequiredBeforeVictory);
            Assert.AreEqual(0,enemy.GetPhase(2).SharedExitThreshold);
            CollectionAssert.AreEqual(new[]{"Move_UncertainHands","Move_DistantVoices"},enemy.GetPhase(2).MoveSet.Entries.Select(e=>e.Move.name));
            foreach(var entry in enemy.GetPhase(1).MoveSet.Entries)Assert.IsTrue(entry.Move.Validate(out error),error);
            Assert.IsEmpty(ShaderUtil.GetShaderMessages(move.RainbowEchoMaterial.shader));
        }

        [TestCase("Move_WatchingAisle",-.23f)] [TestCase("Move_PressureCorridor",-.19f)]
        public void ProcessionHasRealGapPausesAndReturnsOnlyItsOwnLeases(string asset,float offset)
        {
            var gate=AssetDatabase.LoadAssetAtPath<ProcessionGateMove>("Assets/_Audere/Data/Combat/Crowd/"+asset+".asset");
            var execution=gate.CreateExecution(new CombatMoveExecutionContext(board,actor,new SystemCombatRandom(21),901,2));
            execution.Tick(.01f);
            var bullets=board.GetComponentsInChildren<CombatBulletView>();Assert.GreaterOrEqual(bullets.Length,2);
            float center=offset*(gate.FromBothSides?board.PlayArea.rect.height:board.PlayArea.rect.width);
            foreach(var b in bullets)
            {
                float p=gate.FromBothSides?b.RectTransform.anchoredPosition.y:b.RectTransform.anchoredPosition.x;
                Assert.GreaterOrEqual(Mathf.Abs(p-center),gate.GapWidth*.5f+8f);
                Assert.IsFalse(b.CollisionActive);
            }
            Vector3[] positions=bullets.Select(b=>b.transform.position).ToArray();execution.Tick(0f);board.TickBullets(0f,.65f);
            CollectionAssert.AreEqual(positions,bullets.Select(b=>b.transform.position));
            execution.Cancel();Assert.IsEmpty(board.GetComponentsInChildren<CombatBulletView>());
            var second=gate.CreateExecution(new CombatMoveExecutionContext(board,actor,new SystemCombatRandom(21),902,3));
            second.Tick(.01f);execution.Cancel();Assert.IsNotEmpty(board.GetComponentsInChildren<CombatBulletView>());
            second.Cancel();Assert.IsEmpty(board.GetComponentsInChildren<CombatBulletView>());
        }

        [Test]
        public void EachHitCountersThenReturnsToBasicAndThreeHpCancelsEverything()
        {
            var enemy=AssetDatabase.LoadAssetAtPath<CombatEnemyDefinition>("Assets/_Audere/Data/Combat/Crowd/Enemy_Crowd.asset");
            var runtime=new CombatEnemyRuntime(enemy,board,new SystemCombatRandom(41),902);
            runtime.Start();
            Assert.AreEqual(CombatEnemyProgression.PhaseBreak,runtime.ApplyDamage(99,out _));
            Assert.AreEqual(10,runtime.CurrentHealth);runtime.CompletePhaseBreak();
            Assert.IsInstanceOf<EnemyMountDiveMove>(runtime.CurrentMove);Assert.IsTrue(board.IsMountDiveActive);
            runtime.Tick(runtime.CurrentMove.Duration+.01f);
            Assert.IsInstanceOf<ProcessionGateMove>(runtime.CurrentMove);Assert.IsFalse(board.IsMountDiveActive);
            runtime.Tick(.6f);Assert.IsNotEmpty(board.GetComponentsInChildren<CombatBulletView>());
            runtime.ApplyDamage(1,out int applied);Assert.AreEqual(1,applied);
            Assert.IsInstanceOf<EnemyMountDiveMove>(runtime.CurrentMove);Assert.IsTrue(board.IsMountDiveActive);
            Assert.IsEmpty(board.GetComponentsInChildren<CombatBulletView>());
            runtime.Tick(.65f);Vector3 p=mount.localPosition;
            runtime.PauseForDialogue();runtime.Tick(2f);Assert.AreEqual(p,mount.localPosition);runtime.ResumeFromDialogue();
            runtime.ApplyDamage(1,out _); // A second hit earns a second counter, without snapping a diving body home.
            int version=runtime.MoveVersion;runtime.Tick(2f);
            Assert.Greater(runtime.MoveVersion,version);Assert.IsInstanceOf<EnemyMountDiveMove>(runtime.CurrentMove);
            runtime.Tick(2f);Assert.IsInstanceOf<ProcessionGateMove>(runtime.CurrentMove);
            Assert.AreEqual(CombatEnemyProgression.PhaseBreak,runtime.ApplyDamage(99,out _));
            Assert.AreEqual(3,runtime.CurrentHealth);Assert.IsFalse(board.IsMountDiveActive);
            runtime.CompletePhaseBreak();Assert.AreEqual(2,runtime.PhaseIndex);Assert.IsInstanceOf<GraspingHandsMove>(runtime.CurrentMove);
            runtime.RestartFromBeginning();Assert.AreEqual(19,runtime.CurrentHealth);Assert.IsFalse(board.IsMountDiveActive);
            runtime.Cancel();Assert.AreEqual(0,board.ActiveMountEchoes);
        }

        [Test]
        public void CatchHandCommitsAndOneSideStepDodgesWithoutTeleportingAcrossBoard()
        {
            var clasp=AssetDatabase.LoadAssetAtPath<ConvergingHandsMove>("Assets/_Audere/Data/Combat/Crowd/Move_ClaspAndStab.asset");
            var execution=clasp.CreateExecution(new CombatMoveExecutionContext(board,actor,new SystemCombatRandom(41),903,1));
            board.CatchCursor.anchoredPosition=Vector2.zero;
            Tick(execution,.2f);Assert.IsTrue(board.IsAttackWarningVisible);
            Assert.AreEqual(board.PlayArea.rect.xMin+22f,board.AttackWarningPosition.x,.01f);
            Tick(execution,.67f);Assert.IsFalse(board.IsAttackWarningVisible);
            // A 90-unit sideways motion after steering stops is sufficient.
            for(int i=0;i<22;i++)
            {
                board.CatchCursor.anchoredPosition=new Vector2(0,Mathf.Min(90f,(i+1)*9f));
                Tick(execution,.02f);Assert.IsFalse(board.HasForcedPlayerControl);
            }
            execution.Cancel();Assert.IsEmpty(board.GetComponentsInChildren<CombatBulletView>());
        }
    }
}
#endif
