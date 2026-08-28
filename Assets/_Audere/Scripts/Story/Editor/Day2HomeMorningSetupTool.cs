#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Audere.Dialogue;
using Audere.Puzzle;
using Audere.Puzzle.Board;
using Audere.Puzzle.PathPieces;
using Audere.Story.Steps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Audere.Story.Editor
{
    public static class Day2HomeMorningSetupTool
    {
        private const string SourceScenePath = "Assets/_Audere/Scenes/20_D1_Home_Morning.unity";
        private const string TargetScenePath = "Assets/_Audere/Scenes/50_D2_Home_Morning.unity";
        private const string DialogueFolder = "Assets/_Audere/Data/Dialogue/Day2/Home";
        private const string PuzzleFolder = "Assets/_Audere/Data/Puzzle/Day2";

        [MenuItem("Audere/Story/Author Day 2 Home Morning")]
        public static void Author()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning(
                    "[Day2HomeMorningSetup] Exit Play Mode before authoring Day 2. " +
                    "EditorSceneManager.OpenScene cannot be used while the game is running.");
                return;
            }

            EnsureFolder(DialogueFolder);
            EnsureFolder(PuzzleFolder);

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SourceScenePath) == null)
                throw new InvalidOperationException($"Missing Day 1 source scene: {SourceScenePath}");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PuzzleContentConstants.AssetPaths.OneUsePrefab) == null)
                throw new InvalidOperationException("Create the shared OneUse tile prefab before authoring Day 2.");

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TargetScenePath) == null &&
                !AssetDatabase.CopyAsset(SourceScenePath, TargetScenePath))
            {
                throw new InvalidOperationException($"Could not copy {SourceScenePath} to {TargetScenePath}.");
            }

            DialogueAssets dialogues = CreateDialogueAssets();
            PuzzleAssets puzzles = CreatePuzzleAssets();
            Scene scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);

            StoryDirector director = RequireRootComponent<StoryDirector>(scene, "STORY");
            StoryEvent morning = FindEvent(scene, "D2_HOME_MORNING", "D1_HOME_MORNING");
            StoryEvent busStop = FindEvent(scene, "D2_TO_BUS_STOP", "D1_TO_BUS_STOP");
            ConfigureEvent(morning, "D2_HOME_MORNING", true, busStop);
            ConfigureEvent(busStop, "D2_TO_BUS_STOP", false, null);
            ConfigureDirector(director, morning);

            DialogueController dialogueController = AssignDialogue(
                morning,
                "10_MorningDialogue",
                "10_WakeWithDistance",
                dialogues.Opening,
                null);
            AssignDialogue(morning, "50_AfterBrushingDialogue", "50_AfterBrushing", dialogues.AfterBrushing, dialogueController);
            AssignDialogue(morning, "90_AfterBreakfastDialogue", "90_CheckBagAgain", dialogues.CheckBag, dialogueController);
            AssignDialogue(morning, "120_LeavingChecklistDialogue", "120_CheckLockedDoor", dialogues.CheckDoor, dialogueController);
            EnsureOneUseTutorialStep(morning, dialogues.OneUseTutorial, dialogueController);

            AssignDialogue(busStop, "30_BusStopApproachDialogue", "30_BiancaThoughtRedirect", dialogues.BiancaThought, dialogueController);
            AssignDialogue(busStop, "70_BusStopArrivalDialogue", "70_EventThoughtRedirect", dialogues.EventThought, dialogueController);
            AssignDialogue(busStop, "90_BusStopSafetyDialogue", "90_StandWhereSafe", dialogues.StandSafe, dialogueController);
            ConfigureBusStopEnding(scene, busStop);

            PuzzleController washroom = FindPuzzle(scene, "PZ_D2_WASHROOM", "PZ_D1_WASHROOM");
            PuzzleController breakfast = FindPuzzle(scene, "PZ_D2_BREAKFAST", "PZ_D1_BREAKFAST");
            PuzzleController bus = FindPuzzle(scene, "PZ_D2_BUS_STOP", "PZ_D1_BUS_STOP");

            ConfigureLevel(
                washroom,
                "PZ_D2_WASHROOM",
                puzzles.Washroom,
                new Vector2Int(0, 0),
                WashroomCells());
            ConfigureLevel(
                breakfast,
                "PZ_D2_BREAKFAST",
                puzzles.Breakfast,
                new Vector2Int(0, 0),
                BreakfastCells());
            ConfigureLevel(
                bus,
                "PZ_D2_BUS_STOP",
                puzzles.BusStop,
                new Vector2Int(0, 0),
                BusStopCells());

            ConfigureGoalItem(
                washroom,
                "Assets/_Audere/AssetGame/Item/banchai.aseprite",
                new Vector3(-0.1f, 0.147f, 0f),
                Vector3.one * 2f);
            ConfigureGoalItem(
                breakfast,
                "Assets/_Audere/AssetGame/Item/banhmi.aseprite",
                new Vector3(0.03f, 0.256f, 0f),
                Vector3.one * 1.36f);
            ConfigureBusStopGoalPresentation(bus);

            ConfigurePuzzleChain(morning, busStop, washroom, breakfast, bus);

            ConfigurePuzzleGuidance(washroom, "Ô đỏ chỉ chịu được một lần.", "Ô đỏ đã sập. Mình thử lại nhé.", true);
            ConfigurePuzzleGuidance(breakfast, "Dùng hết mảnh. Đừng quay lại ô đỏ.", "Nhìn lại đường đi rồi thử lần nữa nhé.", false);
            ConfigurePuzzleGuidance(bus, "Mỗi ô đỏ chỉ đi qua một lần.", "Đi chậm lại. Mình thử từ đầu nhé.", false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, TargetScenePath);
            AddSceneToBuildSettings(TargetScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(TargetScenePath);
            Debug.Log("[Day2HomeMorningSetup] Authored D2 home morning, reusable OneUse tiles, and three validated compact puzzle boards.");
        }

        private static void ConfigureGoalItem(
            PuzzleController controller,
            string spritePath,
            Vector3 localPosition,
            Vector3 localScale)
        {
            GoalTileBehaviour goal = FindGoalBehaviour(controller);
            Transform visualRoot = goal.transform.Find("Visual Root");
            Transform item = visualRoot != null ? visualRoot.Find("Item") : null;
            if (item == null)
                throw new InvalidOperationException($"Goal '{goal.name}' needs Visual Root/Item.");

            SpriteRenderer renderer = item.GetComponent<SpriteRenderer>();
            if (renderer == null)
                renderer = item.gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadSprite(spritePath);
            renderer.color = Color.white;
            renderer.sortingOrder = 3;

            item.localPosition = localPosition;
            item.localRotation = Quaternion.identity;
            item.localScale = localScale;
            item.gameObject.SetActive(true);

            GoalItemMotion motion = item.GetComponent<GoalItemMotion>();
            if (motion == null)
                motion = item.gameObject.AddComponent<GoalItemMotion>();
            motion.Motion = GoalItemMotionMode.Floating;

            SerializedObject goalSerialized = new SerializedObject(goal);
            goalSerialized.FindProperty("itemVisual").objectReferenceValue = item.gameObject;
            goalSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item.gameObject);
            EditorUtility.SetDirty(goal);
        }

        private static void ConfigureBusStopGoalPresentation(PuzzleController controller)
        {
            GoalTileBehaviour goal = FindGoalBehaviour(controller);
            Transform visualRoot = goal.transform.Find("Visual Root");
            Transform item = visualRoot != null ? visualRoot.Find("Item") : null;
            if (item != null)
            {
                SpriteRenderer itemRenderer = item.GetComponent<SpriteRenderer>();
                if (itemRenderer != null)
                    itemRenderer.sprite = null;
                item.gameObject.SetActive(false);
            }

            const string spritePath = "Assets/_Audere/AssetGame/Item/busstop.aseprite";
            ConfigureGoalScenerySprite(
                goal.transform,
                "busstop_0",
                LoadSprite(spritePath, "busstop_0"),
                new Vector3(-1.98f, 1.69f, 0f),
                Vector3.one * 2f,
                3);
            ConfigureGoalScenerySprite(
                goal.transform,
                "busstop_2",
                LoadSprite(spritePath, "busstop_2"),
                new Vector3(-0.005f, 0.9f, 0f),
                Vector3.one * 2f,
                4);
        }

        private static void ConfigureGoalScenerySprite(
            Transform goal,
            string objectName,
            Sprite sprite,
            Vector3 localPosition,
            Vector3 localScale,
            int sortingOrder)
        {
            Transform child = goal.Find(objectName);
            if (child == null)
            {
                child = new GameObject(objectName, typeof(SpriteRenderer)).transform;
                child.SetParent(goal, false);
            }

            child.localPosition = localPosition;
            child.localRotation = Quaternion.identity;
            child.localScale = localScale;
            child.gameObject.SetActive(true);
            SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.sortingOrder = sortingOrder;
            EditorUtility.SetDirty(child.gameObject);
        }

        private static Sprite LoadSprite(string assetPath, string preferredName = null)
        {
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().ToArray();
            Sprite sprite = string.IsNullOrEmpty(preferredName)
                ? sprites.FirstOrDefault()
                : sprites.FirstOrDefault(item => item.name == preferredName);
            if (sprite == null)
                throw new InvalidOperationException(
                    string.IsNullOrEmpty(preferredName)
                        ? $"Missing sprite asset: {assetPath}"
                        : $"Missing sprite '{preferredName}' in asset: {assetPath}");
            return sprite;
        }

        private static void ConfigurePuzzleChain(
            StoryEvent morning,
            StoryEvent busStop,
            PuzzleController washroom,
            PuzzleController breakfast,
            PuzzleController bus)
        {
            GoalTileBehaviour washroomGoal = FindGoalBehaviour(washroom);
            GoalTileBehaviour breakfastGoal = FindGoalBehaviour(breakfast);

            // The scene-authored pose and the runtime hand-off share one truth:
            // each following PlayerStart occupies the previous Goal world position.
            if (!breakfast.AlignPlayerStartToAnchor(washroomGoal.transform))
                throw new InvalidOperationException("Could not align Breakfast PlayerStart to the Washroom Goal.");
            if (!bus.AlignPlayerStartToAnchor(breakfastGoal.transform))
                throw new InvalidOperationException("Could not align Bus Stop PlayerStart to the Breakfast Goal.");

            ConfigureCollapseAnchor(morning, "40_HideWashroomBoard", washroom, washroomGoal);
            ConfigureRevealAnchor(morning, "60_RevealBreakfastBoard", breakfast, washroomGoal.transform);
            ConfigureCollapseAnchor(morning, "80_HideBreakfastBoard", breakfast, breakfastGoal);

            Transform prepareBusTransform = FindDirectChild(busStop.transform, "10_PrepareBusStopPuzzle");
            PuzzleSequencePrepareStep prepareBus = prepareBusTransform != null
                ? prepareBusTransform.GetComponent<PuzzleSequencePrepareStep>()
                : null;
            if (prepareBus == null)
                throw new InvalidOperationException("Missing PuzzleSequencePrepareStep '10_PrepareBusStopPuzzle'.");

            SerializedObject prepareSerialized = new SerializedObject(prepareBus);
            prepareSerialized.FindProperty("startingPuzzle").objectReferenceValue = bus;
            prepareSerialized.FindProperty("alignToPreviousGoal").boolValue = true;
            prepareSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(prepareBus);
        }

        private static void ConfigureCollapseAnchor(
            StoryEvent storyEvent,
            string stepName,
            PuzzleController source,
            GoalTileBehaviour goal)
        {
            BoardTileTransitionStep step = FindTransitionStep(storyEvent, stepName);
            SerializedObject serialized = new SerializedObject(step);
            serialized.FindProperty("sourcePuzzle").objectReferenceValue = source;
            serialized.FindProperty("goalToBecomeAnchor").objectReferenceValue = goal;
            serialized.FindProperty("rootToDisableAfterHide").objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(step);
        }

        private static void ConfigureRevealAnchor(
            StoryEvent storyEvent,
            string stepName,
            PuzzleController target,
            Transform sourceGoal)
        {
            BoardTileTransitionStep step = FindTransitionStep(storyEvent, stepName);
            SerializedObject serialized = new SerializedObject(step);
            serialized.FindProperty("revealPuzzle").objectReferenceValue = target;
            serialized.FindProperty("revealFromAnchor").objectReferenceValue = sourceGoal;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(step);
        }

        private static BoardTileTransitionStep FindTransitionStep(StoryEvent storyEvent, string stepName)
        {
            Transform child = FindDirectChild(storyEvent.transform, stepName);
            BoardTileTransitionStep step = child != null ? child.GetComponent<BoardTileTransitionStep>() : null;
            if (step == null)
                throw new InvalidOperationException($"Missing BoardTileTransitionStep '{stepName}'.");
            return step;
        }

        private static GoalTileBehaviour FindGoalBehaviour(PuzzleController controller)
        {
            BoardManager board = controller.Puzzle.Board;
            board.RegisterExistingTiles();
            if (!board.TryGetLevelGoal(out BoardTile tile) ||
                !tile.TryGetBehaviour(out GoalTileBehaviour goal))
            {
                throw new InvalidOperationException($"Puzzle '{controller.PuzzleRoot.name}' needs one GoalTileBehaviour.");
            }

            return goal;
        }

        private static DialogueAssets CreateDialogueAssets()
        {
            return new DialogueAssets(
                ConfigureDialogue(
                    "Dialogue_D2_HOME_MORNING_OPENING",
                    "d2-home-morning-opening",
                    L("...Chào buổi sáng."),
                    R("Chào cậu."),
                    R("Cậu vẫn còn sợ tớ à?"),
                    L("...Một chút."),
                    R("Tớ biết."),
                    R("Tớ không muốn làm cậu sợ."),
                    R("Để tớ giúp như mọi hôm nhé."),
                    R("Đánh răng trước thôi."),
                    L("...Ừ.")),
                ConfigureDialogue(
                    "Dialogue_D2_HOME_AFTER_BRUSHING",
                    "d2-home-after-brushing",
                    L("Tớ nhớ là phải ăn sáng."),
                    R("Ừ."),
                    R("Bánh mì ở trong bếp."),
                    R("Ăn hết rồi mình mới đi."),
                    L("Tớ tự làm được."),
                    R("Tớ biết."),
                    R("Tớ chỉ muốn chắc cậu không bỏ bữa.")),
                ConfigureDialogue(
                    "Dialogue_D2_HOME_CHECK_BAG",
                    "d2-home-check-bag",
                    R("Kiểm tra cặp lại đi."),
                    L("Tớ kiểm tra rồi."),
                    R("Tớ biết."),
                    R("Làm lại một lần sẽ yên tâm hơn."),
                    L("...Ừ.")),
                ConfigureDialogue(
                    "Dialogue_D2_HOME_CHECK_DOOR",
                    "d2-home-check-door",
                    R("Cậu chắc đã khóa cửa chưa?"),
                    L("Tớ vừa khóa rồi."),
                    R("Thử tay nắm một lần nữa nhé."),
                    L("...Timor."),
                    R("Một lần thôi."),
                    L("...Ừ.")),
                ConfigureDialogue(
                    "Dialogue_D2_ONE_USE_TILE_TUTORIAL",
                    "d2-one-use-tile-tutorial",
                    R("Ô đỏ chỉ đứng được một lần."),
                    R("Rời khỏi nó rồi sẽ không quay lại được."),
                    L("...Tớ hiểu.")),
                ConfigureDialogue(
                    "Dialogue_D2_BUS_BIANCA_THOUGHT",
                    "d2-bus-bianca-thought",
                    L("Không biết Bianca đang nghĩ gì."),
                    R("Đừng đi sát người kia."),
                    L("Tớ chỉ đang nghĩ về tin nhắn."),
                    R("Tớ biết."),
                    R("Nhìn đường trước đã.")),
                ConfigureDialogue(
                    "Dialogue_D2_BUS_EVENT_THOUGHT",
                    "d2-bus-event-thought",
                    L("Hôm nay lớp còn chuẩn bị sự kiện."),
                    R("Đứng ở đây an toàn hơn."),
                    L("Tớ muốn thử giúp tiếp."),
                    R("Xe sắp đến rồi."),
                    R("Đừng nghĩ chuyện đó lúc này.")),
                ConfigureDialogue(
                    "Dialogue_D2_BUS_FINAL_REDIRECT",
                    "d2-bus-final-redirect",
                    L("Timor."),
                    R("Ừ?"),
                    L("Tối qua..."),
                    R("Cặp của cậu chưa kéo kín."),
                    L("..."),
                    R("Khóa lại đi."),
                    L("...Ừ."),
                    R("Tớ chỉ không muốn cậu sơ suất.")));
        }

        private static PuzzleAssets CreatePuzzleAssets()
        {
            PathPieceData line2 = LoadRequired<PathPieceData>("Assets/_Audere/Data/Puzzle/PathPieces/PathPiece_Line_2.asset");
            PathPieceData line3 = LoadRequired<PathPieceData>("Assets/_Audere/Data/Puzzle/PathPieces/PathPiece_Line_3.asset");
            PathPieceData line4 = LoadRequired<PathPieceData>("Assets/_Audere/Data/Puzzle/PathPieces/PathPiece_Line_4.asset");
            PathPieceData corner3 = LoadRequired<PathPieceData>("Assets/_Audere/Data/Puzzle/PathPieces/PathPiece_L_Corner_3.asset");
            PathPieceData corner4 = LoadRequired<PathPieceData>("Assets/_Audere/Data/Puzzle/PathPieces/PathPiece_L_Corner.asset");

            PuzzleData washroom = ConfigurePuzzleData(
                "Puzzle_D2_WASHROOM_ONE_USE_TUTORIAL",
                "d2-washroom-one-use-tutorial",
                new Vector2Int(0, 0),
                WashroomCells(),
                line2,
                line2,
                line2);
            PuzzleData breakfast = ConfigurePuzzleData(
                "Puzzle_D2_BREAKFAST_ONE_USE",
                "d2-breakfast-one-use",
                new Vector2Int(0, 0),
                BreakfastCells(),
                corner4,
                line2,
                corner3,
                line3);
            PuzzleData bus = ConfigurePuzzleData(
                "Puzzle_D2_BUS_STOP_ONE_USE",
                "d2-bus-stop-one-use",
                new Vector2Int(0, 0),
                BusStopCells(),
                line4,
                corner4,
                line3,
                line2);
            return new PuzzleAssets(washroom, breakfast, bus);
        }

        private static TileCell[] WashroomCells()
        {
            return new[]
            {
                T(0, 0), T(1, 0, PuzzleTileType.OneUse), T(2, 0), T(3, 0, PuzzleTileType.Goal),
            };
        }

        private static TileCell[] BreakfastCells()
        {
            return new[]
            {
                T(0, 0), T(2, 0), T(3, 0), T(4, 0),
                T(0, 1), T(1, 1), T(3, 1, PuzzleTileType.OneUse),
                T(0, 2), T(1, 2), T(2, 2, PuzzleTileType.OneUse),
                T(3, 2, PuzzleTileType.OneUse), T(4, 2, PuzzleTileType.Goal),
                T(0, 3), T(2, 3), T(3, 3),
            };
        }

        private static TileCell[] BusStopCells()
        {
            return new[]
            {
                T(0, 0), T(1, 0, PuzzleTileType.OneUse), T(2, 0), T(3, 0), T(4, 0),
                T(0, 1), T(4, 1, PuzzleTileType.OneUse),
                T(0, 2), T(4, 2), T(5, 2, PuzzleTileType.OneUse), T(6, 2),
                T(0, 3), T(6, 3, PuzzleTileType.Goal),
            };
        }

        private static void ConfigureLevel(
            PuzzleController controller,
            string levelName,
            PuzzleData data,
            Vector2Int start,
            IReadOnlyList<TileCell> cells)
        {
            Transform root = controller.PuzzleRoot;
            GameObject outermost = PrefabUtility.GetOutermostPrefabInstanceRoot(root.gameObject);
            if (outermost != null)
                PrefabUtility.UnpackPrefabInstance(outermost, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            root.name = levelName;

            PuzzleManager manager = controller.Puzzle;
            BoardManager board = manager.Board;
            SerializedObject managerSerialized = new SerializedObject(manager);
            managerSerialized.FindProperty("puzzleData").objectReferenceValue = data;
            managerSerialized.FindProperty("retryWhenOutOfPieces").boolValue = true;
            managerSerialized.FindProperty("failedAttemptResetDelay").floatValue = 0.65f;
            managerSerialized.ApplyModifiedPropertiesWithoutUndo();

            HashSet<GameObject> tileObjects = new HashSet<GameObject>();
            foreach (BoardTile tile in board.BoardVisualRoot.GetComponentsInChildren<BoardTile>(true))
                tileObjects.Add(tile.gameObject);
            if (board.LevelObjectiveRoot != null)
                foreach (BoardTile tile in board.LevelObjectiveRoot.GetComponentsInChildren<BoardTile>(true))
                    tileObjects.Add(tile.gameObject);
            foreach (GameObject tileObject in tileObjects)
                UnityEngine.Object.DestroyImmediate(tileObject);

            BoardTile grass = LoadRequired<BoardTile>(PuzzleContentConstants.AssetPaths.GrassPrefab);
            BoardTile oneUse = LoadRequired<BoardTile>(PuzzleContentConstants.AssetPaths.OneUsePrefab);
            BoardTile goal = LoadRequired<BoardTile>(PuzzleContentConstants.AssetPaths.GoalPrefab);
            foreach (TileCell cell in cells)
            {
                BoardTile prefab = cell.Type == PuzzleTileType.Goal
                    ? goal
                    : cell.Type == PuzzleTileType.OneUse
                        ? oneUse
                        : grass;
                Transform parent = cell.Type == PuzzleTileType.Goal && board.LevelObjectiveRoot != null
                    ? board.LevelObjectiveRoot
                    : board.BoardVisualRoot;
                GameObject instance = PrefabUtility.InstantiatePrefab(prefab.gameObject, parent) as GameObject;
                if (instance == null)
                    throw new InvalidOperationException($"Could not instantiate {cell.Type} at {cell.Position}.");
                instance.name = $"{cell.Type} ({cell.Position.x}, {cell.Position.y})";
                instance.transform.position = board.GridSpace.CellToWorldCenter(cell.Position);
                BoardTile tile = instance.GetComponent<BoardTile>();
                SerializedObject tileSerialized = new SerializedObject(tile);
                tileSerialized.FindProperty("gridPosition").vector2IntValue = cell.Position;
                tileSerialized.FindProperty("tileType").enumValueIndex = (int)cell.Type;
                tileSerialized.ApplyModifiedPropertiesWithoutUndo();
            }

            manager.PlayerStartTransform.position = board.GridSpace.CellToWorldCenter(start);
            board.RegisterExistingTiles();
            EditorUtility.SetDirty(root.gameObject);
            EditorUtility.SetDirty(manager);
            EditorUtility.SetDirty(board);
        }

        private static void ConfigurePuzzleGuidance(
            PuzzleController controller,
            string opening,
            string retry,
            bool replaceStepTileGuide)
        {
            Transform root = controller.PuzzleRoot;
            PuzzleManager manager = controller.Puzzle;
            PathPieceHand hand = null;
            PuzzleHud hud = null;

            StepTileTutorialGuide stepGuide = root.GetComponentInChildren<StepTileTutorialGuide>(true);
            if (stepGuide != null)
            {
                SerializedObject source = new SerializedObject(stepGuide);
                hand = source.FindProperty("hand").objectReferenceValue as PathPieceHand;
                hud = source.FindProperty("hud").objectReferenceValue as PuzzleHud;
                if (replaceStepTileGuide)
                    UnityEngine.Object.DestroyImmediate(stepGuide);
            }

            UseAllPiecesTutorialGuide guide = root.GetComponent<UseAllPiecesTutorialGuide>();
            if (guide == null)
                guide = root.gameObject.AddComponent<UseAllPiecesTutorialGuide>();
            SerializedObject serialized = new SerializedObject(guide);
            serialized.FindProperty("puzzle").objectReferenceValue = manager;
            if (hand != null)
                serialized.FindProperty("hand").objectReferenceValue = hand;
            if (hud != null)
                serialized.FindProperty("hud").objectReferenceValue = hud;
            serialized.FindProperty("openingInstruction").stringValue = opening;
            serialized.FindProperty("skippedPieceMessage").stringValue = "Vẫn còn mảnh. Đừng đi tới đích vội.";
            serialized.FindProperty("retryInstruction").stringValue = retry;
            serialized.FindProperty("attemptFailedMessages").ClearArray();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(guide);
        }

        private static DialogueController AssignDialogue(
            StoryEvent storyEvent,
            string oldName,
            string newName,
            DialogueData data,
            DialogueController fallbackController)
        {
            Transform child = FindDirectChild(storyEvent.transform, newName) ??
                              FindDirectChild(storyEvent.transform, oldName);
            if (child == null)
                throw new InvalidOperationException($"Missing DialogueStep '{oldName}' under {storyEvent.name}.");
            DialogueStep step = child.GetComponent<DialogueStep>();
            if (step == null)
                throw new InvalidOperationException($"'{child.name}' is not a DialogueStep.");

            SerializedObject serialized = new SerializedObject(step);
            DialogueController controller = serialized.FindProperty("dialogueController").objectReferenceValue as DialogueController;
            if (controller == null)
                controller = fallbackController;
            serialized.FindProperty("dialogueData").objectReferenceValue = data;
            serialized.FindProperty("dialogueController").objectReferenceValue = controller;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            child.name = newName;
            return controller;
        }

        private static void EnsureOneUseTutorialStep(
            StoryEvent storyEvent,
            DialogueData data,
            DialogueController controller)
        {
            Transform tutorial = FindDirectChild(storyEvent.transform, "25_OneUseTileTutorial");
            if (tutorial == null)
            {
                GameObject child = new GameObject("25_OneUseTileTutorial", typeof(DialogueStep));
                tutorial = child.transform;
                tutorial.SetParent(storyEvent.transform, false);
            }

            DialogueStep step = tutorial.GetComponent<DialogueStep>();
            SerializedObject serialized = new SerializedObject(step);
            serialized.FindProperty("dialogueData").objectReferenceValue = data;
            serialized.FindProperty("dialogueController").objectReferenceValue = controller;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            Transform puzzle = FindDirectChild(storyEvent.transform, "30_WashroomStepTileTutorial");
            if (puzzle != null)
                tutorial.SetSiblingIndex(puzzle.GetSiblingIndex());
        }

        private static void ConfigureBusStopEnding(Scene scene, StoryEvent busStop)
        {
            Transform settle = FindDirectChild(busStop.transform, "50_SettleAtBusStop") ??
                               FindDirectChild(busStop.transform, "50_HoldBusStopPresentation");
            if (settle == null)
                throw new InvalidOperationException("Missing bus-stop settle step.");

            // The Day 1 source collapses the board here. Day 2 deliberately keeps the completed
            // Goal and both bus-stop scenery sprites visible throughout the remaining dialogue.
            BoardTileTransitionStep collapse = settle.GetComponent<BoardTileTransitionStep>();
            if (collapse != null)
                UnityEngine.Object.DestroyImmediate(collapse);

            WaitStep hold = settle.GetComponent<WaitStep>();
            if (hold == null)
                hold = settle.gameObject.AddComponent<WaitStep>();
            settle.name = "50_HoldBusStopPresentation";
            SerializedObject holdSerialized = new SerializedObject(hold);
            holdSerialized.FindProperty("duration").floatValue = .12f;
            holdSerialized.FindProperty("useUnscaledTime").boolValue = true;
            holdSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hold);

            for (int index = busStop.transform.childCount - 1; index >= 0; index--)
            {
                Transform child = busStop.transform.GetChild(index);
                if (child.GetComponent<SceneLoadStep>() != null || child.GetComponent<CanvasFadeStep>() != null)
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }

            CanvasGroup transitionFade = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<CanvasGroup>(true))
                .SingleOrDefault(group => group.name == "Transition Fade");
            if (transitionFade == null)
                throw new InvalidOperationException("Missing the scene-authored 'Transition Fade' CanvasGroup.");

            GameObject fadeObject = new GameObject("110_FadeAfterBusStopDialogue", typeof(CanvasFadeStep));
            fadeObject.transform.SetParent(busStop.transform, false);
            CanvasFadeStep fade = fadeObject.GetComponent<CanvasFadeStep>();
            SerializedObject fadeSerialized = new SerializedObject(fade);
            fadeSerialized.FindProperty("canvasGroup").objectReferenceValue = transitionFade;
            fadeSerialized.FindProperty("targetAlpha").floatValue = 1f;
            fadeSerialized.FindProperty("duration").floatValue = .6f;
            fadeSerialized.FindProperty("useUnscaledTime").boolValue = true;
            fadeSerialized.ApplyModifiedPropertiesWithoutUndo();
            fadeObject.transform.SetAsLastSibling();
            EditorUtility.SetDirty(fade);
            Audere.EditorTools.Day2SchoolMorningSetupTool.LinkHome();
        }

        private static DialogueData ConfigureDialogue(
            string assetName,
            string dialogueId,
            params DialogueLine[] lines)
        {
            DialogueData data = EnsureAsset<DialogueData>($"{DialogueFolder}/{assetName}.asset");
            SerializedObject serialized = new SerializedObject(data);
            serialized.FindProperty("dialogueId").stringValue = dialogueId;
            serialized.FindProperty("leftCharacter").intValue = (int)DialogueCharacterId.Audere;
            serialized.FindProperty("rightCharacter").intValue = (int)DialogueCharacterId.Timor;
            serialized.FindProperty("leftPortraitOverride").objectReferenceValue = null;
            serialized.FindProperty("rightPortraitOverride").objectReferenceValue = null;
            SerializedProperty lineList = serialized.FindProperty("lines");
            lineList.arraySize = lines.Length;
            for (int index = 0; index < lines.Length; index++)
            {
                SerializedProperty line = lineList.GetArrayElementAtIndex(index);
                line.FindPropertyRelative("speaker").intValue = (int)lines[index].Side;
                line.FindPropertyRelative("text").stringValue = lines[index].Text;
                line.FindPropertyRelative("portraitOverride").objectReferenceValue = null;
                if (lines[index].Text.Length > 42)
                    Debug.LogWarning($"[Day2HomeMorningSetup] '{assetName}' line {index + 1} exceeds 42 characters.", data);
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }

        private static PuzzleData ConfigurePuzzleData(
            string assetName,
            string puzzleId,
            Vector2Int start,
            IReadOnlyList<TileCell> cells,
            params PathPieceData[] pieces)
        {
            PuzzleData data = EnsureAsset<PuzzleData>($"{PuzzleFolder}/{assetName}.asset");
            SerializedObject serialized = new SerializedObject(data);
            serialized.FindProperty("puzzleId").stringValue = puzzleId;
            serialized.FindProperty("playerStartPosition").vector2IntValue = start;
            TileCell goal = cells.Single(cell => cell.Type == PuzzleTileType.Goal);
            serialized.FindProperty("goalPosition").vector2IntValue = goal.Position;
            serialized.FindProperty("requireAllPathPieces").boolValue = true;

            SerializedProperty tiles = serialized.FindProperty("boardTiles");
            tiles.arraySize = cells.Count;
            for (int index = 0; index < cells.Count; index++)
            {
                SerializedProperty tile = tiles.GetArrayElementAtIndex(index);
                tile.FindPropertyRelative("position").vector2IntValue = cells[index].Position;
                tile.FindPropertyRelative("tileType").enumValueIndex = (int)cells[index].Type;
                tile.FindPropertyRelative("dialogue").objectReferenceValue = null;
                tile.FindPropertyRelative("triggerDialogueOnce").boolValue = false;
            }

            SerializedProperty pieceList = serialized.FindProperty("availablePathPieces");
            pieceList.arraySize = pieces.Length;
            for (int index = 0; index < pieces.Length; index++)
                pieceList.GetArrayElementAtIndex(index).objectReferenceValue = pieces[index];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }

        private static StoryEvent FindEvent(Scene scene, string preferredName, string fallbackName)
        {
            StoryEvent match = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<StoryEvent>(true))
                .FirstOrDefault(item => item.name == preferredName || item.name == fallbackName);
            if (match == null)
                throw new InvalidOperationException($"Missing StoryEvent '{fallbackName}'.");
            return match;
        }

        private static PuzzleController FindPuzzle(Scene scene, string preferredRoot, string fallbackRoot)
        {
            PuzzleController match = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<PuzzleController>(true))
                .FirstOrDefault(item =>
                    item.PuzzleRoot != null &&
                    (item.PuzzleRoot.name == preferredRoot || item.PuzzleRoot.name == fallbackRoot));
            if (match == null)
                throw new InvalidOperationException($"Missing puzzle root '{fallbackRoot}'.");
            return match;
        }

        private static void ConfigureEvent(StoryEvent storyEvent, string eventId, bool autoNext, StoryEvent next)
        {
            storyEvent.name = eventId;
            SerializedObject serialized = new SerializedObject(storyEvent);
            serialized.FindProperty("eventId").stringValue = eventId;
            serialized.FindProperty("autoPlayNextEvent").boolValue = autoNext;
            serialized.FindProperty("nextEvent").objectReferenceValue = next;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureDirector(StoryDirector director, StoryEvent startingEvent)
        {
            SerializedObject serialized = new SerializedObject(director);
            serialized.FindProperty("storyEventsRoot").objectReferenceValue = director.transform;
            serialized.FindProperty("playOnStart").boolValue = true;
            serialized.FindProperty("startingEvent").objectReferenceValue = startingEvent;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T RequireRootComponent<T>(Scene scene, string rootName) where T : Component
        {
            GameObject root = scene.GetRootGameObjects().FirstOrDefault(item => item.name == rootName);
            T component = root != null ? root.GetComponent<T>() : null;
            if (component == null)
                throw new InvalidOperationException($"Missing {typeof(T).Name} on root '{rootName}'.");
            return component;
        }

        private static Transform FindDirectChild(Transform parent, string childName)
        {
            for (int index = 0; index < parent.childCount; index++)
            {
                Transform child = parent.GetChild(index);
                if (child.name == childName)
                    return child;
            }
            return null;
        }

        private static void AddSceneToBuildSettings(string path)
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(scene => scene.path == path))
                return;
            scenes.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] segments = folderPath.Split('/');
            string current = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string next = $"{current}/{segments[index]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, segments[index]);
                current = next;
            }
        }

        private static T EnsureAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;
            asset = ScriptableObject.CreateInstance<T>();
            asset.name = Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new InvalidOperationException($"Missing asset: {path}");
            return asset;
        }

        private static DialogueLine L(string text) => new DialogueLine(DialogueSpeakerSide.Left, text);
        private static DialogueLine R(string text) => new DialogueLine(DialogueSpeakerSide.Right, text);
        private static TileCell T(int x, int y, PuzzleTileType type = PuzzleTileType.Grass) =>
            new TileCell(new Vector2Int(x, y), type);

        private readonly struct DialogueLine
        {
            public DialogueLine(DialogueSpeakerSide side, string text) { Side = side; Text = text; }
            public DialogueSpeakerSide Side { get; }
            public string Text { get; }
        }

        private readonly struct TileCell
        {
            public TileCell(Vector2Int position, PuzzleTileType type) { Position = position; Type = type; }
            public Vector2Int Position { get; }
            public PuzzleTileType Type { get; }
        }

        private readonly struct DialogueAssets
        {
            public DialogueAssets(
                DialogueData opening,
                DialogueData afterBrushing,
                DialogueData checkBag,
                DialogueData checkDoor,
                DialogueData oneUseTutorial,
                DialogueData biancaThought,
                DialogueData eventThought,
                DialogueData standSafe)
            {
                Opening = opening;
                AfterBrushing = afterBrushing;
                CheckBag = checkBag;
                CheckDoor = checkDoor;
                OneUseTutorial = oneUseTutorial;
                BiancaThought = biancaThought;
                EventThought = eventThought;
                StandSafe = standSafe;
            }

            public DialogueData Opening { get; }
            public DialogueData AfterBrushing { get; }
            public DialogueData CheckBag { get; }
            public DialogueData CheckDoor { get; }
            public DialogueData OneUseTutorial { get; }
            public DialogueData BiancaThought { get; }
            public DialogueData EventThought { get; }
            public DialogueData StandSafe { get; }
        }

        private readonly struct PuzzleAssets
        {
            public PuzzleAssets(PuzzleData washroom, PuzzleData breakfast, PuzzleData busStop)
            {
                Washroom = washroom;
                Breakfast = breakfast;
                BusStop = busStop;
            }

            public PuzzleData Washroom { get; }
            public PuzzleData Breakfast { get; }
            public PuzzleData BusStop { get; }
        }
    }
}
#endif
