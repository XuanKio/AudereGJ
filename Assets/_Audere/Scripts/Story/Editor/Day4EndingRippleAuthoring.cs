#if UNITY_EDITOR
using System;
using System.Linq;
using Audere.Story.Presentation;
using Audere.Story.Steps;
using Audere.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Audere.Story.Editor
{
    public static class Day4EndingRippleAuthoring
    {
        public const string ProfilePath = "Assets/_Audere/Data/Transitions/TileRippleWalk_Ending.asset";
        [MenuItem("Audere/Story/Update Scene150 Continuous Ripple Ending")]
        public static void UpdateEnding()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = SceneManager.GetSceneByPath(Day4TimorEveningSetupTool.ScenePath);
            bool loaded = scene.IsValid() && scene.isLoaded;
            if (loaded && scene.isDirty) throw new InvalidOperationException("Scene150 has unsaved edits.");
            if (!loaded) scene = EditorSceneManager.OpenScene(Day4TimorEveningSetupTool.ScenePath, OpenSceneMode.Additive);
            try
            {
                Apply(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                if (!loaded) EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
            Debug.Log("[EndingPath] Scene150 saved: single white row, gradual reveal, slow accelerating walk; peach background only during ending.");
        }

        public static void Apply(Scene scene)
        {
            var profile = AssetDatabase.LoadAssetAtPath<TileRippleWalkProfile>(ProfilePath);
            if (profile == null)
            { profile = ScriptableObject.CreateInstance<TileRippleWalkProfile>(); AssetDatabase.CreateAsset(profile, ProfilePath); }
            const string materialPath = "Assets/_Audere/Materials/EndingTilesWhite.mat";
            Shader shader = Shader.Find("Audere/Ending Tile Ripple");
            if (shader == null) throw new MissingReferenceException("Ending Tile Ripple shader missing.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader); AssetDatabase.CreateAsset(material, materialPath);
            }
            material.shader = shader;
            EditorUtility.SetDirty(material);
            var story = All<StoryEvent>(scene).Single(e => e.transform.Find("160_TimorAgain") != null);
            var actor = All<SpriteRenderer>(scene).Single(r => r.name == "Audere");
            var shadow = actor.GetComponentsInChildren<SpriteRenderer>(true).Single(r => r != actor).transform;
            var stage = actor.transform.parent;
            var source = stage.Find("Night Tile PLACEHOLDER/Visual Root");
            if (source == null) throw new MissingReferenceException("Original floor sprite missing.");
            var camera = All<Camera>(scene).Single(c => c.CompareTag("MainCamera"));
            var cover = All<CanvasGroup>(scene).Single(c => c.name == "ENDING WHITE COVER");
            Transform oldTiles = stage.Find("ENDING TILES");
            Transform oldAnchors = stage.Find("ENDING STAGING");
            int index = story.transform.Find("210_TheFirstTilesOpen") != null
                ? story.transform.Find("210_TheFirstTilesOpen").GetSiblingIndex()
                : story.transform.Find("210_AudereWalksIntoTheLight").GetSiblingIndex();
            // Replace only the former tile/walk/hold/white-fade section; leave boss and credits intact.
            foreach (Transform child in story.transform.Cast<Transform>().ToArray())
                if (int.TryParse(child.name.Split('_')[0], out int number) && number >= 210 && number <= 320)
                    Object.DestroyImmediate(child.gameObject);
            if (oldTiles != null) Object.DestroyImmediate(oldTiles.gameObject);
            if (oldAnchors != null) Object.DestroyImmediate(oldAnchors.gameObject);

            var tileRoot = new GameObject("ENDING TILES"); tileRoot.SetActive(false);
            tileRoot.transform.SetParent(stage, false);
            var anchors = new GameObject("ENDING STAGING"); anchors.transform.SetParent(stage, false);
            anchors.SetActive(false);
            var targets = new Transform[18];
            var renderers = new System.Collections.Generic.List<SpriteRenderer>();
            Vector3 start = actor.transform.position;
            Vector3 feet = new Vector3(actor.bounds.center.x, actor.bounds.min.y, start.z);
            SpriteRenderer startingTile = null;
            for (int x = 0; x <= 20; x++)
            {
                var tile = Object.Instantiate(source.gameObject, tileRoot.transform);
                tile.name = $"Ending Tile {x:00}";
                tile.transform.position = feet + new Vector3(x * .25f, 0f, 0f);
                tile.SetActive(true);
                foreach (var renderer in tile.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    renderer.sharedMaterial = material; renderer.color = Color.white;
                    renderer.sortingOrder = 0;
                    renderers.Add(renderer);
                    if (x == 0) startingTile = renderer;
                }
            }
            for (int i = 0; i < targets.Length; i++)
            {
                targets[i] = new GameObject($"Audere_Path_{i + 1:00}").transform;
                targets[i].SetParent(anchors.transform, false);
                targets[i].position = start + Vector3.right * ((i + 1) * .25f);
            }
            var field = tileRoot.AddComponent<TileRippleField>();
            Set(field, "profile", profile); Set(field, "worldCamera", camera); Set(field, "whiteCover", cover);
            Set(field, "ownerEvent", story); Set(field, "originalFloor", source.parent.gameObject);
            Set(field, "startingTile", startingTile); SetArray(field, "tiles", renderers.ToArray());
            var go = new GameObject("210_AudereWalksIntoTheLight"); go.transform.SetParent(story.transform, false);
            go.transform.SetSiblingIndex(index);
            var walk = go.AddComponent<ContinuousTileWalkStep>();
            Set(walk, "actor", actor.transform); Set(walk, "actorRenderer", actor); Set(walk, "groundedShadow", shadow);
            Set(walk, "profile", profile); Set(walk, "rippleField", field); SetArray(walk, "waypoints", targets);
            var follow = All<StoryCameraFollow2D>(scene).Single();
            var followData = new SerializedObject(follow);
            followData.FindProperty("framingOffset").vector2Value = new Vector2(0f, -.04f);
            followData.FindProperty("smoothTime").floatValue = .12f;
            followData.ApplyModifiedPropertiesWithoutUndo();
            var enable = story.transform.Find("185_FollowAudereBeyondTheFrame").GetComponent<SetActiveStep>();
            SetArray(enable, "objectsToEnable", new[] { follow.gameObject, tileRoot });
        }
        private static T[] All<T>(Scene scene) where T : Component => scene.GetRootGameObjects()
            .SelectMany(go => go.GetComponentsInChildren<T>(true)).ToArray();
        private static void Set(Object target, string name, Object value)
        { var data = new SerializedObject(target); data.FindProperty(name).objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo(); }
        private static void SetArray(Object target, string name, Object[] values)
        {
            var data = new SerializedObject(target); var list = data.FindProperty(name); list.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
