#if UNITY_EDITOR
using System;
using Audere.Dialogue;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Audere.Combat.Editor
{
    public static class CombatTutorialAuthoring
    {
        private const string EnemyPath = "Assets/_Audere/Data/Combat/Enemies/Enemy_KhoangLang.asset";
        private const string TutorialEnemyPath = "Assets/_Audere/Data/Combat/Enemies/Enemy_KhoangLang_TUTORIAL.asset";
        private const string TutorialMoveSetPath =
            "Assets/_Audere/Data/Combat/Moves/MoveSet_KhoangLang_P1_AimedFan.asset";
        private const string TutorialDataPath = "Assets/_Audere/Data/Combat/Tutorials/CombatTutorial_D1_CLASSROOM.asset";
        private const string EncounterPath = "Assets/_Audere/Data/Combat/CombatEncounter_D1_CLASSROOM_KHOANG_LANG.asset";
        private const string UiPrefabPath = "Assets/_Audere/Prefabs/UI/GameplayUIRoot.prefab";
        private const string DialogueFolder = "Assets/_Audere/Data/Dialogue/Day1/Classroom/Combat";
        private const string FontPath = "Assets/_Audere/AssetGame/Font/Mynerve-Regular SDF.asset";
        private const string AttackDicePath = "Assets/_Audere/Prefabs/Combat/Dice/Dice_Attack.prefab";
        private const string ShieldDicePath = "Assets/_Audere/Prefabs/Combat/Dice/Dice_Shield.prefab";
        private const string HealDicePath = "Assets/_Audere/Prefabs/Combat/Dice/Dice_Heal.prefab";

        private readonly struct LineSpec
        {
            public LineSpec(DialogueSpeakerSide speaker, string text)
            {
                Speaker = speaker;
                Text = text;
            }
            public DialogueSpeakerSide Speaker { get; }
            public string Text { get; }
        }

        private readonly struct CueSpec
        {
            public CueSpec(
                string id,
                CombatDialogueCueTrigger trigger,
                DialogueData dialogue,
                string instruction,
                float duration = 0f,
                bool filterBySymbol = false,
                CombatSymbol symbol = CombatSymbol.Attack,
                float triggerValue = 0f,
                CombatTutorialFocus tutorialFocus = CombatTutorialFocus.None,
                CombatSymbol showcasedSymbol = CombatSymbol.Attack,
                string oneShotKey = null)
            {
                Id = id;
                Trigger = trigger;
                Dialogue = dialogue;
                Instruction = instruction;
                Duration = duration;
                FilterBySymbol = filterBySymbol;
                Symbol = symbol;
                TriggerValue = triggerValue;
                TutorialFocus = tutorialFocus;
                ShowcasedSymbol = showcasedSymbol;
                OneShotKey = oneShotKey;
            }
            public string Id { get; }
            public CombatDialogueCueTrigger Trigger { get; }
            public DialogueData Dialogue { get; }
            public string Instruction { get; }
            public float Duration { get; }
            public bool FilterBySymbol { get; }
            public CombatSymbol Symbol { get; }
            public float TriggerValue { get; }
            public CombatTutorialFocus TutorialFocus { get; }
            public CombatSymbol ShowcasedSymbol { get; }
            public string OneShotKey { get; }
        }

        [MenuItem("Audere/Combat/Apply D1 Classroom Combat Tutorial")]
        public static void ApplyD1ClassroomTutorial()
        {
            EnsureFolder("Assets/_Audere/Data/Dialogue/Day1/Classroom", "Combat");
            EnsureFolder("Assets/_Audere/Data/Combat", "Tutorials");
            DialogueData overview = EnsureDialogue(
                "CATCH", "d1-combat-tutorial-overview",
                R("Có ba loại cậu cần nhớ."),
                R("Tấn công, khiên và hồi nhịp."),
                R("TIME về 0 là mình phải dừng."));
            DialogueData attack = EnsureDialogue(
                "ATTACK", "d1-combat-tutorial-attack",
                L("Tớ có thể làm nó yếu đi…"),
                R("Ừ. Từng chút một."));
            DialogueData reroll = EnsureDialogue(
                "REROLL", "d1-combat-tutorial-reroll",
                R("Không phải mặt cậu cần à?"),
                R("Gieo lại nó. Mình còn cách khác."));
            DialogueData stun = EnsureDialogue(
                "STUN_ZONE", "d1-combat-tutorial-stun-zone",
                R("Vùng nhiễu chỉ cản lúc cậu bắt."),
                R("Ở đó, mình vẫn gieo lại được."));
            DialogueData shield = EnsureDialogue(
                "SHIELD", "d1-combat-tutorial-shield",
                L("Đạn biến mất rồi…"),
                R("Mặt khiên cho cậu một khoảng thở."));
            DialogueData hit = EnsureDialogue(
                "PLAYER_HIT", "d1-combat-tutorial-player-hit",
                L("…TIME tụt rồi."),
                R("Không sao. Mình vẫn còn nhịp."));
            DialogueData heal = EnsureDialogue(
                "HEAL", "d1-combat-tutorial-heal",
                L("Tớ có thêm thời gian…"),
                R("Ừ. Mình vẫn tiếp tục được."));
            DialogueData final = EnsureDialogue(
                "FINAL", "d1-combat-tutorial-final",
                L("Càng im… nó càng lớn."),
                R("Ừ."),
                R("Đừng cố nghĩ hết mọi chuyện sau đó."),
                R("Chỉ giữ lại câu cậu thật sự muốn nói."),
                L("Tớ muốn thử."),
                R("Vậy đừng để mất câu đó."));

            CombatEnemyDefinition enemy = AssetDatabase.LoadAssetAtPath<CombatEnemyDefinition>(EnemyPath);
            if (enemy == null)
                throw new MissingReferenceException($"Missing Khoảng Lặng definition at '{EnemyPath}'.");
            if (!enemy.Validate(out string productionEnemyError))
                throw new InvalidOperationException(productionEnemyError);

            SerializedObject productionEnemy = new SerializedObject(enemy);
            SerializedProperty productionPhases = productionEnemy.FindProperty("phases");
            if (productionPhases.arraySize != 1 || enemy.GetPhase(0).MaxHealth != 6)
                throw new InvalidOperationException("D1 Classroom production combat expects one authored 6 HP phase.");
            // Production combat owns its narrative cues. Reapplying the isolated
            // tutorial must never erase authored boss dialogue or story beats.
            productionEnemy.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(enemy);

            CombatMoveSet tutorialMoveSet = AssetDatabase.LoadAssetAtPath<CombatMoveSet>(TutorialMoveSetPath);
            if (tutorialMoveSet == null)
                throw new InvalidOperationException($"Missing tutorial moveset at '{TutorialMoveSetPath}'.");
            if (!tutorialMoveSet.Validate(out string tutorialMoveSetError))
                throw new InvalidOperationException(tutorialMoveSetError);

            CombatEnemyDefinition tutorialEnemy = LoadOrCreate<CombatEnemyDefinition>(TutorialEnemyPath);
            SerializedObject tutorialEnemySerialized = new SerializedObject(tutorialEnemy);
            tutorialEnemySerialized.FindProperty("enemyId").stringValue = "d1-classroom-khoang-lang-tutorial";
            tutorialEnemySerialized.FindProperty("displayName").stringValue = enemy.DisplayName;
            tutorialEnemySerialized.FindProperty("actorPrefab").objectReferenceValue = enemy.ActorPrefab;
            tutorialEnemySerialized.FindProperty("phasePolicy").enumValueIndex = (int)CombatPhasePolicy.PerPhaseHealth;
            tutorialEnemySerialized.FindProperty("sharedMaxHealth").intValue = 99;
            SerializedProperty tutorialPhases = tutorialEnemySerialized.FindProperty("phases");
            tutorialPhases.arraySize = 1;
            SerializedProperty tutorialPhase = tutorialPhases.GetArrayElementAtIndex(0);
            tutorialPhase.FindPropertyRelative("phaseId").stringValue = "tutorial-only-placeholder";
            tutorialPhase.FindPropertyRelative("maxHealth").intValue = 99;
            tutorialPhase.FindPropertyRelative("sharedExitThreshold").intValue = 0;
            tutorialPhase.FindPropertyRelative("duration").floatValue = 120f;
            // Keep the learning round predictable even though the production phase
            // loops all authored projectile patterns.
            tutorialPhase.FindPropertyRelative("moveSet").objectReferenceValue = tutorialMoveSet;
            tutorialPhase.FindPropertyRelative("dialogueCues").arraySize = 0;
            tutorialEnemySerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tutorialEnemy);

            CombatTutorialData tutorial = LoadOrCreate<CombatTutorialData>(TutorialDataPath);
            SerializedObject tutorialSerialized = new SerializedObject(tutorial);
            tutorialSerialized.FindProperty("tutorialId").stringValue = "d1-classroom-combat-basics";
            tutorialSerialized.FindProperty("enemyDefinition").objectReferenceValue = tutorialEnemy;
            tutorialSerialized.FindProperty("playerTime").floatValue = 120f;
            SerializedProperty openingDice = tutorialSerialized.FindProperty("openingDice");
            openingDice.arraySize = 3;
            openingDice.GetArrayElementAtIndex(0).enumValueIndex = (int)CombatSymbol.Attack;
            openingDice.GetArrayElementAtIndex(1).enumValueIndex = (int)CombatSymbol.Shield;
            openingDice.GetArrayElementAtIndex(2).enumValueIndex = (int)CombatSymbol.Heal;
            SetCues(tutorialSerialized.FindProperty("cues"),
                new CueSpec("tutorial-overview", CombatDialogueCueTrigger.DiceBatchReady, overview,
                    "TRÁI: BẮT · PHẢI: GIEO LẠI · TIME VỀ 0: THUA",
                    tutorialFocus: CombatTutorialFocus.DiceAll),
                new CueSpec("tutorial-stun-zone", CombatDialogueCueTrigger.ElapsedActiveTime, stun,
                    "VÙNG NHIỄU · KHÔNG BẮT ĐƯỢC · VẪN GIEO LẠI ĐƯỢC",
                    tutorialFocus: CombatTutorialFocus.StunZone),
                new CueSpec("tutorial-attack", CombatDialogueCueTrigger.DiceCaught, attack,
                    "TẤN CÔNG · GÂY 1 SÁT THƯƠNG",
                    filterBySymbol: true, symbol: CombatSymbol.Attack,
                    tutorialFocus: CombatTutorialFocus.Dice,
                    showcasedSymbol: CombatSymbol.Attack, oneShotKey: "tutorial-attack"),
                DiceCaughtCue("tutorial-shield", "tutorial-shield", shield, CombatSymbol.Shield,
                    "KHIÊN · DỌN MỌI ĐẠN TRONG VÒNG BẢO VỆ"),
                DiceCaughtCue("tutorial-heal", "tutorial-heal", heal, CombatSymbol.Heal,
                    "HỒI NHỊP · TIME TĂNG 3 GIÂY"),
                new CueSpec("tutorial-reroll", CombatDialogueCueTrigger.DiceRerolled, reroll,
                    "GIEO LẠI · MẶT XÚC XẮC ĐÃ ĐỔI"),
                new CueSpec("tutorial-player-hit", CombatDialogueCueTrigger.PlayerHit, hit,
                    "TRÚNG ĐẠN · TIME GIẢM 3 GIÂY",
                    tutorialFocus: CombatTutorialFocus.Time),
                IntroCompleteCue("tutorial-intro-complete", final));
            tutorialSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tutorial);

            CombatEncounterData encounter = AssetDatabase.LoadAssetAtPath<CombatEncounterData>(EncounterPath);
            if (encounter == null)
                throw new MissingReferenceException($"Missing Classroom encounter at '{EncounterPath}'.");
            SerializedObject encounterSerialized = new SerializedObject(encounter);
            encounterSerialized.FindProperty("tutorialData").objectReferenceValue = tutorial;
            encounterSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(encounter);

            EnsureTutorialUi();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!tutorialEnemy.Validate(out string tutorialEnemyError))
                throw new InvalidOperationException(tutorialEnemyError);
            if (!tutorial.Validate(out string tutorialError))
                throw new InvalidOperationException(tutorialError);
            Debug.Log("[CombatTutorialAuthoring] Isolated D1 tutorial runtime, dialogue cues and HUD are ready.");
        }

        private static CueSpec DiceCaughtCue(
            string id,
            string oneShotKey,
            DialogueData dialogue,
            CombatSymbol symbol,
            string instruction)
        {
            return new CueSpec(
                id,
                CombatDialogueCueTrigger.DiceCaught,
                dialogue,
                instruction,
                duration: 2.8f,
                filterBySymbol: true,
                symbol: symbol,
                tutorialFocus: CombatTutorialFocus.Dice,
                showcasedSymbol: symbol,
                oneShotKey: oneShotKey);
        }

        private static CueSpec IntroCompleteCue(string id, DialogueData dialogue)
        {
            return new CueSpec(
                id,
                CombatDialogueCueTrigger.AllDiceTypesCaught,
                dialogue,
                string.Empty,
                oneShotKey: "tutorial-intro-complete");
        }

        private static DialogueData EnsureDialogue(string suffix, string id, params LineSpec[] lineSpecs)
        {
            string path = $"{DialogueFolder}/Dialogue_D1_COMBAT_TUTORIAL_{suffix}.asset";
            DialogueData data = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<DialogueData>();
                AssetDatabase.CreateAsset(data, path);
            }

            SerializedObject serialized = new SerializedObject(data);
            serialized.FindProperty("dialogueId").stringValue = id;
            serialized.FindProperty("leftCharacter").enumValueIndex = (int)DialogueCharacterId.Audere;
            serialized.FindProperty("rightCharacter").enumValueIndex = (int)DialogueCharacterId.Timor;
            SerializedProperty lines = serialized.FindProperty("lines");
            lines.arraySize = lineSpecs.Length;
            for (int i = 0; i < lineSpecs.Length; i++)
            {
                SerializedProperty line = lines.GetArrayElementAtIndex(i);
                line.FindPropertyRelative("speaker").enumValueIndex =
                    (int)lineSpecs[i].Speaker;
                line.FindPropertyRelative("text").stringValue = lineSpecs[i].Text;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }

        private static void SetCues(SerializedProperty cues, params CueSpec[] specs)
        {
            cues.arraySize = specs.Length;
            for (int i = 0; i < specs.Length; i++)
            {
                CueSpec spec = specs[i];
                SerializedProperty cue = cues.GetArrayElementAtIndex(i);
                cue.FindPropertyRelative("cueId").stringValue = spec.Id;
                cue.FindPropertyRelative("oneShotKey").stringValue = spec.OneShotKey ?? string.Empty;
                cue.FindPropertyRelative("trigger").enumValueIndex = (int)spec.Trigger;
                cue.FindPropertyRelative("triggerValue").floatValue = spec.TriggerValue;
                cue.FindPropertyRelative("filterBySymbol").boolValue = spec.FilterBySymbol;
                cue.FindPropertyRelative("symbol").enumValueIndex = (int)spec.Symbol;
                cue.FindPropertyRelative("instruction").stringValue = spec.Instruction;
                // Tutorial cards are interaction-dismissed by CombatController.
                cue.FindPropertyRelative("instructionDuration").floatValue = 0f;
                cue.FindPropertyRelative("tutorialFocus").enumValueIndex = (int)spec.TutorialFocus;
                cue.FindPropertyRelative("showcasedSymbol").enumValueIndex = (int)spec.ShowcasedSymbol;
                cue.FindPropertyRelative("isTutorial").boolValue = true;
                SerializedProperty sequence = cue.FindPropertyRelative("sequence");
                sequence.arraySize = spec.Dialogue != null ? 1 : 0;
                if (spec.Dialogue != null)
                    sequence.GetArrayElementAtIndex(0).objectReferenceValue = spec.Dialogue;
            }
        }

        private static void EnsureTutorialUi()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(UiPrefabPath);
            try
            {
                Transform existing = root.transform.Find("CombatTutorialUI");
                GameObject tutorialObject = existing != null
                    ? existing.gameObject
                    : new GameObject("CombatTutorialUI", typeof(RectTransform), typeof(CanvasGroup), typeof(CombatTutorialView));
                RectTransform tutorialRect = tutorialObject.GetComponent<RectTransform>();
                tutorialRect.SetParent(root.transform, false);
                Stretch(tutorialRect);

                Transform retry = root.transform.Find("CombatRetryUI");
                if (retry != null)
                {
                    retry.SetAsLastSibling();
                    tutorialRect.SetSiblingIndex(Mathf.Max(0, retry.GetSiblingIndex() - 1));
                }
                else
                {
                    tutorialRect.SetAsLastSibling();
                }

                for (int i = tutorialRect.childCount - 1; i >= 0; i--)
                    UnityEngine.Object.DestroyImmediate(tutorialRect.GetChild(i).gameObject, true);

                RectTransform spotlight = EnsureRect(tutorialRect, "Spotlight");
                Stretch(spotlight);
                Image dimTop = CreateDimPanel(spotlight, "Dim Top");
                Image dimBottom = CreateDimPanel(spotlight, "Dim Bottom");
                Image dimLeft = CreateDimPanel(spotlight, "Dim Left");
                Image dimRight = CreateDimPanel(spotlight, "Dim Right");

                RectTransform showcase = EnsureRect(tutorialRect, "Dice Showcase");
                showcase.anchorMin = showcase.anchorMax = new Vector2(.5f, .5f);
                showcase.pivot = new Vector2(.5f, .5f);
                showcase.anchoredPosition = new Vector2(0f, 36f);
                showcase.sizeDelta = new Vector2(520f, 180f);

                RectTransform textRect = EnsureRect(tutorialRect, "Tutorial Instruction");
                textRect.anchorMin = textRect.anchorMax = new Vector2(.5f, 0f);
                textRect.pivot = new Vector2(.5f, .5f);
                textRect.anchoredPosition = new Vector2(0f, 72f);
                textRect.sizeDelta = new Vector2(1500f, 78f);
                TextMeshProUGUI text = GetOrAdd<TextMeshProUGUI>(textRect.gameObject);
                text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
                text.fontSize = 40f;
                text.fontStyle = FontStyles.Bold;
                text.color = Color.white;
                text.alignment = TextAlignmentOptions.Center;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.overflowMode = TextOverflowModes.Overflow;
                text.raycastTarget = false;

                CanvasGroup group = tutorialObject.GetComponent<CanvasGroup>();
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;
                CombatTutorialView view = GetOrAdd<CombatTutorialView>(tutorialObject);
                SerializedObject viewSerialized = new SerializedObject(view);
                viewSerialized.FindProperty("group").objectReferenceValue = group;
                viewSerialized.FindProperty("content").objectReferenceValue = textRect;
                viewSerialized.FindProperty("instructionText").objectReferenceValue = text;
                viewSerialized.FindProperty("fadeDuration").floatValue = .12f;
                viewSerialized.FindProperty("verticalTravel").floatValue = 8f;
                viewSerialized.FindProperty("spotlightRoot").objectReferenceValue = spotlight;
                viewSerialized.FindProperty("dimTop").objectReferenceValue = dimTop;
                viewSerialized.FindProperty("dimBottom").objectReferenceValue = dimBottom;
                viewSerialized.FindProperty("dimLeft").objectReferenceValue = dimLeft;
                viewSerialized.FindProperty("dimRight").objectReferenceValue = dimRight;
                viewSerialized.FindProperty("diceShowcaseRoot").objectReferenceValue = showcase;
                viewSerialized.FindProperty("attackDicePrefab").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>(AttackDicePath).GetComponent<CombatDieView>();
                viewSerialized.FindProperty("shieldDicePrefab").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>(ShieldDicePath).GetComponent<CombatDieView>();
                viewSerialized.FindProperty("healDicePrefab").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<GameObject>(HealDicePath).GetComponent<CombatDieView>();
                viewSerialized.ApplyModifiedPropertiesWithoutUndo();

                GameplayUIRoot uiRoot = root.GetComponent<GameplayUIRoot>();
                SerializedObject rootSerialized = new SerializedObject(uiRoot);
                rootSerialized.FindProperty("combatTutorial").objectReferenceValue = view;
                rootSerialized.ApplyModifiedPropertiesWithoutUndo();

                // CombatTutorialView owns visibility through its CanvasGroup. Keeping
                // the host active is required because its presentation uses a coroutine.
                tutorialObject.SetActive(true);
                PrefabUtility.SaveAsPrefabAsset(root, UiPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static RectTransform EnsureRect(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            RectTransform rect = existing as RectTransform;
            if (rect != null) return rect;
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing.gameObject, true);
            GameObject created = new GameObject(name, typeof(RectTransform));
            rect = created.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static Image CreateDimPanel(RectTransform parent, string name)
        {
            RectTransform rect = EnsureRect(parent, name);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            Image image = GetOrAdd<Image>(rect.gameObject);
            image.color = new Color(0f, 0f, 0f, .76f);
            image.raycastTarget = false;
            return image;
        }

        private static T GetOrAdd<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(parent, child);
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static LineSpec L(string text) => new LineSpec(DialogueSpeakerSide.Left, text);
        private static LineSpec R(string text) => new LineSpec(DialogueSpeakerSide.Right, text);
    }
}
#endif
