#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Audere.Audio;
using Audere.Combat;
using Audere.Core;
using Audere.Dialogue;
using Audere.Story.Steps;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Audere.Story.Editor.Tests
{
    public sealed class Day4CrowdTests
    {
        private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        [Test]
        public void Content_DirectSceneBindingsNineteenHealthAndAnEveningDestination()
        {
            var scene=EditorSceneManager.OpenScene(Day4CrowdSetupTool.ScenePath);
            Assert.IsFalse(scene.GetRootGameObjects().Any(x=>x.name=="DAY FOUR CLASSROOM COVER"));
            Assert.IsTrue(new SerializedObject(All<StoryDirector>(scene).Single())
                .FindProperty("playOnStart").boolValue,
                "Scene140 production StoryDirector must start automatically after Scene130 loads it.");
            var combat=All<CombatStep>(scene).Single();var data=combat.CombatEncounterData;
            Assert.IsTrue(data.EnemyDefinition.Validate(out var error),error);Assert.AreEqual(19,data.EnemyDefinition.SharedMaxHealth);Assert.AreEqual(90,data.EncounterDuration);
            Assert.AreEqual(CombatPhasePolicy.SharedHealthThresholds,data.EnemyDefinition.PhasePolicy);
            foreach(var phase in data.EnemyDefinition.Phases)foreach(var entry in phase.MoveSet.Entries)AssertNoCornerPull(entry.Move);
            CollectionAssert.AreEqual(
                new[]{"Move_HandWaves","Move_ClaspAndStab","Move_RushingVoices","Move_ShiftingPalms"},
                data.EnemyDefinition.GetPhase(0).MoveSet.Entries.Select(entry=>entry.Move.name).ToArray());
            Assert.AreEqual(10,data.EnemyDefinition.GetPhase(0).SharedExitThreshold);
            Assert.AreEqual(3,data.EnemyDefinition.GetPhase(1).SharedExitThreshold);
            Assert.AreEqual(0,data.EnemyDefinition.GetPhase(2).SharedExitThreshold);
            Assert.IsTrue(data.EnemyDefinition.Phases.All(x=>Mathf.Approximately(0f,x.PlayerTimeExitFraction)));
            Assert.IsTrue(data.EnemyDefinition.GetPhase(2).DialogueCues.Single().RequiredBeforeVictory);
            Assert.IsNotNull(combat.EnemyActorOverride);Assert.AreEqual(3,data.DicePerBatch);Assert.AreEqual(1,data.MaximumAttacksPerBatch);Assert.AreEqual(1,data.AdditionalRerolledAttacksPerBatch);
            Assert.AreEqual(8,All<Transform>(scene).Count(x=>x.name=="Desk centered on tile"));
            var stage=All<Transform>(scene).Single(t=>t.name=="DAY FOUR TILE CLASSROOM");
            var masks=All<SpriteRenderer>(scene).Where(r=>r.name.StartsWith("Mask ")).ToArray();
            float left=masks.Single(r=>r.name=="Mask Left").bounds.max.x;
            float right=masks.Single(r=>r.name=="Mask Right").bounds.min.x;
            float bottom=masks.Single(r=>r.name=="Mask Bottom").bounds.max.y;
            float top=masks.Single(r=>r.name=="Mask Top").bounds.min.y;
            foreach(var r in stage.GetComponentsInChildren<SpriteRenderer>())
            {
                Assert.Greater(r.bounds.min.x,left+.06f,r.name);Assert.Less(r.bounds.max.x,right-.06f,r.name);
                Assert.Greater(r.bounds.min.y,bottom+.06f,r.name);Assert.Less(r.bounds.max.y,top-.06f,r.name);
            }
            Assert.AreEqual(UnityEngine.UI.CanvasScaler.ScreenMatchMode.Expand,All<UnityEngine.UI.CanvasScaler>(scene).Single(x=>x.name=="GameplayUIRoot").screenMatchMode);
            var a=All<Transform>(scene).Single(x=>x.name=="Audere");var b=All<Transform>(scene).Single(x=>x.name=="Bianca");
            AssertFeet(a,All<Transform>(scene).Single(x=>x.name=="Audere Tile"));AssertFeet(b,All<Transform>(scene).Single(x=>x.name=="Bianca Tile"));
            foreach(var e in All<StoryEvent>(scene))foreach(Transform t in e.transform)Assert.AreEqual(1,t.GetComponents<StoryStep>().Length,t.name);
            Assert.AreEqual(2,All<ParallelStoryStep>(scene).Single(x=>x.name=="140_BothFindTheirFeet").Branches.Count);
            foreach(var t in All<Transform>(scene))Assert.AreEqual(0,GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
            foreach(var guid in AssetDatabase.FindAssets("t:DialogueData",new[]{Day4CrowdSetupTool.DialogueFolder}))
                foreach(var line in AssetDatabase.LoadAssetAtPath<DialogueData>(AssetDatabase.GUIDToAssetPath(guid)).Lines)Assert.LessOrEqual(line.Text.Length,42,line.Text);
            Assert.IsEmpty(ShaderUtil.GetShaderMessages(Shader.Find("Audere/UI/WrithingHand")));
            Assert.IsEmpty(ShaderUtil.GetShaderMessages(Shader.Find("Audere/UI/DriftingSpriteField")));
            Assert.IsTrue(new SerializedObject(All<Audere.World.WorldModeController>(scene).Single()).FindProperty("storyUsesPuzzleViewportMask").boolValue);
            Assert.AreEqual(1,All<DriftingSpriteField>(scene).Length);
            Assert.AreEqual(3,AssetDatabase.LoadAssetAtPath<DialogueData>(Day4CrowdSetupTool.DialogueFolder+"/Dialogue_D4_FALL.asset").Lines.Count);
            Assert.AreEqual(GameScenes.Day4HomeEvening,new SerializedObject(All<SceneLoadStep>(scene).Single()).FindProperty("sceneName").stringValue);
            Assert.IsTrue(EditorBuildSettings.scenes.Any(x=>x.path==Day4CrowdSetupTool.EveningPath&&x.enabled));
        }

        [UnityTest]
        public IEnumerator Production_FallHandsRealVoiceVictoryHelpAndEvening()
        {
            EditorSceneManager.OpenScene(Day4CrowdSetupTool.ScenePath);
            yield return new EnterPlayMode();yield return null;
            yield return ProductionChecks();yield return new ExitPlayMode();
        }
        private static IEnumerator ProductionChecks()
        {
            Services();System.IO.Directory.CreateDirectory("Temp/Day4Crowd");
            var scene=SceneManager.GetActiveScene();var director=All<StoryDirector>(scene).Single();var c=All<CombatController>(scene).Single();
            yield return Until(()=>GameplayUIRoot.Instance.Dialogue.IsPlaying);
            yield return Capture("classroom-standing");
            yield return Until(()=>director.CurrentEvent.CurrentStep.name=="060_TheyMustBeLaughing",true);
            var a=All<Transform>(scene).Single(x=>x.name=="Audere");Assert.Less(Quaternion.Angle(a.rotation,Quaternion.Euler(0,0,90)),.1f);
            yield return Capture("classroom-fallen");
            yield return Until(()=>c.CurrentState==CombatController.State.Playing,true);
            int firstPhase=c.EnemyRuntime.PhaseVersion;bool sawHand=false,sawClasp=false,sawNormal=false;
            double deadline=EditorApplication.timeSinceStartup+19;
            while(EditorApplication.timeSinceStartup<deadline&&c.IsPlaying)
            {
                sawHand|=c.BoardView.GetComponentsInChildren<CombatBulletView>().Any(x=>x.SourcePrefab!=null&&x.SourcePrefab.name=="Bullet_CrowdHand");
                sawNormal|=c.BoardView.GetComponentsInChildren<CombatBulletView>().Any(x=>x.SourcePrefab!=null&&x.SourcePrefab.name=="EnemyBullet");
                sawClasp|=c.EnemyRuntime.CurrentMove is ConvergingHandsMove;
                if(c.EnemyRuntime.CurrentMove!=null)AssertNoCornerPull(c.EnemyRuntime.CurrentMove);
                if(c.PlayerTime<55)c.DebugApplyDiceEffect(CombatSymbol.Heal);
                if(c.EnemyRuntime.PhaseElapsed>7.4f&&c.EnemyRuntime.PhaseElapsed<7.5f)ScreenCapture.CaptureScreenshot("Temp/Day4Crowd/hands-and-volley.png");
                if(c.EnemyRuntime.PhaseElapsed>2.4f&&c.EnemyRuntime.PhaseElapsed<2.5f)ScreenCapture.CaptureScreenshot("Temp/Day4Crowd/crowd-portrait.png");
                yield return null;
            }
            Assert.IsTrue(sawHand);Assert.IsTrue(sawClasp);Assert.IsTrue(sawNormal);
            Assert.AreEqual(0,c.EnemyRuntime.PhaseIndex);
            c.EnemyRuntime.ApplyDamage(c.EnemyHealth-11,out int denseDamage);
            Assert.Greater(denseDamage,0);
            Assert.AreEqual(11,c.EnemyHealth);
            Assert.AreEqual(0,c.EnemyRuntime.PhaseIndex,"Crowd must stay dense until only 10 HP remain.");
            c.EnemyRuntime.ApplyDamage(1,out int thresholdDamage);
            Assert.AreEqual(1,thresholdDamage);
            yield return Until(()=>c.EnemyRuntime.PhaseIndex==1);
            Assert.AreEqual(10,c.EnemyHealth,"The mount pressure phase starts when the crowd reaches 10 HP.");
            Assert.IsFalse(c.BoardView.HasForcedPlayerControl);
            Assert.IsFalse(c.BoardView.GetComponentsInChildren<CombatBulletView>().Any(x=>x.OwnerPhaseVersion==firstPhase));
            yield return Until(()=>c.BoardView.IsMountDiveActive,false,5);
            yield return new WaitForSecondsRealtime(.98f);yield return Capture("mount-dive-pressure");
            c.EnemyRuntime.ApplyDamage(6,out _);Assert.AreEqual(4,c.EnemyHealth);Assert.AreEqual(1,c.EnemyRuntime.PhaseIndex);
            c.EnemyRuntime.ApplyDamage(1,out _);
            yield return Until(()=>c.EnemyRuntime.PhaseIndex==2);
            Assert.AreEqual(3,c.EnemyHealth);Assert.IsFalse(c.BoardView.IsMountDiveActive);
            Assert.AreEqual(0,c.BoardView.BoardSeparation);Assert.AreEqual(0,c.BoardView.ActiveMountEchoes);
            yield return new WaitForSecondsRealtime(1.3f);yield return Capture("bianca-real-voice");
            yield return Until(()=>c.EnemyRuntime.IsCueResolved("crowd-bianca-real-voice"),false,35);
            yield return Until(()=>c.EnemyRuntime.State==CombatEnemyRuntimeState.Playing,false,5);
            c.EnemyRuntime.ApplyDamage(99,out int finishingDamage);
            Assert.AreEqual(3,finishingDamage);
            yield return Until(()=>director.CurrentEvent.CurrentStep.name=="120_DoNotRush",false,10);
            var b=All<Transform>(scene).Single(x=>x.name=="Bianca");AssertFeet(b,All<Transform>(scene).Single(x=>x.name=="Bianca Tile"));
            Assert.Less(Quaternion.Angle(a.rotation,Quaternion.Euler(0,0,90)),.1f);
            yield return Capture("bianca-beside-fallen-audere");
            yield return Until(()=>director.CurrentEvent.CurrentStep.name=="160_NotAnApology",true);
            Assert.Less(Quaternion.Angle(a.rotation,Quaternion.identity),.1f);AssertFeet(a,All<Transform>(scene).Single(x=>x.name=="Audere Tile"));
            yield return Capture("both-standing");
            LogAssert.Expect(LogType.Log,"[SceneFlow] Loading '"+GameScenes.Day4HomeEvening+"'...");
            LogAssert.Expect(LogType.Log,"[SceneFlow] Loaded '"+GameScenes.Day4HomeEvening+"'.");
            yield return Until(()=>SceneManager.GetActiveScene().name==GameScenes.Day4HomeEvening,true);
            yield return Until(()=>GameplayUIRoot.Instance.Dialogue.IsPlaying);
            All<StoryDirector>(SceneManager.GetActiveScene()).Single().CancelCurrentEvent();
            Assert.AreEqual(0,GameplayUIRoot.Instance.InputGate.ActiveClaimCount);Assert.IsFalse(GameplayUIRoot.Instance.Dialogue.IsPlaying);
            Assert.IsTrue(All<SpriteRenderer>(SceneManager.GetActiveScene()).Any(x=>x.gameObject.activeInHierarchy));
            Assert.IsTrue(All<CanvasGroup>(SceneManager.GetActiveScene()).Where(x=>x.name=="Fade"||x.name=="Cover").All(x=>x.alpha==0f&&!x.blocksRaycasts));
            yield return Capture("evening-arrival");LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator CancelPosePullAndDefeatRetry_ReleaseOwnership()
        {
            var scene=EditorSceneManager.OpenScene(Day4CrowdSetupTool.ScenePath);var so=new SerializedObject(All<StoryDirector>(scene).Single());so.FindProperty("playOnStart").boolValue=false;so.ApplyModifiedPropertiesWithoutUndo();
            yield return new EnterPlayMode();yield return null;
            yield return LifecycleChecks();yield return new ExitPlayMode();
        }
        [UnityTest]
        public IEnumerator DefeatRetry_PresentationReleasesInputAndRestartsCombat()
        {
            var scene = EditorSceneManager.OpenScene(Day4CrowdSetupTool.ScenePath);
            var data = new SerializedObject(All<StoryDirector>(scene).Single());
            data.FindProperty("playOnStart").boolValue = false;
            data.ApplyModifiedPropertiesWithoutUndo();
            yield return new EnterPlayMode();
            yield return null;
            yield return RetryPresentationChecks();
            yield return new ExitPlayMode();
        }

        private static IEnumerator RetryPresentationChecks()
        {
            Services();
            var scene = SceneManager.GetActiveScene();
            All<Audere.World.WorldModeController>(scene).Single()
                .ApplyModeImmediate(Audere.World.WorldGameplayMode.Combat);
            var step = All<CombatStep>(scene).Single();
            var combat = step.CombatController;
            Assert.IsTrue(step.Play());
            // The authored four-part opening cue exceeds this test's 20-second wait.
            yield return Until(() => combat.CurrentState == CombatController.State.Playing, true);
            var previousAttempt = combat.EnemyRuntime;
            combat.BoardView.CatchCursor.anchoredPosition = new Vector2(35f, -20f);
            var deathPose = combat.BoardView.CapturePlayerHeartPose();
            Assert.IsTrue(deathPose.IsValid, "The world-space Heart needs a valid camera projection.");
            combat.DebugExpireTimer();
            Assert.AreEqual(deathPose.ViewportCenter, combat.LastDefeatHeartPose.ViewportCenter);
            var retry = GameplayUIRoot.Instance.CombatRetry;
            yield return Until(() => retry.IsReadyToRetry);
            var heart = (RectTransform)retry.transform.Find("Retry Panel/Retry Content/Broken Heart");
            Assert.That(Vector2.Distance(deathPose.ViewportCenter, heart.anchorMin), Is.LessThan(.0001f));
            Assert.IsFalse(combat.IsPlaying);
            Assert.AreEqual(Audere.GameplayInput.GameplayInputMode.Modal,
                GameplayUIRoot.Instance.InputGate.CurrentMode);
            var button = retry.GetComponentInChildren<UnityEngine.UI.Button>(true);
            button.onClick.Invoke();
            button.onClick.Invoke();
            yield return Until(() => combat.CurrentState == CombatController.State.Playing, true);
            Assert.AreNotSame(previousAttempt, combat.EnemyRuntime);
            Assert.AreEqual(0, combat.EnemyRuntime.PhaseIndex);
            Assert.AreEqual(19, combat.EnemyHealth);
            Assert.IsFalse(retry.IsShowing);
            Assert.IsFalse(combat.LastDefeatHeartPose.IsValid, "Retry must clear the previous death snapshot.");
            step.Cancel();
            yield return null;
            Assert.AreEqual(0, GameplayUIRoot.Instance.InputGate.ActiveClaimCount);
            LogAssert.NoUnexpectedReceived();
        }

        private static IEnumerator LifecycleChecks()
        {
            Services();var scene=SceneManager.GetActiveScene();var a=All<Transform>(scene).Single(x=>x.name=="Audere");var position=a.position;var rotation=a.rotation;
            var shadow=a.GetComponentsInChildren<SpriteRenderer>().Single(x=>x.sortingOrder==4).transform;var shadowPosition=shadow.position;var shadowScale=shadow.lossyScale;
            var fall=All<CharacterPoseStep>(scene).Single(x=>x.name=="050_AudereFalls");
            // EnterPlayMode can have a long first frame. Keep this cancellation probe mid-pose.
            typeof(CharacterPoseStep).GetField("duration",Private).SetValue(fall,8f);
            yield return null;yield return null;
            Assert.IsTrue(fall.Play());yield return new WaitForSecondsRealtime(.15f);
            Assert.Less(Vector3.Distance(shadowPosition,shadow.position),.0001f);Assert.Less(Vector3.Distance(shadowScale,shadow.lossyScale),.0001f);fall.Cancel();
            Assert.AreEqual(position,a.position);Assert.Less(Quaternion.Angle(rotation,a.rotation),.01f);
            foreach(var group in All<CanvasGroup>(scene).Where(x=>x.name=="Fade"&&x.transform.parent!=null&&x.transform.parent.name=="Scene Transition Overlay")){group.alpha=0;group.blocksRaycasts=false;}
            var mode=All<Audere.World.WorldModeController>(scene).Single();mode.ApplyModeImmediate(Audere.World.WorldGameplayMode.Combat);yield return null;
            var step=All<CombatStep>(scene).Single();var c=step.CombatController;Assert.IsTrue(step.Play());
            yield return Until(()=>c.CurrentState==CombatController.State.Playing,true);Assert.IsInstanceOf<OscillatingHandWallMove>(c.EnemyRuntime.CurrentMove);
            yield return Until(()=>c.EnemyRuntime.CurrentMove is ConvergingHandsMove,false,15);
            System.IO.Directory.CreateDirectory("Temp/Day4Crowd");
            yield return Capture("clasp-chase");step.Cancel();yield return null;
            Assert.IsFalse(c.BoardView.HasForcedPlayerControl);Assert.IsFalse(c.BoardView.HasForcedMovementProtection);Assert.AreEqual(0,GameplayUIRoot.Instance.InputGate.ActiveClaimCount);
            Assert.IsTrue(step.Play());yield return Until(()=>c.CurrentState==CombatController.State.Playing,true);c.DebugExpireTimer();
            yield return Until(()=>GameplayUIRoot.Instance.CombatRetry.IsReadyToRetry);
            var button=GameplayUIRoot.Instance.CombatRetry.GetComponentInChildren<UnityEngine.UI.Button>(true);button.onClick.Invoke();button.onClick.Invoke();yield return null;
            Assert.AreEqual(19,c.EnemyHealth);Assert.Greater(c.PlayerTime,c.ActiveMaximumTime-2f);Assert.AreEqual(0,c.EnemyRuntime.PhaseIndex);
            // Retry replays the opening speech before spawning dice; finish that pause first.
            yield return Until(()=>c.CurrentState==CombatController.State.Playing,true);
            // Exercise the real cursor-overlap/catch path, without replacing symbols or applying debug damage.
            int caught=0, currentBatch=-1, attacks=0, firstBatch=c.BatchIndex;
            double catchDeadline=EditorApplication.timeSinceStartup+18;
            while(caught<9&&EditorApplication.timeSinceStartup<catchDeadline&&c.IsPlaying)
            {
                if(c.BatchIndex!=currentBatch){currentBatch=c.BatchIndex;attacks=0;}
                var dice=(System.Collections.Generic.List<CombatDieView>)typeof(CombatController).GetField("activeDice",Private).GetValue(c);
                Assert.LessOrEqual(dice.Count,3);
                var die=dice.FirstOrDefault(x=>x!=null&&x.CanInteract);
                if(die!=null&&!c.BoardView.HasForcedPlayerControl)
                {
                    var symbol=die.Symbol;c.BoardView.CatchCursor.position=die.RectTransform.position;
                    int count=dice.Count;typeof(CombatController).GetMethod("TryCatchUnderCursor",Private).Invoke(c,null);
                    if(dice.Count<count){caught++;if(symbol==CombatSymbol.Attack)attacks++;Assert.LessOrEqual(attacks,2);}
                }
                yield return new WaitForSecondsRealtime(.12f);
            }
            Assert.GreaterOrEqual(caught,9);Assert.Greater(c.BatchIndex,firstBatch);
            step.Cancel();yield return null;Assert.AreEqual(0,GameplayUIRoot.Instance.InputGate.ActiveClaimCount);Assert.IsFalse(GameplayUIRoot.Instance.CombatRetry.IsShowing);
            Assert.IsFalse(c.BoardView.GetComponentsInChildren<CombatBulletView>().Any());LogAssert.NoUnexpectedReceived();
        }
        [Test]
        public void HandWaves_AlternateWallsLeaveCorridorPauseAndReturnAllLeases()
        {
            var board=Object.Instantiate(AssetDatabase.LoadAssetAtPath<CombatBoardView>("Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab"));
            try
            {
                board.gameObject.SetActive(true);Canvas.ForceUpdateCanvases();board.CatchCursor.anchoredPosition=Vector2.zero;
                var move=AssetDatabase.LoadAssetAtPath<OscillatingHandWallMove>(Day4CrowdSetupTool.Folder+"/Move_HandWaves.asset");
                var execution=move.CreateExecution(new CombatMoveExecutionContext(board,null,new SystemCombatRandom(52),211,1));
                execution.Tick(.01f);
                var hands=board.GetComponentsInChildren<CombatBulletView>();Assert.AreEqual(16,hands.Length);
                Assert.IsTrue(hands.All(x=>!x.CollisionActive));
                for(int i=0;i<95;i++){execution.Tick(.02f);board.TickBullets(.02f,1f);}
                Assert.AreEqual(16,board.GetComponentsInChildren<CombatBulletView>().Length);
                var ys=hands.Select(x=>x.RectTransform.anchoredPosition.y).ToArray();
                Assert.Greater(ys.Take(8).Max()-ys.Take(8).Min(),board.PlayArea.rect.height*.1f);
                for(int i=0;i<8;i++)Assert.Greater(ys[i+8]-ys[i],board.PlayArea.rect.height*.4f);
                execution.Tick(0f);board.TickBullets(0f,1f);CollectionAssert.AreEqual(ys,hands.Select(x=>x.RectTransform.anchoredPosition.y).ToArray());
                execution.Cancel();execution.Cancel();Assert.IsFalse(board.GetComponentsInChildren<CombatBulletView>().Any());
            }
            finally { Object.DestroyImmediate(board.gameObject); }
        }

        [Test]
        public void Clasp_DodgedHandsFadeWithoutHoldingOrStabbing()
        {
            var board=Object.Instantiate(AssetDatabase.LoadAssetAtPath<CombatBoardView>("Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab"));
            try
            {
                board.gameObject.SetActive(true);Canvas.ForceUpdateCanvases();board.CatchCursor.anchoredPosition=Vector2.zero;
                var move=AssetDatabase.LoadAssetAtPath<ConvergingHandsMove>(Day4CrowdSetupTool.Folder+"/Move_ClaspAndStab.asset");
                var execution=move.CreateExecution(new CombatMoveExecutionContext(board,null,new SystemCombatRandom(18),211,1));
                board.GetComponentInChildren<CombatPlayerView>().ResetPlayer();
                Rect area=board.PlayArea.rect;
                Vector2[] corners={
                    new Vector2(area.xMin+35f,area.yMin+35f),
                    new Vector2(area.xMax-35f,area.yMin+35f),
                    new Vector2(area.xMax-35f,area.yMax-35f),
                    new Vector2(area.xMin+35f,area.yMax-35f)};
                int peakHands=0,totalHits=0;
                for(int frame=0;frame<Mathf.CeilToInt(move.Duration/.02f)+2&&!execution.IsComplete;frame++)
                {
                    var live=board.GetComponentsInChildren<CombatBulletView>();
                    if(live.Length>0)
                        board.CatchCursor.anchoredPosition=corners.OrderByDescending(
                            point=>Vector2.Distance(point,live[0].RectTransform.anchoredPosition)).First();
                    execution.Tick(.02f);
                    totalHits+=board.TickBullets(.02f,.65f);
                    peakHands=Mathf.Max(peakHands,board.GetComponentsInChildren<CombatBulletView>().Length);
                    Assert.IsFalse(board.HasForcedPlayerControl,"A dodge must keep mouse control.");
                }
                Assert.IsTrue(execution.IsComplete);
                Assert.AreEqual(1,peakHands,"Pursuers should arrive one after another.");
                Assert.AreEqual(0,totalHits);
                Assert.IsFalse(board.GetComponentsInChildren<CombatBulletView>().Any());
            }
            finally { Object.DestroyImmediate(board.gameObject); }
        }

        [Test]
        public void Clasp_CaptureOnlyOnContactThenStabsAndReleases()
        {
            var board=Object.Instantiate(AssetDatabase.LoadAssetAtPath<CombatBoardView>("Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab"));
            try
            {
                board.gameObject.SetActive(true);Canvas.ForceUpdateCanvases();board.CatchCursor.anchoredPosition=Vector2.zero;
                var move=AssetDatabase.LoadAssetAtPath<ConvergingHandsMove>(Day4CrowdSetupTool.Folder+"/Move_ClaspAndStab.asset");
                var execution=move.CreateExecution(new CombatMoveExecutionContext(board,null,new SystemCombatRandom(18),211,1));
                board.GetComponentInChildren<CombatPlayerView>().ResetPlayer();
                bool captured=false,sawStabs=false;int hits=0;
                for(int frame=0;frame<Mathf.CeilToInt(move.Duration/.02f)+2&&!execution.IsComplete;frame++)
                {
                    execution.Tick(.02f);
                    captured|=board.HasForcedPlayerControl;
                    sawStabs|=captured&&board.GetComponentsInChildren<CombatBulletView>().Length>1;
                    hits+=board.TickBullets(.02f,.65f);
                }
                Assert.IsTrue(captured,"An unmoving Heart should be caught by a pursuer.");
                Assert.IsTrue(sawStabs,"Stabs should spawn only after a catch.");
                Assert.AreEqual(1,hits,"One caught sequence should apply one TIME hit.");
                Assert.IsTrue(execution.IsComplete);
                Assert.IsFalse(board.HasForcedPlayerControl);
                Assert.IsFalse(board.GetComponentsInChildren<CombatBulletView>().Any());
            }
            finally { Object.DestroyImmediate(board.gameObject); }
        }

        [TestCase(.2f)] [TestCase(.8f)] [TestCase(1.8f)]
        public void Clasp_CancelReturnsAllHandsAndReleasesControl(float until)
        {
            var board=Object.Instantiate(AssetDatabase.LoadAssetAtPath<CombatBoardView>("Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab"));
            try
            {
                board.gameObject.SetActive(true);Canvas.ForceUpdateCanvases();board.CatchCursor.anchoredPosition=Vector2.zero;
                var move=AssetDatabase.LoadAssetAtPath<ConvergingHandsMove>(Day4CrowdSetupTool.Folder+"/Move_ClaspAndStab.asset");
                var execution=move.CreateExecution(new CombatMoveExecutionContext(board,null,new SystemCombatRandom(18),211,1));
                for(float time=0;time<until;time+=.02f){execution.Tick(.02f);board.TickBullets(.02f,.65f);}
                execution.Cancel();execution.Cancel();board.ClearCombatRuntime();
                Assert.IsFalse(board.HasForcedPlayerControl);
                Assert.IsFalse(board.HasForcedMovementProtection);
                Assert.IsFalse(board.GetComponentsInChildren<CombatBulletView>().Any());
            }
            finally { Object.DestroyImmediate(board.gameObject); }
        }

        [UnityTest]
        public IEnumerator NewHandsAndBackground_VisiblePauseAndShutdown()
        {
            var scene=EditorSceneManager.OpenScene(Day4CrowdSetupTool.ScenePath);
            var so=new SerializedObject(All<StoryDirector>(scene).Single());so.FindProperty("playOnStart").boolValue=false;so.ApplyModifiedPropertiesWithoutUndo();
            yield return new EnterPlayMode();yield return null;Services();scene=SceneManager.GetActiveScene();
            System.IO.Directory.CreateDirectory("Temp/Day4CrowdPolish");
            foreach(var g in All<CanvasGroup>(scene).Where(x=>x.name=="Cover"||x.name=="Fade")){g.alpha=0;g.blocksRaycasts=false;}
            All<Audere.World.WorldModeController>(scene).Single().ApplyModeImmediate(Audere.World.WorldGameplayMode.Combat);
            var board=All<CombatBoardView>(scene).Single();var field=All<DriftingSpriteField>(scene).Single();
            field.Initialize(new CombatEnemyMechanicContext(board,211));field.OnPhaseEnter(null,0);
            Assert.IsTrue(field.IsPresenting);yield return new WaitForSecondsRealtime(.5f);float clock=field.MotionTime;
            field.SetPaused(true);yield return new WaitForSecondsRealtime(.2f);Assert.AreEqual(clock,field.MotionTime);field.SetPaused(false);
            foreach(var name in new[]{"Move_HandWaves","Move_ClaspAndStab"})
            {
                var move=AssetDatabase.LoadAssetAtPath<CombatMoveDefinition>(Day4CrowdSetupTool.Folder+"/"+name+".asset");
                var execution=move.CreateExecution(new CombatMoveExecutionContext(board,null,new SystemCombatRandom(52),211,1));
                float elapsed=0;bool captured=false;
                while(elapsed<4.7f)
                {
                    float dt=Time.unscaledDeltaTime;execution.Tick(dt);board.TickBullets(dt,1f);elapsed+=dt;
                    if(!captured&&elapsed>2.1f){ScreenCapture.CaptureScreenshot("Temp/Day4CrowdPolish/"+name+".png");captured=true;}
                    yield return null;
                }
                execution.Cancel();board.ClearCombatRuntime();Assert.IsFalse(board.HasForcedPlayerControl);
            }
            field.Shutdown();yield return null;Assert.IsFalse(field.IsPresenting);
            Assert.IsFalse(board.GetComponentsInChildren<CombatBulletView>().Any());
            LogAssert.NoUnexpectedReceived();yield return new ExitPlayMode();
        }

        [UnityTest]
        public IEnumerator MountDive_ProductionVisualsBoundsAndDisableCleanup()
        {
            var scene=EditorSceneManager.OpenScene(Day4CrowdSetupTool.ScenePath);
            var so=new SerializedObject(All<StoryDirector>(scene).Single());so.FindProperty("playOnStart").boolValue=false;so.ApplyModifiedPropertiesWithoutUndo();
            yield return new EnterPlayMode();yield return null;Services();scene=SceneManager.GetActiveScene();
            System.IO.Directory.CreateDirectory("Temp/CrowdPhase2QA");
            foreach(var g in All<CanvasGroup>(scene).Where(x=>x.name=="Cover"||x.name=="Fade")){g.alpha=0;g.blocksRaycasts=false;}
            All<Audere.World.WorldModeController>(scene).Single().ApplyModeImmediate(Audere.World.WorldGameplayMode.Combat);
            var board=All<CombatBoardView>(scene).Single();
            var enemy=AssetDatabase.LoadAssetAtPath<CombatEnemyDefinition>(Day4CrowdSetupTool.Folder+"/Enemy_Crowd.asset");
            board.PrepareEncounter(enemy.DisplayName);board.SetEncounterPresentationVisible(true);board.SetCursorVisible(true);
            var actor=board.SpawnEnemyActor(enemy.ActorPrefab,921);actor.Initialize(new CombatEnemyMechanicContext(board,921));
            board.GetComponentInChildren<CombatPlayerView>(true).ResetPlayer();board.CatchCursor.anchoredPosition=new Vector2(-280,0);
            Transform mount=actor.transform.parent;Vector3 home=mount.localPosition,scale=actor.VisualRoot.localScale;
            var move=enemy.GetPhase(1).DamageReactionMove;
            var execution=move.CreateExecution(new CombatMoveExecutionContext(board,actor,new SystemCombatRandom(52),921,2));
            float clock=0;
            foreach(var at in new[]{.38f,.74f,.98f,1.3f,1.95f})
            {
                while(clock<at){float dt=Mathf.Min(.01f,at-clock);execution.Tick(dt);board.TickBullets(dt,.65f);clock+=dt;}
                Canvas.ForceUpdateCanvases();yield return null;
                ScreenCapture.CaptureScreenshot("Temp/CrowdPhase2QA/dive-"+at.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)+".png");
                yield return new WaitForSecondsRealtime(.12f);
                Assert.AreEqual(scale,actor.VisualRoot.localScale);
                if(at==.38f)Assert.IsTrue(board.IsAttackWarningVisible);
                if(at==.74f)Assert.IsFalse(board.IsAttackWarningVisible);
                if(at==.98f)
                {
                    var camera=All<Audere.World.WorldModeController>(scene).Single();
                    var cam=(Camera)new SerializedObject(camera).FindProperty("worldCamera").objectReferenceValue;
                    foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(960,720),new Vector2Int(1680,720)})
                        CaptureCamera(cam,size,"Temp/CrowdPhase2QA/split-"+size.x+"x"+size.y+".png",board);
                }
            }
            Assert.IsFalse(board.IsMountDiveActive);Assert.AreEqual(0,board.ActiveMountEchoes);Assert.Less(Vector3.Distance(home,mount.localPosition),.001f);
            execution=move.CreateExecution(new CombatMoveExecutionContext(board,actor,new SystemCombatRandom(52),921,2));
            execution.Tick(.75f);Assert.IsTrue(board.IsMountDiveActive);board.gameObject.SetActive(false);yield return null;
            Assert.IsFalse(board.IsMountDiveActive);Assert.AreEqual(0,board.BoardSeparation);Assert.AreEqual(0,board.ActiveMountEchoes);
            Assert.Less(Vector3.Distance(home,mount.localPosition),.001f);execution.Cancel();board.gameObject.SetActive(true);
            board.PrepareEncounter(enemy.DisplayName);board.SetEncounterPresentationVisible(true);board.SetCursorVisible(true);
            actor=board.SpawnEnemyActor(enemy.ActorPrefab,922);
            foreach(var entry in enemy.GetPhase(1).MoveSet.Entries)
            {
                var basic=entry.Move.CreateExecution(new CombatMoveExecutionContext(board,actor,new SystemCombatRandom(52),922,2));
                for(int i=0;i<160;i++){basic.Tick(.01f);board.TickBullets(.01f,.65f);}
                yield return null;ScreenCapture.CaptureScreenshot("Temp/CrowdPhase2QA/"+entry.Move.name+".png");yield return new WaitForSecondsRealtime(.12f);
                basic.Cancel();Assert.IsEmpty(board.GetComponentsInChildren<CombatBulletView>());
            }
            var clasp=AssetDatabase.LoadAssetAtPath<ConvergingHandsMove>(Day4CrowdSetupTool.Folder+"/Move_ClaspAndStab.asset");
            var reach=clasp.CreateExecution(new CombatMoveExecutionContext(board,actor,new SystemCombatRandom(52),922,2));
            reach.Tick(.01f);reach.Tick(.2f);Assert.IsTrue(board.IsAttackWarningVisible);
            yield return null;ScreenCapture.CaptureScreenshot("Temp/CrowdPhase2QA/hand-warning.png");yield return new WaitForSecondsRealtime(.12f);
            reach.Cancel();Assert.IsFalse(board.IsAttackWarningVisible);
            board.ClearCombatRuntime();LogAssert.NoUnexpectedReceived();yield return new ExitPlayMode();
        }

        private static void CaptureCamera(Camera cam,Vector2Int size,string path,CombatBoardView board)
        {
            var previous=cam.targetTexture;float aspect=cam.aspect;var active=RenderTexture.active;
            var rt=RenderTexture.GetTemporary(size.x,size.y,24);
            var texture=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);
            try
            {
                cam.targetTexture=rt;cam.aspect=size.x/(float)size.y;Canvas.ForceUpdateCanvases();cam.Render();RenderTexture.active=rt;
                texture.ReadPixels(new Rect(0,0,size.x,size.y),0,0);texture.Apply();System.IO.File.WriteAllBytes(path,texture.EncodeToPNG());
                Rect area=board.PlayArea.rect;
                foreach(var p in new[]{new Vector2(area.xMin-board.BoardSeparation,area.yMin),new Vector2(area.xMax+board.BoardSeparation,area.yMax)})
                {Vector3 v=cam.WorldToViewportPoint(board.PlayArea.TransformPoint(p));Assert.That(v.x,Is.InRange(0f,1f));Assert.That(v.y,Is.InRange(0f,1f));}
            }
            finally{cam.targetTexture=previous;cam.aspect=aspect;RenderTexture.active=active;Object.Destroy(texture);RenderTexture.ReleaseTemporary(rt);}
        }

        private static void AssertNoCornerPull(CombatMoveDefinition move)
        {
            if(move is GraspingHandsMove hand)Assert.IsFalse(hand.PullToCorners,"Corner pull is retired from Crowd: "+move.name);
            if(move is CompositeCombatMove composite)foreach(var child in composite.Children)AssertNoCornerPull(child);
        }
        private static void AssertFeet(Transform actor,Transform tile)
        {var r=actor.GetComponent<SpriteRenderer>();var feet=actor.TransformPoint(new Vector3(r.sprite.bounds.center.x,r.sprite.bounds.min.y,0));Assert.Less(Vector2.Distance(feet,tile.position),.001f);}
        private static IEnumerator Capture(string name){ScreenCapture.CaptureScreenshot("Temp/Day4Crowd/"+name+".png");yield return new WaitForSecondsRealtime(.2f);}
        private static void Services()
        {
            Application.runInBackground=true;EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
            if(SceneFlow.Instance!=null)return;var go=new GameObject("TEST Crowd Services");Object.DontDestroyOnLoad(go);go.AddComponent<SceneFlow>().Initialize();
            var audio=go.AddComponent<AudioService>();typeof(AudioService).GetField("catalog",Private).SetValue(audio,AssetDatabase.LoadAssetAtPath<AudioCatalog>("Assets/_Audere/Data/Audio/AudioCatalog.asset"));audio.Initialize();
        }
        private static IEnumerator Until(Func<bool> condition,bool advance=false,float timeout=20f)
        {
            double deadline=EditorApplication.timeSinceStartup+timeout;
            while(!condition()&&EditorApplication.timeSinceStartup<deadline)
            {
                var d=GameplayUIRoot.Instance==null?null:GameplayUIRoot.Instance.Dialogue;
                if(advance&&d!=null&&d.IsPlaying)typeof(DialogueController).GetMethod("EndPlayback",Private).Invoke(d,new object[]{DialogueResult.Completed,true});
                EditorApplication.QueuePlayerLoopUpdate();yield return null;
            }
            Assert.IsTrue(condition(),"Timed out in "+SceneManager.GetActiveScene().name);
        }
        private static T[] All<T>(Scene s)where T:Component=>s.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<T>(true)).ToArray();
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if(EditorApplication.isPlaying)yield return new ExitPlayMode();
            var scene=EditorSceneManager.OpenScene(Day4CrowdSetupTool.ScenePath);
            var director=All<StoryDirector>(scene).Single();
            var serialized=new SerializedObject(director);
            serialized.FindProperty("playOnStart").boolValue=true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
        }
    }
}
#endif



