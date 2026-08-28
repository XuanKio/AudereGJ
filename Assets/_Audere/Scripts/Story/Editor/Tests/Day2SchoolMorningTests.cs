#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using Audere.Dialogue;
using Audere.EditorTools;
using Audere.Story;
using Audere.Story.Steps;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace Audere.Story.Editor.Tests
{
    public sealed class Day2SchoolMorningTests
    {
        [Test]
        public void CooperativeBoards_KeepBothAnchorsFitMaskAndBindSceneObjects()
        {
            var scene=EditorSceneManager.OpenScene(Day2SchoolMorningSetupTool.ScenePath,OpenSceneMode.Single);
            var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
            var pairs=all.Select(t=>t.GetComponent<Audere.Puzzle.CooperativePuzzleSession>()).Where(p=>p!=null).OrderBy(p=>p.Puzzle.PuzzleData.PuzzleId).ToArray();
            Assert.AreEqual(3,pairs.Length);
            Assert.AreEqual(2,all.Count(t=>t.GetComponent<Audere.Puzzle.GridPlayer>()!=null));
            Assert.AreEqual(1,all.Count(t=>t.GetComponent<Audere.Puzzle.PuzzleRuntime>()!=null));
            Assert.AreEqual(0,all.Single(t=>t.name=="COOP PUZZLE CONTROLS").GetComponentsInChildren<UnityEngine.UI.Button>(true).Length);
            var world=all.Select(t=>t.GetComponent<Audere.World.WorldModeController>()).Single(w=>w!=null);
            Assert.IsFalse(new SerializedObject(world).FindProperty("allowChildFadeFallback").boolValue);
            var camera=all.Single(t=>t.name=="Main Camera");
            Bounds left=all.Single(t=>t.name=="Mask Left").GetComponent<SpriteRenderer>().bounds;
            Bounds right=all.Single(t=>t.name=="Mask Right").GetComponent<SpriteRenderer>().bounds;
            Bounds top=all.Single(t=>t.name=="Mask Top").GetComponent<SpriteRenderer>().bounds;
            Bounds bottom=all.Single(t=>t.name=="Mask Bottom").GetComponent<SpriteRenderer>().bounds;
            for(int i=0;i<3;i++)
            {
                var pair=pairs[i];var puzzle=pair.Puzzle;var controller=puzzle.GetComponent<Audere.Puzzle.PuzzleController>();
                var tiles=controller.PuzzleRoot.GetComponentsInChildren<Audere.Puzzle.Board.BoardTile>(true);
                Assert.AreEqual(4,puzzle.PuzzleData.AvailablePathPieces.Count);
                Assert.IsTrue(puzzle.PuzzleData.RequireAllPathPieces);
                var cells=tiles.Select(t=>puzzle.Board.GridSpace.WorldToCell(t.transform.position)).ToArray();
                Assert.LessOrEqual(cells.Max(c=>c.x)-cells.Min(c=>c.x)+1,6);
                Assert.LessOrEqual(cells.Max(c=>c.y)-cells.Min(c=>c.y)+1,3);
                Assert.IsNull(new SerializedObject(controller).FindProperty("cameraFollow").objectReferenceValue);
                Vector3 shift=all.Single(t=>t.name=="Camera_Coop_0"+(i+1)).position-camera.position;
                foreach(var tile in tiles)
                {
                    var sr=tile.GetComponentsInChildren<SpriteRenderer>(true).First();var b=sr.bounds;
                    Assert.Greater(b.min.x,left.max.x+shift.x+.01f);
                    Assert.Less(b.max.x,right.min.x+shift.x-.01f);
                    Assert.Greater(b.min.y,bottom.max.y+shift.y+.01f);
                    Assert.Less(b.max.y,top.min.y+shift.y-.01f);

                }
                foreach(string name in new[]{"audereArrivalFade","partnerArrivalFade","audereRestore","partnerRestore","encouragement"})
                    Assert.IsNotNull(new SerializedObject(pair).FindProperty(name).objectReferenceValue,name);
                if(i>0)
                {
                    Assert.Less(Vector3.Distance(pairs[i-1].AudereGoal.transform.position,puzzle.PlayerStartTransform.position),.0001f);
                    Assert.Less(Vector3.Distance(pairs[i-1].PartnerGoal.transform.position,pair.PartnerStart.position),.0001f);
                }
            }
            var final=all.Select(t=>t.GetComponent<StoryEvent>()).Single(e=>e!=null&&e.EventId=="D2_SCHOOL_WRONG_SUPPLIES");
            var transition=final.GetComponentInChildren<FullscreenWorldModeTransitionStep>(true);
            Assert.AreEqual("WorldTransition_DreamyDisorientation",transition.TransitionProfile.name);
            var combat=final.GetComponentInChildren<CombatStep>(true);
            Assert.AreEqual("Bianca",combat.CombatEncounterData.EnemyDisplayName);
            Assert.IsTrue(combat.CombatEncounterData.EnemyDefinition.Validate(out string error),error);
            var board=all.Select(t=>t.GetComponent<Audere.Combat.CombatBoardView>()).Single(b=>b!=null);
            var actor=(Audere.Combat.CombatEnemyActor)new SerializedObject(board).FindProperty("authoredEnemyActor").objectReferenceValue;
            Assert.AreSame(combat.CombatEncounterData.EnemyDefinition.ActorPrefab,PrefabUtility.GetCorrespondingObjectFromSource(actor));
            foreach(var speech in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<DialogueStep>(true)))
                foreach(var line in speech.DialogueData.Lines)Assert.LessOrEqual(line.Text.Length,42,speech.name);
        }

        [UnityTest]
        public IEnumerator CooperativeRules_SharedCellArrivalResetAndCancel()
        {
            var scene=EditorSceneManager.OpenScene(Day2SchoolMorningSetupTool.ScenePath,OpenSceneMode.Single);
            var director=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<StoryDirector>(true)).Single();
            var so=new SerializedObject(director);so.FindProperty("playOnStart").boolValue=false;so.ApplyModifiedPropertiesWithoutUndo();
            yield return new EnterPlayMode();
            yield return VerifyCooperativeRules();
            yield return new ExitPlayMode();
        }
        private static IEnumerator VerifyCooperativeRules()
        {
            Application.runInBackground=true;
            EditorWindow.GetWindow(System.Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
            var pair=Object.FindObjectsByType<Audere.Puzzle.CooperativePuzzleSession>(FindObjectsInactive.Include,FindObjectsSortMode.None)
                .Single(p=>p.Puzzle.PuzzleData.PuzzleId=="PZ_D2_COOP_03");
            var puzzle=pair.Puzzle;var level=puzzle.GetComponent<Audere.Puzzle.PuzzleController>();
            var grid=puzzle.Board.GridSpace;
            level.PuzzleRoot.parent.gameObject.SetActive(true);level.PuzzleRoot.gameObject.SetActive(true);
            Assert.IsTrue(level.Play());yield return null;
            var a=puzzle.Player;var b=pair.Partner;
            var runtime=grid.GetComponentInChildren<Audere.Puzzle.PuzzleRuntime>(true);
            GameplayUIRoot.Instance.PathPieceHand.Select(0);runtime.Placement.Cancel();yield return null;
            Assert.IsFalse(runtime.Preview.GetComponentsInChildren<SpriteRenderer>(true).Any(r=>r.gameObject.activeInHierarchy),"Same-frame select/cancel must not leave a ghost preview.");
            b.SetPosition(a.GridPosition,grid.CellToWorldCenter(a.GridPosition));
            var savedRandom=Random.state;Random.InitState(712);
            var previewRandom=Random.state;
            for(int i=0;i<12;i++)Assert.AreSame(a,pair.ActorAtStart(a.GridPosition,false));
            Assert.AreEqual(previewRandom,Random.state,"Preview must not roll a new actor each frame.");
            bool sawA=false,sawB=false;
            for(int i=0;i<64;i++){var selected=pair.ActorAtStart(a.GridPosition,true);sawA|=selected==a;sawB|=selected==b;}
            Assert.IsTrue(sawA&&sawB,"A shared-cell drop must be able to select either actor.");Random.state=savedRandom;
            yield return TickUntil(()=>b.transform.position.x-a.transform.position.x>.10f);
            Assert.AreEqual(a.GridPosition,b.GridPosition);
            Assert.Greater(b.transform.position.x-a.transform.position.x,.10f);
            Assert.AreEqual(5,a.GetComponent<SpriteRenderer>().sortingOrder);Assert.AreEqual(5,b.GetComponent<SpriteRenderer>().sortingOrder);
            Assert.Less(a.GetComponent<UnityEngine.Rendering.SortingGroup>().sortingOrder,b.GetComponent<UnityEngine.Rendering.SortingGroup>().sortingOrder);
            Assert.IsTrue(puzzle.ResetPuzzle(true));yield return null;
            Assert.AreEqual(5,b.GetComponent<SpriteRenderer>().sortingOrder);
            var red=level.PuzzleRoot.GetComponentsInChildren<Audere.Puzzle.CooperativeRedTileBehaviour>(true).First();
            var redTile=red.GetComponent<Audere.Puzzle.Board.BoardTile>();
            puzzle.Board.NotifyPlayerEntered(redTile.GridPosition,b);
            Assert.IsTrue(red.HasBeenEntered);
            Assert.IsTrue(puzzle.Board.CanPlayerEnter(redTile.GridPosition,a));
            puzzle.Board.NotifyPlayerEntered(redTile.GridPosition,a);
            puzzle.Board.NotifyPlayerExited(redTile.GridPosition,a);
            Assert.IsFalse(red.IsCollapsed,"The tile must remain while Bianca is still holding it.");
            Assert.IsTrue(redTile.GetComponentInChildren<SpriteRenderer>(true).enabled);
            puzzle.Board.NotifyPlayerExited(redTile.GridPosition,b);
            Assert.IsTrue(red.IsCollapsed);Assert.IsTrue(red.BothPassed);
            Assert.IsFalse(redTile.GetComponentInChildren<SpriteRenderer>(true).enabled);
            Assert.IsFalse(puzzle.Board.CanPlayerEnter(redTile.GridPosition,a));
            yield return TickFor(.1f);
            Assert.IsTrue(puzzle.ResetPuzzle(true));Assert.IsFalse(red.HasBeenEntered);Assert.IsFalse(red.IsCollapsed);
            yield return TickFor(.3f);
            foreach(var tile in level.PuzzleRoot.GetComponentsInChildren<Audere.Puzzle.Board.BoardTile>(true))
                Assert.AreEqual(1f,tile.GetComponentsInChildren<SpriteRenderer>(true).First().color.a,.001f,"Every tile must reset, including an interrupted collapse.");
            puzzle.Board.NotifyPlayerEntered(redTile.GridPosition,b);
            puzzle.Board.NotifyPlayerExited(redTile.GridPosition,b);
            Assert.IsFalse(puzzle.Board.CanPlayerEnter(redTile.GridPosition,a),"Leaving a red tile too early strands the other actor.");
            Assert.IsTrue(red.IsCollapsed);
            Assert.IsFalse(redTile.GetComponentInChildren<SpriteRenderer>(true).enabled);
            Assert.IsFalse(red.BothPassed, "A stranded tile is hidden but does not count as both having passed.");
            Assert.IsTrue(puzzle.ResetPuzzle(true));
            var hand=GameplayUIRoot.Instance.PathPieceHand;
            puzzle.Board.NotifyPlayerEntered(redTile.GridPosition,b);
            hand.Select(0);
            var fall=Audere.Puzzle.PathPieces.PathPlacementValidator.Validate(hand.SelectedPiece,a.GridPosition,
                Audere.Puzzle.PathPieces.GridRotation.Degrees90,a.GridPosition,puzzle.Board,a);
            Assert.IsTrue(fall.WillFall);puzzle.SubmitPlacement(fall);
            yield return TickUntil(()=>puzzle.CurrentState==Audere.Puzzle.PuzzleManager.State.Playing && hand.Count==4);
            Assert.IsFalse(red.HasBeenEntered,"Falling resets shared red state.");
            hand.Setup(new[]{puzzle.PuzzleData.AvailablePathPieces[0]});hand.Select(0);
            var shortAttempt=Audere.Puzzle.PathPieces.PathPlacementValidator.Validate(hand.SelectedPiece,a.GridPosition,
                Audere.Puzzle.PathPieces.GridRotation.Degrees0,a.GridPosition,puzzle.Board,a);
            Assert.IsTrue(shortAttempt.CanCommit);Assert.IsFalse(shortAttempt.WillFall);puzzle.SubmitPlacement(shortAttempt);
            yield return TickUntil(()=>puzzle.CurrentState==Audere.Puzzle.PuzzleManager.State.Playing && hand.Count==4);
            Assert.IsFalse(red.HasBeenEntered,"Running out of path pieces resets every shared red tile.");
            a.SetPosition(pair.AudereGoal.GridPosition,grid.CellToWorldCenter(pair.AudereGoal.GridPosition));
            puzzle.StartCoroutine(pair.ResolveLanding(a));
            yield return TickUntil(()=>pair.HasArrived(a) && a.GetComponent<SpriteRenderer>().color.a<.001f);
            Assert.IsTrue(pair.HasArrived(a));Assert.IsNull(pair.ActorAtStart(a.GridPosition,true));
            Assert.IsFalse(pair.BothAtGoals);
            foreach(var sr in a.GetComponentsInChildren<SpriteRenderer>(true))Assert.Less(sr.color.a,.001f);
            Assert.IsTrue(puzzle.Board.CanPlayerEnter(pair.PartnerGoal.GridPosition,b));
            Assert.IsTrue(puzzle.ResetPuzzle(true));
            Assert.IsFalse(pair.HasArrived(a));Assert.AreEqual(4,GameplayUIRoot.Instance.PathPieceHand.Count);
            Assert.IsTrue(redTile.GetComponentInChildren<SpriteRenderer>(true).enabled);
            foreach(var actor in new[]{a,b})
            {
                Assert.AreEqual(1f,actor.GetComponent<SpriteRenderer>().color.a,.001f);
                Assert.AreEqual(5,actor.GetComponent<SpriteRenderer>().sortingOrder);
                Assert.AreEqual(100f/255f,actor.GetComponentsInChildren<SpriteRenderer>(true).Single(sr=>sr.sortingOrder==4).color.a,.001f);
            }
            a.SetPosition(pair.AudereGoal.GridPosition,grid.CellToWorldCenter(pair.AudereGoal.GridPosition));
            var fade=(SpriteGroupFadeStep)new SerializedObject(pair).FindProperty("audereArrivalFade").objectReferenceValue;
            Assert.IsTrue(fade.Play());yield return TickFor(.1f);level.Cancel();
            Assert.IsFalse(fade.IsRunning);Assert.AreEqual(0,GameplayUIRoot.Instance.InputGate.ActiveClaimCount);
            Assert.IsTrue(level.Play());yield return null;
            Assert.AreEqual(1f,a.GetComponent<SpriteRenderer>().color.a,.001f);
            level.Cancel();Assert.AreEqual(0,GameplayUIRoot.Instance.InputGate.ActiveClaimCount);
        }

        [Test]
        public void SchoolScene_HasDirectStagingAndOrderedProductionFlow()
        {
            var scene = EditorSceneManager.OpenScene(Day2SchoolMorningSetupTool.ScenePath, OpenSceneMode.Single);
            var transforms = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
            var events = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<StoryEvent>(true)).ToArray();
            Assert.AreEqual(events.Length, events.Select(e => e.EventId).Distinct().Count());
            foreach (Transform t in transforms)
                Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject), t.name);
            foreach (StoryEvent e in events)
                foreach (Transform child in e.transform)
                    Assert.AreEqual(1, child.GetComponents<StoryStep>().Length, e.name + "/" + child.name);

            StoryEvent main = events.Single(e => e.EventId == "D2_SCHOOL_BIANCA_MORNING");
            Assert.Less(main.transform.Find("080_ThreeSecondsAlone").GetSiblingIndex(),
                main.transform.Find("090_RevealBiancaTile").GetSiblingIndex());
            Assert.Less(main.transform.Find("250_BriefMorningGreeting").GetSiblingIndex(),
                main.transform.Find("270_AudereLeavesAsBiancaFades").GetSiblingIndex());
            foreach (CharacterMotionStep motion in main.GetComponentsInChildren<CharacterMotionStep>(true))
            {
                Assert.IsNotNull(motion.Actor, motion.name);
                Assert.IsNotNull(motion.TargetTransform, motion.name);
                Assert.IsNotNull(motion.GroundedShadow, motion.name);
                Assert.IsNotNull(motion.ActorRenderer, motion.name);
                Assert.AreEqual("Player", motion.ActorRenderer.sortingLayerName);
                Assert.AreEqual(5, motion.ActorRenderer.sortingOrder);
                Assert.AreEqual(4, motion.GroundedShadow.GetComponent<SpriteRenderer>().sortingOrder);
            }
            foreach (ParallelStoryStep parallel in main.GetComponentsInChildren<ParallelStoryStep>(true))
                foreach (StoryEvent branch in parallel.Branches)
                    Assert.AreSame(parallel.transform, branch.transform.parent);
            foreach (DialogueStep dialogue in main.GetComponentsInChildren<DialogueStep>(true))
            {
                Assert.AreEqual(DialogueCharacterId.Audere, dialogue.DialogueData.LeftCharacter);
                Assert.IsNotNull(dialogue.DialogueController);
                foreach (DialogueData.Line line in dialogue.DialogueData.Lines)
                    Assert.LessOrEqual(line.Text.Length, 42);
            }
            Transform audere = transforms.Single(t => t.name == "Audere");
            Transform bianca = transforms.Single(t => t.name == "Bianca_PLACEHOLDER");
            Assert.AreEqual(audere.GetComponent<SpriteRenderer>().flipX, bianca.GetComponent<SpriteRenderer>().flipX);
            Transform goal = transforms.Single(t => t.name == "Audere_ThreeTilesAway");
            Transform greeting = transforms.Single(t => t.name == "Bianca_Greeting");
            Assert.AreEqual(.75f, goal.position.x - greeting.position.x, .001f);
            Assert.Greater(goal.position.x, audere.position.x);
            Assert.AreEqual(audere.position.y, goal.position.y, .0001f);
            Assert.IsFalse(transforms.Any(t => t.name.Contains("PassInFront") || t.name.Contains("RejoinWalkway")));
            Transform turn = main.transform.Find("255_AudereTurnsAwayFromBianca");
            Transform followStep = main.transform.Find("256_CameraFollowsAudere");
            Assert.AreEqual(main.transform.Find("250_BriefMorningGreeting").GetSiblingIndex() + 1, turn.GetSiblingIndex());
            Assert.AreEqual(turn.GetSiblingIndex() + 1, followStep.GetSiblingIndex());
            var facing = new SerializedObject(turn.GetComponent<SetActorFacingStep>());
            Assert.IsTrue(facing.FindProperty("faceRight").boolValue);
            var follow = transforms.Single(t => t.name == "Main Camera").GetComponent<UnityEngine.Animations.PositionConstraint>();
            Assert.IsNotNull(follow);
            Assert.IsFalse(follow.enabled);
            Assert.AreEqual(UnityEngine.Animations.Axis.X, follow.translationAxis);
            Assert.AreSame(audere, follow.GetSource(0).sourceTransform);
            var startFollow = followStep.GetComponent<SetBehaviourEnabledStep>();
            Assert.AreSame(follow, startFollow.Target);
            Assert.IsTrue(startFollow.Enable);
            var stopFollow = main.transform.Find("002_StopCameraFollowForOpening").GetComponent<SetBehaviourEnabledStep>();
            Assert.AreSame(follow, stopFollow.Target);
            Assert.IsFalse(stopFollow.Enable);
            Assert.IsTrue(EditorBuildSettings.scenes.Any(s => s.path == scene.path && s.enabled));
        }

        [Test]
        public void SchoolAuthoring_RerunPreservesCountsAndHomeLink()
        {
            EditorSceneManager.OpenScene(Day2SchoolMorningSetupTool.ScenePath, OpenSceneMode.Single);
            int countBefore = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()
                .Sum(r => r.GetComponentsInChildren<Transform>(true).Length);
            var dialoguePaths = AssetDatabase.FindAssets("t:DialogueData", new[] { "Assets/_Audere/Data/Dialogue/Day2/School" })
                .Select(AssetDatabase.GUIDToAssetPath).ToArray();
            var dialogueBefore = dialoguePaths.Select(System.IO.File.ReadAllText).ToArray();
            Day2SchoolMorningSetupTool.Setup();
            CollectionAssert.AreEqual(dialogueBefore, dialoguePaths.Select(System.IO.File.ReadAllText).ToArray());
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            Assert.AreEqual(countBefore, scene.GetRootGameObjects().Sum(r => r.GetComponentsInChildren<Transform>(true).Length));
            var home = EditorSceneManager.OpenScene("Assets/_Audere/Scenes/50_D2_Home_Morning.unity", OpenSceneMode.Additive);
            try
            {
                var bus = home.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<StoryEvent>(true)).Single(e => e.EventId == "D2_TO_BUS_STOP");
                Assert.AreEqual(1, bus.GetComponentsInChildren<SceneLoadStep>(true).Length);
                Assert.AreEqual("120_LoadDay2School", bus.transform.GetChild(bus.transform.childCount - 1).name);
            }
            finally { EditorSceneManager.CloseScene(home, true); }
        }

        [Test]
        public void ClassroomSupplies_ThreeAnswersConvergeAndRearTileFitsMask()
        {
            var scene = EditorSceneManager.OpenScene(Day2SchoolMorningSetupTool.ScenePath, OpenSceneMode.Single);
            var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
            var classroom = all.Select(t => t.GetComponent<StoryEvent>()).Single(e => e != null && e.EventId == "D2_CLASSROOM_SUPPLIES");
            var morning = all.Select(t => t.GetComponent<StoryEvent>()).Single(e => e != null && e.EventId == "D2_SCHOOL_BIANCA_MORNING");
            Assert.IsTrue(morning.AutoPlayNextEvent);
            Assert.AreSame(classroom, morning.NextEvent);
            var choice = classroom.GetComponentInChildren<StoryChoiceBranchStep>(true);
            Assert.AreEqual(3, choice.Options.Count);
            Assert.AreEqual(3, choice.Branches.Count);
            Assert.IsNotNull(choice.ChoiceView);
            for (int i = 0; i < 3; i++)
            {
                var branch = choice.Branches[i];
                Assert.AreSame(choice.transform, branch.transform.parent);
                Assert.IsFalse(branch.AutoPlayNextEvent);
                Assert.IsNull(branch.NextEvent);
                var speech = branch.GetComponentsInChildren<DialogueStep>(true);
                Assert.AreEqual(1, speech.Length);
                Assert.AreEqual(choice.Options[i], speech[0].DialogueData.Lines[0].Text);
                Assert.AreEqual(DialogueCharacterId.Audere, speech[0].DialogueData.LeftCharacter);
            }
            Assert.AreEqual(choice.transform.GetSiblingIndex() + 1,
                classroom.transform.Find("220_BiancaTurnsBackToAudere").GetSiblingIndex());
            Assert.AreEqual(3f, classroom.transform.Find("240_TimorSaysNothing").GetComponent<WaitStep>().Duration);
            Transform audere = all.Single(t => t.name == "Audere_ClassroomLeft");
            Transform bianca = all.Single(t => t.name == "Bianca_ClassroomBehindAudere");
            Transform teacher = all.Single(t => t.name == "Teacher_ClassroomRight");
            Assert.AreEqual(.25f, audere.position.x - bianca.position.x, .0001f);
            Assert.AreEqual(audere.position.y, bianca.position.y, .0001f);
            Assert.Greater(teacher.position.x, audere.position.x);
            var tile = all.Single(t => t.name == "Tile_ClassroomBiancaBehind").GetComponentsInChildren<SpriteRenderer>(true).First();
            var camera = all.Single(t => t.name == "Main Camera");
            var viewportMask = camera.Find("PuzzleViewportMask").gameObject;
            Assert.IsTrue(viewportMask.activeSelf, "The opening encounter also needs the viewport mask.");
            var normalize = morning.transform.Find("010_NormalizeSchool").GetComponent<SetActiveStep>();
            CollectionAssert.Contains(normalize.ObjectsToEnable, viewportMask);
            CollectionAssert.DoesNotContain(normalize.ObjectsToDisable, viewportMask);
            var lastLanding = all.Single(t => t.name == "Audere_FinalWalkPose");
            Assert.Less(Vector3.Distance(lastLanding.position, audere.position), .0001f,
                "Audere must keep the last hallway pose through the classroom fade.");
            var lastTile = all.Single(t => t.name == "Tile_AudereFinal");
            var classTile = all.Single(t => t.name == "Tile_ClassroomAudere");
            Assert.Less(Vector3.Distance(lastTile.position, classTile.position), .0001f);
            var reframe = morning.transform.Find("316_FrameClassroomFromLastTile").GetComponent<MoveActorStep>();
            var stopFollow = morning.transform.Find("315_StopFollowAtLastTile").GetComponent<SetBehaviourEnabledStep>();
            Assert.AreSame(camera.GetComponent<UnityEngine.Animations.PositionConstraint>(), stopFollow.Target);
            Assert.IsFalse(stopFollow.Enable);
            Assert.AreEqual(morning.transform.Find("310_TimorAnswersWhileWalking").GetSiblingIndex() + 1,
                stopFollow.transform.GetSiblingIndex());
            Assert.AreEqual(stopFollow.transform.GetSiblingIndex() + 1, reframe.transform.GetSiblingIndex());
            Assert.AreEqual(reframe.transform.GetSiblingIndex() + 1,
                morning.transform.Find("320_HoldUnansweredFeeling").GetSiblingIndex());
            Assert.AreSame(camera, reframe.Actor);
            Assert.Greater(reframe.Duration, 0f, "Reframe visibly before the fade, never snap at the cut.");
            Assert.IsTrue(reframe.UseUnscaledTime);
            var classCamera = classroom.transform.Find("020_ResetCameraForClassroom").GetComponent<MoveActorStep>();
            Assert.AreSame(reframe.TargetTransform, classCamera.TargetTransform);
            Assert.Less(audere.position.x, classCamera.TargetTransform.position.x);
            Assert.Greater(teacher.position.x, classCamera.TargetTransform.position.x);
            var mask = all.Single(t => t.name == "Mask Left").GetComponent<SpriteRenderer>();
            var maskRight = all.Single(t => t.name == "Mask Right").GetComponent<SpriteRenderer>();
            float cameraShift = classCamera.TargetTransform.position.x - camera.position.x;
            Assert.Greater(tile.bounds.min.x, mask.bounds.max.x + cameraShift + .02f,
                "Bianca's whole tile must fit inside the mask at the classroom camera pose.");
            var teacherTile = all.Single(t => t.name == "Tile_ClassroomTeacher").GetComponentsInChildren<SpriteRenderer>(true).First();
            Assert.Less(teacherTile.bounds.max.x, maskRight.bounds.min.x + cameraShift - .02f);
            foreach (var speech in classroom.GetComponentsInChildren<DialogueStep>(true))
                foreach (var line in speech.DialogueData.Lines)
                    Assert.LessOrEqual(line.Text.Length, 42);
        }

        [UnityTest]
        public IEnumerator ClassroomChoice_UsesRetainedGateAndReleasesOnCancel()
        {
            var scene = EditorSceneManager.OpenScene(Day2SchoolMorningSetupTool.ScenePath, OpenSceneMode.Single);
            var director = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<StoryDirector>(true)).Single();
            var directorData = new SerializedObject(director);
            directorData.FindProperty("playOnStart").boolValue = false;
            directorData.ApplyModifiedPropertiesWithoutUndo();
            yield return new EnterPlayMode();
            yield return VerifyClassroomChoice();
            yield return new ExitPlayMode();
        }

        private static IEnumerator VerifyClassroomChoice()
        {
            var choice = Object.FindFirstObjectByType<StoryChoiceBranchStep>(FindObjectsInactive.Include);
            var view = choice.ChoiceView;
            view.transform.parent.gameObject.SetActive(true);
            // Reproduce the destroyed local UI reference after loading from the home scene.
            var data = new SerializedObject(view);
            data.FindProperty("inputGate").objectReferenceValue = null;
            data.ApplyModifiedPropertiesWithoutUndo();
            var gate = GameplayUIRoot.Instance.InputGate;
            Assert.AreEqual(0, gate.ActiveClaimCount);
            int callbacks = 0;
            Assert.IsTrue(choice.Play(_ => callbacks++));
            yield return null;
            Assert.IsTrue(view.IsShowing);
            Assert.AreEqual(1, gate.ActiveClaimCount);
            choice.Cancel();
            Assert.IsFalse(view.IsShowing);
            Assert.AreEqual(0, gate.ActiveClaimCount);
            Assert.AreEqual(1, callbacks);
            Assert.IsTrue(choice.Play(_ => callbacks++));
            yield return null;
            Assert.IsTrue(view.IsShowing);
            choice.gameObject.SetActive(false);
            Assert.IsFalse(view.IsShowing);
            Assert.AreEqual(0, gate.ActiveClaimCount);
            Assert.AreEqual(2, callbacks);
        }

        [UnityTest]
        public IEnumerator ParallelBranches_JoinCancelAndReplayWithoutLeaking()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            yield return new EnterPlayMode();
            yield return VerifyParallelBranches();
            yield return new ExitPlayMode();
        }

        // Keep callback closures after EnterPlayMode's domain reload.
        private static IEnumerator VerifyParallelBranches()
        {
            Application.runInBackground = true;
            GameObject root = new GameObject("Parallel Test");
            ParallelStoryStep parallel = root.AddComponent<ParallelStoryStep>();
            StoryEvent fast = WaitBranch(root.transform, "Fast", .02f);
            StoryEvent slow = WaitBranch(root.transform, "Slow", 1.5f);
            AssignBranches(parallel, fast, slow);
            int callbacks = 0;
            Assert.IsTrue(parallel.Play(_ => callbacks++));
            // Editor tests and player coroutines tick on different callbacks. Wait for
            // observed state instead of assuming a wall-clock delay ran a coroutine.
            yield return TickUntil(() => !fast.IsPlaying);
            Assert.IsTrue(slow.IsPlaying);
            Assert.IsTrue(parallel.IsRunning);
            yield return TickUntil(() => !parallel.IsRunning);
            Assert.AreEqual(StoryStepState.Completed, parallel.CurrentState);
            Assert.AreEqual(1, callbacks);
            parallel.Play(_ => callbacks++);
            yield return TickUntil(() => slow.IsPlaying);
            parallel.Cancel();
            Assert.IsFalse(fast.IsPlaying);
            Assert.IsFalse(slow.IsPlaying);
            Assert.AreEqual(StoryStepState.Cancelled, parallel.CurrentState);
            yield return TickFor(.1f);
            Assert.AreEqual(2, callbacks);
            parallel.Play(_ => callbacks++);
            yield return TickUntil(() => !parallel.IsRunning);
            Assert.AreEqual(StoryStepState.Completed, parallel.CurrentState);
            Assert.AreEqual(3, callbacks);
            parallel.Play(_ => callbacks++);
            yield return TickUntil(() => slow.IsPlaying);
            slow.Cancel();
            yield return TickUntil(() => !parallel.IsRunning);
            Assert.AreEqual(StoryStepState.Cancelled, parallel.CurrentState);
            Assert.AreEqual(4, callbacks);
            Assert.IsFalse(fast.IsPlaying);
            Object.Destroy(root);
        }

        private static IEnumerator TickUntil(System.Func<bool> predicate)
        {
            double deadline=EditorApplication.timeSinceStartup+8;
            while(!predicate() && EditorApplication.timeSinceStartup<deadline)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                yield return null;
            }
            Assert.IsTrue(predicate(),"Timed out waiting for the player coroutine state.");
        }

        [UnityTearDown]
        public IEnumerator LeavePlayModeAfterFailure()
        {
            if (EditorApplication.isPlaying) yield return new ExitPlayMode();
        }

        private static IEnumerator TickFor(float seconds)
        {
            float end = Time.unscaledTime + seconds;
            double deadline = EditorApplication.timeSinceStartup + 5;
            while (Time.unscaledTime < end && EditorApplication.timeSinceStartup < deadline)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                yield return null;
            }
        }

        private static StoryEvent WaitBranch(Transform parent, string name, float duration)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            StoryEvent branch = go.AddComponent<StoryEvent>();
            GameObject child = new GameObject("Wait");
            child.transform.SetParent(go.transform);
            WaitStep wait = child.AddComponent<WaitStep>();
            var so = new SerializedObject(wait);
            so.FindProperty("duration").floatValue = duration;
            so.FindProperty("useUnscaledTime").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            return branch;
        }
        private static void AssignBranches(ParallelStoryStep step, params StoryEvent[] branches)
        {
            var so = new SerializedObject(step);
            var property = so.FindProperty("branches");
            property.arraySize = branches.Length;
            for (int i = 0; i < branches.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = branches[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
