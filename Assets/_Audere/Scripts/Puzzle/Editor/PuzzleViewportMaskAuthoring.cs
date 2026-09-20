#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Audere.Puzzle.Editor
{
    public static class PuzzleViewportMaskAuthoring
    {
        public const string PrefabPath = "Assets/_Audere/Prefabs/Puzzle/Camera/PuzzleViewportMask.prefab";
        private static readonly string[] Names = { "Mask Top", "Mask Bottom", "Mask Left", "Mask Right" };

        [MenuItem("Audere/Puzzle/Repair Shared Viewport Mask")]
        public static void RepairAll()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before viewport authoring.");
            string original = SceneManager.GetActiveScene().path;
            if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the current scene before viewport authoring.");
            ConfigurePrefab();
            int count = 0;
            try
            {
                foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/_Audere/Scenes" }))
                    count += MigrateScene(AssetDatabase.GUIDToAssetPath(guid));
            }
            finally { if (!string.IsNullOrEmpty(original)) EditorSceneManager.OpenScene(original); }
            Debug.Log($"[PuzzleViewportMask] {count} scene masks connected to the shared adaptive prefab.");
        }

        public static void ConfigurePrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var originals = Names.Select(n => root.transform.Find(n).GetComponent<SpriteRenderer>()).ToArray();
                Transform coverageRoot = root.transform.Find("Screen Coverage");
                if (coverageRoot == null)
                {
                    coverageRoot = new GameObject("Screen Coverage").transform;
                    coverageRoot.SetParent(root.transform, false);
                }
                var covers = new SpriteRenderer[4];
                for (int i = 0; i < Names.Length; i++)
                {
                    string coverageName = Names[i].Replace("Mask ", "Coverage ");
                    Transform child = coverageRoot.Find(coverageName);
                    if (child == null) child = coverageRoot.Find(Names[i]);
                    if (child == null)
                    {
                        child = new GameObject(coverageName).transform;
                        child.SetParent(coverageRoot, false);
                    }
                    child.name = coverageName;
                    covers[i] = child.GetComponent<SpriteRenderer>();
                    if (covers[i] == null) covers[i] = child.gameObject.AddComponent<SpriteRenderer>();
                    EditorUtility.CopySerialized(originals[i], covers[i]);
                    child.localPosition = originals[i].transform.localPosition;
                    child.localRotation = originals[i].transform.localRotation;
                    child.localScale = originals[i].transform.localScale;
                }
                var fitter = root.GetComponent<PuzzleViewportMaskFitter>();
                if (fitter == null) fitter = root.AddComponent<PuzzleViewportMaskFitter>();
                var serialized = new SerializedObject(fitter);
                Bind(serialized.FindProperty("originalEdges"), originals);
                Bind(serialized.FindProperty("coverageEdges"), covers);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        public static int MigrateScene(string path)
        {
            var scene = EditorSceneManager.OpenScene(path);
            var masks = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .Where(t => t.name == "PuzzleViewportMask").ToArray();
            if (masks.Length == 0) return 0;
            Directory.CreateDirectory("Temp/ViewportQA/BeforeMigration");
            string backup = "Temp/ViewportQA/BeforeMigration/" + Path.GetFileName(path);
            if (!File.Exists(backup)) File.Copy(path, backup);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            foreach (var mask in masks)
            {
                var originals = new[] { mask }.Concat(Names.Select(n => mask.Find(n))).ToArray();
                var poses = originals.Select(t => (t.localPosition, t.localRotation, t.localScale)).ToArray();
                var references = CaptureReferences(scene, originals);
                if (!PrefabUtility.IsPartOfPrefabInstance(mask.gameObject))
                    PrefabUtility.ConvertToPrefabInstance(mask.gameObject, prefab, new ConvertToPrefabInstanceSettings
                    {
                        objectMatchMode = ObjectMatchMode.ByHierarchy,
                        componentsNotMatchedBecomesOverride = true,
                        gameObjectsNotMatchedBecomesOverride = true,
                        recordPropertyOverridesOfMatches = true
                    }, InteractionMode.AutomatedAction);
                if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(mask.gameObject) != PrefabPath)
                    throw new InvalidOperationException(path + ": unexpected viewport prefab source.");
                for (int i = 0; i < originals.Length; i++)
                    if (originals[i] == null || originals[i].localPosition != poses[i].localPosition ||
                        originals[i].localRotation != poses[i].localRotation || originals[i].localScale != poses[i].localScale)
                        throw new InvalidOperationException(path + ": original mask identity/pose changed.");
                foreach (var reference in references)
                    if (new SerializedObject(reference.owner).FindProperty(reference.path).objectReferenceValue != reference.target)
                        throw new InvalidOperationException(path + ": mask reference changed: " + reference.path);
                foreach (string name in Names)
                {
                    var renderer = mask.Find(name).GetComponent<SpriteRenderer>();
                    var color = new SerializedObject(renderer).FindProperty("m_Color");
                    if (color.prefabOverride) PrefabUtility.RevertPropertyOverride(color, InteractionMode.AutomatedAction);
                }
                if (mask.GetComponent<PuzzleViewportMaskFitter>() == null)
                    throw new InvalidOperationException(path + ": missing shared fitter.");
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return masks.Length;
        }

        private static List<(UnityEngine.Object owner, string path, UnityEngine.Object target)> CaptureReferences(Scene scene, Transform[] originals)
        {
            var targets = new HashSet<UnityEngine.Object>(originals.SelectMany(t => t.GetComponents<Component>().Cast<UnityEngine.Object>()
                .Append(t.gameObject)));
            var result = new List<(UnityEngine.Object, string, UnityEngine.Object)>();
            foreach (var component in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true)))
            {
                if (component == null) continue;
                var iterator = new SerializedObject(component).GetIterator();
                while (iterator.Next(true))
                    if (iterator.propertyType == SerializedPropertyType.ObjectReference && targets.Contains(iterator.objectReferenceValue))
                        result.Add((component, iterator.propertyPath, iterator.objectReferenceValue));
            }
            return result;
        }

        private static void Bind(SerializedProperty property, SpriteRenderer[] renderers)
        {
            property.arraySize = renderers.Length;
            for (int i = 0; i < renderers.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
        }
    }
}
#endif
