#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Audere.Audio;
using Audere.Core;
using Audere.Dialogue;
using Audere.Puzzle;
using Audere.Puzzle.Board;
using Audere.Puzzle.PathPieces;
using Audere.Story.Steps;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Audere.Story.Editor.Tests
{
    public sealed class Day4MorningTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void AuthoredMorning_NoTimorGuidesAndContinuousSharedPlayer()
        {
            var scene=EditorSceneManager.OpenScene(Day4MorningSetupTool.HomePath);
            var puzzles=All<PuzzleController>(scene);Assert.AreEqual(2,puzzles.Length);
            Assert.AreEqual(1,All<GridPlayer>(scene).Length);Assert.AreEqual(1,All<PuzzleRuntime>(scene).Length);
            Assert.AreEqual(0,All<StepTileTutorialGuide>(scene).Length);Assert.AreEqual(0,All<UseAllPiecesTutorialGuide>(scene).Length);
            var coordinator=All<PuzzleRootCoordinator>(scene).Single();Assert.IsTrue(coordinator.ValidateConfiguration(false));
            var wash=puzzles.Single(x=>x.PuzzleRoot.name=="PZ_D4_WASHROOM");var breakfast=puzzles.Single(x=>x.PuzzleRoot.name=="PZ_D4_BREAKFAST");
            Assert.IsTrue(wash.TryGetGoalAnchor(out var goal,false));Assert.Less(Vector3.Distance(goal.position,breakfast.Puzzle.PlayerStartTransform.position),.0001f);
            foreach(var p in puzzles)
            {
                Assert.IsNull(new SerializedObject(p.Puzzle).FindProperty("hud").objectReferenceValue);
                Assert.IsTrue(p.Puzzle.PuzzleData.RequireAllPathPieces);
                Assert.IsTrue(p.Puzzle.PuzzleData.AvailablePathPieces.All(x=>x!=null));
                p.Puzzle.Board.RegisterExistingTiles();
                Assert.AreEqual(p==wash?7:10,p.Puzzle.Board.GridPositions.Count);
                AssertNoArrows(p);
                p.Puzzle.Board.ResetSceneAuthoredState();AssertNoArrows(p);
            }
            var dialogue=All<DialogueStep>(scene).Single().DialogueData;
            Assert.AreEqual(DialogueCharacterId.Audere,dialogue.LeftCharacter);Assert.AreEqual(DialogueCharacterId.None,dialogue.RightCharacter);
            CollectionAssert.AreEqual(new[]{"Đánh răng… thay đồ… ăn sáng.","Ba việc thôi."},dialogue.Lines.Select(x=>x.Text));
            Assert.IsTrue(dialogue.Lines.All(x=>x.Speaker==DialogueSpeakerSide.Left));
            Assert.IsNotNull(dialogue.LeftPortraitOverride);
            foreach(var e in All<StoryEvent>(scene))foreach(Transform child in e.transform)Assert.AreEqual(1,child.GetComponents<StoryStep>().Length,child.name);
            foreach(var t in All<Transform>(scene))Assert.AreEqual(0,GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject),t.name);
        }

        [Test]
        public void SceneLinks_DayThreeToDayFourToNewClassroom()
        {
            var scene=EditorSceneManager.OpenScene("Assets/_Audere/Scenes/120_D3_School_Teacher.unity");
            var ending=All<StoryEvent>(scene).Single(x=>x.EventId=="D3_BIANCA_REPRISE_AND_SILENCE");
            Assert.AreEqual(GameScenes.Day4HomeMorning,new SerializedObject(ending.GetComponentsInChildren<SceneLoadStep>().Single()).FindProperty("sceneName").stringValue);
            Assert.IsInstanceOf<SceneLoadStep>(ending.transform.GetChild(ending.transform.childCount-1).GetComponent<StoryStep>());
            scene=EditorSceneManager.OpenScene(Day4MorningSetupTool.HomePath);
            Assert.AreEqual(GameScenes.Day4Classroom,new SerializedObject(All<SceneLoadStep>(scene).Single()).FindProperty("sceneName").stringValue);
            scene=EditorSceneManager.OpenScene(Day4MorningSetupTool.ClassroomPath);
            Assert.IsTrue(All<StoryEvent>(scene).Any(x=>x.EventId=="D4_CLASSROOM_CROWD"));Assert.IsTrue(All<DialogueStep>(scene).Length > 0);
            foreach(var path in new[]{Day4MorningSetupTool.HomePath,Day4MorningSetupTool.ClassroomPath})Assert.IsTrue(EditorBuildSettings.scenes.Any(x=>x.path==path&&x.enabled));
        }

        [UnityTest]
        public IEnumerator ProductionMorning_WrongGoalFallCancelRetryBothWinsAndClassroom()
        {
            EditorSceneManager.OpenScene(Day4MorningSetupTool.HomePath);
            yield return new EnterPlayMode();yield return null;
            yield return MorningChecks();
            yield return new ExitPlayMode();
        }

        private static IEnumerator MorningChecks()
        {
            EnsureServices();
            ExpectLoad(GameScenes.Day4Classroom);
            yield return Until(()=>GameplayUIRoot.Instance!=null&&GameplayUIRoot.Instance.Dialogue.IsPlaying);
            yield return new WaitForSecondsRealtime(1.75f);
            var right=(DialogueCharacterSlotView)typeof(DialogueController).GetField("rightSlot",Private).GetValue(GameplayUIRoot.Instance.Dialogue);
            var portrait=(UnityEngine.UI.Image)typeof(DialogueCharacterSlotView).GetField("characterImage",Private).GetValue(right);
            Assert.IsFalse(portrait.enabled);Assert.IsNull(portrait.sprite);
            System.IO.Directory.CreateDirectory("Temp/Day4");ScreenCapture.CaptureScreenshot("Temp/Day4/opening.png");
            yield return new WaitForSecondsRealtime(.25f);
            yield return Until(()=>ActivePuzzle()!=null);
            var scene=SceneManager.GetActiveScene();var director=All<StoryDirector>(scene).Single();var morning=director.CurrentEvent;
            var wash=ActivePuzzle();var player=wash.Puzzle.Player;var hand=GameplayUIRoot.Instance.PathPieceHand;
            Assert.AreEqual("PZ_D4_WASHROOM",wash.PuzzleRoot.name);Assert.AreEqual(3,hand.Count);
            ScreenCapture.CaptureScreenshot("Temp/Day4/washroom.png");yield return new WaitForSecondsRealtime(.25f);
            // A tempting short route reaches the toothbrush with pieces still in hand.
            Commit(wash.Puzzle,0,Path(0,0,1,0,1,1));
            yield return Until(()=>wash.Puzzle.CurrentState==PuzzleManager.State.Playing&&hand.Count==3);
            Assert.AreEqual(Vector2Int.zero,player.GridPosition);AssertSilent(wash);AssertNoArrows(wash);
            Commit(wash.Puzzle,2,Path(0,0,0,-1),true);
            yield return Until(()=>wash.Puzzle.CurrentState==PuzzleManager.State.Playing&&hand.Count==3);
            AssertSilent(wash);AssertNoArrows(wash);
            director.CancelCurrentEvent();yield return null;
            Assert.AreEqual(0,GameplayUIRoot.Instance.InputGate.ActiveClaimCount);Assert.IsFalse(GameplayUIRoot.Instance.Dialogue.IsPlaying);
            Assert.IsTrue(director.PlayEvent(morning));
            yield return Until(()=>wash.IsPlaying);
            Assert.IsTrue(player.gameObject.activeInHierarchy);
            Commit(wash.Puzzle,2,Path(0,0,0,1));yield return Settled(wash);
            Commit(wash.Puzzle,1,Path(0,1,0,0,1,0,2,0));yield return Settled(wash);
            Commit(wash.Puzzle,0,Path(2,0,2,1,1,1));
            // Monitor the real collapse/change-clothes/reveal hand-off for player blink.
            yield return Until(()=>ActivePuzzle()!=null&&ActivePuzzle()!=wash,()=>Assert.IsTrue(player.gameObject.activeInHierarchy));
            var breakfast=ActivePuzzle();Assert.AreSame(player,breakfast.Puzzle.Player);
            Assert.IsTrue(wash.TryGetGoalAnchor(out var goal,false));Assert.Less(Vector3.Distance(goal.position,breakfast.Puzzle.PlayerStartTransform.position),.0001f);
            AssertSilent(breakfast);AssertNoArrows(breakfast);Assert.AreEqual(4,hand.Count);
            ScreenCapture.CaptureScreenshot("Temp/Day4/breakfast.png");yield return new WaitForSecondsRealtime(.25f);
            // Authored breakfast local coordinates are translated by (-1,+1) in the shared grid.
            var delta=player.GridPosition-new Vector2Int(2,0);
            Commit(breakfast.Puzzle,2,Shift(Path(2,0,3,0,3,1),delta));yield return Settled(breakfast);
            Commit(breakfast.Puzzle,0,Shift(Path(3,1,3,2,2,2,1,2),delta));yield return Settled(breakfast);
            Commit(breakfast.Puzzle,0,Shift(Path(1,2,1,1,1,0),delta));yield return Settled(breakfast);
            Commit(breakfast.Puzzle,0,Shift(Path(1,0,1,1),delta));
            yield return Until(()=>SceneManager.GetActiveScene().name==GameScenes.Day4Classroom);
            yield return Until(()=>GameplayUIRoot.Instance.Dialogue.IsPlaying);
            All<StoryDirector>(SceneManager.GetActiveScene()).Single().CancelCurrentEvent();yield return null;
            Assert.AreEqual(0,GameplayUIRoot.Instance.InputGate.ActiveClaimCount);Assert.IsFalse(GameplayUIRoot.Instance.Dialogue.IsPlaying);
            Assert.AreEqual(0,GameplayUIRoot.Instance.PathPieceHand.Count);
            Assert.IsFalse(ActivePuzzle()!=null);
            var cover=All<CanvasGroup>(SceneManager.GetActiveScene()).Single(x=>x.name=="Fade"&&x.transform.parent!=null&&x.transform.parent.name=="Scene Transition Overlay");Assert.AreEqual(0,cover.alpha);
            ScreenCapture.CaptureScreenshot("Temp/Day4/classroom.png");yield return new WaitForSecondsRealtime(.25f);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator DayThreeTitle_LoadsDayFourThroughSceneFlow()
        {
            var scene=EditorSceneManager.OpenScene("Assets/_Audere/Scenes/120_D3_School_Teacher.unity");
            var so=new SerializedObject(All<StoryDirector>(scene).Single());so.FindProperty("playOnStart").boolValue=false;so.ApplyModifiedPropertiesWithoutUndo();
            yield return new EnterPlayMode();yield return null;
            yield return DayThreeChecks();yield return new ExitPlayMode();
        }
        private static IEnumerator DayThreeChecks()
        {
            EnsureServices(); ExpectLoad(GameScenes.Day4HomeMorning);
            var scene=SceneManager.GetActiveScene();var e=All<StoryEvent>(scene).Single(x=>x.EventId=="D3_BIANCA_REPRISE_AND_SILENCE");
            foreach(Transform step in e.transform)if(int.Parse(step.name.Split('_')[0])<215)step.gameObject.SetActive(false);
            var title=scene.GetRootGameObjects().Single(x=>x.name=="DAY THREE END TITLE").GetComponent<CanvasGroup>();
            var cover=scene.GetRootGameObjects().Single(x=>x.name=="DAY THREE STORY COVER").GetComponentInChildren<CanvasGroup>();
            yield return new WaitForSecondsRealtime(1f);
            title.alpha=1f; // A replay must not leave the previous ending title visible over its fade.
            Assert.IsTrue(All<StoryDirector>(scene).Single().PlayEvent(e));
            yield return Until(()=>cover.alpha>.25f&&cover.alpha<.95f);
            Assert.AreEqual(0f,title.alpha);ScreenCapture.CaptureScreenshot("Temp/Day4/day3-fading.png");
            yield return Until(()=>e.CurrentStep!=null&&e.CurrentStep.name=="225_HoldBlackBeforeTitle");
            Assert.AreEqual(1f,cover.alpha);Assert.AreEqual(0f,title.alpha);
            ScreenCapture.CaptureScreenshot("Temp/Day4/day3-black-before-title.png");
            yield return new WaitForSecondsRealtime(.12f);
            Assert.AreEqual(1f,cover.alpha);Assert.AreEqual(0f,title.alpha);
            yield return Until(()=>e.CurrentStep!=null&&e.CurrentStep.name=="230_DayThreeEnds");
            yield return new WaitForSecondsRealtime(.3f);
            Assert.AreEqual(1f,cover.alpha);Assert.Greater(title.alpha,0f);
            ScreenCapture.CaptureScreenshot("Temp/Day4/day3-title-after-black.png");
            yield return Until(()=>SceneManager.GetActiveScene().name==GameScenes.Day4HomeMorning);
            yield return Until(()=>ActivePuzzle()!=null);
            Assert.AreEqual("PZ_D4_WASHROOM",ActivePuzzle().PuzzleRoot.name);AssertSilent(ActivePuzzle());LogAssert.NoUnexpectedReceived();
        }

        private static void ExpectLoad(string name) { LogAssert.Expect(LogType.Log,"[SceneFlow] Loading '"+name+"'..."); LogAssert.Expect(LogType.Log,"[SceneFlow] Loaded '"+name+"'."); }
        private static void EnsureServices()
        {
            Application.runInBackground=true;
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
            if(SceneFlow.Instance!=null)return;
            var flow=new GameObject("TEST Day4 Services").AddComponent<SceneFlow>();flow.Initialize();Object.DontDestroyOnLoad(flow.gameObject);
            var audio=flow.gameObject.AddComponent<AudioService>();
            typeof(AudioService).GetField("catalog",Private).SetValue(audio,AssetDatabase.LoadAssetAtPath<AudioCatalog>("Assets/_Audere/Data/Audio/AudioCatalog.asset"));audio.Initialize();
        }
        private static void AssertSilent(PuzzleController p)
        {
            Assert.IsNull(typeof(PuzzleManager).GetField("hud",Private).GetValue(p.Puzzle));
            foreach(var hud in Object.FindObjectsByType<PuzzleHud>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                Assert.IsTrue(!hud.gameObject.activeInHierarchy||hud.GetComponentsInChildren<TMPro.TMP_Text>(true).All(t=>!t.gameObject.activeInHierarchy||string.IsNullOrEmpty(t.text)));
        }
        private static void AssertNoArrows(PuzzleController p)
        {
            foreach(var goal in p.PuzzleRoot.GetComponentsInChildren<GoalTileBehaviour>(true))Assert.IsFalse(goal.transform.Find("Visual Root/Goal Visual").gameObject.activeSelf);
        }
        private static PuzzleController ActivePuzzle()=>Object.FindObjectsByType<PuzzleController>(FindObjectsSortMode.None).FirstOrDefault(x=>x.IsPlaying&&x.Puzzle.CurrentState==PuzzleManager.State.Playing);
        private static IEnumerator Settled(PuzzleController p){yield return Until(()=>p.Puzzle.CurrentState==PuzzleManager.State.Playing&&!p.Puzzle.Player.IsMoving);AssertSilent(p);}
        private static Vector2Int[] Path(params int[] xy)=>Enumerable.Range(0,xy.Length/2).Select(i=>new Vector2Int(xy[i*2],xy[i*2+1])).ToArray();
        private static Vector2Int[] Shift(Vector2Int[] p,Vector2Int d)=>p.Select(x=>x+d).ToArray();
        private static void Commit(PuzzleManager p,int card,Vector2Int[] wanted,bool fall=false)
        {
            var placement=p.Board.GridSpace.GetComponentInChildren<PuzzleRuntime>(true).Placement;
            GameplayUIRoot.Instance.PathPieceHand.Select(card);
            for(int r=0;r<4;r++)
            {
                typeof(PathPlacementController).GetField("rotation",Private).SetValue(placement,(GridRotation)r);
                for(int x=wanted.Min(v=>v.x)-1;x<=wanted.Max(v=>v.x)+1;x++)for(int y=wanted.Min(v=>v.y)-1;y<=wanted.Max(v=>v.y)+1;y++)
                {
                    typeof(PathPlacementController).GetField("hasAnchoredOrigin",Private).SetValue(placement,false);
                    if(!placement.TryMovePreviewToScreenPosition(Camera.main.WorldToScreenPoint(p.Board.GridSpace.CellToWorldCenter(new Vector2Int(x,y)))))continue;
                    var result=(PlacementResult)typeof(PathPlacementController).GetField("currentResult",Private).GetValue(placement);
                    if(!result.CanCommit||result.WillFall!=fall||!result.GridPath.SequenceEqual(wanted))continue;
                    Assert.IsTrue(placement.TryCommitPreview());return;
                }
            }
            Assert.Fail("No real preview/drop for "+string.Join(" -> ",wanted.Select(x=>x.ToString())));
        }
        private static IEnumerator Until(Func<bool> ready,Action check=null)
        {
            double deadline=EditorApplication.timeSinceStartup+35;
            while(!ready()&&EditorApplication.timeSinceStartup<deadline)
            {
                check?.Invoke();var d=GameplayUIRoot.Instance!=null?GameplayUIRoot.Instance.Dialogue:null;
                if(d!=null&&d.IsPlaying)typeof(DialogueController).GetMethod("EndPlayback",Private).Invoke(d,new object[]{DialogueResult.Completed,true});
                EditorApplication.QueuePlayerLoopUpdate();yield return null;
            }
            Assert.IsTrue(ready(),"Timed out in "+SceneManager.GetActiveScene().name);
        }
        private static T[] All<T>(Scene s)where T:Component=>s.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<T>(true)).ToArray();
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if(EditorApplication.isPlaying)yield return new ExitPlayMode();

            // DayThreeTitle_LoadsDayFourThroughSceneFlow temporarily disables Scene120's
            // production director. Restore and persist the production startup contract so a
            // later Save All or player build cannot ship the test override.
            var teacherScene=EditorSceneManager.OpenScene("Assets/_Audere/Scenes/120_D3_School_Teacher.unity");
            var teacherDirector=All<StoryDirector>(teacherScene).Single();
            var serialized=new SerializedObject(teacherDirector);
            serialized.FindProperty("playOnStart").boolValue=true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(teacherScene);

            EditorSceneManager.OpenScene(Day4MorningSetupTool.HomePath);
        }
    }
}
#endif
