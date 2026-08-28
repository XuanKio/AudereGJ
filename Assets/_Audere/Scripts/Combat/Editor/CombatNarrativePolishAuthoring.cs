#if UNITY_EDITOR
using System;
using System.Linq;
using Audere.Dialogue;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Audere.Combat.Editor
{
    public static class CombatNarrativePolishAuthoring
    {
        private const string EnemyPath = "Assets/_Audere/Data/Combat/Enemies/Enemy_KhoangLang.asset";
        private const string MoveFolder = "Assets/_Audere/Data/Combat/Moves";
        private const string BoardPath = "Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab";
        private const string UiPath = "Assets/_Audere/Prefabs/UI/GameplayUIRoot.prefab";
        private const string DialogueFolder = "Assets/_Audere/Data/Dialogue/Day1/Classroom/Combat";
        private const string CatalogPath = "Assets/_Audere/Data/Dialogue/DialogueCharacterCatalog.asset";
        private const string FontPath = "Assets/_Audere/AssetGame/Font/Mynerve-Regular SDF.asset";
        private const string BulletPath = "Assets/_Audere/Prefabs/Combat/Bullets/EnemyBullet.prefab";

        private readonly struct LineSpec
        {
            public LineSpec(DialogueSpeakerSide speaker, string text) { Speaker = speaker; Text = text; }
            public DialogueSpeakerSide Speaker { get; }
            public string Text { get; }
        }

        [MenuItem("Audere/Combat/Apply D1 Khoang Lang Narrative Polish")]
        public static void ApplyD1CombatNarrativePolish()
        {
            DialogueData opening = EnsureDialogue(
                "Dialogue_D1_COMBAT_KHOANG_LANG_OPENING_PLACEHOLDER",
                "d1-combat-khoang-lang-opening-placeholder",
                DialogueCharacterId.Audere,
                DialogueCharacterId.KhoangLang,
                R("Nói gì đi."), R("Cậu ấy đang chờ."), R("Im lâu quá rồi."),
                R("Cậu ấy bắt đầu thấy kỳ lạ rồi."));
            DialogueData sideSweep = EnsureDialogue(
                "Dialogue_D1_COMBAT_KHOANG_LANG_SIDE_SWEEP_PLACEHOLDER",
                "d1-combat-khoang-lang-side-sweep-placeholder",
                DialogueCharacterId.Audere,
                DialogueCharacterId.KhoangLang,
                R("Bây giờ trả lời còn kỳ hơn."), R("Đừng nhìn lên."), R("Cứ để cô ấy đi."));
            DialogueData anchor = EnsureDialogue(
                "Dialogue_D1_COMBAT_AUDERE_TIMOR_ANCHOR",
                "d1-combat-audere-timor-anchor",
                DialogueCharacterId.Audere,
                DialogueCharacterId.Timor,
                L("Tớ không tìm được câu nào nghe đủ đúng."),
                R("Không cần đủ đúng."),
                R("Chỉ cần đó là câu của cậu."));
            DialogueData anxiety = EnsureDialogue(
                "Dialogue_D1_COMBAT_KHOANG_LANG_ANXIETY_FIELD_PLACEHOLDER",
                "d1-combat-khoang-lang-anxiety-field-placeholder",
                DialogueCharacterId.Audere,
                DialogueCharacterId.KhoangLang,
                R("Rồi cô ấy sẽ hỏi thêm."),
                R("Rồi cậu sẽ phải làm cùng mọi người."),
                R("Nếu làm không tốt thì sao?"),
                R("Đồng ý rồi sẽ không rút lại được."));

            ConvergingSideCorridorMove sideMove = EnsureSideSweepMove();
            CombatMoveSet moveSet = ConfigureProductionMoveSet(sideMove);
            ConfigureProductionEnemy(moveSet, sideMove, opening, sideSweep, anchor, anxiety);
            ConfigureKhoangLangPlaceholderPortrait();
            RemoveLegacyCombatBarkUi();
            ConfigureAnxietyLayer();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CombatNarrativePolishAuthoring] D1 Khoảng Lặng standard auto-dialogue, pacing and anxiety field are authored.");
        }

        private static ConvergingSideCorridorMove EnsureSideSweepMove()
        {
            string path = MoveFolder + "/Move_KhoangLang_SideSweep_Converging.asset";
            ConvergingSideCorridorMove move = AssetDatabase.LoadAssetAtPath<ConvergingSideCorridorMove>(path);
            if (move == null)
            {
                move = ScriptableObject.CreateInstance<ConvergingSideCorridorMove>();
                AssetDatabase.CreateAsset(move, path);
            }
            GameObject bulletObject = AssetDatabase.LoadAssetAtPath<GameObject>(BulletPath);
            CombatBulletView bullet = bulletObject != null ? bulletObject.GetComponent<CombatBulletView>() : null;
            if (bullet == null) throw new MissingReferenceException($"Missing bullet prefab at '{BulletPath}'.");
            SerializedObject serialized = new SerializedObject(move);
            serialized.FindProperty("duration").floatValue = 9f;
            serialized.FindProperty("projectilePrefab").objectReferenceValue = bullet;
            serialized.FindProperty("waveInterval").floatValue = 1.35f;
            serialized.FindProperty("speed").floatValue = 125f;
            serialized.FindProperty("rowSpacing").floatValue = 42f;
            serialized.FindProperty("startingSafeGapFraction").floatValue = .46f;
            serialized.FindProperty("endingSafeGapFraction").floatValue = .24f;
            serialized.FindProperty("minimumSafeGap").floatValue = 72f;
            serialized.FindProperty("telegraphDuration").floatValue = .35f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(move);
            return move;
        }

        private static CombatMoveSet ConfigureProductionMoveSet(CombatMoveDefinition sideMove)
        {
            CombatMoveDefinition aimed = AssetDatabase.LoadAssetAtPath<CombatMoveDefinition>(MoveFolder + "/Move_AimedFan.asset");
            CombatMoveDefinition rain = AssetDatabase.LoadAssetAtPath<CombatMoveDefinition>(MoveFolder + "/Move_Rain.asset");
            if (aimed == null || rain == null) throw new MissingReferenceException("Khoảng Lặng requires Aimed Fan and Rain move assets.");
            string path = MoveFolder + "/MoveSet_KhoangLang_Main.asset";
            CombatMoveSet moveSet = AssetDatabase.LoadAssetAtPath<CombatMoveSet>(path);
            if (moveSet == null)
            {
                moveSet = ScriptableObject.CreateInstance<CombatMoveSet>();
                AssetDatabase.CreateAsset(moveSet, path);
            }
            SerializedObject serialized = new SerializedObject(moveSet);
            serialized.FindProperty("selectionPolicy").enumValueIndex = (int)CombatMoveSelectionPolicy.OrderedLoop;
            SerializedProperty entries = serialized.FindProperty("entries");
            entries.arraySize = 3;
            CombatMoveDefinition[] moves = { aimed, sideMove, rain };
            for (int i = 0; i < moves.Length; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("move").objectReferenceValue = moves[i];
                entry.FindPropertyRelative("weight").floatValue = 1f;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(moveSet);
            return moveSet;
        }

        private static void ConfigureProductionEnemy(
            CombatMoveSet moveSet,
            CombatMoveDefinition sideMove,
            DialogueData opening,
            DialogueData sideSweep,
            DialogueData anchor,
            DialogueData anxiety)
        {
            CombatEnemyDefinition enemy = AssetDatabase.LoadAssetAtPath<CombatEnemyDefinition>(EnemyPath);
            if (enemy == null) throw new MissingReferenceException($"Missing enemy at '{EnemyPath}'.");
            SerializedObject serialized = new SerializedObject(enemy);
            SerializedProperty phases = serialized.FindProperty("phases");
            if (phases.arraySize != 1) throw new InvalidOperationException("Khoảng Lặng production encounter must remain one phase.");
            SerializedProperty phase = phases.GetArrayElementAtIndex(0);
            phase.FindPropertyRelative("maxHealth").intValue = 6;
            phase.FindPropertyRelative("moveSet").objectReferenceValue = moveSet;
            SerializedProperty cues = phase.FindPropertyRelative("dialogueCues");
            cues.arraySize = 4;
            ConfigureCue(cues.GetArrayElementAtIndex(0),
                "khoang-lang-opening", CombatDialogueCueTrigger.PhaseEnter,
                CombatDialoguePresentation.AutoCombatDialogue, opening);
            ConfigureCue(cues.GetArrayElementAtIndex(1),
                "khoang-lang-side-sweep", CombatDialogueCueTrigger.MoveStarted,
                CombatDialoguePresentation.AutoCombatDialogue, sideSweep,
                triggerMove: sideMove, playLoseRhythm: true);
            ConfigureCue(cues.GetArrayElementAtIndex(2),
                "audere-timor-anchor", CombatDialogueCueTrigger.CueCompleted,
                CombatDialoguePresentation.ModalDialogue, anchor,
                triggerCueId: "khoang-lang-side-sweep", requiredBeforeVictory: true);
            ConfigureCue(cues.GetArrayElementAtIndex(3),
                "khoang-lang-anxiety-field", CombatDialogueCueTrigger.HealthAtOrBelow,
                CombatDialoguePresentation.BackgroundTextField, anxiety, triggerValue: 2f);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(enemy);
            if (!enemy.Validate(out string error)) throw new InvalidOperationException(error);
        }

        private static void ConfigureCue(
            SerializedProperty cue,
            string id,
            CombatDialogueCueTrigger trigger,
            CombatDialoguePresentation presentation,
            DialogueData dialogue,
            float triggerValue = 0f,
            CombatMoveDefinition triggerMove = null,
            string triggerCueId = null,
            bool playLoseRhythm = false,
            bool requiredBeforeVictory = false)
        {
            cue.FindPropertyRelative("cueId").stringValue = id;
            cue.FindPropertyRelative("oneShotKey").stringValue = id;
            cue.FindPropertyRelative("trigger").enumValueIndex = (int)trigger;
            cue.FindPropertyRelative("triggerValue").floatValue = triggerValue;
            cue.FindPropertyRelative("triggerMove").objectReferenceValue = triggerMove;
            cue.FindPropertyRelative("triggerCueId").stringValue = triggerCueId ?? string.Empty;
            cue.FindPropertyRelative("filterBySymbol").boolValue = false;
            cue.FindPropertyRelative("sequence").arraySize = 1;
            cue.FindPropertyRelative("sequence").GetArrayElementAtIndex(0).objectReferenceValue = dialogue;
            cue.FindPropertyRelative("instruction").stringValue = string.Empty;
            cue.FindPropertyRelative("tutorialFocus").enumValueIndex = (int)CombatTutorialFocus.None;
            cue.FindPropertyRelative("isTutorial").boolValue = false;
            cue.FindPropertyRelative("presentation").enumValueIndex = (int)presentation;
            cue.FindPropertyRelative("minimumLineDuration").floatValue = 1.4f;
            cue.FindPropertyRelative("charactersPerSecond").floatValue = 20f;
            cue.FindPropertyRelative("interLineGap").floatValue = .18f;
            cue.FindPropertyRelative("playLoseRhythmOnComplete").boolValue = playLoseRhythm;
            cue.FindPropertyRelative("requiredBeforeVictory").boolValue = requiredBeforeVictory;
        }

        private static DialogueData EnsureDialogue(
            string assetName,
            string id,
            DialogueCharacterId left,
            DialogueCharacterId right,
            params LineSpec[] specs)
        {
            string path = $"{DialogueFolder}/{assetName}.asset";
            DialogueData data = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<DialogueData>();
                AssetDatabase.CreateAsset(data, path);
            }
            SerializedObject serialized = new SerializedObject(data);
            serialized.FindProperty("dialogueId").stringValue = id;
            serialized.FindProperty("leftCharacter").enumValueIndex = (int)left;
            serialized.FindProperty("rightCharacter").enumValueIndex = (int)right;
            SerializedProperty lines = serialized.FindProperty("lines");
            lines.arraySize = specs.Length;
            for (int i = 0; i < specs.Length; i++)
            {
                SerializedProperty line = lines.GetArrayElementAtIndex(i);
                line.FindPropertyRelative("speaker").enumValueIndex = (int)specs[i].Speaker;
                line.FindPropertyRelative("text").stringValue = specs[i].Text;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }

        private static void ConfigureKhoangLangPlaceholderPortrait()
        {
            DialogueCharacterCatalog catalog = AssetDatabase.LoadAssetAtPath<DialogueCharacterCatalog>(CatalogPath);
            if (catalog == null) throw new MissingReferenceException($"Missing catalog at '{CatalogPath}'.");
            SerializedObject serialized = new SerializedObject(catalog);
            SerializedProperty entries = serialized.FindProperty("characters");
            Sprite auderePortrait = null;
            int khoangLangIndex = -1;
            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                DialogueCharacterId character = (DialogueCharacterId)entry.FindPropertyRelative("character").enumValueIndex;
                if (character == DialogueCharacterId.Audere)
                    auderePortrait = entry.FindPropertyRelative("portrait").objectReferenceValue as Sprite;
                if (character == DialogueCharacterId.KhoangLang)
                    khoangLangIndex = i;
            }
            if (khoangLangIndex < 0)
            {
                khoangLangIndex = entries.arraySize;
                entries.arraySize++;
            }
            SerializedProperty khoangLang = entries.GetArrayElementAtIndex(khoangLangIndex);
            khoangLang.FindPropertyRelative("character").enumValueIndex = (int)DialogueCharacterId.KhoangLang;
            khoangLang.FindPropertyRelative("displayName").stringValue = "Khoảng Lặng";
            khoangLang.FindPropertyRelative("portrait").objectReferenceValue = auderePortrait;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        private static void RemoveLegacyCombatBarkUi()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(UiPath);
            try
            {
                Transform existing = root.transform.Find("CombatBarkUI");
                if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject, true);
                Transform retry = root.transform.Find("CombatRetryUI");
                if (retry != null) retry.SetAsLastSibling();
                PrefabUtility.SaveAsPrefabAsset(root, UiPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void ConfigureAnxietyLayer()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(BoardPath);
            try
            {
                RectTransform boardRect = root.transform as RectTransform;
                RectTransform viewport = root.transform.Find("Combat Viewport") as RectTransform;
                if (viewport == null)
                    viewport = UiObject("Combat Viewport", root.transform).GetComponent<RectTransform>();
                viewport.SetParent(root.transform, false);
                viewport.anchorMin = viewport.anchorMax = new Vector2(.5f, .5f);
                viewport.pivot = new Vector2(.5f, .5f);
                viewport.anchoredPosition = Vector2.zero;
                viewport.sizeDelta = boardRect != null ? boardRect.sizeDelta : new Vector2(1240f, 560f);
                // Camera-sized ambient layer stays behind every combat surface. The opaque
                // board naturally masks its center, so text remains background-only.
                viewport.SetAsFirstSibling();

                Transform existing = viewport.Find("Combat Anxiety Text Layer") ??
                    root.transform.Find("Combat Anxiety Text Layer");
                if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject, true);
                GameObject layer = UiObject("Combat Anxiety Text Layer", viewport, typeof(CombatAnxietyTextFieldView));
                RectTransform rect = layer.GetComponent<RectTransform>();
                Stretch(rect);
                rect.SetAsFirstSibling();
                CombatAnxietyTextFieldView field = layer.GetComponent<CombatAnxietyTextFieldView>();
                SerializedObject fieldSerialized = new SerializedObject(field);
                fieldSerialized.FindProperty("content").objectReferenceValue = rect;
                fieldSerialized.FindProperty("font").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
                fieldSerialized.FindProperty("labelCount").intValue = 384;
                fieldSerialized.FindProperty("simulationFramesPerSecond").intValue = 12;
                fieldSerialized.FindProperty("minimumSpiralDuration").floatValue = 36f;
                fieldSerialized.FindProperty("maximumSpiralDuration").floatValue = 52f;
                fieldSerialized.FindProperty("fadeDuration").floatValue = .65f;
                fieldSerialized.FindProperty("textColor").colorValue = new Color(.86f, .78f, .88f, .28f);
                fieldSerialized.ApplyModifiedPropertiesWithoutUndo();
                CombatBoardView board = root.GetComponent<CombatBoardView>();
                SerializedObject boardSerialized = new SerializedObject(board);
                boardSerialized.FindProperty("anxietyTextField").objectReferenceValue = field;
                boardSerialized.FindProperty("combatViewport").objectReferenceValue = viewport;
                boardSerialized.ApplyModifiedPropertiesWithoutUndo();
                layer.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, BoardPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static GameObject UiObject(string name, Transform parent, params Type[] components)
        {
            Type[] all = new[] { typeof(RectTransform) }.Concat(components).Distinct().ToArray();
            GameObject result = new GameObject(name, all);
            result.transform.SetParent(parent, false);
            return result;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static LineSpec L(string text) => new LineSpec(DialogueSpeakerSide.Left, text);
        private static LineSpec R(string text) => new LineSpec(DialogueSpeakerSide.Right, text);
    }

}
#endif
