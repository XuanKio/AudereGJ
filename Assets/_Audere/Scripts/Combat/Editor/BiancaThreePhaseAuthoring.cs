#if UNITY_EDITOR
using System;
using System.Linq;
using Audere.Combat;
using Audere.Dialogue;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Audere.EditorTools
{
    // Design Intent: three HP bands, a silent side-stage, then a centered box-choice opening.
    // Deliberately scoped: no story hierarchy, dialogue text, or other encounter is regenerated.
    public static class BiancaThreePhaseAuthoring
    {
        public const string Folder = "Assets/_Audere/Data/Combat/BiancaSupplies/";
        public const string BowPath = "Assets/_Audere/Prefabs/Combat/Bullets/Bullet_Bianca_Ribbon.prefab";
        [MenuItem("Audere/Combat/Apply Bianca Three Phases Only")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Author in Edit Mode.");
            var enemy = AssetDatabase.LoadAssetAtPath<CombatEnemyDefinition>(BiancaCombatAuthoring.EnemyPath);
            if (enemy == null) throw new InvalidOperationException("Missing authored Bianca encounter.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(BowPath) == null)
                AssetDatabase.CopyAsset("Assets/_Audere/Prefabs/Combat/Bullets/Bullet_Bianca_Returning.prefab", BowPath);
            var bowRoot = PrefabUtility.LoadPrefabContents(BowPath);
            try { bowRoot.GetComponent<RectTransform>().sizeDelta = new Vector2(56f, 56f); PrefabUtility.SaveAsPrefabAsset(bowRoot, BowPath); }
            finally { PrefabUtility.UnloadPrefabContents(bowRoot); }
            var bow = AssetDatabase.LoadAssetAtPath<CombatBulletView>(BowPath);
            var side = Asset<CombatPhasePresentationProfile>("Presentation_Bianca_Side.asset", a => Set(a,
                "boardWidth", .72f, "playerOffset", new Vector2(-260f, 0f), "enemyOffset", new Vector2(350f, -340f), "enemyLabelOffset", new Vector2(-270f, 170f), "duration", 1.15f));
            var arc = Asset<RibbonArcMove>("Moves/Move_Bianca_RibbonArc.asset", a => Set(a,
                "projectilePrefab", bow, "duration", 9.6f, "leadInDuration", 1.15f,
                "waveInterval", 1.65f, "flightDuration", 3.8f, "telegraphDuration", .65f,
                "ribbonsPerWave", 4, "curvature", 110f, "reverseFirstArc", false));
            var reverse = Asset<RibbonArcMove>("Moves/Move_Bianca_RibbonArcReverse.asset", a => Set(a,
                "projectilePrefab", bow, "duration", 9.6f, "leadInDuration", 1.15f,
                "waveInterval", 1.75f, "flightDuration", 3.6f, "telegraphDuration", .7f,
                "ribbonsPerWave", 5, "curvature", 145f, "reverseFirstArc", true));
            var weave = Asset<RibbonWeaveMove>("Moves/Move_Bianca_RibbonWeave.asset", a => Set(a,
                "projectilePrefab", bow, "duration", 8.4f, "leadInDuration", 1.2f,
                "waveInterval", 1.8f, "flightDuration", 2.8f, "telegraphDuration", .7f));
            var bloom = Asset<CornerBloomMove>("Moves/Move_Bianca_CornerBloom.asset", a => Set(a,
                "projectilePrefab", bow, "duration", 9f, "leadInDuration", 1.3f, "waveInterval", 2f, "telegraphDuration", .8f));
            var fan = Asset<RibbonFanSweepMove>("Moves/Move_Bianca_RibbonFanSweep.asset", a => Set(a,
                "projectilePrefab", bow, "duration", 12.8f, "leadInDuration", .65f,
                "ribbonsPerArm", 9, "horizontalSpacing", 96f, "firstDistance", 48f, "telegraphDuration", .8f,
                "halfAngle", 35f, "strokeDuration", 1.05f, "turnDuration", .45f,
                "segmentDelay", .1f, "extensionDuration", .6f));
            var phase2 = MoveSet("RibbonArcs", fan, arc, reverse);
            var phase3 = MoveSet("RibbonFinale", weave, bloom);
            var opening = AssetDatabase.LoadAssetAtPath<CombatMoveSet>(Folder + "Moves/MoveSet_Bianca_Opening.asset");
            var wrongBox = AssetDatabase.LoadAssetAtPath<WrongBoxChoiceMove>(Folder + "Moves/Move_Bianca_WrongBox.asset");
            Set(wrongBox, "leadInDuration", .9f, "telegraphDuration", .7f, "roundDelay", 1.6f);
            // Retain the familiar combinations, with a readable quiet gap between them.
            for (int i = 1; i < 4; i++)
                Set(AssetDatabase.LoadAssetAtPath<CombatMoveDefinition>(Folder + "Moves/Move_Bianca_" + i + ".asset"), "leadInDuration", 1.2f);

            Set(enemy, "sharedMaxHealth", 30);
            Set(AssetDatabase.LoadAssetAtPath<CombatEncounterData>(BiancaCombatAuthoring.EncounterPath), "encounterDuration", 150f);
            var so = new SerializedObject(enemy);
            var phases = so.FindProperty("phases"); phases.arraySize = 3;
            var sets = new[] { opening, phase2, phase3 };
            int[] thresholds = { 18, 9, 0 };
            string[] ids = { "judgement-opening", "ribbon-side-stage", "box-and-ribbons" };
            for (int i = 0; i < 3; i++)
            {
                var p = phases.GetArrayElementAtIndex(i);
                p.FindPropertyRelative("phaseId").stringValue = ids[i];
                p.FindPropertyRelative("sharedExitThreshold").intValue = thresholds[i];
                p.FindPropertyRelative("moveSet").objectReferenceValue = sets[i];
                p.FindPropertyRelative("openingMove").objectReferenceValue = i == 2 ? wrongBox : null;
                p.FindPropertyRelative("presentation").objectReferenceValue = i == 1 ? side : null;
                p.FindPropertyRelative("spawnDice").boolValue = true;
                p.FindPropertyRelative("advanceOnMoveComplete").boolValue = false;
                p.FindPropertyRelative("damageReactionMove").objectReferenceValue = null;
                p.FindPropertyRelative("damageReactionOnEnter").boolValue = false;
                p.FindPropertyRelative("allowsPlayerDefeat").boolValue = true;
                p.FindPropertyRelative("diceBatch").objectReferenceValue = null;
                var cues = p.FindPropertyRelative("dialogueCues");
                cues.ClearArray();
                if (i == 0)
                {
                    cues.arraySize = 5;
                    Cue(cues.GetArrayElementAtIndex(0), "bianca-opening", CombatDialogueCueTrigger.PhaseEnter, null, "PROJECTION_PREFIX", "TAUNT_01");
                    for (int n = 1; n < 4; n++)
                        Cue(cues.GetArrayElementAtIndex(n), "bianca-combination-" + n, CombatDialogueCueTrigger.MoveStarted,
                            opening.Entries[n].Move, "PROJECTION_PREFIX", "TAUNT_0" + (n + 1));
                    Cue(cues.GetArrayElementAtIndex(4), "bianca-side-stage-boundary", CombatDialogueCueTrigger.PhaseExit, null, "PROJECTION_PREFIX", "RETURNING_THOUGHT");
                }
                if (i == 2)
                {
                    cues.arraySize = 1;
                    Cue(cues.GetArrayElementAtIndex(0), "bianca-centered-box", CombatDialogueCueTrigger.PhaseEnter, null, "PROJECTION_PREFIX", "WRONG_BOX");
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(enemy);
            if (!enemy.Validate(out string error)) throw new InvalidOperationException(error);
            const string boardPath = "Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab";
            var root = PrefabUtility.LoadPrefabContents(boardPath);
            try { Bind(root.GetComponent<CombatBoardView>()); PrefabUtility.SaveAsPrefabAsset(root, boardPath); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            var scene = SceneManager.GetSceneByPath("Assets/_Audere/Scenes/60_D2_School_Morning.unity");
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (var board in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<CombatBoardView>(true))) Bind(board);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
        }

        private static void Bind(CombatBoardView board)
        {
            var enemy = board.transform.Find("Enemy") as RectTransform;
            if (enemy == null) throw new InvalidOperationException("Board requires its authored Enemy presentation group.");
            Set(board, "enemyPresentationRoot", enemy, "enemyLabelRoot", enemy.Find("Name") as RectTransform, "ribbonEchoMaterial",
                AssetDatabase.LoadAssetAtPath<Material>("Assets/_Audere/Materials/Combat/MountRainbowEcho.mat"));
            PrefabUtility.RecordPrefabInstancePropertyModifications(board);
        }
        private static void Cue(SerializedProperty p, string id, CombatDialogueCueTrigger trigger, CombatMoveDefinition move, params string[] suffixes)
        {
            p.FindPropertyRelative("cueId").stringValue = id;
            p.FindPropertyRelative("oneShotKey").stringValue = id;
            p.FindPropertyRelative("trigger").intValue = (int)trigger;
            p.FindPropertyRelative("triggerMove").objectReferenceValue = move;
            p.FindPropertyRelative("repeatOnTrigger").boolValue = false;
            p.FindPropertyRelative("interruptsAutoDialogue").boolValue = false;
            p.FindPropertyRelative("filterBySymbol").boolValue = false;
            p.FindPropertyRelative("presentation").intValue = (int)CombatDialoguePresentation.AutoCombatDialogue;
            p.FindPropertyRelative("minimumLineDuration").floatValue = .9f;
            p.FindPropertyRelative("charactersPerSecond").floatValue = 32f;
            p.FindPropertyRelative("interLineGap").floatValue = .12f;
            foreach (var field in new[] { "requiredBeforeVictory", "requiredBeforePhaseAdvance", "requiredBeforePlayerDefeat", "isTutorial", "playLoseRhythmOnComplete" })
                p.FindPropertyRelative(field).boolValue = false;
            var sequence = p.FindPropertyRelative("sequence"); sequence.arraySize = suffixes.Length;
            for (int i = 0; i < suffixes.Length; i++)
            {
                string suffix = suffixes[i];
                var path = AssetDatabase.FindAssets("t:DialogueData", new[] { "Assets/_Audere/Data/Dialogue/Day2/School/Combat" })
                    .Select(AssetDatabase.GUIDToAssetPath).Single(x => x.EndsWith("_" + suffix + ".asset", StringComparison.Ordinal));
                sequence.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
            }
        }
        private static CombatMoveSet MoveSet(string suffix, params CombatMoveDefinition[] moves)
            => Asset<CombatMoveSet>("Moves/MoveSet_Bianca_" + suffix + ".asset", a =>
            {
                var so = new SerializedObject(a); so.FindProperty("selectionPolicy").intValue = 0;
                var entries = so.FindProperty("entries"); entries.arraySize = moves.Length;
                for (int i = 0; i < moves.Length; i++)
                { var e = entries.GetArrayElementAtIndex(i); e.FindPropertyRelative("move").objectReferenceValue = moves[i]; e.FindPropertyRelative("weight").floatValue = 1f; }
                so.ApplyModifiedPropertiesWithoutUndo();
            });
        private static T Asset<T>(string path, Action<T> configure) where T : ScriptableObject
        {
            T a = AssetDatabase.LoadAssetAtPath<T>(Folder + path);
            bool create = a == null;
            if (create) a = ScriptableObject.CreateInstance<T>();
            configure(a);
            if (create) AssetDatabase.CreateAsset(a, Folder + path);
            EditorUtility.SetDirty(a); return a;
        }
        private static void Set(Object target, params object[] pairs)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            var so = new SerializedObject(target);
            for (int i = 0; i < pairs.Length; i += 2)
            {
                var p = so.FindProperty((string)pairs[i]); var value = pairs[i + 1];
                if (value is float f) p.floatValue = f;
                else if (value is int n) p.intValue = n;
                else if (value is bool b) p.boolValue = b;
                else if (value is Vector2 v) p.vector2Value = v;
                else p.objectReferenceValue = value as Object;
            }
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target);
        }
    }
}
#endif
