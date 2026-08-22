using Audere.Puzzle.Board;
using Audere.Puzzle.PathPieces;
using Audere.Puzzle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Audere.Puzzle.Editor
{
    public static class PuzzleSceneArchitectureBuilder
    {
        [MenuItem("Audere/Puzzle/Migrate Gameplay Scene Architecture")]
        public static void MigrateOpenGameplayScene()
        {
            PuzzleRuntime existingRuntime = Object.FindFirstObjectByType<PuzzleRuntime>(
                FindObjectsInactive.Include);
            if (existingRuntime != null)
            {
                Debug.Log(
                    "[PuzzleSceneArchitectureBuilder] This scene already uses one shared Puzzle Runtime. " +
                    "The legacy migration was skipped so it cannot pull Path Placement back into a level branch.",
                    existingRuntime);
                Selection.activeObject = existingRuntime.gameObject;
                return;
            }

            Transform worldRoot = FindOrCreateRoot("WORLD");
            Transform systemsRoot = FindOrCreateRoot("SYSTEMS");
            Transform puzzleRoot = FindOrCreateChild(worldRoot, "Puzzle Root");
            Transform placedPathRoot = puzzleRoot.Find("Placed Path Root");
            if (placedPathRoot == null)
            {
                placedPathRoot = worldRoot.Find("Placed Path Root");
                if (placedPathRoot == null)
                    placedPathRoot = FindOrCreateChild(puzzleRoot, "Placed Path Root");
            }
            placedPathRoot.SetParent(puzzleRoot, true);

            BoardManager oldBoard = Object.FindFirstObjectByType<BoardManager>();
            bool hasSystemBoard = oldBoard != null && oldBoard.transform.parent == systemsRoot;
            if (puzzleRoot.GetComponent<GridSpace2D>() == null && oldBoard != null && !hasSystemBoard)
                puzzleRoot.position = oldBoard.transform.position;

            Transform boardVisualRoot = GetConfiguredBoardVisualRoot(oldBoard);
            if (boardVisualRoot == null || boardVisualRoot.GetComponent<BoardManager>() != null)
                boardVisualRoot = FindOrCreateChild(puzzleRoot, "StepTile Board");
            boardVisualRoot.name = "StepTile Board";
            boardVisualRoot.SetParent(puzzleRoot, true);
            boardVisualRoot.localPosition = Vector3.zero;
            RemoveGeneratedBoardChildren(boardVisualRoot);
            RemoveDuplicateEmptyBoardRoots(puzzleRoot, boardVisualRoot);
            Transform objectiveRoot = FindOrCreateChild(puzzleRoot, "Goal");

            GridSpace2D gridSpace = puzzleRoot.GetComponent<GridSpace2D>();
            if (gridSpace == null)
                gridSpace = puzzleRoot.gameObject.AddComponent<GridSpace2D>();

            GridPlayer player = Object.FindFirstObjectByType<GridPlayer>();
            if (player != null)
                player.transform.SetParent(puzzleRoot, true);

            PuzzleManager puzzleManager = Object.FindFirstObjectByType<PuzzleManager>();
            PathPlacementController placement = Object.FindFirstObjectByType<PathPlacementController>();
            if (puzzleManager != null) puzzleManager.transform.SetParent(systemsRoot, true);
            if (placement != null) placement.transform.SetParent(systemsRoot, true);

            Transform boardControllerTransform = FindOrCreateChild(systemsRoot, "Board Controller");
            BoardManager boardController = boardControllerTransform.GetComponent<BoardManager>();
            if (boardController == null)
                boardController = boardControllerTransform.gameObject.AddComponent<BoardManager>();

            if (oldBoard != null && oldBoard != boardController)
            {
                CopyBoardAuthoringSettings(oldBoard, boardController);
                Object.DestroyImmediate(oldBoard);
            }

            ConfigureBoardController(
                boardController,
                gridSpace,
                boardVisualRoot,
                objectiveRoot);

            Camera gameplayCamera = Camera.main != null
                ? Camera.main
                : Object.FindFirstObjectByType<Camera>();
            if (gameplayCamera != null)
            {
                ConfigureGameplayCamera(gameplayCamera);
                ConfigurePuzzleViewportMask(worldRoot, puzzleRoot, gameplayCamera);
                GridCameraFollow2D follow = gameplayCamera.GetComponent<GridCameraFollow2D>();
                if (follow == null)
                    follow = gameplayCamera.gameObject.AddComponent<GridCameraFollow2D>();
                follow.enabled = false;
                follow.Configure(player, boardController);
                follow.ConfigureMotion(Vector2.zero, .18f);
                follow.ConfigureFramingCoverage(new Vector2(.56f, .60f));
                follow.ConfigureBoardClamping(false);
            }

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            PathPreview preview = Object.FindFirstObjectByType<PathPreview>();
            PathPieceHand hand = Object.FindFirstObjectByType<PathPieceHand>();
            PuzzleHud hud = Object.FindFirstObjectByType<PuzzleHud>();

            if (preview != null)
            {
                preview.gameObject.name = "Path Preview";
                preview.transform.SetAsFirstSibling();
            }

            if (hand != null)
                hand.gameObject.name = "Path Piece Hand UI";

            if (canvas != null && hud != null)
            {
                Transform debugRoot = FindOrCreateChild(canvas.transform, "Debug UI");
                hud.transform.SetParent(debugRoot, true);
            }

            ConfigureSystems(
                puzzleManager,
                boardController,
                player,
                placement,
                gridSpace,
                gameplayCamera,
                canvas,
                preview,
                hand,
                hud);

            GameObject legacyRuntime = GameObject.Find("Puzzle Runtime");
            if (legacyRuntime != null && legacyRuntime.transform.childCount == 0)
                Object.DestroyImmediate(legacyRuntime);

            placedPathRoot.localPosition = Vector3.zero;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Selection.activeObject = puzzleRoot.gameObject;
        }

        private static void ConfigureGameplayCamera(Camera gameplayCamera)
        {
            GameObject obsoleteOverview = GameObject.Find("Overview Camera");
            if (obsoleteOverview != null && obsoleteOverview != gameplayCamera.gameObject)
                Object.DestroyImmediate(obsoleteOverview);

            gameplayCamera.gameObject.name = "Main Camera";
            gameplayCamera.tag = "MainCamera";
            gameplayCamera.orthographic = true;
            gameplayCamera.orthographicSize = 1.25f;
            gameplayCamera.rect = new Rect(0f, 0f, 1f, 1f);
            gameplayCamera.depth = 0f;

        }

        private static void ConfigurePuzzleViewportMask(
            Transform worldRoot,
            Transform puzzleRoot,
            Camera gameplayCamera)
        {
            Transform viewportMask = puzzleRoot.Find("PuzzleViewportMask");
            if (viewportMask == null)
                viewportMask = worldRoot.Find("GameplayMask");
            if (viewportMask == null)
                viewportMask = gameplayCamera.transform.Find("GameplayMask");
            if (viewportMask == null)
            {
                GameObject namedMask = GameObject.Find("GameplayMask");
                viewportMask = namedMask != null ? namedMask.transform : null;
            }
            if (viewportMask == null)
                return;

            viewportMask.name = "PuzzleViewportMask";
            viewportMask.SetParent(gameplayCamera.transform, false);
            viewportMask.localPosition = new Vector3(0f, 0f, 9f);
            viewportMask.localRotation = Quaternion.identity;
            viewportMask.localScale = Vector3.one * .5814f;
            viewportMask.gameObject.SetActive(true);
        }

        private static void CopyBoardAuthoringSettings(BoardManager source, BoardManager destination)
        {
            SerializedObject sourceObject = new SerializedObject(source);
            SerializedObject destinationObject = new SerializedObject(destination);
            CopyProperty("tileCatalog");
            CopyProperty("buildDemoBoardOnAwake");
            CopyProperty("demoCells");
            destinationObject.ApplyModifiedPropertiesWithoutUndo();

            void CopyProperty(string propertyName)
            {
                SerializedProperty from = sourceObject.FindProperty(propertyName);
                SerializedProperty to = destinationObject.FindProperty(propertyName);
                if (from != null && to != null)
                    destinationObject.CopyFromSerializedProperty(from);
            }
        }

        private static void ConfigureBoardController(
            BoardManager board,
            GridSpace2D gridSpace,
            Transform boardVisualRoot,
            Transform objectiveRoot)
        {
            SerializedObject serializedBoard = new SerializedObject(board);
            serializedBoard.FindProperty("gridSpace").objectReferenceValue = gridSpace;
            serializedBoard.FindProperty("boardVisualRoot").objectReferenceValue = boardVisualRoot;
            serializedBoard.FindProperty("levelObjectiveRoot").objectReferenceValue = objectiveRoot;
            serializedBoard.FindProperty("buildDemoBoardOnAwake").boolValue = false;
            serializedBoard.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(board);
        }

        private static void ConfigureSystems(
            PuzzleManager puzzleManager,
            BoardManager board,
            GridPlayer player,
            PathPlacementController placement,
            GridSpace2D gridSpace,
            Camera gameplayCamera,
            Canvas canvas,
            PathPreview preview,
            PathPieceHand hand,
            PuzzleHud hud)
        {
            if (puzzleManager != null)
            {
                SerializedObject serializedPuzzle = new SerializedObject(puzzleManager);
                serializedPuzzle.FindProperty("board").objectReferenceValue = board;
                serializedPuzzle.FindProperty("player").objectReferenceValue = player;
                serializedPuzzle.FindProperty("hand").objectReferenceValue = hand;
                serializedPuzzle.FindProperty("placement").objectReferenceValue = placement;
                serializedPuzzle.FindProperty("hud").objectReferenceValue = hud;
                serializedPuzzle.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(puzzleManager);

                PuzzleController controller = puzzleManager.GetComponent<PuzzleController>();
                if (controller == null)
                    controller = puzzleManager.gameObject.AddComponent<PuzzleController>();
                SerializedObject serializedController = new SerializedObject(controller);
                serializedController.FindProperty("puzzle").objectReferenceValue = puzzleManager;
                serializedController.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(controller);
            }

            if (placement != null)
            {
                placement.enabled = true;
                SerializedObject serializedPlacement = new SerializedObject(placement);
                serializedPlacement.FindProperty("boardCamera").objectReferenceValue = gameplayCamera;
                serializedPlacement.FindProperty("gridSpace").objectReferenceValue = gridSpace;
                serializedPlacement.FindProperty("puzzleCanvas").objectReferenceValue = canvas;
                serializedPlacement.FindProperty("preview").objectReferenceValue = preview;
                serializedPlacement.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(placement);
            }
        }

        private static void RemoveGeneratedBoardChildren(Transform boardRoot)
        {
            for (int index = boardRoot.childCount - 1; index >= 0; index--)
            {
                Transform child = boardRoot.GetChild(index);
                // Only remove the obsolete runtime container. Direct BoardTile children
                // are scene-authored layout and must survive architecture migrations.
                if (child.name == "Board Tiles")
                    Object.DestroyImmediate(child.gameObject);
            }
        }

        private static Transform GetConfiguredBoardVisualRoot(BoardManager board)
        {
            if (board == null)
                return null;

            SerializedObject serializedBoard = new SerializedObject(board);
            return serializedBoard.FindProperty("boardVisualRoot")?.objectReferenceValue as Transform;
        }

        private static void RemoveDuplicateEmptyBoardRoots(
            Transform puzzleRoot,
            Transform retainedBoardRoot)
        {
            for (int index = puzzleRoot.childCount - 1; index >= 0; index--)
            {
                Transform child = puzzleRoot.GetChild(index);
                if (child == retainedBoardRoot || child.name != "StepTile Board")
                    continue;
                if (child.childCount == 0 && child.GetComponents<Component>().Length == 1)
                    Object.DestroyImmediate(child.gameObject);
            }
        }

        private static Transform FindOrCreateRoot(string name)
        {
            GameObject existing = GameObject.Find(name);
            if (existing != null && existing.transform.parent == null)
                return existing.transform;

            return new GameObject(name).transform;
        }

        private static Transform FindOrCreateChild(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
                return existing;

            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.transform;
        }
    }
}
