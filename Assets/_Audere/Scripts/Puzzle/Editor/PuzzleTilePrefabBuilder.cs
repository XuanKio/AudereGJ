using System.IO;
using System.Linq;
using Audere.Puzzle.Board;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Audere.Puzzle.Editor
{
    public static class PuzzleTilePrefabBuilder
    {
        private const string GrassSourcePath = "Assets/_Audere/AssetGame/Step Tile/grass.aseprite";
        private const string GoalOverlaySourcePath = "Assets/_Audere/AssetGame/Step Tile/cursor.aseprite";
        private const string TilePrefabFolder = "Assets/_Audere/Prefabs/Puzzle/Tiles";

        [MenuItem("Audere/Puzzle/Create Missing Tile Prefabs & Refresh Catalog")]
        public static void CreateMissingTilePrefabs()
        {
            EnsureAssetFolder(TilePrefabFolder);
            EnsureAssetFolder(Path.GetDirectoryName(PuzzleContentConstants.AssetPaths.TileCatalog)?.Replace('\\', '/'));

            BoardTile grassPrefab = BuildGrassPrefab();
            BoardTile goalPrefab = BuildGoalPrefab();
            PuzzleTileCatalog catalog = BuildCatalog(grassPrefab, goalPrefab);
            AssignCatalogToOpenScenes(catalog);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = catalog;
            Debug.Log("[PuzzleTilePrefabBuilder] Tile prefabs are preserved; missing prefabs were created and catalog references refreshed.");
        }

        private static BoardTile BuildGrassPrefab()
        {
            BoardTile existing = AssetDatabase.LoadAssetAtPath<BoardTile>(
                PuzzleContentConstants.AssetPaths.GrassPrefab);
            if (existing != null)
                return existing;

            Sprite grassSprite = AssetDatabase.LoadAllAssetsAtPath(GrassSourcePath)
                .OfType<Sprite>()
                .FirstOrDefault();

            if (grassSprite == null)
                throw new System.InvalidOperationException($"No Sprite found in {GrassSourcePath}.");

            GameObject root = new GameObject("Grass");
            try
            {
                SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
                renderer.sprite = grassSprite;
                renderer.sortingOrder = 0;
                root.AddComponent<GrassTileBehaviour>();
                root.AddComponent<BoardTile>();

                GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, PuzzleContentConstants.AssetPaths.GrassPrefab);
                if (savedPrefab == null)
                    throw new System.InvalidOperationException("Unity could not save Grass.prefab.");

                return savedPrefab.GetComponent<BoardTile>();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static BoardTile BuildGoalPrefab()
        {
            BoardTile existing = AssetDatabase.LoadAssetAtPath<BoardTile>(
                PuzzleContentConstants.AssetPaths.GoalPrefab);
            if (existing != null)
                return existing;

            Sprite grassSprite = AssetDatabase.LoadAllAssetsAtPath(GrassSourcePath)
                .OfType<Sprite>()
                .FirstOrDefault();
            Sprite goalSprite = AssetDatabase.LoadAllAssetsAtPath(GoalOverlaySourcePath)
                .OfType<Sprite>()
                .FirstOrDefault();

            if (grassSprite == null || goalSprite == null)
                throw new System.InvalidOperationException("Goal tile source sprites are missing.");

            GameObject root = new GameObject("Goal");
            try
            {
                GameObject baseVisual = new GameObject("Tile Visual");
                baseVisual.transform.SetParent(root.transform, false);
                SpriteRenderer baseRenderer = baseVisual.AddComponent<SpriteRenderer>();
                baseRenderer.sprite = grassSprite;
                baseRenderer.sortingOrder = 0;

                GameObject goalVisual = new GameObject("Goal Visual");
                goalVisual.transform.SetParent(root.transform, false);
                goalVisual.transform.localScale = Vector3.one * .58f;
                SpriteRenderer goalRenderer = goalVisual.AddComponent<SpriteRenderer>();
                goalRenderer.sprite = goalSprite;
                goalRenderer.sortingOrder = 2;

                root.AddComponent<GoalTileBehaviour>();
                BoardTile tile = root.AddComponent<BoardTile>();
                SerializedObject serializedTile = new SerializedObject(tile);
                serializedTile.FindProperty("tileType").enumValueIndex = (int)PuzzleTileType.Goal;
                serializedTile.ApplyModifiedPropertiesWithoutUndo();

                GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(
                    root,
                    PuzzleContentConstants.AssetPaths.GoalPrefab);
                if (savedPrefab == null)
                    throw new System.InvalidOperationException("Unity could not save Goal.prefab.");

                return savedPrefab.GetComponent<BoardTile>();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static PuzzleTileCatalog BuildCatalog(BoardTile grassPrefab, BoardTile goalPrefab)
        {
            PuzzleTileCatalog catalog = AssetDatabase.LoadAssetAtPath<PuzzleTileCatalog>(
                PuzzleContentConstants.AssetPaths.TileCatalog);

            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<PuzzleTileCatalog>();
                AssetDatabase.CreateAsset(catalog, PuzzleContentConstants.AssetPaths.TileCatalog);
            }

            SerializedObject serializedCatalog = new SerializedObject(catalog);
            SerializedProperty entries = serializedCatalog.FindProperty("entries");
            AddOrUpdateEntry(entries, PuzzleTileType.Grass, grassPrefab);
            AddOrUpdateEntry(entries, PuzzleTileType.Goal, goalPrefab);
            serializedCatalog.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static void AddOrUpdateEntry(
            SerializedProperty entries,
            PuzzleTileType type,
            BoardTile prefab)
        {
            int entryIndex = -1;
            for (int index = 0; index < entries.arraySize; index++)
            {
                SerializedProperty candidate = entries.GetArrayElementAtIndex(index);
                if (candidate.FindPropertyRelative("tileType").enumValueIndex == (int)type)
                {
                    entryIndex = index;
                    break;
                }
            }

            if (entryIndex < 0)
            {
                entryIndex = entries.arraySize;
                entries.InsertArrayElementAtIndex(entryIndex);
            }

            SerializedProperty entry = entries.GetArrayElementAtIndex(entryIndex);
            entry.FindPropertyRelative("tileType").enumValueIndex = (int)type;
            entry.FindPropertyRelative("prefab").objectReferenceValue = prefab;
        }

        private static void AssignCatalogToOpenScenes(PuzzleTileCatalog catalog)
        {
            foreach (BoardManager manager in Object.FindObjectsByType<BoardManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                SerializedObject serializedManager = new SerializedObject(manager);
                serializedManager.FindProperty("tileCatalog").objectReferenceValue = catalog;
                serializedManager.FindProperty("buildDemoBoardOnAwake").boolValue = false;
                serializedManager.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(manager);
                EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            }
        }

        private static void EnsureAssetFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                return;

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
    }
}
