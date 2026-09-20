using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Audere.Combat.Editor
{
    public static class CombatBoardPrefabMigration
    {
        private const string ScenePath = "Assets/_Audere/Scenes/150_D4_Home_Evening.unity";
        private const string PrefabPath = "Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab";
        private const string TestOutput = "Temp/CombatSharedPolishQA";
        private static TestRunnerApi testRunner;

        [MenuItem("Audere/Combat/Run Shared Board And Bullet Tests")]
        public static void RunSharedPolishTests()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Wait for Unity Edit Mode and compilation before testing.");
            Directory.CreateDirectory(TestOutput);
            File.Delete(TestOutput + "/summary.txt");
            testRunner = ScriptableObject.CreateInstance<TestRunnerApi>();
            testRunner.RegisterCallbacks(new SharedPolishTestCallbacks());
            testRunner.Execute(new ExecutionSettings(new Filter
            {
                testMode = TestMode.EditMode,
                groupNames = new[] { "Audere.Combat.Editor.Tests.CombatSharedPolishTests" },
            }));
        }

        [MenuItem("Audere/Combat/Connect Day4 Timor Board To Shared Prefab")]
        public static void ConnectDay4TimorBoard()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before connecting the combat board.");

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            CombatBoardView board = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<CombatBoardView>(true))
                .Single();
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                throw new MissingReferenceException(PrefabPath);

            if (PrefabUtility.GetCorrespondingObjectFromSource(board.gameObject) == prefab)
                return;
            if (PrefabUtility.IsPartOfPrefabInstance(board.gameObject))
                throw new InvalidOperationException("Scene150 board is linked to a different prefab.");

            RectTransform root = (RectTransform)board.transform;
            Vector3 rootScale = root.localScale;
            RectLayout field = Capture(root, "Dice Field");
            RectLayout timer = Capture(root, "Timer Track");
            RectLayout health = Capture(root, "Health");
            RectLayout name = Capture(root, "Name");
            RectLayout stunZone = Capture(root, "Stun Zone");
            CombatEnemyActor actor = board.GetComponentInChildren<CombatEnemyActor>(true);
            if (actor == null)
                throw new MissingReferenceException("Scene150 requires its authored Timor actor.");

            var settings = new ConvertToPrefabInstanceSettings
            {
                objectMatchMode = ObjectMatchMode.ByHierarchy,
                gameObjectsNotMatchedBecomesOverride = true,
                componentsNotMatchedBecomesOverride = true,
                recordPropertyOverridesOfMatches = false,
                changeRootNameToAssetName = false,
            };
            // ByHierarchy preserves scene references to matched children. Unmatched
            // children, including the authored Timor actor, stay as scene additions.
            PrefabUtility.ConvertToPrefabInstance(board.gameObject, prefab, settings,
                InteractionMode.AutomatedAction);

            board = root.GetComponent<CombatBoardView>();
            if (board == null || PrefabUtility.GetCorrespondingObjectFromSource(board.gameObject) != prefab)
                throw new InvalidOperationException("Scene150 board conversion did not retain the shared prefab link.");

            root.localScale = rootScale;
            Restore(root, "Dice Field", field);
            Restore(root, "Timer Track", timer);
            Restore(root, "Health", health);
            Restore(root, "Name", name);
            Restore(root, "Stun Zone", stunZone);
            var serialized = new SerializedObject(board);
            serialized.FindProperty("authoredEnemyActor").objectReferenceValue = actor;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(board);

            if (!actor.transform.IsChildOf(root))
                throw new InvalidOperationException("Scene150 Timor actor was lost during board conversion.");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[CombatBoardPrefabMigration] Scene150 now inherits CombatBoard.prefab; Timor layout and actor are preserved.");
        }

        private readonly struct RectLayout
        {
            public readonly Vector2 Position;
            public readonly Vector2 Size;

            public RectLayout(RectTransform rect)
            {
                Position = rect.anchoredPosition;
                Size = rect.sizeDelta;
            }
        }

        private static RectLayout Capture(Transform root, string childName) =>
            new RectLayout(FindRect(root, childName));

        private static void Restore(Transform root, string childName, RectLayout layout)
        {
            RectTransform rect = FindRect(root, childName);
            rect.anchoredPosition = layout.Position;
            rect.sizeDelta = layout.Size;
            PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
        }

        private static RectTransform FindRect(Transform root, string childName)
        {
            RectTransform[] matches = root.GetComponentsInChildren<RectTransform>(true)
                .Where(rect => rect.name == childName).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException($"Expected one combat board child named '{childName}', found {matches.Length}.");
            return matches[0];
        }

        private sealed class SharedPolishTestCallbacks : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                TestRunnerApi.SaveResultToFile(result, TestOutput + "/tests.xml");
                File.WriteAllText(TestOutput + "/summary.txt",
                    $"{result.ResultState} passed={result.PassCount} failed={result.FailCount}");
            }
        }
    }
}
