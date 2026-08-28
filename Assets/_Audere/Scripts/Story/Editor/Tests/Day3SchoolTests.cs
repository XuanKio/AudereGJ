#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Audere.Combat;
using Audere.Core;
using Audere.Dialogue;
using Audere.EditorTools;
using Audere.Story.Presentation;
using Audere.Story.Steps;
using Audere.World;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Audere.Story.Editor.Tests
{
    public sealed class Day3SchoolTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        [Test]
        public void Authoring_DirectBindingsDialogueAndTwelveSharedHealth()
        {
            foreach (string path in new[] { Day3SchoolSetupTool.HomePath, Day3SchoolSetupTool.BoardPath, Day3SchoolSetupTool.TeacherPath })
            {
                var scene = EditorSceneManager.OpenScene(path);
                foreach (var t in All<Transform>(scene)) Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
                foreach (var e in All<StoryEvent>(scene)) foreach (Transform child in e.transform)
                    Assert.AreEqual(1, child.GetComponents<StoryStep>().Length, child.name);
                foreach (var d in All<DialogueStep>(scene))
                {
                    Assert.AreEqual(DialogueCharacterId.Audere, d.DialogueData.LeftCharacter);
                    foreach (var line in d.DialogueData.Lines) Assert.LessOrEqual(line.Text.Length, 42, line.Text);
                }
                foreach (var hop in All<CharacterMotionStep>(scene)) Assert.IsNotNull(hop.GroundedShadow);
                Assert.IsTrue(EditorBuildSettings.scenes.Any(s => s.path == path && s.enabled));
                Assert.AreEqual(1, All<Camera>(scene).Length);
            }
            var data = AssetDatabase.LoadAssetAtPath<CombatEncounterData>(Day3SchoolSetupTool.EncounterPath);
            Assert.AreEqual(120f, data.EncounterDuration);
            Assert.AreEqual(3, data.DicePerBatch); Assert.AreEqual(2, data.MaximumAttacksPerBatch);
            Assert.IsFalse(data.HasTutorial);
            Assert.IsTrue(data.EnemyDefinition.Validate(out var error), error);
            Assert.AreEqual(CombatPhasePolicy.SharedHealthThresholds, data.EnemyDefinition.PhasePolicy);
            Assert.AreEqual(12, data.EnemyDefinition.SharedMaxHealth);
            CollectionAssert.AreEqual(new[] { 8, 4, 0 }, Enumerable.Range(0,3).Select(i => data.EnemyDefinition.GetPhase(i).SharedExitThreshold));
            Assert.IsEmpty(ShaderUtil.GetShaderMessages(Shader.Find("Audere/UI/Chalk")));
            var prefab = AssetDatabase.LoadAssetAtPath<ChalkDrawingView>(Day3SchoolSetupTool.DrawingPath);
            Assert.IsNotNull(prefab.Surface.GetComponent<CanvasRenderer>());
            Assert.IsNotNull(prefab.Surface.GetComponentInParent<UnityEngine.UI.RectMask2D>(true));
        }

        [Test]
        public void TeacherProjectiles_OrdinaryStreamUsesSharedBullet_ChalkOnlyForSpecials()
        {
            const string moves = "Assets/_Audere/Data/Combat/Teacher/Moves/";
            var ordinary = AssetDatabase.LoadAssetAtPath<CombatBulletView>(
                "Assets/_Audere/Prefabs/Combat/Bullets/EnemyBullet.prefab");
            var chalk = AssetDatabase.LoadAssetAtPath<CombatBulletView>(
                "Assets/_Audere/Prefabs/Combat/Bullets/Bullet_ChalkRod.prefab");
            Assert.IsNotNull(ordinary); Assert.IsNotNull(chalk);
            Assert.AreNotSame(ordinary, chalk);
            Assert.AreEqual("Assets/_Audere/AssetGame/Item/dan.aseprite",
                AssetDatabase.GetAssetPath(ordinary.GetComponent<UnityEngine.UI.Image>().sprite));
            Assert.AreEqual("Assets/_Audere/AssetGame/Item/phan.aseprite",
                AssetDatabase.GetAssetPath(chalk.GetComponent<UnityEngine.UI.Image>().sprite));
            foreach (string name in new[] { "Move_ChalkSineStream", "Move_TeacherLaserColumns", "Move_ChalkFence", "Move_ChalkSweep" })
            {
                var move = AssetDatabase.LoadAssetAtPath<CombatMoveDefinition>(moves + name + ".asset");
                Assert.IsNotNull(move, name);
                Assert.IsTrue(move.Validate(out string error), error);
                var expected = name == "Move_ChalkFence" || name == "Move_ChalkSweep" ? chalk : ordinary;
                Assert.AreSame(expected, new SerializedObject(move).FindProperty("projectilePrefab").objectReferenceValue, name);
            }
        }

        [Test]
        public void TeacherResistance_OneUninterruptedAutoSequencePerHealthMilestone()
        {
            var encounter = AssetDatabase.LoadAssetAtPath<CombatEncounterData>(Day3SchoolSetupTool.EncounterPath);
            var enemy = encounter.EnemyDefinition;
            Assert.IsTrue(enemy.Validate(out string error), error);
            Assert.AreEqual(120f, encounter.EncounterDuration);
            Assert.AreEqual(12, enemy.SharedMaxHealth);
            for (int i = 0; i < 3; i++)
            {
                var cues = enemy.GetPhase(i).DialogueCues;
                Assert.AreEqual(1, cues.Count);
                var cue = cues[0];
                Assert.AreEqual(CombatDialogueCueTrigger.PhaseEnter, cue.Trigger);
                Assert.AreEqual(CombatDialoguePresentation.AutoCombatDialogue, cue.Presentation);
                Assert.IsFalse(cue.InterruptsAutoDialogue);
                Assert.IsFalse(cue.RepeatOnTrigger);
                Assert.IsFalse(cue.PausesCombatForPresentation);
                Assert.AreEqual(i < 2, cue.RequiredBeforePhaseAdvance);
                Assert.AreEqual(i == 2, cue.RequiredBeforeVictory);
                Assert.AreEqual(3, cue.Sequence.Count);
                Assert.AreEqual(DialogueCharacterId.Timor, cue.Sequence[0].RightCharacter);
                Assert.AreEqual(DialogueCharacterId.Teacher, cue.Sequence[1].RightCharacter);
                Assert.AreEqual(DialogueCharacterId.Timor, cue.Sequence[2].RightCharacter);
                foreach (var data in cue.Sequence)
                {
                    Assert.AreEqual(DialogueCharacterId.Audere, data.LeftCharacter);
                    Assert.IsNotNull(data.LeftPortraitOverride);
                    Assert.IsNotNull(data.RightPortraitOverride);
                    foreach (var line in data.Lines) Assert.LessOrEqual(line.Text.Length, 42, line.Text);
                }
                Assert.IsTrue(cue.Sequence[1].Lines.Any(l => l.PortraitOverride != null &&
                    l.PortraitOverride.name == "Co_giao_0" && l.GlitchPortraitTransition));
                Assert.AreEqual(DialogueSpeakerSide.Left, cue.Sequence[2].Lines.Last().Speaker);
            }
            var last = enemy.GetPhase(2).DialogueCues[0].Sequence[2];
            Assert.AreEqual("Để tớ tự trả lời.", last.Lines.Last().Text);
            Assert.IsTrue(last.Lines.Any(l => l.PortraitOverride != null && l.PortraitOverride.name == "Audere_Tired_0"));
        }

        [Test]
        public void RerunAuthor_PreservesAllDayThreeAndPreviousScenes()
        {
            var paths = new[] { Day2SchoolMorningSetupTool.ScenePath, Day2NightDreamSetupTool.WakePath,
                Day3SchoolSetupTool.HomePath, Day3SchoolSetupTool.BoardPath, Day3SchoolSetupTool.TeacherPath };
            var before = paths.Select(System.IO.File.ReadAllText).ToArray();
            Day3SchoolSetupTool.Author();
            CollectionAssert.AreEqual(before, paths.Select(System.IO.File.ReadAllText).ToArray());
        }

        [Test]
        public void RotatedChalk_HitsOnlyItsOrientedRectangle()
        {
            var chalk = new GameObject("Chalk", typeof(RectTransform)).GetComponent<RectTransform>();
            var heart = new GameObject("Heart", typeof(RectTransform)).GetComponent<RectTransform>();
            try
            {
                chalk.sizeDelta = new Vector2(120, 12); chalk.localRotation = Quaternion.Euler(0,0,45);
                heart.sizeDelta = new Vector2(8,8); heart.position = new Vector3(35,-35,0);
                Assert.IsFalse(CombatRectCollision.Overlaps(chalk,heart));
                heart.position = new Vector3(25,25,0); Assert.IsTrue(CombatRectCollision.Overlaps(chalk,heart));
            }
            finally { Object.DestroyImmediate(chalk.gameObject); Object.DestroyImmediate(heart.gameObject); }
        }

        [Test]
        public void ChalkMotion_TelegraphPauseCancelAndPoolReset()
        {
            var go = new GameObject("Chalk test", typeof(RectTransform), typeof(CombatBulletView));
            try
            {
                var b = go.GetComponent<CombatBulletView>(); var bounds = new Rect(-400,-200,800,400);
                b.Setup(null, Vector2.zero, Vector2.zero, 20, 2, .5f);
                b.ConfigurePathMotion(new ParametricProjectileMotion(2f,t=>new Vector2(t*100,0),t=>t*180));
                b.TickMovement(bounds,.5f); Assert.AreEqual(Vector2.zero,b.RectTransform.anchoredPosition);
                b.TickMovement(bounds,.5f); Assert.AreEqual(25f,b.RectTransform.anchoredPosition.x,.001f);
                var p=b.RectTransform.anchoredPosition;b.TickMovement(bounds,0);Assert.AreEqual(p,b.RectTransform.anchoredPosition);
                b.ReturnToPool();Assert.IsFalse(b.CollisionActive);
                b.Setup(null,Vector2.zero,Vector2.right*10,21,1);
                b.TickMovement(bounds,.1f);Assert.AreEqual(new Vector2(1,0),b.RectTransform.anchoredPosition);
                Assert.AreEqual(Quaternion.identity,b.RectTransform.localRotation);
                Assert.AreEqual(0f,ChalkFenceMove.Reach(0),.00001f);
                Assert.AreEqual(0f,ChalkFenceMove.Reach(1),.00001f);
                Assert.AreEqual(1f,ChalkFenceMove.Reach(.5f),.00001f);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [UnityTest]
        public IEnumerator HomeWalkDrawingDizzinessTeacherCombat_ProductionFlow()
        {
            EditorSceneManager.OpenScene(Day3SchoolSetupTool.HomePath);
            yield return new EnterPlayMode();
            yield return Production();
            yield return new ExitPlayMode();
        }

        private static IEnumerator Production()
        {
            Application.runInBackground = true;
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
            var flow = new GameObject("TEST SceneFlow").AddComponent<SceneFlow>(); flow.Initialize(); Object.DontDestroyOnLoad(flow.gameObject);
            var audio = flow.gameObject.AddComponent<Audere.Audio.AudioService>();
            typeof(Audere.Audio.AudioService).GetField("catalog",Private).SetValue(audio,
                AssetDatabase.LoadAssetAtPath<Audere.Audio.AudioCatalog>("Assets/_Audere/Data/Audio/AudioCatalog.asset"));
            audio.Initialize();
            foreach (var name in new[] {GameScenes.Day3SchoolBoard,GameScenes.Day3SchoolTeacher})
            {LogAssert.Expect(LogType.Log,"[SceneFlow] Loading '"+name+"'...");LogAssert.Expect(LogType.Log,"[SceneFlow] Loaded '"+name+"'.");}
            System.IO.Directory.CreateDirectory("Temp/Day3QA");
            yield return Until(()=>GameplayUIRoot.Instance != null && GameplayUIRoot.Instance.Dialogue.IsPlaying,false);
            yield return new WaitForSecondsRealtime(.6f);ScreenCapture.CaptureScreenshot("Temp/Day3QA/home.png");
            var opening=All<StoryEvent>(SceneManager.GetActiveScene()).Single();
            yield return Until(()=>opening.CurrentStep!=null&&opening.CurrentStep.name=="060_FadeToSchool",true,25);
            var sfx=(AudioSource)typeof(Audere.Audio.AudioService).GetField("sfxSource",Private).GetValue(audio);
            Assert.IsTrue(sfx.isPlaying,"School bell should play before the school scene load.");
            yield return Until(()=>SceneManager.GetActiveScene().name==GameScenes.Day3SchoolBoard,true,25);
            var scene=SceneManager.GetActiveScene();var e=All<StoryEvent>(scene).Single(x=>x.EventId=="D3_SCHOOL_DECORATE_BOARD");
            var actor=All<SpriteRenderer>(scene).Single(x=>x.gameObject.name=="Audere");float startX=actor.transform.position.x;
            yield return Until(()=>e.CurrentStep is ChalkDrawingStep,true,40);
            Assert.AreEqual(startX+1.25f,actor.transform.position.x,.001f);
            var view=All<ChalkDrawingView>(scene).Single();Assert.IsTrue(view.IsShowing);
            Assert.AreEqual(1,GameplayUIRoot.Instance.InputGate.ActiveClaimCount);
            Assert.IsFalse(view.CompleteButton.interactable);
            var surface=view.Surface;Canvas.ForceUpdateCanvases();
            var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,pointerId=-1};
            pointer.position=RectTransformUtility.WorldToScreenPoint(null,surface.rectTransform.TransformPoint(new Vector3(-200,-50,0)));
            var hits=new System.Collections.Generic.List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            Assert.IsTrue(hits.Any(h=>h.gameObject==surface.gameObject),"Blank drawing surface must receive the very first pointer hit.");
            surface.OnPointerDown(pointer);
            for(int i=0;i<70;i++)
            {pointer.position=RectTransformUtility.WorldToScreenPoint(null,surface.rectTransform.TransformPoint(new Vector3(-200+i*6,Mathf.Sin(i*.14f)*100,0)));surface.OnDrag(pointer);}
            surface.OnPointerUp(pointer);Assert.Greater(surface.SegmentCount,50);
            Assert.IsTrue(view.CompleteButton.interactable);
            yield return new WaitForSecondsRealtime(2f);ScreenCapture.CaptureScreenshot("Temp/Day3QA/chalk-drawing.png");yield return null;
            view.CompleteButton.onClick.Invoke();view.CompleteButton.onClick.Invoke();Assert.IsFalse(view.IsShowing);
            yield return Until(()=>e.CurrentStep!=null&&e.CurrentStep.name=="170_TheRoomDriftsWhileBiancaCalls",true);
            var fx=All<FullscreenTransitionController>(scene).Single();
            yield return new WaitForSecondsRealtime(1f);
            Assert.IsTrue(fx.IsTransitioning);Assert.IsTrue(GameplayUIRoot.Instance.Dialogue.IsPlaying);
            Assert.AreEqual(0,GameplayUIRoot.Instance.InputGate.ActiveClaimCount,"Auto dialogue must not capture input.");
            ScreenCapture.CaptureScreenshot("Temp/Day3QA/fatigue.png");
            yield return Until(()=>SceneManager.GetActiveScene().name==GameScenes.Day3SchoolTeacher,false,15);
            scene=SceneManager.GetActiveScene();e=All<StoryEvent>(scene).Single();
            yield return Until(()=>e.CurrentStep is CombatStep,true,30);
            var combat=((CombatStep)e.CurrentStep).CombatController;
            yield return Until(()=>combat.CurrentState==CombatController.State.Playing,false);
            Assert.AreEqual(12,combat.EnemyHealth);Assert.Greater(combat.PlayerTime,115);
            var board=combat.BoardView;
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.IsTrue(board.GetComponentsInChildren<CombatBulletView>().Any(b=>b.CollisionActive));
            Assert.AreEqual(3,board.GetComponentsInChildren<CombatDieView>().Length);
            Assert.IsFalse(All<FullscreenTransitionController>(scene).Single().RendererFeature.isActive);
            ScreenCapture.CaptureScreenshot("Temp/Day3QA/teacher-chalk-fences.png");
            for(int phase=0;phase<3;phase++)
            {
                yield return Until(()=>combat.CurrentState==CombatController.State.Playing,false);
                Assert.AreEqual(phase,combat.EnemyRuntime.PhaseIndex);
                if(phase==1)
                {
                    yield return Until(()=>board.HasVerticalPlayerControl,false,8);
                    ScreenCapture.CaptureScreenshot("Temp/Day3QA/teacher-vertical-pressure.png");
                }
                if(phase==2)
                {
                    yield return new WaitForSecondsRealtime(1.3f);
                    ScreenCapture.CaptureScreenshot("Temp/Day3QA/teacher-laser-stream.png");
                }
                for(int hit=0;hit<4;hit++){combat.DebugApplyDiceEffect(CombatSymbol.Attack);yield return null;}
                if(phase<2)yield return Until(()=>combat.EnemyRuntime.PhaseIndex==phase+1,false);
            }
            yield return Until(()=>!e.IsPlaying,false);
            Assert.IsFalse(combat.IsPlaying);Assert.AreEqual(0,board.GetComponentsInChildren<CombatBulletView>().Length);
            Assert.IsFalse(board.HasVerticalPlayerControl);Assert.AreEqual(0,GameplayUIRoot.Instance.InputGate.ActiveClaimCount);
            Assert.IsFalse(GameplayUIRoot.Instance.CombatRetry.IsShowing);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator DrawingCancelReplayAndTeacherRetry_ReleaseAllOwners()
        {
            EditorSceneManager.OpenScene(Day3SchoolSetupTool.TeacherPath);
            var director=All<StoryDirector>(SceneManager.GetActiveScene()).Single();
            var so=new SerializedObject(director);so.FindProperty("playOnStart").boolValue=false;so.ApplyModifiedPropertiesWithoutUndo();
            yield return new EnterPlayMode();
            yield return CleanupChecks();
            yield return new ExitPlayMode();
        }
        private static IEnumerator CleanupChecks()
        {
            var scene=SceneManager.GetActiveScene();var step=All<CombatStep>(scene).Single();var board=step.CombatController.BoardView;
            var view=Object.Instantiate(AssetDatabase.LoadAssetAtPath<ChalkDrawingView>(Day3SchoolSetupTool.DrawingPath));
            var owner=new GameObject("TEST drawing owner");int callback=0;
            Assert.IsTrue(view.Show(owner,()=>callback++));view.ForceHide();view.ForceHide();
            Assert.AreEqual(0,callback);Assert.AreEqual(0,GameplayUIRoot.Instance.InputGate.ActiveClaimCount);
            Assert.IsTrue(view.Show(owner,()=>callback++));Object.Destroy(owner);yield return Until(()=>!view.IsShowing);
            Assert.IsFalse(view.IsShowing);Assert.AreEqual(0,callback);Object.Destroy(view.gameObject);
            var mode=All<WorldModeController>(scene).Single();mode.ApplyModeImmediate(WorldGameplayMode.Combat);
            step.Play(_=>{});
            yield return Until(()=>step.CombatController.CurrentState==CombatController.State.Playing,false);
            var c=step.CombatController;var old=c.EnemyRuntime; c.DebugExpireTimer();
            yield return Until(()=>GameplayUIRoot.Instance.CombatRetry.IsShowing,false);
            Assert.IsFalse(c.IsPlaying);Assert.AreEqual(0,board.GetComponentsInChildren<CombatBulletView>().Length);
            var button=GameplayUIRoot.Instance.CombatRetry.GetComponentInChildren<UnityEngine.UI.Button>(true);
            button.onClick.Invoke();button.onClick.Invoke();
            yield return Until(()=>c.CurrentState==CombatController.State.Playing,false);
            Assert.AreNotSame(old,c.EnemyRuntime);Assert.AreEqual(0,c.EnemyRuntime.PhaseIndex);Assert.AreEqual(12,c.EnemyHealth);
            var impulse=AssetDatabase.LoadAssetAtPath<VerticalPlayerImpulseMove>("Assets/_Audere/Data/Combat/Teacher/Moves/Move_VerticalImpulse.asset");
            var move=impulse.CreateExecution(new CombatMoveExecutionContext(board,null,new SystemCombatRandom(3),c.EnemyRuntime.SessionVersion,c.EnemyRuntime.PhaseVersion));
            move.Tick(.4f);Assert.IsFalse(board.HasVerticalPlayerControl);move.Tick(.6f);Assert.IsTrue(board.HasVerticalPlayerControl);
            move.Cancel();move.Cancel();Assert.IsFalse(board.HasVerticalPlayerControl);
            step.Cancel();yield return null;
            Assert.AreEqual(0,GameplayUIRoot.Instance.InputGate.ActiveClaimCount);Assert.IsFalse(GameplayUIRoot.Instance.CombatRetry.IsShowing);
            Assert.AreEqual(0,board.GetComponentsInChildren<CombatBulletView>().Length);
            Assert.IsFalse(GameplayUIRoot.Instance.Dialogue.IsPlaying);LogAssert.NoUnexpectedReceived();
        }

        private static IEnumerator Until(Func<bool> ready,bool advance=false,float timeout=20)
        {
            double deadline=EditorApplication.timeSinceStartup+timeout;
            while(!ready()&&EditorApplication.timeSinceStartup<deadline)
            {
                var d=GameplayUIRoot.Instance!=null?GameplayUIRoot.Instance.Dialogue:null;
                if(advance&&d!=null&&d.IsPlaying)typeof(DialogueController).GetMethod("EndPlayback",Private).Invoke(d,new object[]{DialogueResult.Completed,true});
                EditorApplication.QueuePlayerLoopUpdate();yield return null;
            }
            Assert.IsTrue(ready(),"Timed out in "+SceneManager.GetActiveScene().name);
        }
        private static T[] All<T>(Scene scene)where T:Component=>scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>(true)).ToArray();
        [UnityTearDown]public IEnumerator Cleanup(){if(EditorApplication.isPlaying)yield return new ExitPlayMode();EditorSceneManager.OpenScene(Day3SchoolSetupTool.HomePath);}
    }
}
#endif
