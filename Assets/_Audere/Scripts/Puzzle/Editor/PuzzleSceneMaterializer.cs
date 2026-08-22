using System.Collections.Generic;
using Audere.Puzzle.Board;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Audere.Puzzle.Editor
{
    public static class PuzzleSceneMaterializer
    {
        public static int CountExistingTiles(PuzzleManager manager)
        {
            return manager != null && manager.Board != null
                ? CollectTiles(manager.Board).Count
                : 0;
        }

        public static bool Materialize(
            PuzzleManager manager,
            PuzzleData source,
            PuzzleTileCatalog catalog,
            bool replaceExisting)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[PuzzleSceneMaterializer] Bake is only available in Edit Mode.");
                return false;
            }

            if (manager == null || source == null || catalog == null || manager.Board == null)
            {
                Debug.LogError(
                    "[PuzzleSceneMaterializer] Assign PuzzleManager, PuzzleData, Tile Catalog and BoardManager.",
                    manager);
                return false;
            }

            BoardManager board = manager.Board;
            if (board.GridSpace == null || board.BoardVisualRoot == null)
            {
                Debug.LogError(
                    "[PuzzleSceneMaterializer] BoardManager needs GridSpace and Board Visual Root references.",
                    board);
                return false;
            }

            HashSet<BoardTile> existingTiles = CollectTiles(board);
            if (existingTiles.Count > 0 && !replaceExisting)
                return false;

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Materialize Puzzle Into Scene");

            foreach (BoardTile existingTile in existingTiles)
                if (existingTile != null)
                    Undo.DestroyObjectImmediate(existingTile.gameObject);

            int tileIndex = 0;
            foreach (PuzzleTileData tileData in source.BoardTiles)
            {
                if (!catalog.TryGetPrefab(tileData.TileType, out BoardTile prefab))
                {
                    Debug.LogError(
                        $"[PuzzleSceneMaterializer] No prefab registered for {tileData.TileType}.",
                        catalog);
                    continue;
                }

                Transform parent = tileData.TileType == PuzzleTileType.Goal && board.LevelObjectiveRoot != null
                    ? board.LevelObjectiveRoot
                    : board.BoardVisualRoot;
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(
                    prefab.gameObject,
                    parent);
                Undo.RegisterCreatedObjectUndo(instance, "Create Puzzle Tile");

                BoardTile tile = instance.GetComponent<BoardTile>();
                tile.transform.position = board.GridSpace.CellToWorldCenter(tileData.Position);
                tile.Initialize(tileData, board.GridSpace.CellSize);
                tile.name = $"Tile_{tileIndex:00}_{tileData.TileType}_{tileData.Position.x}_{tileData.Position.y}";
                RecordPrefabOverrides(instance);
                tileIndex++;
            }

            PuzzlePlayerStart playerStart = EnsurePlayerStart(manager, board.GridSpace);
            Undo.RecordObject(playerStart.transform, "Move Player Start");
            playerStart.transform.position = board.GridSpace.CellToWorldCenter(source.PlayerStartPosition);
            EditorUtility.SetDirty(playerStart.transform);

            if (manager.Player != null)
            {
                Undo.RecordObject(manager.Player.transform, "Move Puzzle Player Preview");
                manager.Player.transform.position = playerStart.transform.position;
                EditorUtility.SetDirty(manager.Player.transform);
            }

            SerializedObject serializedManager = new SerializedObject(manager);
            serializedManager.FindProperty("puzzleData").objectReferenceValue = source;
            serializedManager.FindProperty("playerStart").objectReferenceValue = playerStart;
            serializedManager.ApplyModifiedProperties();

            SerializedObject serializedBoard = new SerializedObject(board);
            serializedBoard.FindProperty("buildDemoBoardOnAwake").boolValue = false;
            serializedBoard.ApplyModifiedProperties();

            PuzzleController controller = manager.GetComponent<PuzzleController>();
            if (controller == null)
                controller = Undo.AddComponent<PuzzleController>(manager.gameObject);
            SerializedObject serializedController = new SerializedObject(controller);
            serializedController.FindProperty("puzzle").objectReferenceValue = manager;
            serializedController.ApplyModifiedProperties();

            board.RegisterExistingTiles();
            EditorUtility.SetDirty(manager);
            EditorUtility.SetDirty(board);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeObject = manager.gameObject;

            Debug.Log(
                $"[PuzzleSceneMaterializer] Baked {tileIndex} scene-authored tiles from {source.name}. " +
                "The scene/prefab objects are now the layout source of truth.",
                manager);
            return true;
        }

        private static PuzzlePlayerStart EnsurePlayerStart(
            PuzzleManager manager,
            GridSpace2D gridSpace)
        {
            PuzzlePlayerStart playerStart = manager.PlayerStart;
            if (playerStart != null)
                return playerStart;

            // Player is shared by the location, but PlayerStart belongs to one
            // concrete scene-authored puzzle. Search only inside that puzzle's
            // branch so baking a second board cannot steal the first marker.
            Transform puzzleRoot = manager.transform;
            while (puzzleRoot.parent != null && puzzleRoot.parent != gridSpace.transform)
                puzzleRoot = puzzleRoot.parent;

            playerStart = puzzleRoot.GetComponentInChildren<PuzzlePlayerStart>(true);
            if (playerStart != null)
                return playerStart;

            GameObject marker = new GameObject("PlayerStart");
            Undo.RegisterCreatedObjectUndo(marker, "Create Player Start");
            marker.transform.SetParent(puzzleRoot, false);
            return Undo.AddComponent<PuzzlePlayerStart>(marker);
        }

        private static HashSet<BoardTile> CollectTiles(BoardManager board)
        {
            HashSet<BoardTile> results = new HashSet<BoardTile>();
            CollectTiles(board.BoardVisualRoot, results);
            CollectTiles(board.LevelObjectiveRoot, results);
            return results;
        }

        private static void CollectTiles(Transform root, HashSet<BoardTile> results)
        {
            if (root == null)
                return;

            foreach (BoardTile tile in root.GetComponentsInChildren<BoardTile>(true))
                results.Add(tile);
        }

        private static void RecordPrefabOverrides(GameObject instance)
        {
            foreach (Component component in instance.GetComponentsInChildren<Component>(true))
            {
                EditorUtility.SetDirty(component);
                if (PrefabUtility.IsPartOfPrefabInstance(component))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            }
        }
    }
}
