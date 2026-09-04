#if UNITY_INCLUDE_TESTS
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Audere.Core;
using Audere.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Audere.Combat.Editor.Tests
{
    public sealed class GameplayDifficultyTests
    {
        private readonly List<Object> cleanup = new List<Object>();
        private bool hadPreference;
        private int storedPreference;

        private sealed class NoopMove : CombatMoveDefinition
        {
            public override ICombatMoveExecution CreateExecution(CombatMoveExecutionContext context)
            {
                return new Execution();
            }

            private sealed class Execution : ICombatMoveExecution
            {
                public bool IsComplete => false;
                public void Tick(float activeDeltaTime) { }
                public void Cancel() { }
            }
        }

        private sealed class FixedRandom : ICombatRandom
        {
            public float Value01() => 0.5f;
            public float Range(float minimum, float maximum) => Mathf.Lerp(minimum, maximum, 0.5f);
        }

        [SetUp]
        public void SetUp()
        {
            hadPreference = PlayerPrefs.HasKey(GameplayDifficultySettings.DifficultyPrefKey);
            storedPreference = PlayerPrefs.GetInt(GameplayDifficultySettings.DifficultyPrefKey);
        }

        [TearDown]
        public void TearDown()
        {
            if (hadPreference)
                PlayerPrefs.SetInt(GameplayDifficultySettings.DifficultyPrefKey, storedPreference);
            else
                PlayerPrefs.DeleteKey(GameplayDifficultySettings.DifficultyPrefKey);

            for (int i = cleanup.Count - 1; i >= 0; i--)
                if (cleanup[i] != null)
                    Object.DestroyImmediate(cleanup[i]);
            cleanup.Clear();
        }

[Test]
        public void DifficultyModifiers_GlobalPlayerTimeReductionStacksWithDifficulty()
        {
            GameplayDifficultySettings.Current = GameDifficulty.Easy;
            Assert.AreEqual(GameDifficulty.Easy, GameplayDifficultySettings.Current);
            Assert.AreEqual(10, GameplayDifficultySettings.ScaleEnemyHealth(10, GameDifficulty.Easy));
            Assert.AreEqual(72f, GameplayDifficultySettings.ScalePlayerTime(90f, GameDifficulty.Easy), 0.001f);

            GameplayDifficultySettings.Current = GameDifficulty.Hard;
            Assert.AreEqual(GameDifficulty.Hard, GameplayDifficultySettings.Current);
            Assert.AreEqual(14, GameplayDifficultySettings.ScaleEnemyHealth(10, GameDifficulty.Hard));
            Assert.AreEqual(59.04f, GameplayDifficultySettings.ScalePlayerTime(90f, GameDifficulty.Hard), 0.001f);

            Assert.AreEqual(3f,
                CombatDiceConstants.GetDefinition(CombatSymbol.Heal).EffectAmount,
                0.001f,
                "Heal die stays at its authored 3 second recovery; only maximum player TIME is reduced.");
        }

        [Test]
        public void HardPerPhaseHealth_ScalesMaxAndRetry_WithoutBypassingRequiredDialogueGate()
        {
            CombatEnemyRuntime runtime = CreateRuntime(
                CombatPhasePolicy.PerPhaseHealth,
                1,
                GameplayDifficultySettings.HardEnemyHealthMultiplier,
                ("first", 10, 0),
                ("last", 5, 0));

            var cue = new CombatDialogueCue();
            SetField(cue, "cueId", "required-phase-words");
            SetField(cue, "instruction", "gate");
            SetField(cue, "requiredBeforePhaseAdvance", true);
            SetField(runtime.CurrentPhase, "dialogueCues", new[] { cue });

            Assert.AreEqual(14, runtime.CurrentHealth);
            Assert.AreEqual(14, runtime.CurrentMaxHealth);
            Assert.AreEqual(CombatEnemyProgression.None, runtime.ApplyDamage(99, out int applied));
            Assert.AreEqual(13, applied);
            Assert.AreEqual(1, runtime.CurrentHealth);
            Assert.IsFalse(runtime.AcceptsDamage);

            runtime.MarkCueResolved(cue);
            runtime.Tick(0f);
            Assert.AreEqual(CombatEnemyRuntimeState.TransitioningPhase, runtime.State);
            runtime.CompletePhaseBreak();
            Assert.AreEqual(7, runtime.CurrentHealth);
            Assert.AreEqual(7, runtime.CurrentMaxHealth);

            runtime.RestartFromBeginning();
            Assert.AreEqual(14, runtime.CurrentHealth);
            Assert.AreEqual(0, runtime.PhaseIndex);
            runtime.Cancel();
        }

        [Test]
        public void HardSharedHealth_ScalesPhaseThresholdsWithSharedMaximum()
        {
            CombatEnemyRuntime runtime = CreateRuntime(
                CombatPhasePolicy.SharedHealthThresholds,
                10,
                GameplayDifficultySettings.HardEnemyHealthMultiplier,
                ("first", 1, 6),
                ("last", 1, 0));

            Assert.AreEqual(14, runtime.CurrentHealth);
            Assert.AreEqual(14, runtime.CurrentMaxHealth);
            Assert.AreEqual(CombatEnemyProgression.PhaseBreak, runtime.ApplyDamage(5, out int applied));
            Assert.AreEqual(5, applied);
            Assert.AreEqual(9, runtime.CurrentHealth);

            runtime.CompletePhaseBreak();
            Assert.AreEqual(9, runtime.CurrentHealth);
            Assert.AreEqual(CombatEnemyProgression.Victory, runtime.ApplyDamage(20, out applied));
            Assert.AreEqual(9, applied);
            runtime.Cancel();
        }

        [Test]
        public void CombatController_UsesDifficultySnapshotForMaximumTime()
        {
            CombatEncounterData encounter = Create<CombatEncounterData>();
            SetField(encounter, "encounterDuration", 90f);

            GameObject controllerObject = new GameObject("Difficulty Controller");
            controllerObject.SetActive(false);
            cleanup.Add(controllerObject);
            CombatController controller = controllerObject.AddComponent<CombatController>();
            SetField(controller, "encounterData", encounter);

            SetField(controller, "activeDifficulty", GameDifficulty.Easy);
            Assert.AreEqual(72f, controller.ActiveMaximumTime, 0.001f);
            SetField(controller, "activeDifficulty", GameDifficulty.Hard);
            Assert.AreEqual(59.04f, controller.ActiveMaximumTime, 0.001f);
        }

        [Test]
        public void MainMenuScene_DifficultyControlsAreAuthoredAndDirectlyBound()
        {
            const string scenePath = "Assets/_Audere/Scenes/10_MainMenu.unity";
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened)
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

            try
            {
                MainMenuSettingsPanel settings = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<MainMenuSettingsPanel>(true))
                    .Single();
                SerializedObject serialized = new SerializedObject(settings);
                Button easy = serialized.FindProperty("easyDifficultyButton").objectReferenceValue as Button;
                Button hard = serialized.FindProperty("hardDifficultyButton").objectReferenceValue as Button;

                Assert.IsNotNull(easy);
                Assert.IsNotNull(hard);
                Assert.AreEqual("DifficultyEasy", easy.name);
                Assert.AreEqual("DifficultyHard", hard.name);
                Assert.IsNotNull(serialized.FindProperty("easyDifficultyText").objectReferenceValue);
                Assert.IsNotNull(serialized.FindProperty("hardDifficultyText").objectReferenceValue);
                Assert.IsNotNull(serialized.FindProperty("difficultyDescriptionText").objectReferenceValue);
                Assert.IsTrue(easy.gameObject.activeSelf);
                Assert.IsTrue(hard.gameObject.activeSelf);
                Assert.AreSame(easy.GetComponent<Image>(), easy.targetGraphic);
                Assert.AreSame(hard.GetComponent<Image>(), hard.targetGraphic);
            }
            finally
            {
                if (opened)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        private CombatEnemyRuntime CreateRuntime(
            CombatPhasePolicy policy,
            int sharedHealth,
            float healthMultiplier,
            params (string id, int hp, int threshold)[] phaseData)
        {
            GameObject boardObject = new GameObject("Difficulty Board", typeof(RectTransform), typeof(CombatBoardView));
            cleanup.Add(boardObject);
            GameObject mountObject = new GameObject("Enemy Mount", typeof(RectTransform));
            mountObject.transform.SetParent(boardObject.transform, false);
            SetObject(boardObject.GetComponent<CombatBoardView>(), "enemyMount", mountObject.transform);

            GameObject actorObject = new GameObject("Difficulty Actor", typeof(RectTransform), typeof(CombatEnemyActor));
            cleanup.Add(actorObject);
            CombatEnemyActor actor = actorObject.GetComponent<CombatEnemyActor>();
            SetObject(actor, "visualRoot", actorObject.transform);
            SetObject(actor, "projectileOrigin", actorObject.transform);
            SetObject(actor, "vfxAnchor", actorObject.transform);

            NoopMove move = Create<NoopMove>();
            CombatMoveSet moveSet = Create<CombatMoveSet>();
            SerializedObject moveSetSerialized = new SerializedObject(moveSet);
            moveSetSerialized.FindProperty("selectionPolicy").enumValueIndex = (int)CombatMoveSelectionPolicy.OrderedLoop;
            SerializedProperty entries = moveSetSerialized.FindProperty("entries");
            entries.arraySize = 1;
            entries.GetArrayElementAtIndex(0).FindPropertyRelative("move").objectReferenceValue = move;
            entries.GetArrayElementAtIndex(0).FindPropertyRelative("weight").floatValue = 1f;
            moveSetSerialized.ApplyModifiedPropertiesWithoutUndo();

            CombatEnemyDefinition definition = Create<CombatEnemyDefinition>();
            SerializedObject serialized = new SerializedObject(definition);
            serialized.FindProperty("enemyId").stringValue = "difficulty-test";
            serialized.FindProperty("displayName").stringValue = "Difficulty Test";
            serialized.FindProperty("actorPrefab").objectReferenceValue = actor;
            serialized.FindProperty("phasePolicy").enumValueIndex = (int)policy;
            serialized.FindProperty("sharedMaxHealth").intValue = sharedHealth;
            SerializedProperty phases = serialized.FindProperty("phases");
            phases.arraySize = phaseData.Length;
            for (int i = 0; i < phaseData.Length; i++)
            {
                SerializedProperty phase = phases.GetArrayElementAtIndex(i);
                phase.FindPropertyRelative("phaseId").stringValue = phaseData[i].id;
                phase.FindPropertyRelative("maxHealth").intValue = phaseData[i].hp;
                phase.FindPropertyRelative("sharedExitThreshold").intValue = phaseData[i].threshold;
                phase.FindPropertyRelative("duration").floatValue = 5f;
                phase.FindPropertyRelative("moveSet").objectReferenceValue = moveSet;
                phase.FindPropertyRelative("dialogueCues").arraySize = 0;
                phase.FindPropertyRelative("spawnDice").boolValue = true;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var runtime = new CombatEnemyRuntime(
                definition,
                boardObject.GetComponent<CombatBoardView>(),
                new FixedRandom(),
                7,
                true,
                healthMultiplier);
            runtime.Start();
            return runtime;
        }

        private T Create<T>() where T : ScriptableObject
        {
            T value = ScriptableObject.CreateInstance<T>();
            cleanup.Add(value);
            return value;
        }

        private static void SetObject(Object target, string property, Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "Missing field '" + fieldName + "'.");
            field.SetValue(target, value);
        }
    }
}
#endif
