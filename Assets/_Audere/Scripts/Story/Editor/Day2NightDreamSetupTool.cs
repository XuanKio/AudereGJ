#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Audere.Audio;
using Audere.Core;
using Audere.Dialogue;
using Audere.Puzzle;
using Audere.Puzzle.Board;
using Audere.Puzzle.PathPieces;
using Audere.Story;
using Audere.Story.Presentation;
using Audere.Story.Steps;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Audere.EditorTools
{
    /// <summary>Creates missing scenes only; never rebuilds an existing authored scene.</summary>
    public static class Day2NightDreamSetupTool
    {
        public const string HomePath = "Assets/_Audere/Scenes/70_D2_Home_Night.unity";
        public const string DreamPath = "Assets/_Audere/Scenes/80_D2_Dream.unity";
        public const string WakePath = "Assets/_Audere/Scenes/90_D2_Home_Awakening.unity";
        private const string EveningPath = "Assets/_Audere/Scenes/40_Evening.unity";
        private const string DataFolder = "Assets/_Audere/Data/Dialogue/Day2/NightDream";
        private const string PlayerPath = "Assets/_Audere/Prefabs/Puzzle/Actors/Player.prefab";
        private const string GrassPath = "Assets/_Audere/Prefabs/Puzzle/Tiles/Grass.prefab";
        private const string FontPath = "Assets/_Audere/AssetGame/Font/Mynerve-Regular SDF.asset";

        [MenuItem("Audere/Story/Author Day 2 Night and Dream")]
        public static void Author()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play before authoring the Day 2 continuation.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save dirty scenes first; authoring never discards user edits.");
            Scene original = SceneManager.GetActiveScene();
            Scene reference = SceneManager.GetSceneByPath(EveningPath);
            bool openedReference = !reference.isLoaded;
            if (openedReference) reference = EditorSceneManager.OpenScene(EveningPath, OpenSceneMode.Additive);
            try
            {
                CreateMissing(HomePath, reference, BuildHome);
                CreateMissing(DreamPath, reference, BuildDream);
                CreateMissing(WakePath, reference, BuildWake);
                LinkSchool();
                var scenes = EditorBuildSettings.scenes.ToList();
                foreach (string path in new[] { HomePath, DreamPath, WakePath })
                {
                    var entry = scenes.FirstOrDefault(s => s.path == path);
                    if (entry == null) scenes.Add(new EditorBuildSettingsScene(path, true));
                    else entry.enabled = true;
                }
                EditorBuildSettings.scenes = scenes.ToArray();
                AssetDatabase.SaveAssets();
            }
            finally
            {
                if (openedReference) EditorSceneManager.CloseScene(reference, true);
                if (original.IsValid() && original.isLoaded) SceneManager.SetActiveScene(original);
            }
            Debug.Log("[Day2NightDream] School closure and three scene-first scenes authored. Existing scenes/data preserved.");
        }

        private static void CreateMissing(string path, Scene reference, Action<Stage> build)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null) return;
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            // Leave an unsaved scene open on error for inspection; do not silently discard it.
            Stage stage = Common(scene, reference);
            build(stage);
            EditorSceneManager.SaveScene(scene, path);
            EditorSceneManager.CloseScene(scene, true);
        }

        private static Stage Common(Scene scene, Scene source)
        {
            foreach (string name in new[] { "Main Camera", "Directional Light", "EventSystem", "Scene Transition Overlay" })
            {
                GameObject root = Object.Instantiate(Root(source, name));
                root.name = name;
                SceneManager.MoveGameObjectToScene(root, scene);
            }
            Camera camera = Root(scene, "Main Camera").GetComponent<Camera>();
            var oldFollow = camera.GetComponent<GridCameraFollow2D>();
            if (oldFollow != null) Object.DestroyImmediate(oldFollow);
            camera.transform.position = new Vector3(0f, -.04f, -10f);
            Transform viewport = camera.transform.Find("PuzzleViewportMask");
            if (viewport != null) viewport.gameObject.SetActive(true);
            CanvasGroup fade = Root(scene, "Scene Transition Overlay").GetComponentInChildren<CanvasGroup>(true);
            fade.gameObject.SetActive(true);
            fade.alpha = 1f;
            fade.blocksRaycasts = true;
            var ui = Prefab("Assets/_Audere/Prefabs/UI/GameplayUIRoot.prefab", null, "GameplayUIRoot").GetComponent<GameplayUIRoot>();
            ui.PuzzleUi.gameObject.SetActive(false);
            Transform world = Child(null, "WORLD");
            Transform stage = Child(world, "Home Stage PLACEHOLDER_NO_ART");
            stage.localScale = Vector3.one * .25f;
            stage.position = new Vector3(0f, -.04f, 0f);
            var tile = Prefab(GrassPath, stage, "Night Tile PLACEHOLDER").transform;
            foreach (var r in tile.GetComponentsInChildren<SpriteRenderer>(true)) r.color = new Color(.38f, .43f, .56f, 1f);
            var actor = Prefab(PlayerPath, stage, "Audere").transform;
            actor.localScale = Vector3.one * 1.5f;
            SpriteRenderer body = actor.GetComponent<SpriteRenderer>();
            body.sortingLayerName = "Player";
            body.sortingOrder = 5;
            body.flipX = true;
            // Feet, not the centre-body pivot, sit at the tile centre.
            actor.localPosition = new Vector3(0f, -body.sprite.bounds.min.y * 1.5f, -1f);
            Transform shadow = actor.GetComponentsInChildren<SpriteRenderer>(true).Single(r => r != body && r.sortingOrder == 4).transform;
            var anchors = Child(world, "STAGING TARGETS");
            anchors.gameObject.SetActive(false);
            Transform pose = Child(anchors, "Audere_CenteredGroundPose");
            pose.position = actor.position;
            Transform cameraPose = Child(anchors, "Camera_AuthoredPose");
            cameraPose.position = camera.transform.position;
            var story = Child(null, "STORY").gameObject.AddComponent<StoryDirector>();
            return new Stage { Scene = scene, World = world, StageRoot = stage, Tile = tile, Actor = actor,
                Shadow = shadow, Pose = pose, CameraPose = cameraPose, Camera = camera, Fade = fade, Ui = ui, Director = story };
        }

        private static void BuildHome(Stage s)
        {
            StoryEvent e = Event(s, "D2_HOME_NIGHT_DOUBT");
            Opening(e, s);
            Facing(e, "015_AudereFacesAwayBeforeAsking", s.Actor, false);
            Talk(e, "020_AskWhatHeKnows", D("HOME_QUESTION", DialogueCharacterId.Timor, "Audere_Tired", "TimorLolang",
                "L|Timor.", "R|Ừ?", "L|Lúc ở kho…", "L|Cậu thật sự biết Bianca đang nghĩ gì à?"));
            Wait(e, "030_TimorDoesNotAnswerYet", 2.4f);
            Talk(e, "040_OnlyWhatSheCouldThink", D("HOME_POSSIBILITY", DialogueCharacterId.Timor, "Audere_Tired", "TimorLolang",
                "R|Tớ biết cô ấy có thể nghĩ gì."));
            Facing(e, "050_AudereLooksTowardTimor", s.Actor, true);
            Wait(e, "060_HoldOnCould", .45f);
            Talk(e, "070_TodayItDidNotHappen", D("HOME_TODAY", DialogueCharacterId.Timor, "Audere_Tired", "TimorLoLangKhongVui",
                "L|Có thể.", "R|Ừ.", "R|Nên tớ mới phải nhắc cậu.", "R|Cứ chuẩn bị cho điều tệ nhất.",
                "R|Nếu nó đến, cậu sẽ không bị bất ngờ.", "L|Nhưng hôm nay… nó không xảy ra.", "R|Hôm nay thôi."));
            Wait(e, "080_SilenceAfterToday", 1.6f);
            Talk(e, "090_DontForgetTheOtherTimes", D("HOME_WARNING", DialogueCharacterId.Timor, "Audere_Tired", "TimorLoLangKhongVui",
                "R|Audere.", "R|Tớ không muốn chỉ vì hôm nay mọi thứ ổn…", "R|mà cậu quên những lần nó có thể không ổn."));
            Wait(e, "100_AudereCannotAnswer", 1.1f);
            Talk(e, "110_GoodNightWithoutResolution", D("HOME_GOODNIGHT", DialogueCharacterId.Timor, "Audere_Tired", "TimorLoLangKhongVui",
                "L|…Tớ biết.", "L|Tớ đi ngủ đây.", "R|Ừ. Nghỉ đi.", "L|Ngủ ngon, Timor.", "R|Ngủ ngon, Audere."));
            Facing(e, "120_AudereTurnsAwayToRest", s.Actor, false);
            Wait(e, "130_SettleIntoSleep", .6f);
            Fade(e, "140_FadeToSleep", s.Fade, 1f, 1.35f);
            Load(e, "150_EnterDream", GameScenes.Day2Dream);
        }

        private static void BuildWake(Stage s)
        {
            StoryEvent e = Event(s, "D2_HOME_WAKE_FROM_DREAM");
            Opening(e, s, .35f);
            Startle(e, "020_AudereWakesWithAStart", s.Actor, s.Pose, s.Shadow, .09f);
            Wait(e, "030_HoldAfterLanding", .7f);
            // The requested day label is intentionally editable; do not infer Day 3 here.
            var canvas = Child(null, "Wake Day Label UI").gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            var group = canvas.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            var label = new GameObject("Ngày 2…", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            label.transform.SetParent(canvas.transform, false);
            label.rectTransform.anchorMin = label.rectTransform.anchorMax = new Vector2(.5f, .8f);
            label.rectTransform.sizeDelta = new Vector2(600f, 100f);
            label.font = Required<TMP_FontAsset>(FontPath);
            label.fontSize = 42f;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            label.color = new Color(.86f, .82f, .9f, 1f);
            Set(Step<StoryTitleCardStep>(e, "040_DayTwoEllipsis"), "overlay", group, "titleText", label,
                "title", "Ngày 2…", "fadeDuration", .7f, "holdDuration", 1.6f, "leaveVisible", true);
            // No new dialogue/day progression is invented after this landing beat.
        }

        private static void BuildDream(Stage s)
        {
            StoryEvent e = Event(s, "D2_DREAM_ONLY_ME");
            s.StageRoot.name = "Dream Path - Scene Authored";
            // This tile belongs to the newly-created scene only. The five boards replace it.
            Object.DestroyImmediate(s.Tile.gameObject);
            var grid = s.StageRoot.gameObject.AddComponent<GridSpace2D>();
            var runtime = Prefab("Assets/_Audere/Prefabs/Puzzle/World/PuzzleRuntime.prefab", s.StageRoot, "Puzzle Runtime").GetComponent<PuzzleRuntime>();
            Set(runtime.Placement, "boardCamera", s.Camera, "gridSpace", grid, "puzzleCanvas", s.Ui.GameplayCanvas);
            var coordinator = s.StageRoot.gameObject.AddComponent<PuzzleRootCoordinator>();
            var puzzles = new List<PuzzleController>();
            var goals = new List<GoalTileBehaviour>();
            var tiles = new List<Transform[]>();
            var pathSprites = new List<SpriteRenderer>();
            var data = DreamPuzzleData();
            for (int segment = 0; segment < 5; segment++)
            {
                Transform root = Child(s.StageRoot, "PZ_D2_DREAM_" + (segment + 1).ToString("00"));
                Transform boardRoot = Child(root, "Board");
                Transform systems = Child(root, "Systems");
                Transform start = Child(root, "PlayerStart");
                start.position = grid.CellToWorldCenter(new Vector2Int(segment * 3, 0));
                var playerStart = start.gameObject.AddComponent<PuzzlePlayerStart>();
                var board = systems.gameObject.AddComponent<BoardManager>();
                Set(board, "gridSpace", grid, "boardVisualRoot", boardRoot,
                    "tileCatalog", Required<PuzzleTileCatalog>("Assets/_Audere/Data/Puzzle/PuzzleTileCatalog.asset"));
                var segmentTiles = new Transform[4];
                for (int cell = 0; cell < 4; cell++)
                {
                    var tile = Prefab(GrassPath, boardRoot, "Dream Tile " + (segment * 3 + cell).ToString("00")).transform;
                    tile.position = grid.CellToWorldCenter(new Vector2Int(segment * 3 + cell, 0));
                    // The path remains perfectly stable; RGB fringes are separate scenery renderers.
                    var sr = tile.GetComponentInChildren<SpriteRenderer>();
                    sr.color = new Color(.35f, .47f, .6f, 1f);
                    var bt = tile.GetComponent<BoardTile>();
                    SetVector(bt, "gridPosition", new Vector2Int(segment * 3 + cell, 0));
                    if (cell == 3) goals.Add(tile.gameObject.AddComponent<GoalTileBehaviour>());
                    AddFringes(sr);
                    pathSprites.AddRange(tile.GetComponentsInChildren<SpriteRenderer>(true));
                    segmentTiles[cell] = tile;
                }
                var manager = systems.gameObject.AddComponent<PuzzleManager>();
                Set(manager, "puzzleData", data, "board", board, "playerStart", playerStart,
                    "player", s.Actor.GetComponent<GridPlayer>(), "runtime", runtime,
                    "placement", runtime.Placement, "hand", s.Ui.PathPieceHand,
                    "retryWhenOutOfPieces", true, "failedAttemptResetDelay", .5f);
                var controller = systems.gameObject.AddComponent<PuzzleController>();
                Set(controller, "puzzle", manager, "puzzleRoot", root, "playOnStart", false);
                puzzles.Add(controller);
                tiles.Add(segmentTiles);
                board.RegisterExistingTiles();
                root.gameObject.SetActive(segment == 0);
            }
            Set(coordinator, "sharedPlayer", s.Actor.GetComponent<GridPlayer>(), "runtime", runtime, "puzzles", puzzles.ToArray());
            // Decor has no BoardTile/collider, hence it cannot accept path placement.
            var decorRoot = Child(s.World, "Floating Tiles PLACEHOLDER - NOT WALKABLE");
            var floating = new List<SpriteRenderer>();
            SpriteRenderer tileSource = tiles[0][0].GetComponentInChildren<SpriteRenderer>();
            for (int i = 0; i < 24; i++)
            {
                var decor = Child(decorRoot, "Floating RGB Tile " + i.ToString("00"));
                decor.position = new Vector3(-.8f + i * .23f, (i % 2 == 0 ? .3f : -.45f) + (i % 3) * .07f, 1f);
                decor.localScale = Vector3.one * (.14f + i % 3 * .025f);
                decor.localRotation = Quaternion.Euler(0f, 0f, i % 2 == 0 ? 12f : -9f);
                var sr = decor.gameObject.AddComponent<SpriteRenderer>();
                sr.sprite = tileSource.sprite;
                sr.sharedMaterial = tileSource.sharedMaterial;
                sr.color = new Color(.32f, .37f, .6f, .38f);
                sr.sortingOrder = -4;
                AddFringes(sr);
                floating.AddRange(decor.GetComponentsInChildren<SpriteRenderer>());
            }
            var textRoot = Child(s.World, "Dream Murmurs - NOT Bianca Dialogue");
            string[] phrases = { "Lại phải sửa cho cậu.", "Biết ngay rồi cũng sẽ nhờ thêm.", "Ngày mai lại phải gặp.", "Con nhỏ phiền phức" };
            var texts = new List<TMP_Text>();
            for (int i = 0; i < 20; i++)
            {
                var text = Child(textRoot, "Murmur " + i.ToString("00")).gameObject.AddComponent<TextMeshPro>();
                text.font = Required<TMP_FontAsset>(FontPath);
                text.text = phrases[i % phrases.Length];
                text.fontSize = .8f + i % 3 * .1f;
                text.rectTransform.sizeDelta = new Vector2(2.7f, .3f);
                text.alignment = TextAlignmentOptions.Center;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.color = new Color(.69f, .54f, .73f, .2f);
                // TMP replaces Transform with RectTransform. Author its serialized anchored pose,
                // otherwise a later rect rebuild can reset x/y while leaving z intact.
                text.rectTransform.anchoredPosition3D = new Vector3(-2f + i % 5 * 1.05f, -.54f + i / 5 * .36f, 2f);
                text.transform.rotation = Quaternion.Euler(0f, 0f, (i % 3 - 1) * 13f);
                text.GetComponent<MeshRenderer>().sortingOrder = -8;
                texts.Add(text);
            }
            var atmosphere = s.World.gameObject.AddComponent<DreamAtmosphereView>();
            Set(atmosphere, "owner", e, "worldCamera", s.Camera, "player", s.Actor.GetComponent<GridPlayer>(),
                "cameraStart", s.CameraPose, "playerStart", s.Pose,
                "floatingTiles", floating.ToArray(), "pathRenderers", pathSprites.ToArray(), "murmurs", texts.ToArray());
            Fade(e, "000_CoverDream", s.Fade, 1f, 0f);
            var prepare = Step<PuzzleSequencePrepareStep>(e, "010_PrepareContinuousPath");
            Set(prepare, "puzzleRootCoordinator", coordinator, "startingPuzzle", puzzles[0],
                "showPlayerAtStart", true, "hideStartingBoardUntilReveal", false);
            Set(Step<DreamAtmosphereStep>(e, "020_BeginDreamDrift"), "atmosphere", atmosphere, "action", 0);
            Fade(e, "030_RevealDream", s.Fade, 0f, .9f);
            for (int i = 0; i < puzzles.Count; i++)
            {
                string prefix = (100 + i * 30).ToString("000");
                Set(Step<PuzzleStep>(e, prefix + "_WalkThreeCells"), "puzzleController", puzzles[i],
                    "puzzleRoot", puzzles[i].PuzzleRoot.gameObject, "resetBeforePlay", false, "normalizeOnCancel", prepare);
                if (i == puzzles.Count - 1) break;
                Set(Step<BoardTileTransitionStep>(e, (110 + i * 30) + "_KeepGoalAsNextStart"),
                    "puzzleRootCoordinator", coordinator, "sourcePuzzle", puzzles[i], "goalToBecomeAnchor", goals[i],
                    "objectsToHide", tiles[i].Take(3).ToArray(), "objectsToReveal", new Object[0],
                    "transitionDuration", .12f, "staggerDelay", 0f, "normalizeOnCancel", prepare);
                // The old goal stays visible while the new route reveals. It is replaced atomically afterwards.
                Set(Step<BoardTileTransitionStep>(e, (115 + i * 30) + "_RevealAheadOfAudere"),
                    "revealPuzzle", puzzles[i + 1], "objectsToReveal", tiles[i + 1].Skip(1).ToArray(),
                    "objectsToKeepHidden", new Object[] { tiles[i + 1][0] }, "revealWaveDuration", .3f,
                    "transitionDuration", .14f, "staggerDelay", 0f, "normalizeOnCancel", prepare);
                Set(Step<SetActiveStep>(e, (120 + i * 30) + "_SwapSharedAnchor"),
                    "objectsToEnable", new Object[] { tiles[i + 1][0].gameObject },
                    "objectsToDisable", new Object[] { puzzles[i].PuzzleRoot.gameObject });
            }
            Wait(e, "250_NoMorePath", .55f);
            Set(Step<DreamAtmosphereStep>(e, "260_TilesDisappearMurmursGrow"), "atmosphere", atmosphere, "action", 1, "duration", 1.25f);
            Talk(e, "270_TimorCallsFromTheVoid", D("DREAM_LOOK_AT_ME", DialogueCharacterId.Timor, "Audere_Scared", "TimorLoLangKhongVui",
                "R|Audere.", "R|Nhìn tớ."));
            Startle(e, "280_AudereStartlesInPlace", s.Actor, s.Pose, s.Shadow, .065f);
            Wait(e, "290_NothingToStandOn", .5f);
            Talk(e, "300_OnlyMe", D("DREAM_ONLY_ME", DialogueCharacterId.Timor, "Audere_Scared", "TimorLoLangKhongVui",
                "L|Timor… đường đâu rồi?", "R|Đừng nhìn chỗ đó nữa.", "R|Nhìn tớ thôi.", "R|Chỉ có tớ giúp cậu an toàn thôi.",
                "R|Chỉ mình tớ là bạn thật sự của cậu.", "L|…Đừng đi.", "R|Tớ ở đây.", "R|Vậy cứ nghe tớ, Audere."));
            Wait(e, "310_HoldOnDependence", .7f);
            Fade(e, "320_FadeOutOfDream", s.Fade, 1f, .85f);
            Load(e, "330_WakeAtHome", GameScenes.Day2HomeAwakening);
        }

        private static void LinkSchool()
        {
            string path = Day2SchoolMorningSetupTool.ScenePath;
            Scene school = SceneManager.GetSceneByPath(path);
            bool opened = !school.isLoaded;
            if (opened) school = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                var e = school.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<StoryEvent>(true))
                    .Single(x => x.EventId == "D2_SCHOOL_WRONG_SUPPLIES");
                if (e.transform.Find("360_GoHomeAfterBell") != null) return;
                var end = e.transform.Find("350_FadeOutAfterAnswer");
                if (end == null) throw new InvalidOperationException("Expected the existing post-combat ending; refusing to rebuild it.");
                Talk(e, "345_PreparationsAreDone", D("SCHOOL_READY_FOR_TOMORROW", DialogueCharacterId.Bianca, "Audere_Tired", null,
                    "R|Vậy là đồ cho ngày mai đủ rồi.", "R|Mai bọn mình trang trí bảng nhé.", "R|Rồi chuẩn bị nốt với cả lớp.",
                    "L|…Với mọi người nữa à?", "R|Ừ. Chia nhau làm thôi.", "L|Phần bảng… tớ vẫn muốn làm.", "R|Ừ, mai làm tiếp."));
                Wait(e, "346_LetTomorrowStaySmall", .45f);
                Talk(e, "347_BiancaSaysGoodbye", D("SCHOOL_GOODBYE", DialogueCharacterId.Bianca, "Audere_Tired", null,
                    "R|Về thôi. Hẹn cậu ngày mai.", "L|Ừ. Mai gặp."));
                var move = e.transform.Find("040_BiancaOnOwnTile").GetComponent<MoveActorStep>();
                var bianca = (Transform)new SerializedObject(move).FindProperty("actor").objectReferenceValue;
                Facing(e, "348_BiancaTurnsToLeave", bianca, true);
                var cover = Root(school, "Scene Transition Overlay").GetComponentInChildren<CanvasGroup>(true);
                Set(Step<SetActiveStep>(e, "348_EnableDepartureCover"), "objectsToEnable", new Object[] { cover.gameObject });
                Set(Step<PlayAudioStep>(e, "349_SchoolBell"), "audioId", (int)AudioId.School_Bell);
                end.SetAsLastSibling();
                Load(e, "360_GoHomeAfterBell", GameScenes.Day2HomeNight);
                EditorSceneManager.MarkSceneDirty(school);
                EditorSceneManager.SaveScene(school);
            }
            finally { if (opened) EditorSceneManager.CloseScene(school, true); }
        }

        private static PuzzleData DreamPuzzleData()
        {
            const string path = "Assets/_Audere/Data/Puzzle/Day2/Puzzle_D2_Dream_ThreeSteps.asset";
            var existing = AssetDatabase.LoadAssetAtPath<PuzzleData>(path);
            if (existing != null) return existing;
            var data = ScriptableObject.CreateInstance<PuzzleData>();
            var piece = Required<PathPieceData>("Assets/_Audere/Data/Puzzle/PathPieces/PathPiece_Line_2.asset");
            AssetDatabase.CreateAsset(data, path);
            Set(data, "puzzleId", "d2-dream-three-steps", "requireAllPathPieces", true,
                "availablePathPieces", new Object[] { piece, piece, piece });
            return data;
        }

        private static void AddFringes(SpriteRenderer source)
        {
            for (int i = 0; i < 2; i++)
            {
                var fringe = Child(source.transform, i == 0 ? "RGB Cyan Fringe" : "RGB Red Fringe");
                fringe.localPosition = Vector3.right * (i == 0 ? -.025f : .025f);
                var r = fringe.gameObject.AddComponent<SpriteRenderer>();
                r.sprite = source.sprite;
                r.sharedMaterial = source.sharedMaterial;
                r.sortingLayerID = source.sortingLayerID;
                r.sortingOrder = source.sortingOrder - 1;
                r.color = i == 0 ? new Color(.15f, .7f, 1f, .28f) : new Color(1f, .22f, .42f, .28f);
            }
        }

        private static DialogueData D(string suffix, DialogueCharacterId counterpart, string audere, string timor, params string[] lines)
        {
            Day2SchoolMorningSetupTool.Folder(DataFolder);
            string path = DataFolder + "/Dialogue_D2_" + suffix + ".asset";
            var data = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
            if (data != null) return data;
            foreach (string line in lines)
                if (line.Substring(2).Length > 42) throw new InvalidOperationException("Split long bubble: " + line);
            data = ScriptableObject.CreateInstance<DialogueData>();
            AssetDatabase.CreateAsset(data, path);
            Set(data, "dialogueId", "d2-" + suffix.ToLowerInvariant().Replace('_', '-'), "leftCharacter", 1,
                "rightCharacter", (int)counterpart, "leftPortraitOverride", Portrait("Audere", audere),
                "rightPortraitOverride", timor == null ? null : Portrait("Timor", timor));
            var so = new SerializedObject(data);
            var array = so.FindProperty("lines");
            array.arraySize = lines.Length;
            for (int i = 0; i < lines.Length; i++)
            {
                var line = array.GetArrayElementAtIndex(i);
                line.FindPropertyRelative("speaker").intValue = lines[i][0] == 'L' ? 0 : 1;
                line.FindPropertyRelative("text").stringValue = lines[i].Substring(2);
                line.FindPropertyRelative("portraitOverride").objectReferenceValue = null;
                line.FindPropertyRelative("glitchPortraitTransition").boolValue = false;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return data;
        }

        private static Sprite Portrait(string folder, string name) => AssetDatabase.LoadAllAssetsAtPath(
            "Assets/_Audere/AssetGame/" + folder + "/" + name + ".png").OfType<Sprite>().Single(x => x.name == name + "_0");
        private static void Opening(StoryEvent e, Stage s, float reveal = .65f)
        {
            Fade(e, "000_CoverArrival", s.Fade, 1f, 0f);
            Set(Step<MoveActorStep>(e, "005_ResetAudereUnderFade"), "actor", s.Actor, "targetTransform", s.Pose, "duration", 0f);
            Fade(e, "010_RevealRoom", s.Fade, 0f, reveal);
        }
        private static void Startle(StoryEvent e, string name, Transform actor, Transform pose, Transform shadow, float arc) =>
            Set(Step<CharacterMotionStep>(e, name), "actor", actor, "targetTransform", pose, "groundedShadow", shadow,
                "actorRenderer", actor.GetComponent<SpriteRenderer>(), "motionMode", 1, "facingMode", 0,
                "duration", .19f, "arcHeight", arc, "useUnscaledTime", true);
        private static void Facing(StoryEvent e, string name, Transform actor, bool right) =>
            Set(Step<SetActorFacingStep>(e, name), "actorRenderer", actor.GetComponent<SpriteRenderer>(), "faceRight", right);
        private static void Talk(StoryEvent e, string name, DialogueData data) =>
            Set(Step<DialogueStep>(e, name), "dialogueData", data);
        private static void Wait(StoryEvent e, string name, float time) => Set(Step<WaitStep>(e, name), "duration", time, "useUnscaledTime", true);
        private static void Fade(StoryEvent e, string name, CanvasGroup cover, float alpha, float duration) =>
            Set(Step<CanvasFadeStep>(e, name), "canvasGroup", cover, "targetAlpha", alpha, "duration", duration, "useUnscaledTime", true);
        private static void Load(StoryEvent e, string name, string scene) => Set(Step<SceneLoadStep>(e, name), "sceneName", scene, "hidePuzzleUiBeforeLoad", true);
        private static StoryEvent Event(Stage s, string id)
        {
            var e = Child(s.Director.transform, id).gameObject.AddComponent<StoryEvent>();
            Set(e, "eventId", id, "autoPlayNextEvent", false);
            Set(s.Director, "storyEventsRoot", s.Director.transform, "startingEvent", e, "playOnStart", true);
            return e;
        }
        private static T Step<T>(StoryEvent e, string name) where T : StoryStep
        {
            var go = Child(e.transform, name).gameObject;
            return go.GetComponent<T>() ?? go.AddComponent<T>();
        }
        private static Transform Child(Transform parent, string name) => parent == null
            ? new GameObject(name).transform : Day2SchoolMorningSetupTool.Child(parent, name);
        private static void Set(Object target, params object[] values) => Day2SchoolMorningSetupTool.Set(target, values);
        private static void SetVector(Object target, string field, Vector2Int value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).vector2IntValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static GameObject Root(Scene s, string name) => s.GetRootGameObjects().Single(r => r.name == name);
        private static T Required<T>(string path) where T : Object => AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("Missing asset: " + path);
        private static GameObject Prefab(string path, Transform parent, string name)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(Required<GameObject>(path), parent);
            go.name = name;
            return go;
        }
        private sealed class Stage
        {
            public Scene Scene;
            public Transform World, StageRoot, Tile, Actor, Shadow, Pose, CameraPose;
            public Camera Camera;
            public CanvasGroup Fade;
            public GameplayUIRoot Ui;
            public StoryDirector Director;
        }
    }
}
#endif
