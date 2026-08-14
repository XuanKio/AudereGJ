using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Audere.Puzzle.Editor
{
    public static class PuzzleActorPrefabBuilder
    {
        private const string ActorFolder = "Assets/_Audere/Prefabs/Puzzle/Actors";

        [MenuItem("Audere/Puzzle/Create Missing Actor Prefabs")]
        public static void CreateMissingActorPrefabs()
        {
            EnsureFolder(ActorFolder);
            GridPlayer playerPrefab = AssetDatabase.LoadAssetAtPath<GridPlayer>(
                PuzzleContentConstants.AssetPaths.PlayerPrefab);

            if (playerPrefab == null)
                playerPrefab = CreatePlayerPrefab();

            ReplaceLooseScenePlayer(playerPrefab);
            AssetDatabase.SaveAssets();
            Selection.activeObject = playerPrefab;
        }

        private static GridPlayer CreatePlayerPrefab()
        {
            GameObject root = new GameObject("Player");
            try
            {
                SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
                renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                renderer.sortingOrder = 5;
                root.AddComponent<GridPlayer>();

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(
                    root,
                    PuzzleContentConstants.AssetPaths.PlayerPrefab);
                return saved.GetComponent<GridPlayer>();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void ReplaceLooseScenePlayer(GridPlayer playerPrefab)
        {
            GridPlayer scenePlayer = Object.FindFirstObjectByType<GridPlayer>();
            GridPlayer effectivePlayer = scenePlayer;

            if (scenePlayer == null || !PrefabUtility.IsPartOfPrefabInstance(scenePlayer))
            {
                Transform parent = scenePlayer != null ? scenePlayer.transform.parent : null;
                Vector3 position = scenePlayer != null ? scenePlayer.transform.position : Vector3.zero;
                if (scenePlayer != null)
                    Object.DestroyImmediate(scenePlayer.gameObject);

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab.gameObject);
                instance.transform.SetParent(parent, true);
                instance.transform.position = position;
                effectivePlayer = instance.GetComponent<GridPlayer>();
            }

            foreach (PuzzleManager manager in Object.FindObjectsByType<PuzzleManager>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                SerializedObject serializedManager = new SerializedObject(manager);
                serializedManager.FindProperty("player").objectReferenceValue =
                    effectivePlayer;
                serializedManager.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(manager);
            }

            if (effectivePlayer != null)
                EditorSceneManager.MarkSceneDirty(effectivePlayer.gameObject.scene);
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] segments = folderPath.Split('/');
            string current = segments[0];
            for (int index = 1; index < segments.Length; index++)
            {
                string next = current + "/" + segments[index];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, segments[index]);
                current = next;
            }
        }
    }
}
