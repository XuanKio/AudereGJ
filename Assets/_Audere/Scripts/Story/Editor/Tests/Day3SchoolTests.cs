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
        public void Authoring_DirectBindingsDialogueAndFifteenSharedHealth()
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
            Assert.AreEqual(90f, data.EncounterDuration);
            Assert.AreEqual(3, data.DicePerBatch); Assert.AreEqual(2, data.MaximumAttacksPerBatch);
            Assert.IsFalse(data.HasTutorial);
            Assert.IsTrue(data.EnemyDefinition.Validate(out var error), error);
            Assert.AreEqual(CombatPhasePolicy.SharedHealthThresholds, data.EnemyDefinition.PhasePolicy);
            Assert.AreEqual(15, data.EnemyDefinition.SharedMaxHealth);
            CollectionAssert.AreEqual(new[] { 7, 4, 0 }, Enumerable.Range(0,3).Select(i => data.EnemyDefinition.GetPhase(i).SharedExitThreshold));
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
            Assert.AreEqual(90f, encounter.EncounterDuration);
            Assert.AreEqual(15, enemy.SharedMaxHealth);
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
                Assert.AreEqual(i == 0 ? DialogueCharacterId.Teacher : DialogueCharacterId.TeacherDistorted,
                    cue.Sequence[1].RightCharacter);
                Assert.AreEqual(DialogueCharacterId.Timor, cue.Sequence[2].RightCharacter);
                foreach (var data in cue.Sequence)
                {
                    Assert.AreEqual(DialogueCharacterId.Audere, data.LeftCharacter);
                    Assert.IsNotNull(data.LeftPortraitOverride);
                    var catalog = AssetDatabase.LoadAssetAtPath<DialogueCharacterCatalog>(
                        "Assets/_Audere/Data/Dialogue/DialogueCharacterCatalog.asset");
                    Assert.IsTrue(catalog.TryGet(data.RightCharacter, out var partner));
                    Assert.IsNotNull(data.RightPortraitOverride != null ? data.RightPortraitOverride : partner.Portrait);
                    foreach (var line in data.Lines) Assert.LessOrEqual(line.Text.Length, 42, line.Text);
                }
                Assert.IsTrue(cue.Sequence[1].Lines.Any(l =>
                    l.CharacterOverride == DialogueCharacterId.Teacher && l.GlitchPortraitTransition));
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
            scene=SceneManager.GetActiveScene();e=All<StoryEvent>(scene).Single(x => x.EventId == "D3_TEACHER_CHECK_IN_PRESSURE");
            yield return Until(()=>e.CurrentStep is CombatStep,true,30);
            var combat=((CombatStep)e.CurrentStep).CombatController;
            yield return Until(()=>combat.CurrentState==CombatController.State.Playing,false);
            Assert.AreEqual(15,combat.EnemyHealth);Assert.Greater(combat.PlayerTime,85);
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
                    yield return Until(()=>board.ActiveStunTrailCount>0,false,8);
                    Assert.IsFalse(board.HasVerticalPlayerControl);
                    Assert.AreEqual(1f,board.BattleBoxWidthFraction);
                    ScreenCapture.CaptureScreenshot("Temp/Day3QA/teacher-radial-trails.png");
                }
                if(phase==2)
                {
                    yield return new WaitForSecondsRealtime(1.3f);
                    ScreenCapture.CaptureScreenshot("Temp/Day3QA/teacher-laser-stream.png");
                }
                for(int hit=0;hit<20 && combat.EnemyRuntime.AcceptsDamage;hit++){combat.DebugApplyDiceEffect(CombatSymbol.Attack);yield return null;}
                if(phase<2)yield return Until(()=>combat.EnemyRuntime.PhaseIndex==phase+1,false,35);
            }
            yield return DriveTeacherAftermath(e, 0);
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
            var scene=SceneManager.GetActiveScene();var step=All<CombatStep>(scene).Single(x => x.name == "090_PlayTeacherPressure");var board=step.CombatController.BoardView;
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
            Assert.AreNotSame(old,c.EnemyRuntime);Assert.AreEqual(0,c.EnemyRuntime.PhaseIndex);Assert.AreEqual(15,c.EnemyHealth);
            var impulse=AssetDatabase.LoadAssetAtPath<VerticalPlayerImpulseMove>("Assets/_Audere/Data/Combat/Teacher/Moves/Move_VerticalImpulse.asset");
            var move=impulse.CreateExecution(new CombatMoveExecutionContext(board,null,new SystemCombatRandom(3),c.EnemyRuntime.SessionVersion,c.EnemyRuntime.PhaseVersion));
            move.Tick(.4f);Assert.IsFalse(board.HasVerticalPlayerControl);move.Tick(.6f);Assert.IsTrue(board.HasVerticalPlayerControl);
            move.Cancel();move.Cancel();Assert.IsFalse(board.HasVerticalPlayerControl);
            step.Cancel();yield return null;
            Assert.AreEqual(0,GameplayUIRoot.Instance.InputGate.ActiveClaimCount);Assert.IsFalse(GameplayUIRoot.Instance.CombatRetry.IsShowing);
            Assert.AreEqual(0,board.GetComponentsInChildren<CombatBulletView>().Length);
            Assert.IsFalse(GameplayUIRoot.Instance.Dialogue.IsPlaying);LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void TeacherAfterCombat_ThreeRepliesNormalPortraitsAndAdjacentTiles()
        {
            var scene = EditorSceneManager.OpenScene(Day3SchoolSetupTool.TeacherPath);
            var choice = All<StoryChoiceBranchStep>(scene).Single(x => x.name == "140_AudereChoosesHerAnswer");
            Assert.AreEqual(3, choice.Options.Count);
            Assert.AreEqual(3, choice.Branches.Distinct().Count());
            foreach (var branch in choice.Branches)
                Assert.IsTrue(branch.GetComponentsInChildren<DialogueStep>().Single().DialogueData.HasLines);
            var data = AssetDatabase.LoadAssetAtPath<CombatEncounterData>(Day3SchoolSetupTool.EncounterPath);
            Assert.IsTrue(data.VictoryPresentation.IsConfigured);
            Assert.AreEqual("Co_giao_0", data.VictoryPresentation.Dialogue.RightPortraitOverride.name);
            Assert.AreEqual("Audere_Tired_0", data.VictoryPresentation.Dialogue.LeftPortraitOverride.name);
            Assert.AreEqual(3, data.VictoryPresentation.Dialogue.Lines.Count);
            var a = All<Transform>(scene).Single(x => x.name == "Start Tile");
            var t = All<Transform>(scene).Single(x => x.name == "Teacher Tile");
            Assert.AreEqual(a.lossyScale.x, t.position.x - a.position.x, .0001f);
            foreach (var motion in All<CharacterMotionStep>(scene))
            {
                Assert.IsNotNull(motion.TargetTransform);
                Assert.IsNotNull(motion.GroundedShadow);
                Assert.AreEqual(5, motion.ActorRenderer.sortingOrder);
                Assert.AreEqual(4, motion.GroundedShadow.GetComponent<SpriteRenderer>().sortingOrder);
            }
        }

        [UnityTest]
        public IEnumerator TeacherVictoryPresentation_HoldsActorAndCancelsThenCompletesOnce()
        {
            OpenTeacherWithoutStartup();
            yield return new EnterPlayMode();
            yield return null;
            yield return Until(() => GameplayUIRoot.Instance != null);
            yield return TeacherVictoryChecks();
            yield return new ExitPlayMode();
        }

        private static IEnumerator TeacherVictoryChecks()
        {
            var scene = SceneManager.GetActiveScene();
            All<WorldModeController>(scene).Single().ApplyModeImmediate(WorldGameplayMode.Combat);
            var step = All<CombatStep>(scene).Single(x => x.name == "090_PlayTeacherPressure"); var c = step.CombatController;
            var board = c.BoardView;
            for (int attempt = 0; attempt < 2; attempt++)
            {
                int callbacks = 0; CombatResult outcome = CombatResult.Special;
                Assert.IsTrue(c.Play(step.CombatEncounterData, r => { outcome = r; callbacks++; }));
                yield return Until(() => c.CurrentState == CombatController.State.Playing);
                typeof(CombatController).GetMethod("EndCombat", Private).Invoke(c, new object[] { CombatController.State.Victory });
                yield return Until(() => GameplayUIRoot.Instance.Dialogue.IsPlaying);
                Assert.AreEqual(CombatController.State.Victory, c.CurrentState);
                Assert.IsNotNull(board.ActiveEnemyActor);
                Assert.IsTrue(board.ActiveEnemyActor.gameObject.activeInHierarchy);
                Assert.IsEmpty(board.GetComponentsInChildren<CombatDieView>());
                Assert.IsEmpty(board.GetComponentsInChildren<CombatBulletView>());
                Assert.AreEqual(0, board.ActiveStunTrailCount);
                Assert.AreEqual(0, callbacks);
                float time = c.PlayerTime;
                yield return new WaitForSecondsRealtime(.15f);
                Assert.AreEqual(time, c.PlayerTime);
                if (attempt == 0) c.Cancel();
                else EndTestDialogue();
                yield return Until(() => !c.IsPlaying);
                Assert.AreEqual(1, callbacks);
                Assert.AreEqual(attempt == 0 ? CombatResult.Cancelled : CombatResult.Victory, outcome);
                Assert.IsNull(board.ActiveEnemyActor);
                Assert.IsFalse(GameplayUIRoot.Instance.Dialogue.IsPlaying);
                Assert.AreEqual(0, GameplayUIRoot.Instance.InputGate.ActiveClaimCount);
            }
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TeacherAfterCombat_AllRepliesMotionCancelAndReplay()
        {
            OpenTeacherWithoutStartup();
            yield return new EnterPlayMode();
            yield return null;
            yield return Until(() => GameplayUIRoot.Instance != null);
            yield return TeacherAftermathChecks();
            yield return new ExitPlayMode();
        }

        private static IEnumerator TeacherAftermathChecks()
        {
            var scene = SceneManager.GetActiveScene();
            var main = All<StoryEvent>(scene).Single(x => x.EventId == "D3_TEACHER_CHECK_IN_PRESSURE");
            // Start at the real post-combat steps, without editing or saving the production scene.
            foreach (Transform child in main.transform)
                if (int.Parse(child.name.Substring(0, 3)) < 100) child.gameObject.SetActive(false);
            var choice = All<StoryChoiceBranchStep>(scene).Single(x => x.name == "140_AudereChoosesHerAnswer");
            Assert.IsTrue(choice.Play(_ => { })); yield return Until(() => choice.ChoiceView.IsShowing);
            choice.Cancel(); yield return null;
            Assert.IsFalse(choice.ChoiceView.IsShowing);
            Assert.AreEqual(0, GameplayUIRoot.Instance.InputGate.ActiveClaimCount);
            for (int option = 0; option < 3; option++)
            {
                int callbacks = 0;
                Assert.IsTrue(main.Play(r => { Assert.AreEqual(StoryEventResult.Completed, r); callbacks++; }));
                yield return DriveTeacherAftermath(main, option);
                Assert.AreEqual(1, callbacks);
                Assert.AreEqual(0, GameplayUIRoot.Instance.InputGate.ActiveClaimCount);
                Assert.IsFalse(choice.ChoiceView.IsShowing);
                Assert.IsFalse(GameplayUIRoot.Instance.Dialogue.IsPlaying);
            }
            var motion = All<CharacterMotionStep>(scene).Single(x => x.name == "195_AudereLeansIn");
            var shadow = motion.GroundedShadow.GetComponent<SpriteRenderer>();
            var scale = motion.Actor.localScale; var shadowScale = shadow.transform.lossyScale;
            var color = shadow.color; var rotation = shadow.transform.rotation;
            float groundY = shadow.transform.position.y;
            Assert.IsTrue(motion.Play(_ => { })); yield return new WaitForSecondsRealtime(.1f);
            motion.Cancel(); yield return null;
            Assert.AreEqual(scale, motion.Actor.localScale);
            Assert.AreEqual(groundY, shadow.transform.position.y, .0001f);
            Assert.AreEqual(shadowScale, shadow.transform.lossyScale);
            Assert.AreEqual(color, shadow.color); Assert.AreEqual(rotation, shadow.transform.rotation);
            Assert.IsTrue(main.Play(_ => { })); yield return DriveTeacherAftermath(main, 0);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void BiancaReprise_SeparateEncounterDirectChainAndIdempotentAuthoring()
        {
            var scene=EditorSceneManager.OpenScene(Day3SchoolSetupTool.TeacherPath);
            var main=All<StoryEvent>(scene).Single(x=>x.EventId=="D3_TEACHER_CHECK_IN_PRESSURE");
            Assert.AreEqual(BiancaRepriseSetupTool.EventId,main.NextEvent.EventId);Assert.IsTrue(main.AutoPlayNextEvent);
            var step=main.NextEvent.GetComponentsInChildren<CombatStep>().Single();
            var data=step.CombatEncounterData;Assert.AreEqual(BiancaRepriseSetupTool.EncounterPath,AssetDatabase.GetAssetPath(data));
            Assert.IsTrue(data.EnemyDefinition.Validate(out string error),error);
            Assert.AreEqual(6,data.EnemyDefinition.GetPhase(0).MaxHealth);
            Assert.IsFalse(data.EnemyDefinition.GetPhase(0).SpawnDice);
            Assert.IsTrue(data.EnemyDefinition.GetPhase(0).DialogueCues.Single().RequiredBeforeVictory);
            Assert.AreEqual(CombatAllowedOutcome.Victory,data.OutcomeRules.AllowedOutcomes);
            Assert.AreNotSame(step.EnemyActorOverride,main.GetComponentsInChildren<CombatStep>().Single().EnemyActorOverride);
            foreach(var d in AssetDatabase.FindAssets("t:DialogueData",new[]{BiancaRepriseSetupTool.DialogueFolder}).Select(g=>AssetDatabase.LoadAssetAtPath<DialogueData>(AssetDatabase.GUIDToAssetPath(g))))
            { Assert.IsTrue(d.Lines.All(l=>l.Text.Length<=42),d.name);Assert.AreEqual("Audere_Tired_0",d.LeftPortraitOverride.name); }
            int before=All<Transform>(scene).Length;string saved=System.IO.File.ReadAllText(scene.path);
            BiancaRepriseSetupTool.AuthorActiveScene();Assert.AreEqual(before,All<Transform>(scene).Length);
            Assert.AreEqual(saved,System.IO.File.ReadAllText(scene.path),"A rerun must not duplicate or move authored staging.");
        }

        [UnityTest]
        public IEnumerator BiancaReprise_CatchFieldRepelsVisibleFanAndOrbitProjectiles()
        {
            OpenTeacherWithoutStartup();yield return new EnterPlayMode();yield return null;
            yield return RepriseCatchChecks();yield return new ExitPlayMode();
        }
        private static IEnumerator RepriseCatchChecks()
        {
            var scene=SceneManager.GetActiveScene();var step=All<CombatStep>(scene).Single(x=>x.name=="070_PlayFadingBianca");
            var controller=step.CombatController;var board=controller.BoardView;
            All<WorldModeController>(scene).Single().ApplyModeImmediate(WorldGameplayMode.Combat);
            int callbacks=0;Assert.IsTrue(step.Play(_=>callbacks++));
            yield return Until(()=>controller.CurrentState==CombatController.State.Playing);
            var sources=new System.Collections.Generic.HashSet<CombatBulletView>();int repelled=0;
            double started=EditorApplication.timeSinceStartup;bool capturedFan=false,capturedOrbit=false;
            while(EditorApplication.timeSinceStartup-started<13f)
            {
                Assert.IsTrue(controller.IsPlaying);
                foreach(var bullet in board.GetComponentsInChildren<CombatBulletView>())
                {
                    Assert.IsTrue(bullet.IsHarmless);Assert.IsFalse(bullet.CollisionActive);
                    if(!bullet.AttackActive)continue;
                    sources.Add(bullet.SourcePrefab);
                    Assert.AreEqual(1f,bullet.GetComponent<UnityEngine.UI.Graphic>().color.a,.001f);
                    // Force the cursor onto a visible projectile, then exercise the same
                    // board-owned movement callback. It must repel, not recycle the lease.
                    Vector2 position=bullet.RectTransform.anchoredPosition;
                    if(board.PlayArea.rect.Contains(position))
                    {
                        board.CatchCursor.position=board.PlayArea.TransformPoint(position);
                        int lease=bullet.PoolLeaseVersion;
                        Assert.IsTrue(bullet.TickMovement(board.PlayArea.rect,0f));
                        Assert.AreEqual(lease,bullet.PoolLeaseVersion);
                        Assert.Greater(Vector2.Distance(board.CatchZoneCenter,bullet.RectTransform.anchoredPosition),board.CatchZoneRadius);
                        repelled++;
                    }
                }
                if(!capturedFan&&sources.Count>=1&&repelled>3)
                { ScreenCapture.CaptureScreenshot("Temp/CatchAvoidance/fan-repel.png");capturedFan=true; }
                if(!capturedOrbit&&sources.Count>=2)
                { ScreenCapture.CaptureScreenshot("Temp/CatchAvoidance/orbit-repel.png");capturedOrbit=true; }
                EditorApplication.QueuePlayerLoopUpdate();yield return null;
            }
            Assert.AreEqual(2,sources.Count);Assert.Greater(repelled,10);
            step.Cancel();yield return null;
            Assert.AreEqual(1,callbacks);Assert.IsFalse(controller.IsPlaying);
            Assert.IsEmpty(board.GetComponentsInChildren<CombatBulletView>());
            Assert.AreEqual(0,GameplayUIRoot.Instance.InputGate.ActiveClaimCount);
            Assert.IsFalse(GameplayUIRoot.Instance.Dialogue.IsPlaying);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator BiancaReprise_AutomaticVictoryKeepsLastWordsAndClearsOnCancel()
        {
            OpenTeacherWithoutStartup();yield return new EnterPlayMode();yield return null;
            yield return RepriseCombatChecks();yield return new ExitPlayMode();
        }
        private static IEnumerator RepriseCombatChecks()
        {
            var scene=SceneManager.GetActiveScene();var step=All<CombatStep>(scene).Single(x=>x.name=="070_PlayFadingBianca");
            var teacher=All<CombatStep>(scene).Single(x=>x.name=="090_PlayTeacherPressure");
            var c=step.CombatController;var board=c.BoardView;
            All<WorldModeController>(scene).Single().ApplyModeImmediate(WorldGameplayMode.Combat);
            for(int attempt=0;attempt<2;attempt++)
            {
                int callbacks=0;StoryStepState result=StoryStepState.Failed;
                Assert.IsTrue(step.Play(r=>{callbacks++;result=r;}));yield return Until(()=>c.CurrentState==CombatController.State.Playing);
                Assert.AreSame(step.EnemyActorOverride,board.ActiveEnemyActor);
                double started=EditorApplication.timeSinceStartup;bool sawOne=false,sawLast=false;int sampled=0;
                while(c.IsPlaying&&EditorApplication.timeSinceStartup-started<45)
                {
                    foreach(var bullet in board.GetComponentsInChildren<CombatBulletView>())
                    {
                        Assert.IsFalse(bullet.CollisionActive);Assert.IsTrue(bullet.IsHarmless);sampled++;
                        // Deliberately overlap the Heart; no damage or collision is ever enabled.
                        bullet.RectTransform.anchoredPosition=board.PlayerPosition;
                    }
                    Assert.IsEmpty(board.GetComponentsInChildren<CombatDieView>());
                    if(c.EnemyHealth==1) { sawOne=true; if(GameplayUIRoot.Instance.Dialogue.IsPlaying)sawLast=true; }
                    if(attempt==0&&EditorApplication.timeSinceStartup-started>1.5) { step.Cancel();break; }
                    EditorApplication.QueuePlayerLoopUpdate();yield return null;
                }
                yield return null;
                Assert.Greater(sampled,0);Assert.IsFalse(c.IsPlaying);Assert.AreEqual(1,callbacks);
                Assert.AreEqual(attempt==0?StoryStepState.Cancelled:StoryStepState.Completed,result);
                if(attempt==1) { Assert.IsTrue(sawOne);Assert.IsTrue(sawLast);Assert.Greater(c.PlayerTime,40f); }
                Assert.IsNull(board.ActiveEnemyActor);Assert.AreEqual(0,GameplayUIRoot.Instance.InputGate.ActiveClaimCount);
                Assert.IsFalse(GameplayUIRoot.Instance.Dialogue.IsPlaying);Assert.AreEqual(0,board.ActiveStunTrailCount);
            }
            // Same scene/board can return to the original Teacher on a later play.
            Assert.IsTrue(teacher.Play(_=>{}));yield return Until(()=>c.IsPlaying);
            Assert.AreSame(teacher.EnemyActorOverride,board.ActiveEnemyActor);teacher.Cancel();yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        private static void AssertBiancaFeetAtTile(Transform bianca, Transform tile)
        {
            var body=bianca.GetComponent<SpriteRenderer>();
            Vector3 feet=bianca.TransformPoint(new Vector3(body.sprite.bounds.center.x,body.sprite.bounds.min.y,0f));
            Assert.AreEqual(tile.position.x,feet.x,.0001f,"Bianca feet must be horizontally centered on her tile.");
            Assert.AreEqual(tile.position.y,feet.y,.0001f,"Bianca feet must rest at the tile center baseline.");
        }

        [UnityTest]
        public IEnumerator BiancaReprise_BiancaArrivalLandsAtTileCentersAfterCancelAndReplay()
        {
            OpenTeacherWithoutStartup();yield return new EnterPlayMode();yield return null;
            yield return BiancaCenteredArrivalChecks();yield return new ExitPlayMode();
        }
        private static IEnumerator BiancaCenteredArrivalChecks()
        {
            var scene=SceneManager.GetActiveScene();var all=All<Transform>(scene);
            var e=All<StoryEvent>(scene).Single(x=>x.EventId==BiancaRepriseSetupTool.EventId);
            var b=all.Single(x=>x.name=="Bianca");b.gameObject.SetActive(true);
            var tiles=Enumerable.Range(0,4).Select(i=>all.Single(x=>x.name=="Bianca Arrival Tile "+i)).ToArray();
            foreach(var tile in tiles)tile.gameObject.SetActive(true);
            var entry=e.transform.Find("012_ResetBiancaAtPath").GetComponent<MoveActorStep>();
            Assert.IsTrue(entry.Play(_=>{}));yield return null;AssertBiancaFeetAtTile(b,tiles[3]);
            var shadow=b.GetComponentsInChildren<SpriteRenderer>().Single(x=>x.sortingOrder==4);
            Vector3 localScale=b.localScale,shadowScale=shadow.transform.lossyScale;
            var hops=e.GetComponentsInChildren<CharacterMotionStep>().Where(x=>x.Actor==b).OrderBy(x=>x.name).ToArray();
            Assert.AreEqual(3,hops.Length);
            float groundY=shadow.transform.position.y;
            Assert.IsTrue(hops[0].Play(_=>{}));yield return new WaitForSecondsRealtime(.16f);
            Assert.AreEqual(groundY,shadow.transform.position.y,.0001f);
            hops[0].Cancel();yield return null;
            Assert.AreEqual(localScale,b.localScale);Assert.AreEqual(shadowScale,shadow.transform.lossyScale);
            Assert.IsTrue(entry.Play(_=>{}));yield return null;
            for(int i=0;i<hops.Length;i++)
            {
                int callbacks=0;Assert.IsTrue(hops[i].Play(_=>callbacks++));
                if(i==2) { yield return new WaitForSecondsRealtime(.16f);ScreenCapture.CaptureScreenshot("Temp/BiancaCenter/arrival-apex.png"); }
                yield return Until(()=>!hops[i].IsRunning);
                Assert.AreEqual(1,callbacks);AssertBiancaFeetAtTile(b,tiles[2-i]);
                Assert.AreEqual(localScale,b.localScale);Assert.AreEqual(shadowScale,shadow.transform.lossyScale);
            }
            Assert.IsTrue(e.transform.Find("105_BiancaWaits").GetComponent<MoveActorStep>().Play(_=>{}));yield return null;
            AssertBiancaFeetAtTile(b,tiles[0]);
            for(int i=1;i<tiles.Length;i++)tiles[i].gameObject.SetActive(false);
            all.Single(x=>x.name=="Teacher PLACEHOLDER").gameObject.SetActive(false);
            all.Single(x=>x.name=="Audere").position=all.Single(x=>x.name=="Audere_ChoosesCompany").position;
            yield return new WaitForSecondsRealtime(.2f);ScreenCapture.CaptureScreenshot("Temp/BiancaCenter/centered-pair.png");
            yield return null;LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator BiancaReprise_ThreeRepliesHopCancelAndDayThreeTitle()
        {
            OpenTeacherWithoutStartup();yield return new EnterPlayMode();yield return null;
            yield return RepriseEndingChecks();yield return new ExitPlayMode();
        }
        private static IEnumerator RepriseEndingChecks()
        {
            var scene=SceneManager.GetActiveScene();var e=All<StoryEvent>(scene).Single(x=>x.EventId==BiancaRepriseSetupTool.EventId);
            var choice=e.GetComponentsInChildren<StoryChoiceBranchStep>().Single();
            var title=scene.GetRootGameObjects().Single(x=>x.name=="DAY THREE END TITLE").GetComponent<CanvasGroup>();
            var actor=All<Transform>(scene).Single(x=>x.name=="Audere");
            var bianca=All<Transform>(scene).Single(x=>x.name=="Bianca");bianca.gameObject.SetActive(true);
            var tile=All<Transform>(scene).Single(x=>x.name=="Bianca Arrival Tile 0");tile.gameObject.SetActive(true);
            foreach(Transform child in e.transform)if(int.Parse(child.name.Substring(0,3))<80)child.gameObject.SetActive(false);
            // This test owns the ending presentation; the Day 4 suite verifies its scene-load tail.
            foreach(var load in e.GetComponentsInChildren<SceneLoadStep>())load.gameObject.SetActive(false);
            var hop=e.transform.Find("140_AudereFindsAWord").GetComponent<CharacterMotionStep>();
            Vector3 scale=actor.localScale;Vector3 shadowScale=hop.GroundedShadow.lossyScale;float y=hop.GroundedShadow.position.y;
            Assert.IsTrue(hop.Play(_=>{}));yield return new WaitForSecondsRealtime(.09f);hop.Cancel();yield return null;
            Assert.AreEqual(scale,actor.localScale);Assert.AreEqual(shadowScale,hop.GroundedShadow.lossyScale);Assert.AreEqual(y,hop.GroundedShadow.position.y,.0001f);
            Assert.IsTrue(choice.Play(_=>{}));yield return Until(()=>choice.ChoiceView.IsShowing);choice.Cancel();yield return null;
            Assert.AreEqual(0,GameplayUIRoot.Instance.InputGate.ActiveClaimCount);
            for(int option=0;option<3;option++)
            {
                title.alpha=0;actor.position=All<Transform>(scene).Single(x=>x.name=="Audere_Opening").position;
                int callbacks=0,selections=0;Assert.IsTrue(e.Play(r=>{Assert.AreEqual(StoryEventResult.Completed,r);callbacks++;}));
                double deadline=EditorApplication.timeSinceStartup+25;
                while(e.IsPlaying&&EditorApplication.timeSinceStartup<deadline)
                {
                    if(choice.ChoiceView.IsShowing) { var button=choice.ChoiceView.GetComponentsInChildren<StoryChoiceOptionView>()[option].Button;button.onClick.Invoke();button.onClick.Invoke();selections++; }
                    EndTestDialogue();EditorApplication.QueuePlayerLoopUpdate();yield return null;
                }
                Assert.IsFalse(e.IsPlaying);Assert.AreEqual(1,callbacks);Assert.AreEqual(1,selections);
                Assert.AreEqual(1,title.alpha);Assert.AreEqual("Ngày 3 - Kết thúc",title.GetComponentInChildren<TMPro.TMP_Text>().text);
                var cover=scene.GetRootGameObjects().Single(x=>x.name=="DAY THREE STORY COVER").GetComponentInChildren<CanvasGroup>();
                Assert.IsTrue(cover.gameObject.activeInHierarchy);Assert.AreEqual(1f,cover.alpha);
                var shadow=bianca.GetComponentsInChildren<SpriteRenderer>().Single(x=>x.sortingOrder==4);
                AssertBiancaFeetAtTile(bianca, tile);
                Assert.AreEqual(.25f,bianca.position.x-actor.position.x,.015f);
                Assert.IsFalse(All<Transform>(scene).Single(x=>x.name=="Teacher PLACEHOLDER").gameObject.activeSelf);
                Assert.AreEqual(0,GameplayUIRoot.Instance.InputGate.ActiveClaimCount);Assert.IsFalse(GameplayUIRoot.Instance.Dialogue.IsPlaying);
            }
            LogAssert.NoUnexpectedReceived();
        }


        private static void OpenTeacherWithoutStartup()
        {
            var scene = EditorSceneManager.OpenScene(Day3SchoolSetupTool.TeacherPath);
            var so = new SerializedObject(All<StoryDirector>(scene).Single());
            so.FindProperty("playOnStart").boolValue = false; so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void EndTestDialogue()
        {
            var d = GameplayUIRoot.Instance.Dialogue;
            if (d.IsPlaying) typeof(DialogueController).GetMethod("EndPlayback", Private)
                .Invoke(d, new object[] { DialogueResult.Completed, true });
        }
        private static IEnumerator DriveTeacherAftermath(StoryEvent main, int option)
        {
            double deadline = EditorApplication.timeSinceStartup + 60;
            var scene = SceneManager.GetActiveScene();
            var choices = All<StoryChoiceBranchStep>(scene).Single(x => x.name == "140_AudereChoosesHerAnswer");
            int selections = 0;
            while (main.IsPlaying && EditorApplication.timeSinceStartup < deadline)
            {
                if (choices.ChoiceView.IsShowing)
                {
                    var buttons = choices.ChoiceView.GetComponentsInChildren<StoryChoiceOptionView>();
                    buttons[option].Button.onClick.Invoke(); buttons[option].Button.onClick.Invoke();
                    selections++;
                }
                SustainStationaryTestPlayer();
                var combat = All<CombatStep>(scene).Single(x => x.name == "090_PlayTeacherPressure").CombatController;
                if (!combat.IsPlaying || combat.CurrentState == CombatController.State.Victory) EndTestDialogue();
                EditorApplication.QueuePlayerLoopUpdate(); yield return null;
            }
            var last = All<CombatStep>(scene).Single(x => x.name == "090_PlayTeacherPressure").CombatController;
            Assert.IsFalse(main.IsPlaying, "Teacher aftermath did not complete: " + (main.CurrentStep != null ? main.CurrentStep.name : "none") + " / " + last.CurrentState + " HP=" + last.EnemyHealth + " TIME=" + last.PlayerTime);
            Assert.AreEqual(1, selections, "Exactly one reply must be chosen per play.");
        }

        private static void SustainStationaryTestPlayer()
        {
            // This story-flow harness does not dodge. Use the existing Heal debug input
            // while waiting for authored dialogue, not a balance change or a victory shortcut.
            foreach (var combat in All<CombatController>(SceneManager.GetActiveScene()))
                if (combat.CurrentState == CombatController.State.Playing && combat.PlayerTime < 65f)
                    combat.DebugApplyDiceEffect(CombatSymbol.Heal);
        }

        private static IEnumerator Until(Func<bool> ready,bool advance=false,float timeout=20)
        {
            double deadline=EditorApplication.timeSinceStartup+timeout;
            while(!ready()&&EditorApplication.timeSinceStartup<deadline)
            {
                SustainStationaryTestPlayer();
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
