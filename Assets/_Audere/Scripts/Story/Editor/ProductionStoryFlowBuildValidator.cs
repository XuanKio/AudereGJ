#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Audere.Core;
using Audere.Story.Steps;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Audere.Story.Editor
{
    /// <summary>
    /// Prevents a player build from shipping a disabled production StoryDirector or a broken
    /// scene edge. Tests may temporarily change scene startup state, so this validation belongs
    /// at the build boundary as the final production contract.
    /// </summary>
    public sealed class ProductionStoryFlowBuildValidator : IPreprocessBuildWithReport
    {
        private static readonly string[] ProductionOrder =
        {
            GameScenes.Day1HomeMorning,
            GameScenes.Classroom,
            GameScenes.Evening,
            GameScenes.Day2HomeMorning,
            GameScenes.Day2SchoolMorning,
            GameScenes.Day2HomeNight,
            GameScenes.Day2Dream,
            GameScenes.Day2HomeAwakening,
            GameScenes.Day3HomeMorning,
            GameScenes.Day3SchoolBoard,
            GameScenes.Day3SchoolTeacher,
            GameScenes.Day4HomeMorning,
            GameScenes.Day4Classroom,
            GameScenes.Day4HomeEvening,
        };

        public int callbackOrder => -100;

        public void OnPreprocessBuild(BuildReport report)
        {
            ValidateOrThrow();
        }

        [MenuItem("Audere/Story/Validate Production Scene Flow")]
        public static void ValidateFromMenu()
        {
            ValidateOrThrow();
            Debug.Log("[ProductionStoryFlow] 14 production scenes and 13 scene edges are valid.");
        }

        private static void ValidateOrThrow()
        {
            EditorBuildSettingsScene[] enabledScenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .ToArray();
            Dictionary<string, string> pathsByName = enabledScenes.ToDictionary(
                scene => Path.GetFileNameWithoutExtension(scene.path),
                scene => scene.path,
                StringComparer.Ordinal);

            List<string> errors = new List<string>();
            ValidateBuildOrder(pathsByName, errors);

            Scene originalScene = SceneManager.GetActiveScene();
            string originalPath = originalScene.path;
            for (int index = 0; index < ProductionOrder.Length; index++)
            {
                string sceneName = ProductionOrder[index];
                if (!pathsByName.TryGetValue(sceneName, out string scenePath))
                    continue;

                bool alreadyLoaded = SceneManager.GetSceneByPath(scenePath).isLoaded;
                Scene scene = alreadyLoaded
                    ? SceneManager.GetSceneByPath(scenePath)
                    : EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                try
                {
                    ValidateScene(scene, index, pathsByName, errors);
                }
                finally
                {
                    if (!alreadyLoaded)
                        EditorSceneManager.CloseScene(scene, true);
                }
            }

            if (!string.IsNullOrEmpty(originalPath))
            {
                Scene restored = SceneManager.GetSceneByPath(originalPath);
                if (restored.IsValid() && restored.isLoaded)
                    SceneManager.SetActiveScene(restored);
            }

            if (errors.Count > 0)
                throw new BuildFailedException(
                    "Production story flow is invalid:\n- " + string.Join("\n- ", errors));
        }

        private static void ValidateBuildOrder(
            IReadOnlyDictionary<string, string> pathsByName,
            ICollection<string> errors)
        {
            if (!pathsByName.ContainsKey(GameScenes.Bootstrap))
                errors.Add("00_Bootstrap is not enabled in Build Settings.");
            if (!pathsByName.ContainsKey(GameScenes.MainMenu))
                errors.Add("10_MainMenu is not enabled in Build Settings.");

            foreach (string sceneName in ProductionOrder)
                if (!pathsByName.ContainsKey(sceneName))
                    errors.Add(sceneName + " is not enabled in Build Settings.");
        }

        private static void ValidateScene(
            Scene scene,
            int orderIndex,
            IReadOnlyDictionary<string, string> pathsByName,
            ICollection<string> errors)
        {
            StoryDirector[] directors = ComponentsInScene<StoryDirector>(scene);
            if (directors.Length != 1)
            {
                errors.Add(scene.name + " must contain exactly one StoryDirector; found " + directors.Length + ".");
                return;
            }

            SerializedObject directorData = new SerializedObject(directors[0]);
            if (!directorData.FindProperty("playOnStart").boolValue)
                errors.Add(scene.name + "/StoryDirector has Play On Start disabled.");
            if (directorData.FindProperty("startingEvent").objectReferenceValue == null)
                errors.Add(scene.name + "/StoryDirector has no Starting Event.");

            foreach (StoryEvent storyEvent in ComponentsInScene<StoryEvent>(scene))
            {
                for (int childIndex = 0; childIndex < storyEvent.transform.childCount; childIndex++)
                {
                    Transform child = storyEvent.transform.GetChild(childIndex);
                    if (!child.gameObject.activeSelf)
                        continue;
                    int stepCount = child.GetComponents<StoryStep>().Length;
                    if (stepCount != 1)
                        errors.Add(scene.name + "/" + storyEvent.EventId + "/" + child.name +
                                   " must contain exactly one StoryStep; found " + stepCount + ".");
                }
            }

            SceneLoadStep[] loads = ComponentsInScene<SceneLoadStep>(scene);
            int expectedLoadCount = orderIndex < ProductionOrder.Length - 1 ? 1 : 0;
            if (loads.Length != expectedLoadCount)
            {
                errors.Add(scene.name + " must contain " + expectedLoadCount +
                           " SceneLoadStep(s); found " + loads.Length + ".");
                return;
            }

            if (expectedLoadCount == 0)
                return;

            SerializedObject loadData = new SerializedObject(loads[0]);
            string target = loadData.FindProperty("sceneName").stringValue;
            string expectedTarget = ProductionOrder[orderIndex + 1];
            if (!string.Equals(target, expectedTarget, StringComparison.Ordinal))
                errors.Add(scene.name + "/" + loads[0].name + " targets '" + target +
                           "' instead of '" + expectedTarget + "'.");
            if (!pathsByName.ContainsKey(target))
                errors.Add(scene.name + "/" + loads[0].name + " targets a scene not enabled in Build Settings: " + target);
        }

        private static T[] ComponentsInScene<T>(Scene scene) where T : Component
        {
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<T>(true))
                .ToArray();
        }
    }
}
#endif
