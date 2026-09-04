#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Audere.Combat;
using Audere.Dialogue;
using Audere.Story;
using Audere.Story.Steps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Object = UnityEngine.Object;
using static Audere.EditorTools.Day2SchoolMorningSetupTool;

namespace Audere.EditorTools
{
    /// <summary>Scoped Design Intent: perceived judgement, then a small question to the real Bianca.</summary>
    public static class BiancaCombatAuthoring
    {
        public const string EncounterPath = "Assets/_Audere/Data/Combat/BiancaSupplies/CombatEncounter_D2_BIANCA_SUPPLIES_PLACEHOLDER.asset";
        public const string EnemyPath = "Assets/_Audere/Data/Combat/BiancaSupplies/Enemy_BiancaSupplies_PLACEHOLDER.asset";
        private const string DataFolder = "Assets/_Audere/Data/Combat/BiancaSupplies";
        private const string DialogueRoot = "Assets/_Audere/Data/Dialogue/Day2/School";
        private static readonly HashSet<Object> touched = new HashSet<Object>();

        [MenuItem("Audere/Combat/Polish Bianca Projectiles Only")]
        public static void PolishProjectilesOnly()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before authoring.");
            var prefab = AssetDatabase.LoadAssetAtPath<CombatBulletView>("Assets/_Audere/Prefabs/Combat/Bullets/EnemyBullet.prefab");
            var shots = Asset<LinearProjectilePatternMove>(DataFolder + "/Moves/Pattern_Bianca_OpeningBullets.asset", a =>
                Set(a, "duration", 6.6f, "projectilePrefab", prefab, "spawnMode", (int)LinearProjectileSpawnMode.ActorAnchor,
                    "targetMode", (int)LinearProjectileTargetMode.AimAtHeart, "shotInterval", 1.15f,
                    "projectilesPerShot", 3, "speed", 145f, "spreadDegrees", 22f));
            var opening = AssetDatabase.LoadAssetAtPath<CompositeCombatMove>(DataFolder + "/Moves/Move_Bianca_0.asset");
            if (opening == null) throw new InvalidOperationException("Author the existing Bianca encounter first.");
            // One active tick retains the existing MoveStarted bark, without a visible empty-board wait.
            Set(opening, "leadInDuration", .01f, "children", opening.Children.Concat(new[] { shots }).Distinct().ToArray());
            var returning = AssetDatabase.LoadAssetAtPath<ReturningOrbitMove>(DataFolder + "/Moves/Move_Bianca_ReturningOrbit.asset");
            Set(returning, "horizontalTraversal", true);
            const string bulletPath = "Assets/_Audere/Prefabs/Combat/Bullets/Bullet_Bianca_Returning.prefab";
            var root = PrefabUtility.LoadPrefabContents(bulletPath);
            try
            {
                root.GetComponent<RectTransform>().sizeDelta = new Vector2(69f, 69f); // 46 × 1.5; idempotent.
                PrefabUtility.SaveAsPrefabAsset(root, bulletPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            EditorUtility.SetDirty(shots); EditorUtility.SetDirty(opening); EditorUtility.SetDirty(returning);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Audere/Combat/Author Bianca Supplies Encounter")]
        public static void AuthorLoadedScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Author in Edit Mode.");
            Scene scene = SceneManager.GetSceneByPath("Assets/_Audere/Scenes/60_D2_School_Morning.unity");
            if (!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("Open scene 60 first.");
            touched.Clear();
            Folder(DataFolder + "/Moves"); Folder(DialogueRoot + "/Combat"); Folder(DialogueRoot + "/PostCombat");
            var story = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<StoryEvent>(true))
                .Single(e => e.EventId == "D2_SCHOOL_WRONG_SUPPLIES");
            var step = story.GetComponentsInChildren<CombatStep>(true).Single();
            var board = step.CombatController.BoardView;
            if (board == null) throw new InvalidOperationException("Combat Step must bind its existing board.");
            board.gameObject.SetActive(true); // The parent Combat Root owns mode visibility.

            CombatBulletView bullet = AssetDatabase.LoadAssetAtPath<CombatBulletView>("Assets/_Audere/Prefabs/Combat/Bullets/EnemyBullet.prefab");
            CombatBulletView returning = ReturningBullet();
            var normalMoves = new List<CombatMoveDefinition>();
            var lateMoves = new List<CombatMoveDefinition>();
            NarrativePressurePatternKind[] kinds = { NarrativePressurePatternKind.VerticalLaserColumns, NarrativePressurePatternKind.MovingGapWall,
                NarrativePressurePatternKind.SequentialFans, NarrativePressurePatternKind.RotatingBlades,
                NarrativePressurePatternKind.SweepingLaser, NarrativePressurePatternKind.SafeZoneRain,
                NarrativePressurePatternKind.SplitBurst, NarrativePressurePatternKind.PendulumLaser };
            for (int i = 0; i < kinds.Length; i++)
            {
                int index = i;
                var pattern = Asset<NarrativePressurePatternMove>(DataFolder + "/Moves/Pattern_Bianca_" + i + ".asset", a =>
                    Set(a, "duration", 6.6f, "projectilePrefab", bullet, "pattern", (int)kinds[index],
                        "waveInterval", index >= 4 ? 1.15f : 1.35f, "speed", index >= 4 ? 160f : 145f,
                        "spacing", 42f, "telegraphDuration", .55f, "safeGapFraction", .34f, "intensity", index >= 4 ? 5 : 4));
                var squeeze = Asset<ShiftingBattleBoxMove>(DataFolder + "/Moves/Field_Bianca_" + i + ".asset", a =>
                {
                    Set(a, "duration", 6.6f, "telegraphDuration", .3f, "squeezeDuration", .4f, "holdDuration", .65f, "returnDuration", .3f);
                    var so = new SerializedObject(a); var poses = so.FindProperty("poses"); poses.arraySize = 4;
                    for (int n = 0; n < 4; n++)
                    {
                        var p = poses.GetArrayElementAtIndex(n);
                        p.FindPropertyRelative("widthFraction").floatValue = n % 2 == 0 ? .76f : index >= 4 ? .58f : .64f;
                        p.FindPropertyRelative("normalizedX").floatValue = (n + index) % 2 == 0 ? -.85f : .85f;
                    }
                    so.ApplyModifiedPropertiesWithoutUndo();
                });
                var combined = Asset<CompositeCombatMove>(DataFolder + "/Moves/Move_Bianca_" + i + ".asset", a =>
                    Set(a, "duration", 6.6f, "leadInDuration", 2.2f, "children", new Object[] { pattern, squeeze }));
                (i < 4 ? normalMoves : lateMoves).Add(combined);
            }
            var wrongBox = Asset<WrongBoxChoiceMove>(DataFolder + "/Moves/Move_Bianca_WrongBox.asset", a =>
                Set(a, "duration", 90f, "leadInDuration", 2.2f, "projectilePrefab", bullet, "explosionChance", .6f,
                    "requiredSuccesses", 2, "roundDelay", 1.5f, "telegraphDuration", .45f, "burstCount", 12, "burstSpeed", 210f));
            var orbit = Asset<ReturningOrbitMove>(DataFolder + "/Moves/Move_Bianca_ReturningOrbit.asset", a =>
                Set(a, "duration", 20f, "leadInDuration", 2.2f, "projectilePrefab", returning, "maximumSimultaneous", 3,
                    "initialFlightDuration", 4f, "finalFlightDuration", 2.8f, "telegraphDuration", .65f, "betweenWaves", .55f));
            var sets = new[] { MoveSet("Opening", normalMoves.ToArray()), MoveSet("WrongBox", wrongBox),
                MoveSet("Pressure", lateMoves.ToArray()), MoveSet("Returning", orbit), MoveSet("Final", lateMoves.AsEnumerable().Reverse().ToArray()) };

            var prefix = D("Combat", "PROJECTION_PREFIX", DialogueCharacterId.Timor, "Audere_Scared_0", "TimorLoLangKhongVui_0", "R|Chắc cậu ấy đang nghĩ…");
            var taunts = new[] {
                D("Combat", "TAUNT_01", DialogueCharacterId.BiancaDistorted, "Audere_Scared_0", "Bianca_Creepy_0", "R|Phiền thật."),
                D("Combat", "TAUNT_02", DialogueCharacterId.BiancaDistorted, "Audere_Scared_0", "Bianca_Creepy_0", "R|Cái này cũng lấy nhầm.", "R|Audere?|Bianca_Worried_0|g|Bianca"),
                D("Combat", "TAUNT_03", DialogueCharacterId.BiancaDistorted, "Audere_Scared_0", "Bianca_Creepy_0", "R|Đi một mình còn nhanh hơn."),
                D("Combat", "TAUNT_04", DialogueCharacterId.BiancaDistorted, "Audere_Scared_0", "Bianca_Creepy_0", "R|Tại sao mình lại rủ cậu ấy nhỉ?", "R|Audere ơi?|Bianca_Worried_0|g|Bianca")
            };
            var wrongLine = D("Combat", "WRONG_BOX", DialogueCharacterId.BiancaDistorted, "Audere_Scared_0", "Bianca_Creepy_0", "R|Cái này cũng lấy nhầm.");
            var returnLine = D("Combat", "RETURNING_THOUGHT", DialogueCharacterId.BiancaDistorted, "Audere_Scared_0", "Bianca_Creepy_0",
                "R|Tại sao mình lại rủ cậu ấy nhỉ?", "R|Audere?|Bianca_Worried_0|g|Bianca");
            var replies = new[] {
                D("Combat", "RESIST_01", DialogueCharacterId.Timor, "Audere_Scared_0", "TimorLoLangKhongVui_0", "L|Không… cậu ấy chưa nói thế."),
                D("Combat", "RESIST_02", DialogueCharacterId.Timor, "Audere_Scared_0", "TimorLoLangKhongVui_0", "L|Tớ chỉ lấy nhầm một hộp thôi."),
                D("Combat", "RESIST_03", DialogueCharacterId.Timor, "Audere_Scared_0", "TimorLoLangKhongVui_0", "L|Để tớ hỏi cậu ấy.|Audere_0")
            };

            var enemy = AssetDatabase.LoadAssetAtPath<CombatEnemyDefinition>(EnemyPath);
            if (enemy == null) throw new InvalidOperationException("Keep the existing Bianca enemy asset/GUID.");
            var es = new SerializedObject(enemy);
            es.FindProperty("enemyId").stringValue = "d2-bianca-perceived-judgement";
            es.FindProperty("displayName").stringValue = "Bianca";
            es.FindProperty("phasePolicy").intValue = (int)CombatPhasePolicy.SharedHealthThresholds;
            es.FindProperty("sharedMaxHealth").intValue = 10;
            var phases = es.FindProperty("phases"); phases.arraySize = 5;
            int[] thresholds = { 6, 6, 2, 2, 0 };
            string[] ids = { "judgement-opening", "wrong-box", "judgement-pressure", "returning-thoughts", "ask-for-yourself" };
            for (int i = 0; i < 5; i++)
            {
                bool special = i == 1 || i == 3;
                var phase = phases.GetArrayElementAtIndex(i);
                phase.FindPropertyRelative("phaseId").stringValue = ids[i];
                phase.FindPropertyRelative("maxHealth").intValue = 10;
                phase.FindPropertyRelative("sharedExitThreshold").intValue = thresholds[i];
                phase.FindPropertyRelative("moveSet").objectReferenceValue = sets[i];
                phase.FindPropertyRelative("advanceOnMoveComplete").boolValue = special;
                phase.FindPropertyRelative("spawnDice").boolValue = !special;
                phase.FindPropertyRelative("allowsPlayerDefeat").boolValue = true;
                phase.FindPropertyRelative("minimumPlayerTimeOnEnter").floatValue = 0f;
                phase.FindPropertyRelative("diceBatch").objectReferenceValue = null;
                var cues = phase.FindPropertyRelative("dialogueCues");
                if (special)
                {
                    cues.arraySize = 1;
                    Cue(cues.GetArrayElementAtIndex(0), ids[i] + "-start", CombatDialogueCueTrigger.PhaseEnter, null,
                        new[] { prefix, i == 1 ? wrongLine : returnLine }, false, false);
                }
                else
                {
                    var moves = sets[i].Entries.Select(e => e.Move).ToArray();
                    cues.arraySize = moves.Length + 2;
                    for (int n = 0; n < moves.Length; n++)
                        Cue(cues.GetArrayElementAtIndex(n), ids[i] + "-wave-" + n, CombatDialogueCueTrigger.MoveStarted, moves[n],
                            new[] { prefix, taunts[(n + i) % taunts.Length] }, true, false);
                    for (int n = 0; n < 2; n++)
                        Cue(cues.GetArrayElementAtIndex(moves.Length + n), ids[i] + "-resist-" + n, CombatDialogueCueTrigger.DiceCaught, null,
                            new[] { replies[Mathf.Min(2, i / 2 + n)] }, n == 1, true);
                }
            }
            es.ApplyModifiedPropertiesWithoutUndo(); touched.Add(enemy);
            if (!enemy.Validate(out string error)) throw new InvalidOperationException(error);
            var encounter = AssetDatabase.LoadAssetAtPath<CombatEncounterData>(EncounterPath);
            Set(encounter, "encounterId", "d2-bianca-perceived-judgement", "enemyDefinition", enemy, "tutorialData", null,
                "encounterDuration", 90f, "dicePerBatch", 3, "maximumAttacksPerBatch", 2, "victoryFadeDuration", .9f);
            var cs = new SerializedObject(encounter);
            cs.FindProperty("outcomeRules.allowedOutcomes").intValue = 3;
            cs.FindProperty("outcomeRules.playerDefeatGate").intValue = 0;
            cs.FindProperty("outcomeRules.showRetryOnDefeat").boolValue = true;
            cs.ApplyModifiedPropertiesWithoutUndo(); touched.Add(encounter);
            Set(step, "combatEncounterData", encounter, "victoryBehaviour", 0, "defeatBehaviour", 2);
            Set(step.CombatController, "encounterData", encounter);
            EnsureMechanicHint(board);
            foreach (string id in new[] { "RETURN_SETTLE", "RETURN_WRONG_LABEL", "RETURN_BIANCA_CHECKS", "RETURN_TIMOR_BLAME", "RETURN_BIANCA_CALL", "RETURN_TIMOR_PROJECTION" })
            {
                var data = story.GetComponentsInChildren<DialogueStep>(true).Select(s => new SerializedObject(s).FindProperty("dialogueData").objectReferenceValue as DialogueData)
                    .FirstOrDefault(d => d != null && d.name.EndsWith(id, StringComparison.Ordinal));
                if (data == null) throw new InvalidOperationException("Missing pre-combat dialogue " + id);
                var so = new SerializedObject(data);
                so.FindProperty("leftPortraitOverride").objectReferenceValue = Portrait(id == "RETURN_SETTLE" ? "Audere_0" : "Audere_Scared_0");
                so.FindProperty("rightPortraitOverride").objectReferenceValue = Portrait(id.Contains("TIMOR") ? "TimorLoLangKhongVui_0" : "Bianca_0");
                so.ApplyModifiedPropertiesWithoutUndo(); touched.Add(data);
            }
            AuthorAfterCombat(story);
            PolishProjectilesOnly();
            foreach (var asset in touched) { EditorUtility.SetDirty(asset); AssetDatabase.SaveAssetIfDirty(asset); }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void Cue(SerializedProperty p, string id, CombatDialogueCueTrigger trigger, CombatMoveDefinition move, DialogueData[] sequence, bool repeat, bool attack)
        {
            p.FindPropertyRelative("cueId").stringValue = id;
            p.FindPropertyRelative("oneShotKey").stringValue = id;
            p.FindPropertyRelative("trigger").intValue = (int)trigger;
            p.FindPropertyRelative("triggerMove").objectReferenceValue = move;
            p.FindPropertyRelative("triggerValue").floatValue = 0f;
            p.FindPropertyRelative("triggerCueId").stringValue = "";
            p.FindPropertyRelative("filterBySymbol").boolValue = attack;
            p.FindPropertyRelative("symbol").intValue = (int)CombatSymbol.Attack;
            p.FindPropertyRelative("repeatOnTrigger").boolValue = repeat;
            p.FindPropertyRelative("interruptsAutoDialogue").boolValue = repeat || attack;
            p.FindPropertyRelative("presentation").intValue = (int)CombatDialoguePresentation.AutoCombatDialogue;
            p.FindPropertyRelative("minimumLineDuration").floatValue = .9f;
            p.FindPropertyRelative("charactersPerSecond").floatValue = 32f;
            p.FindPropertyRelative("interLineGap").floatValue = .06f;
            p.FindPropertyRelative("instruction").stringValue = "";
            p.FindPropertyRelative("isTutorial").boolValue = false;
            p.FindPropertyRelative("requiredBeforeVictory").boolValue = false;
            p.FindPropertyRelative("requiredBeforePhaseAdvance").boolValue = false;
            p.FindPropertyRelative("requiredBeforePlayerDefeat").boolValue = false;
            p.FindPropertyRelative("playLoseRhythmOnComplete").boolValue = false;
            var a = p.FindPropertyRelative("sequence"); a.arraySize = sequence.Length;
            for (int i = 0; i < sequence.Length; i++) a.GetArrayElementAtIndex(i).objectReferenceValue = sequence[i];
        }

        private static T Asset<T>(string path, Action<T> configure) where T : ScriptableObject
        {
            T value = AssetDatabase.LoadAssetAtPath<T>(path);
            bool create = value == null;
            if (create) value = ScriptableObject.CreateInstance<T>();
            configure(value);
            if (create) AssetDatabase.CreateAsset(value, path);
            touched.Add(value); return value;
        }

        private static CombatMoveSet MoveSet(string suffix, params CombatMoveDefinition[] moves)
        {
            return Asset<CombatMoveSet>(DataFolder + "/Moves/MoveSet_Bianca_" + suffix + ".asset", a =>
            {
                var so = new SerializedObject(a); so.FindProperty("selectionPolicy").intValue = 0;
                var entries = so.FindProperty("entries"); entries.arraySize = moves.Length;
                for (int i = 0; i < moves.Length; i++)
                {
                    entries.GetArrayElementAtIndex(i).FindPropertyRelative("move").objectReferenceValue = moves[i];
                    entries.GetArrayElementAtIndex(i).FindPropertyRelative("weight").floatValue = 1f;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            });
        }

        private static Sprite Portrait(string name)
        {
            string folder = name.StartsWith("Audere") ? "Audere" : name.StartsWith("Bianca") ? "Bianca" : "Timor";
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/_Audere/AssetGame/" + folder }))
                foreach (Sprite sprite in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)).OfType<Sprite>())
                    if (sprite.name == name) return sprite;
            throw new InvalidOperationException("Missing verified portrait " + name);
        }

        private static DialogueData D(string folder, string suffix, DialogueCharacterId partner, string left, string right, params string[] lines)
        {
            return Asset<DialogueData>(DialogueRoot + "/" + folder + "/Dialogue_D2_BIANCA_" + suffix + ".asset", a =>
            {
                var so = new SerializedObject(a);
                so.FindProperty("dialogueId").stringValue = "d2-bianca-" + suffix.ToLowerInvariant().Replace('_', '-');
                so.FindProperty("leftCharacter").intValue = (int)DialogueCharacterId.Audere;
                so.FindProperty("rightCharacter").intValue = (int)partner;
                so.FindProperty("leftPortraitOverride").objectReferenceValue = Portrait(left);
                so.FindProperty("rightPortraitOverride").objectReferenceValue =
                    partner == DialogueCharacterId.BiancaDistorted && right == "Bianca_Creepy_0" ? null : Portrait(right);
                var array = so.FindProperty("lines"); array.arraySize = lines.Length;
                for (int i = 0; i < lines.Length; i++)
                {
                    var tokens = lines[i].Split('|');
                    if (tokens[1].Length > 42) throw new InvalidOperationException("Split long speech beat: " + tokens[1]);
                    var line = array.GetArrayElementAtIndex(i);
                    line.FindPropertyRelative("speaker").intValue = tokens[0] == "L" ? 0 : 1;
                    line.FindPropertyRelative("text").stringValue = tokens[1];
                    line.FindPropertyRelative("characterOverride").intValue = tokens.Length > 4
                        ? (int)Enum.Parse(typeof(DialogueCharacterId), tokens[4]) : (int)DialogueCharacterId.None;
                    line.FindPropertyRelative("portraitOverride").objectReferenceValue = tokens.Length > 2 && tokens[2].Length > 0 ? Portrait(tokens[2]) : null;
                    line.FindPropertyRelative("glitchPortraitTransition").boolValue = tokens.Length > 3 && tokens[3] == "g";
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            });
        }

        private static CombatBulletView ReturningBullet()
        {
            const string path = "Assets/_Audere/Prefabs/Combat/Bullets/Bullet_Bianca_Returning.prefab";
            string source = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null ? path : "Assets/_Audere/Prefabs/Combat/Bullets/EnemyBullet.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(source);
            try
            {
                root.name = "Bullet_Bianca_Returning";
                Sprite sprite = AssetDatabase.LoadAllAssetsAtPath("Assets/_Audere/AssetGame/Item/dan_bianca.aseprite").OfType<Sprite>()
                    .Single(s => s.name == "dan_bianca");
                var image = root.GetComponentInChildren<Image>(true);
                image.sprite = sprite; image.color = Color.white; image.preserveAspect = true;
                root.GetComponent<RectTransform>().sizeDelta = new Vector2(46f, 46f);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            return AssetDatabase.LoadAssetAtPath<CombatBulletView>(path);
        }

        private static void EnsureMechanicHint(CombatBoardView board)
        {
            var hintRoot = board.transform.Find("Mechanic Hint");
            if (hintRoot == null)
            {
                var go = new GameObject("Mechanic Hint", typeof(RectTransform), typeof(TextMeshProUGUI));
                go.transform.SetParent(board.transform, false); hintRoot = go.transform;
            }
            var label = hintRoot.GetComponent<TextMeshProUGUI>();
            var source = board.GetComponentsInChildren<TMP_Text>(true).First(t => t != label && t.font != null);
            label.font = source.font; label.fontSize = 22f; label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white; label.raycastTarget = false;
            var rect = label.rectTransform; rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f); rect.sizeDelta = new Vector2(640f, 44f);
            rect.anchoredPosition = new Vector2(0f, -278f);
            Set(board, "mechanicHint", label); label.text = ""; hintRoot.gameObject.SetActive(false);
        }

        private static T EnsureStep<T>(StoryEvent owner, string name) where T : StoryStep
        {
            Transform child = owner.transform.Find(name);
            if (child == null) child = Child(owner.transform, name);
            T step = child.GetComponent<T>();
            return step != null ? step : child.gameObject.AddComponent<T>();
        }

        private static void AuthorAfterCombat(StoryEvent story)
        {
            var dialogue = story.transform.Find("110_PuttingDownSupplies").GetComponent<DialogueStep>();
            var dc = (DialogueController)new SerializedObject(dialogue).FindProperty("dialogueController").objectReferenceValue;
            Transform audere = (Transform)new SerializedObject(story.transform.Find("030_AudereOnOwnTile").GetComponent<MoveActorStep>()).FindProperty("actor").objectReferenceValue;
            Transform bianca = (Transform)new SerializedObject(story.transform.Find("040_BiancaOnOwnTile").GetComponent<MoveActorStep>()).FindProperty("actor").objectReferenceValue;
            foreach (var pair in new[] { new { Name = "251_AudereStaysOnHerTile", Source = "030_AudereOnOwnTile" },
                new { Name = "252_BiancaOnAdjacentRightTile", Source = "040_BiancaOnOwnTile" } })
            {
                var src = new SerializedObject(story.transform.Find(pair.Source).GetComponent<MoveActorStep>());
                Set(EnsureStep<MoveActorStep>(story, pair.Name), "actor", src.FindProperty("actor").objectReferenceValue,
                    "targetTransform", src.FindProperty("targetTransform").objectReferenceValue, "duration", 0f, "useUnscaledTime", true);
            }
            Set(EnsureStep<SetActorFacingStep>(story, "253_AudereFacesBianca"), "actorRenderer", audere.GetComponent<SpriteRenderer>(), "faceRight", true, "sourceSpriteFacesLeft", true);
            Set(EnsureStep<SetActorFacingStep>(story, "254_BiancaFacesAudere"), "actorRenderer", bianca.GetComponent<SpriteRenderer>(), "faceRight", false, "sourceSpriteFacesLeft", true);
            var check = D("PostCombat", "CHECK_IN", DialogueCharacterId.Bianca, "Audere_Scared_0", "Bianca_Worried_0",
                "R|Audere?", "L|…Ừ.", "R|Cậu ổn không?", "L|Tớ lấy nhầm.", "R|Ừ.|Bianca_0", "R|Để lại là được mà.",
                "R|Sáng nay tớ cũng cầm nhầm danh sách.", "R|Tớ lấy luôn của nhóm đồ ăn.", "R|Đi nửa cầu thang mới nhận ra.");
            var stop = D("PostCombat", "DONT_ASK", DialogueCharacterId.Timor, "Audere_Scared_0", "TimorLolang_0",
                "R|Đừng hỏi.", "R|Cậu ấy sẽ chẳng nói thẳng đâu.");
            var ask = D("PostCombat", "ASK_BIANCA", DialogueCharacterId.Bianca, "Audere_Scared_0", "Bianca_0",
                "L|Bianca.", "R|Ừ?", "L|…Lúc nãy cậu có thấy tớ phiền không?", "R|Lúc nào?", "L|Lúc tớ lấy nhầm ấy.",
                "R|Không.", "R|Chuyện đó bình thường mà.", "R|Bọn mình đem trả cùng nhau nhé.|Bianca_Smiled_0");
            var boundary = D("PostCombat", "YOU_DONT_KNOW_EITHER", DialogueCharacterId.Timor, "Audere_0", "TimorLoLangKhongVui_0",
                "R|Cậu ấy có thể chỉ nói thế thôi.", "R|Cậu đâu biết cậu ấy thật sự nghĩ gì.", "L|…Cậu cũng đâu biết.",
                "R|Tớ chỉ muốn cậu cẩn thận.|TimorLolang_0", "L|…Ừ.");
            Set(EnsureStep<DialogueStep>(story, "270_BiancaChecksOnAudere"), "dialogueData", check, "dialogueController", dc);
            Set(EnsureStep<WaitStep>(story, "280_SmallPauseBeforeTimor"), "duration", .3f, "useUnscaledTime", true);
            Set(EnsureStep<DialogueStep>(story, "290_TimorSaysDontAsk"), "dialogueData", stop, "dialogueController", dc);
            Set(EnsureStep<WaitStep>(story, "300_AudereChoosesToAsk"), "duration", .45f, "useUnscaledTime", true);
            Set(EnsureStep<DialogueStep>(story, "310_AudereAsksBianca"), "dialogueData", ask, "dialogueController", dc);
            Set(EnsureStep<WaitStep>(story, "320_LetTheAnswerLand"), "duration", .4f, "useUnscaledTime", true);
            Set(EnsureStep<DialogueStep>(story, "330_AudereQuestionsTimor"), "dialogueData", boundary, "dialogueController", dc);
            Set(EnsureStep<WaitStep>(story, "340_HoldAfterSmallBoundary"), "duration", .65f, "useUnscaledTime", true);
            CanvasGroup cover = (CanvasGroup)new SerializedObject(story.transform.Find("240_CoverCombatResolution").GetComponent<CanvasFadeStep>()).FindProperty("canvasGroup").objectReferenceValue;
            Set(EnsureStep<CanvasFadeStep>(story, "350_FadeOutAfterAnswer"), "canvasGroup", cover, "targetAlpha", 1f, "duration", .9f, "useUnscaledTime", true);
            var order = story.transform.Cast<Transform>().OrderBy(t => t.name, StringComparer.Ordinal).ToArray();
            for (int i = 0; i < order.Length; i++) order[i].SetSiblingIndex(i);
        }
    }
}
#endif
