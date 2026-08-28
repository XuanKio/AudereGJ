#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Audere.Core.Editor
{
    [CustomEditor(typeof(Bootstrapper))]
    public sealed class BootstrapperEditor : UnityEditor.Editor
    {
        private SerializedProperty servicesRoot;
        private SerializedProperty firstSceneAsset;
        private SerializedProperty firstScenePath;

        private void OnEnable()
        {
            servicesRoot = serializedObject.FindProperty("servicesRoot");
            firstSceneAsset = serializedObject.FindProperty("firstSceneAsset");
            firstScenePath = serializedObject.FindProperty("firstScenePath");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(servicesRoot);

            List<EditorBuildSettingsScene> buildScenes = GetEnabledBuildScenes((Bootstrapper)target);
            if (buildScenes.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No enabled scenes found in Build Settings. Add one before choosing the First Scene.",
                    MessageType.Warning);
            }
            else
            {
                string[] labels = new string[buildScenes.Count];
                int selectedIndex = -1;
                for (int i = 0; i < buildScenes.Count; i++)
                {
                    string path = buildScenes[i].path;
                    labels[i] = Path.GetFileNameWithoutExtension(path);
                    if (path == firstScenePath.stringValue)
                        selectedIndex = i;
                }

                int nextIndex = EditorGUILayout.Popup(
                    new GUIContent("First Scene", "Scene loaded after global services initialize."),
                    Mathf.Max(0, selectedIndex),
                    labels);
                if (nextIndex != selectedIndex)
                    AssignScene(buildScenes[nextIndex].path);

                if (selectedIndex < 0 && !string.IsNullOrEmpty(firstScenePath.stringValue))
                    EditorGUILayout.HelpBox(
                        "The saved First Scene is not enabled in Build Settings. Choose one from the list above.",
                        MessageType.Warning);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void AssignScene(string path)
        {
            firstScenePath.stringValue = path;
            firstSceneAsset.objectReferenceValue = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
        }

        private static List<EditorBuildSettingsScene> GetEnabledBuildScenes(Bootstrapper bootstrapper)
        {
            var scenes = new List<EditorBuildSettingsScene>();
            string bootstrapScenePath = bootstrapper != null ? bootstrapper.gameObject.scene.path : string.Empty;
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled &&
                    !string.IsNullOrEmpty(scene.path) &&
                    scene.path != bootstrapScenePath)
                    scenes.Add(scene);
            }
            return scenes;
        }
    }
}
#endif
