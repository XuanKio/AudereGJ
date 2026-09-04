#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Audere.Core;
using Audere.Dialogue;
using Audere.EditorTools;
using Audere.Puzzle;
using Audere.Puzzle.Board;
using Audere.Puzzle.PathPieces;
using Audere.Story.Presentation;
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
    public sealed class Day2NightDreamTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void Scenes_KeepFifteenStepsOnePlayerAndExactHandoffs()
        {
            var s = EditorSceneManager.OpenScene(Day2NightDreamSetupTool.DreamPath);
            var levels = All<PuzzleController>(s).OrderBy(x => x.PuzzleRoot.name).ToArray();
            Assert.AreEqual(5, levels.Length);
            Assert.AreEqual(1, All<GridPlayer>(s).Length);
            Assert.AreEqual(1, All<PuzzleRuntime>(s).Length);
            int total = 0;
            for (int i = 0; i < levels.Length; i++)
            {
                var p = levels[i].Puzzle;
                p.Board.RegisterExistingTiles();
                Assert.AreEqual(4, p.Board.GridPositions.Count);
                Assert.AreEqual(3, p.PuzzleData.AvailablePathPieces.Count);
                Assert.IsTrue(p.PuzzleData.RequireAllPathPieces);
                Assert.IsTrue(p.Board.TryGetLevelGoal(out BoardTile goal));
                Assert.AreEqual(new Vector2Int(i * 3 + 3, 0), goal.GridPosition);
                total += p.PuzzleData.AvailablePathPieces.Sum(x => x.OrderedLocalPath.Count - 1);
                if (i < levels.Length - 1)
                    Assert.Less(Vector3.Distance(goal.transform.position, levels[i + 1].Puzzle.PlayerStartTransform.position), .00001f);
            }
            Assert.AreEqual(15, total);
            Assert.Greater(All<TMPro.TextMeshPro>(s).Select(t => t.transform.position).Distinct().Count(), 15,
                "Murmurs must remain spread across the background after scene serialization.");
            var decor = s.GetRootGameObjects().Single(r => r.name == "WORLD").transform.Find("Floating Tiles PLACEHOLDER - NOT WALKABLE");
            Assert.AreEqual(0, decor.GetComponentsInChildren<BoardTile>(true).Length);
            Assert.AreEqual(0, decor.GetComponentsInChildren<Collider2D>(true).Length);
            foreach (string path in new[] { Day2NightDreamSetupTool.HomePath, Day2NightDreamSetupTool.DreamPath, Day2NightDreamSetupTool.WakePath })
            {
                s = EditorSceneManager.OpenScene(path);
                var cover = s.GetRootGameObjects().Single(r => r.name == "Scene Transition Overlay").GetComponentInChildren<CanvasGroup>(true);
                Assert.IsTrue(cover.gameObject.activeInHierarchy, "Destination must start covered, not just hold an inactive alpha=1 group.");
                foreach (var t in All<Transform>(s)) Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
                foreach (var e in All<StoryEvent>(s))
                    foreach (Transform child in e.transform) Assert.AreEqual(1, child.GetComponents<StoryStep>().Length, child.name);
                foreach (var d in All<DialogueStep>(s))
                {
                    Assert.AreEqual(DialogueCharacterId.Audere, d.DialogueData.LeftCharacter);
                    foreach (var line in d.DialogueData.Lines) Assert.LessOrEqual(line.Text.Length, 42, line.Text);
                }
                Assert.IsTrue(EditorBuildSettings.scenes.Any(x => x.path == path && x.enabled));
            }
        }

        [Test]
        public void RerunAuthor_PreservesExistingAssetsAndSchoolContent()
        {
            var paths = new[] { Day2SchoolMorningSetupTool.ScenePath, Day2NightDreamSetupTool.HomePath,
                Day2NightDreamSetupTool.DreamPath, Day2NightDreamSetupTool.WakePath };
            var before = paths.Select(System.IO.File.ReadAllText).ToArray();
            Day2NightDreamSetupTool.Author();
            CollectionAssert.AreEqual(before, paths.Select(System.IO.File.ReadAllText).ToArray());
        }

        [Test]
        public void SchoolClosure_PreservesEarlierDialogueAndUsesSharedBellBeforeFade()
        {
            var s = EditorSceneManager.OpenScene(Day2SchoolMorningSetupTool.ScenePath);
            var e = All<StoryEvent>(s).Single(x => x.EventId == "D2_SCHOOL_WRONG_SUPPLIES");
            string[] tail = e.GetComponentsInChildren<StoryStep>(true).Select(x => x.name).SkipWhile(n => n != "345_PreparationsAreDone").ToArray();
            CollectionAssert.AreEqual(new[] { "345_PreparationsAreDone", "346_LetTomorrowStaySmall", "347_BiancaSaysGoodbye",
                "348_BiancaTurnsToLeave", "348_EnableDepartureCover", "349_SchoolBell", "350_FadeOutAfterAnswer", "360_GoHomeAfterBell" }, tail);
            var bell = e.transform.Find("349_SchoolBell").GetComponent<PlayAudioStep>();
            Assert.AreEqual((int)Audere.Audio.AudioId.School_Bell, new SerializedObject(bell).FindProperty("audioId").intValue);
            var catalog = AssetDatabase.LoadAssetAtPath<Audere.Audio.AudioCatalog>("Assets/_Audere/Data/Audio/AudioCatalog.asset");
            Assert.IsTrue(catalog.TryGet(Audere.Audio.AudioId.School_Bell, out var sound));
            Assert.AreEqual("Assets/_Audere/Audio/SchoolBell.mp3", AssetDatabase.GetAssetPath(sound.clip));
            Assert.IsNotNull(e.transform.Find("330_AudereQuestionsTimor"));
        }

        [UnityTest]
        public IEnumerator HomeDreamAndAwakening_ProductionFlowFifteenRealDrops()
        {
            EditorSceneManager.OpenScene(Day2NightDreamSetupTool.HomePath);
            yield return new EnterPlayMode();
            yield return RunProduction();
            yield return new ExitPlayMode();
        }

        private static IEnumerator RunProduction()
        {
            Application.runInBackground = true;
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
            // SceneFlow is the same service used by Bootstrap; no scene-loading shortcut in story code.
            var flow = new GameObject("TEST SceneFlow").AddComponent<SceneFlow>();
            flow.Initialize();
            Object.DontDestroyOnLoad(flow.gameObject);
            foreach (string scene in new[] { GameScenes.Day2Dream, GameScenes.Day2HomeAwakening })
            {
                LogAssert.Expect(LogType.Log, "[SceneFlow] Loading '" + scene + "'...");
                LogAssert.Expect(LogType.Log, "[SceneFlow] Loaded '" + scene + "'.");
            }
            System.IO.Directory.CreateDirectory("Temp/Day2NightDreamQA");
            yield return Until(() => GameplayUIRoot.Instance.Dialogue.IsPlaying, false);
            double portraitSettle = EditorApplication.timeSinceStartup + .8;
            yield return Until(() => EditorApplication.timeSinceStartup >= portraitSettle, false);
            ScreenCapture.CaptureScreenshot("Temp/Day2NightDreamQA/home.png");
            yield return Until(() => SceneManager.GetActiveScene().name == GameScenes.Day2Dream, true, 40);
            var s = SceneManager.GetActiveScene();
            var director = All<StoryDirector>(s).Single();
            var e = All<StoryEvent>(s).Single();
            var levels = All<PuzzleController>(s).OrderBy(x => x.PuzzleRoot.name).ToArray();
            var actor = All<GridPlayer>(s).Single();
            int actorId = actor.GetInstanceID();
            for (int segment = 0; segment < 5; segment++)
            {
                var p = levels[segment].Puzzle;
                yield return Until(() => levels[segment].IsPlaying && p.CurrentState == PuzzleManager.State.Playing);
                Assert.AreEqual(segment * 3, actor.GridPosition.x);
                Assert.AreEqual(3, GameplayUIRoot.Instance.PathPieceHand.Count);
                if (segment == 0) ScreenCapture.CaptureScreenshot("Temp/Day2NightDreamQA/dream-start.png");
                for (int cell = 0; cell < 3; cell++)
                {
                    CommitRight(p);
                    int x = segment * 3 + cell + 1;
                    yield return Until(() => !actor.IsMoving && actor.GridPosition.x == x);
                    Assert.IsTrue(actor.gameObject.activeInHierarchy);
                    Assert.AreEqual(actorId, actor.GetInstanceID());
                    Assert.Less(Mathf.Abs(actor.GetComponent<SpriteRenderer>().bounds.min.y - p.Board.GridSpace.CellToWorldCenter(actor.GridPosition).y), .002f);
                    if (cell < 2) yield return Until(() => p.CurrentState == PuzzleManager.State.Playing);
                }
            }
            Assert.AreEqual(15, actor.GridPosition.x);
            yield return Until(() => e.CurrentStep != null && e.CurrentStep.name == "270_TimorCallsFromTheVoid", false);
            var atmosphere = All<DreamAtmosphereView>(s).Single();
            Assert.AreEqual(1f, atmosphere.Chaos);
            var path = (SpriteRenderer[])typeof(DreamAtmosphereView).GetField("pathRenderers", Private).GetValue(atmosphere);
            Assert.IsTrue(path.All(r => r.color.a == 0f));
            Assert.IsFalse(GameplayUIRoot.Instance.PuzzleUi.gameObject.activeSelf);
            ScreenCapture.CaptureScreenshot("Temp/Day2NightDreamQA/dream-collapse.png");
            yield return Until(() => SceneManager.GetActiveScene().name == GameScenes.Day2HomeAwakening, true, 25);
            s = SceneManager.GetActiveScene();
            e = All<StoryEvent>(s).Single();
            actor = All<GridPlayer>(s).Single();
            var motion = All<CharacterMotionStep>(s).Single();
            Vector3 start = actor.transform.position;
            Vector3 shadow = motion.GroundedShadow.position;
            Vector3 shadowScale = motion.GroundedShadow.lossyScale;
            float highest = start.y;
            yield return Until(() => motion.IsRunning, false);
            while (motion.IsRunning)
            {
                highest = Mathf.Max(highest, actor.transform.position.y);
                Assert.AreEqual(start.x, actor.transform.position.x, .00001f);
                Assert.AreEqual(start.z, actor.transform.position.z, .00001f);
                Assert.Less(Vector3.Distance(shadow, motion.GroundedShadow.position), .0001f);
                Assert.Less(Vector3.Distance(shadowScale, motion.GroundedShadow.lossyScale), .0001f);
                EditorApplication.QueuePlayerLoopUpdate();
                yield return null;
            }
            Assert.Greater(highest, start.y + .025f);
            Assert.AreEqual(start.y, actor.transform.position.y, .0001f);
            yield return Until(() => e.CurrentStep is StoryTitleCardStep, false);
            Assert.AreEqual(0, GameplayUIRoot.Instance.InputGate.ActiveClaimCount);
            ScreenCapture.CaptureScreenshot("Temp/Day2NightDreamQA/awake.png");
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator DreamCancelAndReplay_ClearsMotionInputAndEnvironment()
        {
            EditorSceneManager.OpenScene(Day2NightDreamSetupTool.DreamPath);
            yield return new EnterPlayMode();
            yield return CancelReplay();
            yield return new ExitPlayMode();
        }

        private static IEnumerator CancelReplay()
        {
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
            var s = SceneManager.GetActiveScene();
            var director = All<StoryDirector>(s).Single();
            var e = All<StoryEvent>(s).Single();
            var atmosphere = All<DreamAtmosphereView>(s).Single();
            yield return Until(() => e.CurrentStep is PuzzleStep);
            var p = ((PuzzleStep)e.CurrentStep).PuzzleController.Puzzle;
            CommitRight(p);
            yield return null;
            director.CancelCurrentEvent();
            yield return null;
            Assert.IsFalse(p.Player.IsMoving);
            Assert.IsFalse(atmosphere.IsRunning);
            Assert.AreEqual(0, GameplayUIRoot.Instance.InputGate.ActiveClaimCount);
            Assert.IsTrue(director.PlayEvent(e));
            yield return Until(() => e.CurrentStep is PuzzleStep);
            Assert.AreEqual(0, p.Player.GridPosition.x);
            Assert.AreEqual(3, GameplayUIRoot.Instance.PathPieceHand.Count);
            Assert.AreEqual(0f, atmosphere.Chaos);
            director.CancelCurrentEvent();
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }


        [Test]
        public void DreamOpening_KeepsOrdinaryConversationAndStableForegroundDepth()
        {
            var s = EditorSceneManager.OpenScene(Day2NightDreamSetupTool.DreamPath);
            var world = s.GetRootGameObjects().Single(x => x.name == "WORLD").transform;
            var normal = world.Find("Ordinary Classroom - Dream Opening");
            var props = world.Find("Floating Desks - NOT WALKABLE");
            Assert.IsTrue(normal.gameObject.activeSelf);
            Assert.IsFalse(props.gameObject.activeSelf);
            Assert.AreEqual(10, props.childCount);
            Assert.IsEmpty(props.GetComponentsInChildren<Collider2D>(true));
            Assert.IsEmpty(props.GetComponentsInChildren<BoardTile>(true));
            var actor = All<GridPlayer>(s).Single();
            int order = actor.GetComponent<UnityEngine.Rendering.SortingGroup>().sortingOrder;
            foreach (Transform prop in props)
            {
                var group = prop.GetComponent<UnityEngine.Rendering.SortingGroup>();
                Assert.AreEqual("Player", group.sortingLayerName);
                if (prop.name.StartsWith("Foreground")) Assert.Greater(group.sortingOrder, order);
                else Assert.Less(group.sortingOrder, order);
                Assert.AreEqual("Assets/_Audere/AssetGame/Item/ban.aseprite",
                    AssetDatabase.GetAssetPath(prop.GetComponent<SpriteRenderer>().sprite));
            }
            Assert.AreEqual(5, actor.GetComponent<SpriteRenderer>().sortingOrder);
            Assert.IsTrue(actor.GetComponentsInChildren<SpriteRenderer>().Any(x => x.sortingOrder == 4));
            var opening = All<DialogueStep>(s).Single(x => x.name == "016_BiancaOrdinaryConversation").DialogueData;
            Assert.AreEqual(DialogueCharacterId.Bianca, opening.RightCharacter);
            Assert.IsTrue(opening.Lines.All(x => x.Text.Length <= 42));
            var transition = All<FullscreenPresentationStep>(s).Single();
            var profile = (Audere.World.FullscreenTransitionProfile)new SerializedObject(transition).FindProperty("profile").objectReferenceValue;
            Assert.IsTrue(profile.Validate(out string error), error);
            Assert.IsFalse(ShaderUtil.ShaderHasError(profile.Material.shader));
            Assert.AreEqual(0f, profile.FloatTracks.Single(x => x.ShaderProperty == "_Cover").Values.Evaluate(profile.ModeSwapTime),
                "The dream must appear directly behind the falling glass, without a black interlude.");
            Assert.Less(transition.transform.GetSiblingIndex(),
                All<PuzzleStep>(s).First().transform.GetSiblingIndex());
        }

        [UnityTest]
        public IEnumerator DreamFracture_CancelOnBothSidesOfSwapAndReplay()
        {
            EditorSceneManager.OpenScene(Day2NightDreamSetupTool.DreamPath);
            yield return new EnterPlayMode();
            yield return FractureLifecycle();
            yield return new ExitPlayMode();
        }

        private static IEnumerator FractureLifecycle()
        {
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
            var s = SceneManager.GetActiveScene();
            var e = All<StoryEvent>(s).Single();
            var director = All<StoryDirector>(s).Single();
            var controller = All<Audere.World.FullscreenTransitionController>(s).Single();
            var world = s.GetRootGameObjects().Single(x => x.name == "WORLD").transform;
            var normal = world.Find("Ordinary Classroom - Dream Opening").gameObject;
            var props = world.Find("Floating Desks - NOT WALKABLE").gameObject;
            var runtimeField = typeof(Audere.World.FullscreenTransitionController).GetField("runtimeMaterial", Private);
            for (int pass = 0; pass < 3; pass++)
            {
                yield return Until(() => controller.IsTransitioning);
                Assert.IsFalse(GameplayUIRoot.Instance.PuzzleUi.gameObject.activeSelf);
                Texture captured = null;
                if (pass == 1)
                {
                    yield return Until(() => controller.ShatterView != null, false);
                    var source = (Texture2D)controller.ShatterView.mainTexture;
                    Texture2D presented = null;
                    controller.StartCoroutine(CaptureFrame(texture => presented = texture));
                    yield return Until(() => presented != null, false);
                    try
                    {
                        var a = source.GetPixel(source.width / 13, source.height / 17);
                        var b = presented.GetPixel(presented.width / 13, presented.height / 17);
                        Assert.Less(Vector3.Distance(new Vector3(a.r, a.g, a.b), new Vector3(b.r, b.g, b.b)), .035f,
                            "Frozen frame must retain source brightness, not receive a second gamma conversion.");
                    }
                    finally { Object.Destroy(presented); }
                    yield return Until(() => controller.ShatterView != null && controller.ShatterView.FlightTime > .4f, false);
                    captured = controller.ShatterView.mainTexture;
                    Canvas.ForceUpdateCanvases();
                    Assert.IsNotNull(controller.ShatterView.canvasRenderer);
                    Assert.Greater(controller.ShatterView.canvasRenderer.GetMesh().vertexCount, 100, "The actual Canvas renderer must contain shard geometry.");
                    Assert.Greater(controller.ShatterView.PieceCount, 30);
                    Assert.IsFalse(controller.ShatterView.raycastTarget);
                    Assert.IsFalse(GameplayUIRoot.Instance.PuzzleUi.gameObject.activeSelf);
                }
                if (pass == 2) yield return Until(() => props.activeSelf, false);
                Assert.AreEqual(pass == 0, normal.activeSelf);
                Assert.AreEqual(pass > 0, props.activeSelf);
                if (pass == 2)
                {
                    var material = (Material)runtimeField.GetValue(controller);
                    Assert.AreEqual(0f, material.GetFloat("_Cover"), .001f, "The puzzle must be visible behind falling shards.");
                    Assert.IsNotNull(controller.ShatterView, "The source snapshot conceals the scenery swap before its pieces separate.");
                }
                director.CancelCurrentEvent();
                yield return null;
                Assert.IsFalse(controller.IsTransitioning);
                Assert.IsFalse(controller.RendererFeature.isActive);
                Assert.IsNull(runtimeField.GetValue(controller));
                Assert.IsNull(controller.ShatterView);
                Assert.IsTrue(captured == null, "Cancel must destroy the captured texture.");
                Assert.IsNull(typeof(Audere.World.FullscreenTransitionController).GetField("frozenFrame", Private).GetValue(controller));
                Assert.IsFalse(Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Any(x => x.name == "Fullscreen Shatter (Runtime)"));
                Assert.IsTrue(normal.activeSelf);
                Assert.IsFalse(props.activeSelf);
                Assert.AreEqual(0, GameplayUIRoot.Instance.InputGate.ActiveClaimCount);
                Assert.IsTrue(director.PlayEvent(e));
            }
            yield return Until(() => e.CurrentStep is PuzzleStep);
            Assert.IsFalse(normal.activeSelf);
            Assert.IsTrue(props.activeSelf);
            Assert.IsFalse(controller.RendererFeature.isActive);
            Assert.IsNull(controller.ShatterView);
            var atmosphere = All<DreamAtmosphereView>(s).Single();
            var renders = props.GetComponentsInChildren<SpriteRenderer>(true);
            var initialPositions = (Vector3[])typeof(DreamAtmosphereView).GetField("tilePositions", Private).GetValue(atmosphere);
            var floating = (SpriteRenderer[])typeof(DreamAtmosphereView).GetField("floatingTiles", Private).GetValue(atmosphere);
            var orders = renders.Select(x => x.GetComponent<UnityEngine.Rendering.SortingGroup>().sortingOrder).ToArray();
            var before = renders.Select(x => x.transform.localPosition).ToArray();
            double end = EditorApplication.timeSinceStartup + .9;
            yield return Until(() => EditorApplication.timeSinceStartup >= end, false);
            Assert.IsTrue(renders.Where((x,i) => Vector3.Distance(x.transform.localPosition,before[i]) > .002f).Any());
            CollectionAssert.AreEqual(orders,renders.Select(x => x.GetComponent<UnityEngine.Rendering.SortingGroup>().sortingOrder));
            director.CancelCurrentEvent();
            yield return null;
            Assert.IsFalse(atmosphere.IsRunning);
            for (int i = 0; i < floating.Length; i++)
                Assert.Less(Vector3.Distance(initialPositions[i], floating[i].transform.localPosition), .00001f);
            Assert.AreEqual(0, GameplayUIRoot.Instance.InputGate.ActiveClaimCount);
            LogAssert.NoUnexpectedReceived();
        }

        [TestCase(160, 90)]
        [TestCase(120, 90)]
        [TestCase(210, 90)]
        public void DreamShards_CoverSourceMoveBoundariesAndClear(int width, int height)
        {
            var profile = AssetDatabase.LoadAssetAtPath<Audere.World.FullscreenTransitionProfile>(
                "Assets/_Audere/Data/Transitions/WorldTransition_DreamFracture.asset");
            var texture = new Texture2D(width, height);
            var go = new GameObject("Shatter geometry test", typeof(RectTransform), typeof(Audere.World.ScreenShatterGraphic));
            var mesh = new Mesh();
            try
            {
                var graphic = go.GetComponent<Audere.World.ScreenShatterGraphic>();
                graphic.rectTransform.sizeDelta = new Vector2(width, height);
                graphic.Initialize(texture, profile.ScreenShatter);
                float area = 0f;
                var shards = (IEnumerable)typeof(Audere.World.ScreenShatterGraphic).GetField("shards", Private).GetValue(graphic);
                foreach (var shard in shards)
                {
                    var points = (Vector2[])shard.GetType().GetField("points").GetValue(shard);
                    var reveal = (Vector2[])shard.GetType().GetField("edgeTimes").GetValue(shard);
                    var primary = (bool[])shard.GetType().GetField("primaryEdges").GetValue(shard);
                    for (int edge = 0; edge < reveal.Length; edge++)
                    {
                        if (!primary[edge]) Assert.GreaterOrEqual(Mathf.Min(reveal[edge].x, reveal[edge].y), .52f, "Branches must wait until inward cracks reach the center.");
                        if (Mathf.Min(reveal[edge].x, reveal[edge].y) > .001f) continue;
                        Vector2 origin = reveal[edge].x < reveal[edge].y ? points[edge] : points[(edge + 1) % points.Length];
                        Assert.IsTrue(origin.x < .0001f || origin.y < .0001f || Mathf.Abs(origin.x - (float)width / height) < .0001f || origin.y > .9999f, "The first crack must start at the screen edge.");
                    }
                    float polygonArea = 0f;
                    for (int i = 0; i < points.Length; i++)
                    {
                        var a = points[i]; var b = points[(i + 1) % points.Length];
                        Assert.That(a.x, Is.InRange(-.0001f, (float)width / height + .0001f));
                        Assert.That(a.y, Is.InRange(-.0001f, 1.0001f));
                        polygonArea += a.x * b.y - b.x * a.y;
                    }
                    area += Mathf.Abs(polygonArea) * .5f;
                }
                Assert.AreEqual((float)width / height, area, .0001f, "Clipped shards must tile the source without missing area.");
                var populate = typeof(Audere.World.ScreenShatterGraphic).GetMethod("OnPopulateMesh", Private, null, new[] { typeof(UnityEngine.UI.VertexHelper) }, null);
                using (var helper = new UnityEngine.UI.VertexHelper())
                {
                    graphic.SetTime(profile.ScreenShatter.CaptureTime);
                    populate.Invoke(graphic, new object[] { helper });
                    helper.FillMesh(mesh);
                    Assert.Greater(mesh.vertexCount, 100);
                    Assert.Less(mesh.vertexCount, 65000);
                    var original = mesh.bounds;
                    graphic.SetTime(profile.ScreenShatter.BreakTime + .7f);
                    populate.Invoke(graphic, new object[] { helper });
                    helper.FillMesh(mesh);
                    Assert.Greater(Vector3.Distance(original.min, mesh.bounds.min), height * .1f,
                        "Actual shard vertices must move, not just their texture sampling.");
                    Assert.AreSame(texture, graphic.mainTexture, "One frozen source is retained throughout flight.");
                    graphic.SetTime(profile.ScreenShatter.ClearTime);
                    populate.Invoke(graphic, new object[] { helper });
                    Assert.AreEqual(0, helper.currentVertCount);
                }
                Assert.IsFalse(ShaderUtil.ShaderHasError(profile.ScreenShatter.ShardMaterial.shader));
            }
            finally { Object.DestroyImmediate(go); Object.DestroyImmediate(texture); Object.DestroyImmediate(mesh); }
        }

        private static IEnumerator CaptureFrame(Action<Texture2D> captured)
        {
            // EditMode UnityTest enumerators run on Editor update, not the camera's
            // end-of-frame phase. Use a real MonoBehaviour coroutine for this GPU readback.
            yield return new WaitForEndOfFrame();
            captured(ScreenCapture.CaptureScreenshotAsTexture());
        }

        private static void CommitRight(PuzzleManager p)
        {
            var placement = p.Board.GridSpace.GetComponentInChildren<PuzzleRuntime>(true).Placement;
            GameplayUIRoot.Instance.PathPieceHand.Select(0);
            typeof(PathPlacementController).GetField("rotation", Private).SetValue(placement, GridRotation.Degrees0);
            typeof(PathPlacementController).GetField("hasAnchoredOrigin", Private).SetValue(placement, false);
            // Pointer is snapped to a cell by the production solver. Aim inside the destination,
            // not at an exact half-cell where floating-point rounding can choose the previous cell.
            var pointer = p.Board.GridSpace.CellToWorldCenter(p.Player.GridPosition + Vector2Int.right);
            Assert.IsTrue(placement.TryMovePreviewToScreenPosition(Camera.main.WorldToScreenPoint(pointer)));
            var result = (PlacementResult)typeof(PathPlacementController).GetField("currentResult", Private).GetValue(placement);
            Assert.IsFalse(result.WillFall);
            Assert.AreEqual(p.Player.GridPosition + Vector2Int.right, result.GridPath.Last());
            Assert.IsTrue(placement.TryCommitPreview());
        }

        private static IEnumerator Until(Func<bool> ready, bool advanceDialogue = true, float timeout = 20f)
        {
            double deadline = EditorApplication.timeSinceStartup + timeout;
            while (!ready() && EditorApplication.timeSinceStartup < deadline)
            {
                var d = GameplayUIRoot.Instance != null ? GameplayUIRoot.Instance.Dialogue : null;
                if (advanceDialogue && d != null && d.IsPlaying)
                    typeof(DialogueController).GetMethod("EndPlayback", Private).Invoke(d, new object[] { DialogueResult.Completed, true });
                EditorApplication.QueuePlayerLoopUpdate();
                yield return null;
            }
            Assert.IsTrue(ready(), "Timed out in " + SceneManager.GetActiveScene().name);
        }

        private static T[] All<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).ToArray();
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (EditorApplication.isPlaying) yield return new ExitPlayMode();
            EditorSceneManager.OpenScene(Day2SchoolMorningSetupTool.ScenePath);
        }
    }
}
#endif
