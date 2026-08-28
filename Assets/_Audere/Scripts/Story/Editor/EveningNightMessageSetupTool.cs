using System;
using System.Linq;
using Audere.Audio;
using Audere.Combat;
using Audere.Dialogue;
using Audere.Puzzle;
using Audere.Story;
using Audere.Story.Presentation;
using Audere.Story.Steps;
using Audere.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Audere.EditorTools
{
    /// <summary>
    /// Idempotent, scene-first authoring for the Day 1 evening message and Timor pressure
    /// encounter. All runtime references are serialized directly; no scene search is added
    /// to production code.
    /// </summary>
    public static class EveningNightMessageSetupTool
    {
        private const string ScenePath = "Assets/_Audere/Scenes/40_Evening.unity";
        private const string DialogueFolder = "Assets/_Audere/Data/Dialogue/Day1/Evening";
        private const string CombatFolder = "Assets/_Audere/Data/Combat/TimorNightPressure";
        private const string MoveFolder = CombatFolder + "/Moves";
        private const string BatchFolder = CombatFolder + "/DiceBatches";
        private const string EnemyPath = CombatFolder + "/Enemy_TimorNightPressure.asset";
        private const string EncounterPath = CombatFolder + "/CombatEncounter_D1_TIMOR_NIGHT_PRESSURE.asset";
        private const string EnemyPrefabSource = "Assets/_Audere/Prefabs/Combat/Enemies/Enemy_KhoangLang_PLACEHOLDER.prefab";
        private const string EnemyPrefabPath = "Assets/_Audere/Prefabs/Combat/Enemies/Enemy_TimorNightPressure_PLACEHOLDER.prefab";
        private const string BoardPrefabPath = "Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab";
        private const string PlayerPrefabPath = "Assets/_Audere/Prefabs/Puzzle/Actors/Player.prefab";
        private const string TilePrefabPath = "Assets/_Audere/Prefabs/Puzzle/Tiles/Grass.prefab";
        private const string ViewportPrefabPath = "Assets/_Audere/Prefabs/Puzzle/Camera/PuzzleViewportMask.prefab";
        private const string GameplayUiPrefabPath = "Assets/_Audere/Prefabs/UI/GameplayUIRoot.prefab";
        private const string TransitionProfilePath = "Assets/_Audere/Data/Transitions/WorldTransition_DreamyDisorientation.asset";
        private const string Renderer2DPath = "Assets/Settings/Renderer2D.asset";
        private const string AudioCatalogPath = "Assets/_Audere/Data/Audio/AudioCatalog.asset";
        private const string MessageClipPath = "Assets/_Audere/Audio/MessCome.mp3";
        private const string MessageIndicatorSpritePath = "Assets/_Audere/AssetGame/Item/dauchamthan.aseprite";
        private const string TimorArtFolder = "Assets/_Audere/AssetGame/Timor/";
        private const string AudereArtFolder = "Assets/_Audere/AssetGame/Audere/";
        private const string ChoiceFontPath = "Assets/_Audere/AssetGame/Font/Mynerve-Regular SDF.asset";

        [MenuItem("Audere/Story/Setup D1 Home Night Message")]
        public static void Setup()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[EveningNightMessageSetup] Stop Play Mode before authoring.");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                Debug.LogError($"[EveningNightMessageSetup] Open '{ScenePath}' first.");
                return;
            }

            EnsureFolder(DialogueFolder);
            EnsureFolder(MoveFolder);
            EnsureFolder(BatchFolder);
            ConfigureMessageAudio();
            EnsureCombatBoardProjectileMask();

            DialogueAssets dialogues = CreateDialogueAssets();
            CombatEncounterData encounter = CreateCombatAssets(dialogues.Barks, dialogues.Defeat);
            SetupScene(scene, encounter, dialogues);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[EveningNightMessageSetup] D1_HOME_NIGHT_MESSAGE and the 11-beat Timor scripted-defeat encounter are authored.");
        }

        [MenuItem("Audere/Combat/Author Timor Stun Zone Pressure")]
        public static void AuthorTimorStunZonePressure()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("[EveningNightMessageSetup] Stop Play Mode before authoring Stun Zone pressure.");
                return;
            }

            EnsureFolder(MoveFolder);
            int[] phaseIndexes = { 3, 5, 8 };
            for (int i = 0; i < phaseIndexes.Length; i++)
            {
                int phaseIndex = phaseIndexes[i];
                string primaryPath = phaseIndex == 5
                    ? $"{MoveFolder}/Move_TimorNightPressure_06_ShiftingBattleBox.asset"
                    : $"{MoveFolder}/Move_TimorNightPressure_{phaseIndex + 1:00}.asset";
                CombatMoveDefinition primary = LoadRequired<CombatMoveDefinition>(primaryPath);
                CombatMoveDefinition authored = WrapWithStunZonePressure(primary, phaseIndex);
                CombatMoveSet moveSet = LoadRequired<CombatMoveSet>(
                    $"{MoveFolder}/MoveSet_TimorNightPressure_{phaseIndex + 1:00}.asset");
                ConfigureMoveSet(moveSet, authored);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[EveningNightMessageSetup] Timor phases 04, 06 and 09 now compose their primary attack with authored Stun Zone pressure.");
        }

        private static DialogueAssets CreateDialogueAssets()
        {
            Sprite audereNeutral = LoadFirstSprite(AudereArtFolder + "Audere.png", "Audere_0");
            Sprite audereSmiled = LoadFirstSprite(AudereArtFolder + "Audere_smiled.png", "Audere_smiled_0");
            Sprite audereScared = LoadFirstSprite(AudereArtFolder + "Audere_Scared.png", "Audere_Scared_0");
            Sprite audereCrying = LoadFirstSprite(AudereArtFolder + "Audere_Crying.png", "Audere_Crying_0");
            Sprite audereTired = LoadFirstSprite(AudereArtFolder + "Audere_Tired.png", "Audere_Tired_0");
            Sprite timorWorried = LoadFirstSprite(TimorArtFolder + "TimorLolang.png");
            Sprite timorUneasy = LoadFirstSprite(TimorArtFolder + "TimorLoLangKhongVui.png");
            Sprite timorAngry = LoadFirstSprite(TimorArtFolder + "TimorTucGian.png");
            Sprite timorSad = LoadFirstSprite(TimorArtFolder + "TimorBuon.png");

            DialogueData opening = ConfigureDialogue(
                "Dialogue_D1_HOME_NIGHT_AUDERE_AFTER_LONG_DAY",
                "d1-home-night-audere-after-long-day",
                DialogueCharacterId.Timor,
                L("Hôm nay tớ nói chuyện"),
                L("với quá nhiều người rồi."));
            ConfigureDialoguePortraits(opening, audereTired, null);
            DialogueData message = ConfigureDialogue(
                "Dialogue_D1_HOME_NIGHT_BIANCA_MESSAGE",
                "d1-home-night-bianca-message",
                DialogueCharacterId.Bianca,
                R("Ngoài phần làm bảng,"),
                R("mai cậu có muốn chuẩn bị đồ cùng lớp?"));
            ConfigureDialoguePortraits(message, audereSmiled, null);
            DialogueData recognition = ConfigureDialogue(
                "Dialogue_D1_HOME_NIGHT_AUDERE_RECOGNIZES_BIANCA",
                "d1-home-night-audere-recognizes-bianca",
                DialogueCharacterId.Timor,
                L("Bianca nhắn cho tớ này."));
            ConfigureDialoguePortraits(recognition, audereSmiled, null);
            DialogueData question = ConfigureDialogue(
                "Dialogue_D1_HOME_NIGHT_TIMOR_QUESTIONS",
                "d1-home-night-timor-questions",
                DialogueCharacterId.Timor,
                R("Audere... cậu không thấy sợ à?"),
                L("Sợ gì?"),
                R("Nếu Bianca chỉ tìm cậu"),
                R("vì muốn nhờ vả thì sao?"),
                L("Tớ nghĩ cậu ấy chỉ đang hỏi thôi."),
                R("Tớ cũng muốn tin như vậy."),
                R("Nhưng tớ sợ lắm."),
                R("Mẹ cậu cũng từng tin người khác."),
                R("Rồi cậu đã mất bà ấy."),
                L("Timor... chuyện đó không giống nhau."),
                L("Bianca chỉ nhắn cho tớ thôi."));
            ConfigureDialoguePortraits(question, audereNeutral, timorWorried, (6, timorUneasy), (9, audereScared));
            DialogueData conclusion = ConfigureDialogue(
                "Dialogue_D1_HOME_NIGHT_CONCLUSION",
                "d1-home-night-conclusion",
                DialogueCharacterId.Timor,
                R("Cậu không biết cô ấy muốn gì đâu."),
                L("Tớ có thể hỏi lại."),
                R("Rồi nếu cô ấy tiếp tục nhờ thì sao?"),
                L("Nếu nhiều quá... tớ sẽ nói là không."),
                R("Audere."),
                L("Tớ biết cậu đang lo."),
                L("Nhưng tớ vẫn muốn trả lời."),
                R("Không được đâu, Audere."),
                R("Tớ đã thấy chuyện gì xảy ra rồi."),
                L("Timor, đừng lo."),
                R("Đừng bảo tớ đừng lo!"),
                R("Cậu phải nghe tớ lần này."),
                R("Tin tớ đi."),
                R("Giữ khoảng cách với cô ấy."),
                L("...Tớ không muốn."),
                R("Audere."),
                L("Lần này, để tớ tự trả lời."),
                R("Tớ không thể để cậu làm vậy."));
            ConfigureDialoguePortraits(conclusion, audereScared, timorUneasy, (7, timorAngry));

            DialogueLine[][] barkLines =
            {
                new[] { R("Tớ chỉ muốn cậu được an toàn."), L("Tớ biết."), L("Nhưng tớ vẫn muốn trả lời.") },
                new[] { R("Vậy thì đứng yên."), R("Đừng bước ra khỏi chỗ tớ chỉ.") },
                new[] { R("Cậu nghĩ mình sẽ nói được 'không' à?"), L("Tớ có thể thử.") },
                new[] { R("Tớ không muốn mất cậu như mẹ cậu.") },
                new[] { R("Cậu không biết họ sẽ đòi hỏi gì đâu."), R("Đừng tự đẩy mình vào đó.") },
                new[] { L("Bianca đã cho tớ lựa chọn."), R("Lựa chọn cũng là cách họ kéo cậu vào.") },
                new[] { R("Tớ đã ở đây khi chẳng còn ai."), R("Sao cậu lại không tin tớ?") },
                new[] { R("Đừng né lời tớ."), R("Nhìn tớ đi, Audere.") },
                new[] { L("Tớ không bỏ cậu."), R("Vậy sao cậu vẫn chọn cô ấy?") },
                new[] { R("Cậu không cần thêm bất kỳ ai."), R("Có tớ là đủ rồi.") },
                new[] { R("Hôm nay đã đủ rồi."), R("Tớ sẽ không để cậu trả lời.") },
            };
            DialogueData[] barks = new DialogueData[barkLines.Length];
            for (int i = 0; i < barks.Length; i++)
            {
                barks[i] = ConfigureDialogue(
                    $"Dialogue_D1_TIMOR_NIGHT_PRESSURE_BARK_{i + 1:00}",
                    $"d1-timor-night-pressure-bark-{i + 1:00}",
                    DialogueCharacterId.Timor,
                    barkLines[i]);
                Sprite timorPortrait = i switch
                {
                    <= 2 => timorWorried,
                    <= 5 => timorUneasy,
                    <= 9 => timorAngry,
                    _ => timorSad,
                };
                // Audere carries fear into combat; the loyalty demand hurts before exhaustion.
                Sprite auderePortrait = i switch
                {
                    <= 7 => audereScared,
                    <= 9 => audereCrying,
                    _ => audereTired,
                };
                ConfigureDialoguePortraits(barks[i], auderePortrait, timorPortrait);
            }

            DialogueData defeat = ConfigureDialogue(
                "Dialogue_D1_TIMOR_NIGHT_PRESSURE_DEFEAT",
                "d1-timor-night-pressure-defeat",
                DialogueCharacterId.Timor,
                L("…"),
                R("Thấy chưa."),
                R("Cậu mệt rồi."),
                R("Hôm nay cậu đã cố đủ nhiều rồi."),
                L("…Ừ."));
            ConfigureDialoguePortraits(defeat, audereTired, timorSad);

            DialogueData beforeChoice = ConfigureDialogue(
                "Dialogue_D1_HOME_NIGHT_BEFORE_REPLY_CHOICE",
                "d1-home-night-before-reply-choice",
                DialogueCharacterId.Timor,
                R("Không cần ép mình."),
                R("Chọn câu dễ nhất thôi."));
            ConfigureDialoguePortraits(beforeChoice, audereTired, timorSad);

            DialogueData avoidTimor = ConfigureDialogue(
                "Dialogue_D1_HOME_NIGHT_REPLY_AVOID_TIMOR",
                "d1-home-night-reply-avoid-timor",
                DialogueCharacterId.Timor,
                R("Ừ."),
                R("Thế là ngày mai cậu không phải lo nữa."));
            ConfigureDialoguePortraits(avoidTimor, audereTired, timorSad);
            DialogueData avoidAudere = ConfigureDialogue(
                "Dialogue_D1_HOME_NIGHT_REPLY_AVOID_AUDERE",
                "d1-home-night-reply-avoid-audere",
                DialogueCharacterId.Timor,
                L("…Ừ."));
            ConfigureDialoguePortraits(avoidAudere, audereTired, timorSad);

            DialogueData delay = ConfigureDialogue(
                "Dialogue_D1_HOME_NIGHT_REPLY_DELAY",
                "d1-home-night-reply-delay",
                DialogueCharacterId.Timor,
                R("Tốt rồi."),
                R("Mai nếu cậu thấy ổn thì tính tiếp."));
            ConfigureDialoguePortraits(delay, audereTired, timorSad);

            DialogueData silence = ConfigureDialogue(
                "Dialogue_D1_HOME_NIGHT_REPLY_SILENCE",
                "d1-home-night-reply-silence",
                DialogueCharacterId.Timor,
                R("Không trả lời cũng là một câu trả lời."),
                L("Bianca sẽ nghĩ gì nhỉ?"),
                R("Ngày mai rồi tính."),
                R("Nghỉ thôi."));
            ConfigureDialoguePortraits(silence, audereTired, timorSad);

            return new DialogueAssets(
                opening,
                recognition,
                message,
                question,
                conclusion,
                barks,
                defeat,
                beforeChoice,
                avoidTimor,
                avoidAudere,
                delay,
                silence);
        }

        private static DialogueData ConfigureDialogue(
            string assetName,
            string id,
            DialogueCharacterId counterpart,
            params DialogueLine[] lines)
        {
            DialogueData asset = EnsureAsset<DialogueData>($"{DialogueFolder}/{assetName}.asset");
            SerializedObject serialized = new SerializedObject(asset);
            serialized.FindProperty("dialogueId").stringValue = id;
            serialized.FindProperty("leftCharacter").intValue = (int)DialogueCharacterId.Audere;
            serialized.FindProperty("rightCharacter").intValue = (int)counterpart;
            SerializedProperty list = serialized.FindProperty("lines");
            list.arraySize = lines.Length;
            for (int i = 0; i < lines.Length; i++)
            {
                SerializedProperty line = list.GetArrayElementAtIndex(i);
                line.FindPropertyRelative("speaker").intValue = (int)lines[i].Side;
                line.FindPropertyRelative("text").stringValue = lines[i].Text;
                line.FindPropertyRelative("portraitOverride").objectReferenceValue = null;
                if (lines[i].Text.Length > 42)
                    Debug.LogWarning($"[EveningNightMessageSetup] '{assetName}' line {i + 1} exceeds the 42-character dialogue target.", asset);
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void ConfigureDialoguePortraits(
            DialogueData dialogue,
            Sprite leftPortrait,
            Sprite rightPortrait,
            params (int LineIndex, Sprite Portrait)[] lineOverrides)
        {
            SerializedObject serialized = new SerializedObject(dialogue);
            serialized.FindProperty("leftPortraitOverride").objectReferenceValue = leftPortrait;
            serialized.FindProperty("rightPortraitOverride").objectReferenceValue = rightPortrait;
            SerializedProperty lines = serialized.FindProperty("lines");
            if (lineOverrides != null)
            {
                for (int index = 0; index < lineOverrides.Length; index++)
                {
                    (int lineIndex, Sprite portrait) = lineOverrides[index];
                    if (lineIndex < 0 || lineIndex >= lines.arraySize)
                        throw new IndexOutOfRangeException($"Portrait override line {lineIndex} is invalid for '{dialogue.name}'.");
                    lines.GetArrayElementAtIndex(lineIndex)
                        .FindPropertyRelative("portraitOverride")
                        .objectReferenceValue = portrait;
                }
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(dialogue);
        }

        private static CombatEncounterData CreateCombatAssets(DialogueData[] barks, DialogueData defeatDialogue)
        {
            CombatBulletView bulletPrefab = LoadRequired<GameObject>(BoardPrefabPath)
                .GetComponent<CombatBoardView>()
                .GetSerializedReference<CombatBulletView>("enemyBulletPrefab");
            if (bulletPrefab == null)
                throw new MissingReferenceException("CombatBoard prefab requires an enemy bullet prefab.");

            CombatDiceBatchDefinition[] batches = CreateDiceBatches();
            CombatMoveSet[] moveSets = new CombatMoveSet[11];
            for (int i = 0; i < moveSets.Length; i++)
            {
                CombatMoveDefinition move;
                if (i == 5)
                {
                    ShiftingBattleBoxMove shiftingMove = EnsureAsset<ShiftingBattleBoxMove>(
                        $"{MoveFolder}/Move_TimorNightPressure_06_ShiftingBattleBox.asset");
                    ConfigureShiftingBattleBoxMove(shiftingMove);
                    move = shiftingMove;
                }
                else
                {
                    NarrativePressurePatternMove pressureMove = EnsureAsset<NarrativePressurePatternMove>(
                        $"{MoveFolder}/Move_TimorNightPressure_{i + 1:00}.asset");
                    ConfigureMove(pressureMove, bulletPrefab, i);
                    move = pressureMove;
                }
                move = WrapWithStunZonePressure(move, i);
                moveSets[i] = EnsureAsset<CombatMoveSet>(
                    $"{MoveFolder}/MoveSet_TimorNightPressure_{i + 1:00}.asset");
                ConfigureMoveSet(moveSets[i], move);
            }

            CombatEnemyActor actorPrefab = EnsureEnemyPrefab();
            CombatEnemyDefinition enemy = EnsureAsset<CombatEnemyDefinition>(EnemyPath);
            ConfigureEnemy(enemy, actorPrefab, moveSets, batches, barks);

            CombatEncounterData encounter = EnsureAsset<CombatEncounterData>(EncounterPath);
            SerializedObject serialized = new SerializedObject(encounter);
            serialized.FindProperty("encounterId").stringValue = "d1-timor-night-pressure";
            serialized.FindProperty("music").intValue = (int)Audere.Audio.AudioId.Music_TimorCombat;
            SetObject(serialized, "enemyDefinition", enemy);
            SetObject(serialized, "tutorialData", null);
            serialized.FindProperty("encounterDuration").floatValue = 66f;
            SerializedProperty rules = serialized.FindProperty("outcomeRules");
            rules.FindPropertyRelative("allowedOutcomes").intValue = (int)CombatAllowedOutcome.Defeat;
            rules.FindPropertyRelative("playerDefeatGate").intValue = (int)CombatPlayerDefeatGate.CurrentPhaseAndRequiredCues;
            rules.FindPropertyRelative("showRetryOnDefeat").boolValue = false;
            SerializedProperty defeatPresentation = serialized.FindProperty("defeatPresentation");
            SetObject(defeatPresentation, "dialogue", defeatDialogue);
            defeatPresentation.FindPropertyRelative("hazardFadeDuration").floatValue = .62f;
            serialized.FindProperty("dicePerBatch").intValue = 3;
            serialized.FindProperty("batchRespawnDelay").floatValue = .18f;
            serialized.FindProperty("minimumDiceSpeed").floatValue = 105f;
            serialized.FindProperty("maximumDiceSpeed").floatValue = 132f;
            serialized.FindProperty("playerHitInvulnerability").floatValue = .70f;
            serialized.FindProperty("bulletTimePenaltySeconds").floatValue = 3f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(encounter);
            return encounter;
        }

        private static CombatDiceBatchDefinition[] CreateDiceBatches()
        {
            Vector2[][] positions =
            {
                new[] { new Vector2(.18f,.82f), new Vector2(.82f,.18f), new Vector2(.78f,.82f) },
                new[] { new Vector2(.10f,.50f), new Vector2(.90f,.32f), new Vector2(.90f,.70f) },
                new[] { new Vector2(.16f,.78f), new Vector2(.84f,.72f), new Vector2(.18f,.20f) },
                new[] { new Vector2(.15f,.24f), new Vector2(.50f,.82f), new Vector2(.85f,.24f) },
                new[] { new Vector2(.24f,.74f), new Vector2(.76f,.70f), new Vector2(.70f,.24f) },
                new[] { new Vector2(.18f,.22f), new Vector2(.52f,.78f), new Vector2(.84f,.34f) },
                new[] { new Vector2(.24f,.50f), new Vector2(.66f,.76f), new Vector2(.76f,.30f) },
                new[] { new Vector2(.82f,.24f), new Vector2(.76f,.76f), new Vector2(.20f,.54f) },
                new[] { new Vector2(.28f,.70f), new Vector2(.72f,.68f), new Vector2(.50f,.24f) },
                new[] { new Vector2(.80f,.72f), new Vector2(.20f,.32f), new Vector2(.76f,.22f) },
            };
            CombatSymbol[] symbols = { CombatSymbol.Attack, CombatSymbol.Shield, CombatSymbol.Heal };
            CombatDiceBatchDefinition[] result = new CombatDiceBatchDefinition[10];
            for (int batchIndex = 0; batchIndex < result.Length; batchIndex++)
            {
                result[batchIndex] = EnsureAsset<CombatDiceBatchDefinition>(
                    $"{BatchFolder}/DiceBatch_TimorNightPressure_{batchIndex + 1:00}.asset");
                SerializedObject serialized = new SerializedObject(result[batchIndex]);
                serialized.FindProperty("spawnDelay").floatValue = batchIndex == 2 ? 1.1f : .12f;
                SerializedProperty dice = serialized.FindProperty("dice");
                dice.arraySize = 3;
                for (int dieIndex = 0; dieIndex < 3; dieIndex++)
                {
                    SerializedProperty die = dice.GetArrayElementAtIndex(dieIndex);
                    die.FindPropertyRelative("symbol").intValue = (int)symbols[dieIndex];
                    die.FindPropertyRelative("normalizedPosition").vector2Value = positions[batchIndex][dieIndex];
                    Vector2 towardCenter = (new Vector2(.5f, .5f) - positions[batchIndex][dieIndex]).normalized;
                    die.FindPropertyRelative("normalizedDirection").vector2Value = towardCenter;
                    die.FindPropertyRelative("speedMultiplier").floatValue = .78f + dieIndex * .08f;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(result[batchIndex]);
            }
            return result;
        }

        private static void ConfigureMove(
            NarrativePressurePatternMove move,
            CombatBulletView bulletPrefab,
            int index)
        {
            float[] durations = { 5.4f, 5.1f, 5.3f, 5.4f, 5.6f, 5.4f, 5.5f, 5.1f, 5.6f, 5.4f, 30f };
            float[] beats = { 2f, 4f, 2f, 2f, 1.5f, 2f, 2f, 4f, 1.5f, 2f, .5f };
            float[] intervals = System.Array.ConvertAll(beats, count => count * 60f / 110f);
            float[] speeds = { 105f, 110f, 116f, 112f, 118f, 112f, 94f, 106f, 92f, 116f, 136f };
            int[] intensity = { 5, 5, 5, 5, 5, 5, 5, 5, 4, 4, 5 };
            SerializedObject serialized = new SerializedObject(move);
            serialized.FindProperty("duration").floatValue = durations[index];
            SetObject(serialized, "projectilePrefab", bulletPrefab);
            serialized.FindProperty("pattern").intValue = index;
            serialized.FindProperty("waveInterval").floatValue = intervals[index];
            serialized.FindProperty("rhythmMusic").intValue = (int)Audere.Audio.AudioId.Music_TimorCombat;
            serialized.FindProperty("rhythmBpm").floatValue = 110f;
            serialized.FindProperty("rhythmBeatOffset").floatValue = .013f;
            serialized.FindProperty("waveBeats").floatValue = beats[index];
            serialized.FindProperty("speed").floatValue = speeds[index];
            serialized.FindProperty("spacing").floatValue = index == 10 ? 46f : 42f;
            float telegraph = index switch
            {
                1 => .45f,
                7 => .50f,
                9 => .50f,
                10 => .14f,
                _ => .18f,
            };
            telegraph = (index == 1 || index == 7 || index == 9 ? 1f : .5f) * 60f / 110f;
            serialized.FindProperty("telegraphDuration").floatValue = telegraph;
            serialized.FindProperty("safeGapFraction").floatValue = index == 10 ? .12f : .30f;
            serialized.FindProperty("intensity").intValue = intensity[index];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(move);
        }

        private static void ConfigureShiftingBattleBoxMove(ShiftingBattleBoxMove move)
        {
            SerializedObject serialized = new SerializedObject(move);
            serialized.FindProperty("duration").floatValue = 5.19f;
            SerializedProperty poses = serialized.FindProperty("poses");
            poses.arraySize = 3;
            SetBattleBoxPose(poses.GetArrayElementAtIndex(0), .72f, -.70f);
            SetBattleBoxPose(poses.GetArrayElementAtIndex(1), .58f, .75f);
            SetBattleBoxPose(poses.GetArrayElementAtIndex(2), .46f, 0f);
            serialized.FindProperty("telegraphDuration").floatValue = .35f;
            serialized.FindProperty("squeezeDuration").floatValue = .42f;
            serialized.FindProperty("holdDuration").floatValue = .50f;
            serialized.FindProperty("returnDuration").floatValue = .46f;
            serialized.FindProperty("telegraphWidthPulse").floatValue = .035f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(move);
        }

        private static CombatMoveDefinition WrapWithStunZonePressure(
            CombatMoveDefinition primary,
            int phaseIndex)
        {
            if (phaseIndex != 3 && phaseIndex != 5 && phaseIndex != 8)
                return primary;

            int phaseNumber = phaseIndex + 1;
            StunZonePressureMove stun = EnsureAsset<StunZonePressureMove>(
                $"{MoveFolder}/Move_TimorNightPressure_{phaseNumber:00}_StunZone.asset");
            ConfigureStunZoneMove(stun, primary.Duration, phaseIndex);

            CompositeCombatMove composite = EnsureAsset<CompositeCombatMove>(
                $"{MoveFolder}/Move_TimorNightPressure_{phaseNumber:00}_WithStunZone.asset");
            SerializedObject serialized = new SerializedObject(composite);
            serialized.FindProperty("duration").floatValue = primary.Duration;
            SerializedProperty children = serialized.FindProperty("children");
            children.arraySize = 2;
            children.GetArrayElementAtIndex(0).objectReferenceValue = primary;
            children.GetArrayElementAtIndex(1).objectReferenceValue = stun;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(composite);
            return composite;
        }

        private static void ConfigureStunZoneMove(
            StunZonePressureMove move,
            float duration,
            int phaseIndex)
        {
            SerializedObject serialized = new SerializedObject(move);
            serialized.FindProperty("duration").floatValue = duration;
            serialized.FindProperty("telegraphAlpha").floatValue = .42f;
            serialized.FindProperty("activeAlpha").floatValue = .82f;
            serialized.FindProperty("zoneSlot").intValue = 0;
            SerializedProperty pulses = serialized.FindProperty("pulses");

            if (phaseIndex == 3)
            {
                pulses.arraySize = 2;
                SetStunZonePulse(pulses.GetArrayElementAtIndex(0), new Vector2(.22f, .5f), new Vector2(.20f, .88f), .34f, .72f, .18f, .24f);
                SetStunZonePulse(pulses.GetArrayElementAtIndex(1), new Vector2(.78f, .5f), new Vector2(.20f, .88f), .34f, .72f, .18f, .24f);
            }
            else if (phaseIndex == 5)
            {
                pulses.arraySize = 3;
                SetStunZonePulse(pulses.GetArrayElementAtIndex(0), new Vector2(.20f, .5f), new Vector2(.28f, .90f), .26f, .55f, .16f, .18f);
                SetStunZonePulse(pulses.GetArrayElementAtIndex(1), new Vector2(.80f, .5f), new Vector2(.28f, .90f), .26f, .55f, .16f, .18f);
                SetStunZonePulse(pulses.GetArrayElementAtIndex(2), new Vector2(.50f, .5f), new Vector2(.24f, .90f), .30f, .48f, .16f, .22f);
            }
            else
            {
                pulses.arraySize = 3;
                SetStunZonePulse(pulses.GetArrayElementAtIndex(0), new Vector2(.50f, .5f), new Vector2(.22f, .90f), .28f, .60f, .16f, .20f);
                SetStunZonePulse(pulses.GetArrayElementAtIndex(1), new Vector2(.50f, .74f), new Vector2(.82f, .20f), .28f, .55f, .16f, .20f);
                SetStunZonePulse(pulses.GetArrayElementAtIndex(2), new Vector2(.50f, .26f), new Vector2(.82f, .20f), .28f, .55f, .16f, .20f);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(move);
        }

        private static void SetStunZonePulse(
            SerializedProperty pulse,
            Vector2 center,
            Vector2 size,
            float telegraph,
            float active,
            float fadeOut,
            float hidden)
        {
            pulse.FindPropertyRelative("normalizedCenter").vector2Value = center;
            pulse.FindPropertyRelative("normalizedSize").vector2Value = size;
            pulse.FindPropertyRelative("telegraphDuration").floatValue = telegraph;
            pulse.FindPropertyRelative("activeDuration").floatValue = active;
            pulse.FindPropertyRelative("fadeOutDuration").floatValue = fadeOut;
            pulse.FindPropertyRelative("hiddenDuration").floatValue = hidden;
        }

        private static void SetBattleBoxPose(SerializedProperty pose, float widthFraction, float normalizedX)
        {
            pose.FindPropertyRelative("widthFraction").floatValue = widthFraction;
            pose.FindPropertyRelative("normalizedX").floatValue = normalizedX;
        }

        private static void ConfigureMoveSet(CombatMoveSet moveSet, CombatMoveDefinition move)
        {
            SerializedObject serialized = new SerializedObject(moveSet);
            serialized.FindProperty("selectionPolicy").intValue = (int)CombatMoveSelectionPolicy.OrderedLoop;
            SerializedProperty entries = serialized.FindProperty("entries");
            entries.arraySize = 1;
            SetObject(entries.GetArrayElementAtIndex(0), "move", move);
            entries.GetArrayElementAtIndex(0).FindPropertyRelative("weight").floatValue = 1f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(moveSet);
        }

        private static CombatEnemyActor EnsureEnemyPrefab()
        {
            GameObject sourceRoot = PrefabUtility.LoadPrefabContents(EnemyPrefabSource);
            try
            {
                sourceRoot.name = "Enemy_TimorNightPressure_PLACEHOLDER";
                Transform visual = sourceRoot.transform.Cast<Transform>()
                    .FirstOrDefault(child => child.name.StartsWith("Visual_", StringComparison.Ordinal));
                if (visual != null)
                    visual.name = "Visual_TIMOR_ENEMY_PLACEHOLDER_NON_CANON";
                PrefabUtility.SaveAsPrefabAsset(sourceRoot, EnemyPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(sourceRoot);
            }
            return LoadRequired<GameObject>(EnemyPrefabPath).GetComponent<CombatEnemyActor>();
        }

        private static void ConfigureEnemy(
            CombatEnemyDefinition enemy,
            CombatEnemyActor actorPrefab,
            CombatMoveSet[] moveSets,
            CombatDiceBatchDefinition[] batches,
            DialogueData[] barks)
        {
            SerializedObject serialized = new SerializedObject(enemy);
            serialized.FindProperty("enemyId").stringValue = "d1-evening-timor-night-pressure";
            serialized.FindProperty("displayName").stringValue = "Timor";
            SetObject(serialized, "actorPrefab", actorPrefab);
            serialized.FindProperty("phasePolicy").intValue = (int)CombatPhasePolicy.CapturedDiceBatchSequence;
            serialized.FindProperty("sharedMaxHealth").intValue = 36;
            SerializedProperty phases = serialized.FindProperty("phases");
            phases.arraySize = 11;
            for (int i = 0; i < phases.arraySize; i++)
            {
                SerializedProperty phase = phases.GetArrayElementAtIndex(i);
                phase.FindPropertyRelative("phaseId").stringValue = $"night-pressure-{i + 1:00}";
                phase.FindPropertyRelative("maxHealth").intValue = 36;
                phase.FindPropertyRelative("sharedExitThreshold").intValue = 0;
                phase.FindPropertyRelative("duration").floatValue = i == 10 ? 30f : 6f;
                SetObject(phase, "moveSet", moveSets[i]);
                SetObject(phase, "diceBatch", i < 10 ? batches[i] : null);
                phase.FindPropertyRelative("requiredCapturedBatches").intValue = 1;
                phase.FindPropertyRelative("spawnDice").boolValue = i < 10;
                phase.FindPropertyRelative("allowsPlayerDefeat").boolValue = i == 10;
                phase.FindPropertyRelative("minimumPlayerTimeOnEnter").floatValue = i == 10 ? 30f : 0f;

                SerializedProperty cues = phase.FindPropertyRelative("dialogueCues");
                cues.arraySize = 1;
                SerializedProperty cue = cues.GetArrayElementAtIndex(0);
                string cueId = $"timor-night-pressure-{i + 1:00}";
                cue.FindPropertyRelative("cueId").stringValue = cueId;
                cue.FindPropertyRelative("oneShotKey").stringValue = cueId;
                cue.FindPropertyRelative("trigger").intValue = (int)CombatDialogueCueTrigger.PhaseEnter;
                cue.FindPropertyRelative("presentation").intValue = (int)CombatDialoguePresentation.AutoCombatDialogue;
                cue.FindPropertyRelative("minimumLineDuration").floatValue = 1.15f;
                cue.FindPropertyRelative("charactersPerSecond").floatValue = 24f;
                cue.FindPropertyRelative("interLineGap").floatValue = .12f;
                cue.FindPropertyRelative("requiredBeforePhaseAdvance").boolValue = i < 10;
                cue.FindPropertyRelative("requiredBeforePlayerDefeat").boolValue = i == 10;
                SerializedProperty sequence = cue.FindPropertyRelative("sequence");
                sequence.arraySize = 1;
                sequence.GetArrayElementAtIndex(0).objectReferenceValue = barks[i];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(enemy);
            if (!enemy.Validate(out string error))
                throw new InvalidOperationException(error);
        }

        private static void SetupScene(Scene scene, CombatEncounterData encounter, DialogueAssets dialogue)
        {
            Transform preservedIndicator = FindRoot(scene, "WORLD")?.transform.Find(
                "Story Root/Evening Stage PLACEHOLDER_NO_ART/Audere/dauchamthan");
            Vector3 indicatorLocalPosition = preservedIndicator != null
                ? preservedIndicator.localPosition
                : new Vector3(.026f, .708f, .6666667f);
            Quaternion indicatorLocalRotation = preservedIndicator != null
                ? preservedIndicator.localRotation
                : Quaternion.identity;
            Vector3 indicatorLocalScale = preservedIndicator != null
                ? preservedIndicator.localScale
                : Vector3.one * .7f;
            if (preservedIndicator != null)
                preservedIndicator.SetParent(null, true);

            DestroyRoot(scene, "EVENING_PLACEHOLDER_NO_ART");
            DestroyRoot(scene, "WORLD");
            DestroyRoot(scene, "SYSTEMS");
            DestroyRoot(scene, "NIGHT MESSAGE UI");

            Camera camera = RequireRoot(scene, "Main Camera").GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.035f, .023f, .055f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 1.05f;
            camera.transform.position = new Vector3(0f, -.04f, -10f);
            if (camera.GetComponent<AudioListener>() == null)
                camera.gameObject.AddComponent<AudioListener>();
            UniversalAdditionalCameraData cameraData = camera.GetComponent<UniversalAdditionalCameraData>();
            if (cameraData != null)
                cameraData.renderPostProcessing = true;

            GameObject gameplayUiObject = EnsureRootPrefab(
                scene,
                "GameplayUIRoot",
                LoadRequired<GameObject>(GameplayUiPrefabPath));
            GameplayUIRoot gameplayUi = gameplayUiObject.GetComponent<GameplayUIRoot>();
            if (gameplayUi == null || gameplayUi.InputGate == null)
                throw new MissingReferenceException("GameplayUIRoot requires its direct InputGate reference.");
            NightMessageUi nightUi = CreateNightMessageUi(gameplayUi.InputGate);

            GameObject viewport = EnsurePrefabChild(
                camera.transform,
                "PuzzleViewportMask",
                LoadRequired<GameObject>(ViewportPrefabPath));
            viewport.transform.localPosition = new Vector3(0f, 0f, 9f);
            viewport.transform.localRotation = Quaternion.identity;
            viewport.transform.localScale = Vector3.one * .488376f;

            GameObject world = new GameObject("WORLD");
            Transform storyRoot = EnsureChild(world.transform, "Story Root");
            // Match the actor/tile authoring space used by 30_Classroom while keeping
            // the night tile centered on this scene's camera.
            storyRoot.localPosition = new Vector3(0f, -.3275f, 0f);
            storyRoot.localRotation = Quaternion.identity;
            storyRoot.localScale = Vector3.one * .25f;
            Transform stage = EnsureChild(storyRoot, "Evening Stage PLACEHOLDER_NO_ART");
            GameObject tile = EnsurePrefabChild(stage, "Night Tile PLACEHOLDER", LoadRequired<GameObject>(TilePrefabPath));
            tile.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            tile.transform.localRotation = Quaternion.identity;
            tile.transform.localScale = Vector3.one;
            SpriteRenderer tileRenderer = tile.GetComponentInChildren<SpriteRenderer>(true);
            if (tileRenderer != null)
                tileRenderer.color = new Color(.38f, .43f, .56f, 1f);

            GameObject audere = EnsurePrefabChild(stage, "Audere", LoadRequired<GameObject>(PlayerPrefabPath));
            audere.transform.localPosition = new Vector3(0f, 1.8475f, -1f);
            audere.transform.localRotation = Quaternion.identity;
            audere.transform.localScale = Vector3.one * 1.5f;
            SpriteRenderer audereRenderer = audere.GetComponent<SpriteRenderer>();
            audereRenderer.sortingLayerName = "Player";
            audereRenderer.sortingOrder = 5;
            Transform shadow = audere.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t != audere.transform && t.name.StartsWith("shadow", StringComparison.OrdinalIgnoreCase));
            if (shadow == null)
                throw new MissingReferenceException("Player.prefab requires its grounded shadow.");
            SpriteRenderer shadowRenderer = shadow.GetComponent<SpriteRenderer>();
            if (shadowRenderer != null)
            {
                shadowRenderer.sortingLayerName = "Player";
                shadowRenderer.sortingOrder = 4;
            }

            GameObject messageIndicator = preservedIndicator != null
                ? preservedIndicator.gameObject
                : CreateMessageIndicator();
            messageIndicator.name = "dauchamthan";
            messageIndicator.transform.SetParent(audere.transform, false);
            messageIndicator.transform.localPosition = indicatorLocalPosition;
            messageIndicator.transform.localRotation = indicatorLocalRotation;
            messageIndicator.transform.localScale = indicatorLocalScale;
            SpriteRenderer indicatorRenderer = messageIndicator.GetComponent<SpriteRenderer>();
            if (indicatorRenderer == null)
                indicatorRenderer = messageIndicator.AddComponent<SpriteRenderer>();
            indicatorRenderer.sortingLayerName = "Player";
            indicatorRenderer.sortingOrder = 6;
            messageIndicator.SetActive(false);

            Transform staging = EnsureChild(storyRoot, "STAGING TARGETS");
            Transform nightPose = EnsureChild(staging, "Audere_NightCenterPose");
            nightPose.localPosition = audere.transform.localPosition;

            Transform combatRoot = EnsureChild(world.transform, "Combat Root");
            GameObject board = EnsurePrefabChild(combatRoot, "Combat Board", LoadRequired<GameObject>(BoardPrefabPath));
            board.transform.localPosition = new Vector3(0f, -.42f, 0f);
            board.transform.localRotation = Quaternion.identity;
            board.transform.localScale = Vector3.one * .0025f;
            CombatBoardView boardView = board.GetComponent<CombatBoardView>();
            foreach (TMP_Text text in board.GetComponentsInChildren<TMP_Text>(true))
                if (text.name == "Enemy Name") text.text = encounter.EnemyDisplayName;
            Audere.Combat.Editor.CombatEnemyPrototypeAuthoring.BindEnemySceneActor(
                scene,
                encounter.EnemyDefinition.ActorPrefab);

            GameObject systems = new GameObject("SYSTEMS");
            GameObject combatSystems = new GameObject("Combat Systems");
            combatSystems.transform.SetParent(systems.transform, false);
            CombatController controller = combatSystems.AddComponent<CombatController>();
            SerializedObject controllerSerialized = new SerializedObject(controller);
            controllerSerialized.FindProperty("playOnStart").boolValue = false;
            SetObject(controllerSerialized, "encounterData", encounter);
            SetObject(controllerSerialized, "boardView", boardView);
            controllerSerialized.ApplyModifiedPropertiesWithoutUndo();

            CanvasGroup fade = RequireRoot(scene, "Scene Transition Overlay")
                .transform.Find("Fade")?.GetComponent<CanvasGroup>();
            if (fade == null)
                throw new MissingReferenceException("Scene Transition Overlay/Fade requires CanvasGroup.");
            fade.alpha = 1f;
            fade.blocksRaycasts = true;
            fade.interactable = false;

            WorldModeController mode = world.AddComponent<WorldModeController>();
            SerializedObject modeSerialized = new SerializedObject(mode);
            modeSerialized.FindProperty("startingMode").intValue = (int)WorldGameplayMode.Story;
            SetObject(modeSerialized, "puzzleRoot", null);
            SetObject(modeSerialized, "combatRoot", combatRoot.gameObject);
            SetObject(modeSerialized, "storyRoot", storyRoot.gameObject);
            SetObject(modeSerialized, "puzzleViewportMask", viewport);
            modeSerialized.FindProperty("storyUsesPuzzleViewportMask").boolValue = true;
            SetObject(modeSerialized, "combatSystemsRoot", combatSystems);
            SetObject(modeSerialized, "transitionFade", fade);
            modeSerialized.FindProperty("revealStartingModeOnStart").boolValue = false;
            modeSerialized.FindProperty("fadeOutDuration").floatValue = .22f;
            modeSerialized.FindProperty("coveredHoldDuration").floatValue = .05f;
            modeSerialized.FindProperty("fadeInDuration").floatValue = .32f;
            SetObject(modeSerialized, "worldCamera", camera);
            SetObject(modeSerialized, "puzzleCameraFollow", camera.GetComponent<GridCameraFollow2D>());
            modeSerialized.FindProperty("combatCameraPosition").vector3Value = new Vector3(0f, 0f, -10f);
            modeSerialized.FindProperty("combatOrthographicSize").floatValue = 1.25f;
            modeSerialized.FindProperty("storyCameraPosition").vector3Value = new Vector3(0f, -.04f, -10f);
            modeSerialized.FindProperty("storyOrthographicSize").floatValue = 1.05f;
            modeSerialized.FindProperty("enableDebugHotkeys").boolValue = true;
            modeSerialized.ApplyModifiedPropertiesWithoutUndo();

            FullscreenTransitionController fullscreen = world.AddComponent<FullscreenTransitionController>();
            SerializedObject transitionSerialized = new SerializedObject(fullscreen);
            SetObject(transitionSerialized, "worldCamera", camera);
            SetObject(transitionSerialized, "rendererFeature", ResolveFullscreenFeature());
            transitionSerialized.ApplyModifiedPropertiesWithoutUndo();

            storyRoot.gameObject.SetActive(true);
            viewport.SetActive(true);
            combatRoot.gameObject.SetActive(false);
            combatSystems.SetActive(false);

            GameObject story = FindRoot(scene, "STORY") ?? new GameObject("STORY");
            StoryDirector director = story.GetComponent<StoryDirector>() ?? story.AddComponent<StoryDirector>();
            ClearChildren(story.transform);
            GameObject eventObject = new GameObject("D1_HOME_NIGHT_MESSAGE", typeof(StoryEvent));
            eventObject.transform.SetParent(story.transform, false);
            StoryEvent storyEvent = eventObject.GetComponent<StoryEvent>();
            SerializedObject eventSerialized = new SerializedObject(storyEvent);
            eventSerialized.FindProperty("eventId").stringValue = "D1_HOME_NIGHT_MESSAGE";
            eventSerialized.FindProperty("autoPlayNextEvent").boolValue = false;
            SetObject(eventSerialized, "nextEvent", null);
            eventSerialized.ApplyModifiedPropertiesWithoutUndo();

            ConfigureSetActive(CreateStep<SetActiveStep>(storyEvent, "00_NormalizeMessageAlert"), null, new[] { messageIndicator });
            ConfigureFade(CreateStep<CanvasFadeStep>(storyEvent, "10_FadeIn"), fade, 0f, .65f);
            ConfigureDialogue(CreateStep<DialogueStep>(storyEvent, "20_AudereAfterLongDay"), dialogue.Opening);
            ConfigureAudio(CreateStep<PlayAudioStep>(storyEvent, "30_PlayMessageArrival"), AudioId.Message_Arrive);
            ConfigureWait(CreateStep<WaitStep>(storyEvent, "35_HoldForMessage"), .08f);
            ConfigureSetActive(CreateStep<SetActiveStep>(storyEvent, "40_ShowMessageAlert"), new[] { messageIndicator }, null);
            ConfigureWait(CreateStep<WaitStep>(storyEvent, "45_HoldMessageAlert"), .14f);
            ConfigureStartle(CreateStep<CharacterMotionStep>(storyEvent, "50_AudereStartles"), audere.transform, nightPose, audereRenderer, shadow);
            ConfigureWait(CreateStep<WaitStep>(storyEvent, "55_HoldAfterStartle"), .12f);
            ConfigureDialogue(CreateStep<DialogueStep>(storyEvent, "60_AudereRecognizesBianca"), dialogue.Recognition);
            ConfigureSetActive(CreateStep<SetActiveStep>(storyEvent, "65_HideMessageAlert"), null, new[] { messageIndicator });
            ConfigureDialogue(CreateStep<DialogueStep>(storyEvent, "70_BiancaNightMessage"), dialogue.Message);
            ConfigureDialogue(CreateStep<DialogueStep>(storyEvent, "80_TimorQuestionsHer"), dialogue.Question);
            ConfigureWait(CreateStep<WaitStep>(storyEvent, "90_KeepSilence"), .65f);
            ConfigureDialogue(CreateStep<DialogueStep>(storyEvent, "100_AudereAndTimorConclude"), dialogue.Conclusion);
            ConfigureWait(CreateStep<WaitStep>(storyEvent, "110_HoldBeforePressure"), .25f);
            ConfigureFullscreen(CreateStep<FullscreenWorldModeTransitionStep>(storyEvent, "120_EnterNightPressure"), fullscreen, mode, audereRenderer);
            ConfigureCombat(CreateStep<CombatStep>(storyEvent, "130_PlayTimorNightPressure"), controller, encounter);
            ConfigureWorldMode(CreateStep<WorldModeStep>(storyEvent, "140_ReturnToEvening"), mode);
            ConfigureWait(CreateStep<WaitStep>(storyEvent, "145_HoldAfterReturn"), .45f);
            ConfigureDialogue(CreateStep<DialogueStep>(storyEvent, "150_TimorNarrowsTheReply"), dialogue.BeforeChoice);

            StoryChoiceBranchStep choice = CreateStep<StoryChoiceBranchStep>(storyEvent, "160_ChooseBiancaReply");
            StoryEvent avoidBranch = CreateBranch(choice.transform, "00_Avoid", "D1_HOME_NIGHT_REPLY_AVOID");
            ConfigureDialogue(CreateStep<DialogueStep>(avoidBranch, "00_TimorAcceptsAvoidance"), dialogue.AvoidTimor);
            ConfigureMessageStatus(CreateStep<StoryMessageStatusStep>(avoidBranch, "10_AudereLooksAtSent"), nightUi.StatusGroup, nightUi.StatusText);
            ConfigureDialogue(CreateStep<DialogueStep>(avoidBranch, "20_AudereAccepts"), dialogue.AvoidAudere);

            StoryEvent delayBranch = CreateBranch(choice.transform, "10_Delay", "D1_HOME_NIGHT_REPLY_DELAY");
            ConfigureMessageStatus(CreateStep<StoryMessageStatusStep>(delayBranch, "00_SendDelayReply"), nightUi.StatusGroup, nightUi.StatusText);
            ConfigureDialogue(CreateStep<DialogueStep>(delayBranch, "10_TimorAcceptsDelay"), dialogue.Delay);

            StoryEvent silenceBranch = CreateBranch(choice.transform, "20_NoReply", "D1_HOME_NIGHT_REPLY_SILENCE");
            ConfigureWait(CreateStep<WaitStep>(silenceBranch, "00_AudereKeepsLooking"), 1.15f);
            ConfigureDialogue(CreateStep<DialogueStep>(silenceBranch, "10_TimorAcceptsSilence"), dialogue.Silence);

            ConfigureChoice(
                choice,
                nightUi.ChoiceView,
                new[]
                {
                    "Tớ xin lỗi, nhưng mai tớ có việc bận.",
                    "Tớ chưa biết nữa.",
                    "…",
                },
                new[] { avoidBranch, delayBranch, silenceBranch });
            ConfigureWait(CreateStep<WaitStep>(storyEvent, "170_HoldAfterReply"), .45f);
            ConfigureFade(CreateStep<CanvasFadeStep>(storyEvent, "180_LightsOut"), fade, 1f, .8f);
            ConfigureTitleCard(CreateStep<StoryTitleCardStep>(storyEvent, "190_DayOneEnds"), nightUi.TitleGroup, nightUi.TitleText);

            SerializedObject directorSerialized = new SerializedObject(director);
            SetObject(directorSerialized, "storyEventsRoot", story.transform);
            directorSerialized.FindProperty("playOnStart").boolValue = true;
            SetObject(directorSerialized, "startingEvent", storyEvent);
            directorSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static FullScreenPassRendererFeature ResolveFullscreenFeature()
        {
            Renderer2DData data = LoadRequired<Renderer2DData>(Renderer2DPath);
            FullScreenPassRendererFeature feature = data.rendererFeatures
                .OfType<FullScreenPassRendererFeature>()
                .FirstOrDefault(f => f.name == "Audere Fullscreen Combat Transition")
                ?? data.rendererFeatures.OfType<FullScreenPassRendererFeature>().FirstOrDefault();
            if (feature == null)
                throw new MissingReferenceException("Renderer2D has no shared fullscreen transition feature.");
            return feature;
        }

        private static NightMessageUi CreateNightMessageUi(Audere.GameplayInput.GameplayInputGate inputGate)
        {
            TMP_FontAsset font = LoadRequired<TMP_FontAsset>(ChoiceFontPath);
            GameObject root = new GameObject(
                "NIGHT MESSAGE UI",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 1300;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            // The reply choices live inside PuzzleViewportMask/Mask Bottom. Scaling by height
            // keeps that vertical relationship stable at 4:3, 16:9 and ultrawide widths.
            scaler.matchWidthOrHeight = 1f;

            GameObject choiceObject = new GameObject(
                "Reply Choices",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(StoryChoiceView));
            RectTransform choiceRect = choiceObject.GetComponent<RectTransform>();
            choiceRect.SetParent(root.transform, false);
            choiceRect.anchorMin = new Vector2(.5f, 0f);
            choiceRect.anchorMax = new Vector2(.5f, 0f);
            choiceRect.pivot = new Vector2(.5f, 0f);
            choiceRect.sizeDelta = new Vector2(1320f, 210f);
            choiceRect.anchoredPosition = new Vector2(0f, 12f);
            CanvasGroup choiceGroup = choiceObject.GetComponent<CanvasGroup>();
            choiceGroup.alpha = 0f;
            choiceGroup.interactable = false;
            choiceGroup.blocksRaycasts = false;

            StoryChoiceOptionView[] options = new StoryChoiceOptionView[3];
            for (int index = 0; index < options.Length; index++)
            {
                GameObject optionObject = new GameObject(
                    $"Option {index + 1}",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image),
                    typeof(Button),
                    typeof(StoryChoiceOptionView));
                RectTransform optionRect = optionObject.GetComponent<RectTransform>();
                optionRect.SetParent(choiceRect, false);
                optionRect.anchorMin = new Vector2(.5f, 1f);
                optionRect.anchorMax = new Vector2(.5f, 1f);
                optionRect.pivot = new Vector2(.5f, 1f);
                optionRect.sizeDelta = new Vector2(1260f, 62f);
                optionRect.anchoredPosition = new Vector2(0f, -index * 70f);
                Image hitArea = optionObject.GetComponent<Image>();
                hitArea.color = new Color(0f, 0f, 0f, 0f);
                hitArea.raycastTarget = true;
                Button button = optionObject.GetComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.targetGraphic = hitArea;

                TMP_Text label = CreateUiText(optionRect, "Text", font, 36f, TextAlignmentOptions.Center);
                label.textWrappingMode = TextWrappingModes.NoWrap;
                label.overflowMode = TextOverflowModes.Overflow;
                label.raycastTarget = false;

                options[index] = optionObject.GetComponent<StoryChoiceOptionView>();
                SerializedObject optionSerialized = new SerializedObject(options[index]);
                SetObject(optionSerialized, "button", button);
                SetObject(optionSerialized, "label", label);
                optionSerialized.FindProperty("idleColor").colorValue = new Color(.82f, .78f, .88f, .58f);
                optionSerialized.FindProperty("focusedColor").colorValue = Color.white;
                optionSerialized.FindProperty("idleScale").floatValue = .92f;
                optionSerialized.ApplyModifiedPropertiesWithoutUndo();
            }

            StoryChoiceView choiceView = choiceObject.GetComponent<StoryChoiceView>();
            SerializedObject choiceSerialized = new SerializedObject(choiceView);
            SetObject(choiceSerialized, "canvasGroup", choiceGroup);
            SetObject(choiceSerialized, "inputGate", inputGate);
            SerializedProperty optionReferences = choiceSerialized.FindProperty("options");
            optionReferences.arraySize = options.Length;
            for (int index = 0; index < options.Length; index++)
                optionReferences.GetArrayElementAtIndex(index).objectReferenceValue = options[index];
            choiceSerialized.ApplyModifiedPropertiesWithoutUndo();

            GameObject statusObject = new GameObject(
                "Message Status",
                typeof(RectTransform),
                typeof(CanvasGroup));
            RectTransform statusRect = statusObject.GetComponent<RectTransform>();
            statusRect.SetParent(root.transform, false);
            // Match the center of the visible part of Main Camera/PuzzleViewportMask/Mask Bottom.
            // Its lower bound extends off-screen, so centering the full sprite would place this too low.
            // The canvas scales by height, keeping this normalized Y stable across aspect ratios.
            const float maskBottomCenterAnchorY = .107795f;
            statusRect.anchorMin = new Vector2(.5f, maskBottomCenterAnchorY);
            statusRect.anchorMax = new Vector2(.5f, maskBottomCenterAnchorY);
            statusRect.pivot = new Vector2(.5f, .5f);
            statusRect.sizeDelta = new Vector2(560f, 88f);
            statusRect.anchoredPosition = Vector2.zero;
            CanvasGroup statusGroup = statusObject.GetComponent<CanvasGroup>();
            statusGroup.alpha = 0f;
            statusGroup.interactable = false;
            statusGroup.blocksRaycasts = false;
            TMP_Text statusText = CreateUiText(statusRect, "Status", font, 38f, TextAlignmentOptions.Center);
            statusText.text = "Đã gửi";
            statusText.color = new Color(.92f, .88f, .96f, 1f);

            GameObject titleObject = new GameObject(
                "Day End Title",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup));
            RectTransform titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.SetParent(root.transform, false);
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;
            titleObject.GetComponent<Image>().color = Color.black;
            CanvasGroup titleGroup = titleObject.GetComponent<CanvasGroup>();
            titleGroup.alpha = 0f;
            titleGroup.interactable = false;
            titleGroup.blocksRaycasts = false;
            TMP_Text titleText = CreateUiText(titleRect, "Title", font, 62f, TextAlignmentOptions.Center);
            titleText.text = "Ngày 1 - Kết thúc";
            titleText.color = Color.white;

            return new NightMessageUi(choiceView, statusGroup, statusText, titleGroup, titleText);
        }

        private static TMP_Text CreateUiText(
            RectTransform parent,
            string name,
            TMP_FontAsset font,
            float size,
            TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static void ConfigureMessageAudio()
        {
            AudioCatalog catalog = LoadRequired<AudioCatalog>(AudioCatalogPath);
            AudioClip clip = LoadRequired<AudioClip>(MessageClipPath);
            SerializedObject serialized = new SerializedObject(catalog);
            SerializedProperty entries = serialized.FindProperty("entries");
            SerializedProperty target = null;
            for (int i = entries.arraySize - 1; i >= 0; i--)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("id").intValue == (int)AudioId.Message_Arrive)
                    target = entry;
                else if (entry.FindPropertyRelative("clip").objectReferenceValue == clip)
                    entries.DeleteArrayElementAtIndex(i);
            }
            if (target == null)
            {
                entries.arraySize++;
                target = entries.GetArrayElementAtIndex(entries.arraySize - 1);
            }
            target.FindPropertyRelative("id").intValue = (int)AudioId.Message_Arrive;
            target.FindPropertyRelative("clip").objectReferenceValue = clip;
            target.FindPropertyRelative("volume").floatValue = .78f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
        }

        private static void EnsureCombatBoardProjectileMask()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(BoardPrefabPath);
            try
            {
                CombatBoardView boardView = root.GetComponent<CombatBoardView>();
                RectTransform playArea = FindDescendant(root.transform, "Dice Field") as RectTransform;
                if (boardView == null || playArea == null)
                    throw new MissingReferenceException("CombatBoard requires CombatBoardView and Dice Field.");

                Transform existingMask = FindDirectChild(playArea, "Projectile Mask");
                GameObject maskObject = existingMask != null
                    ? existingMask.gameObject
                    : new GameObject("Projectile Mask", typeof(RectTransform), typeof(RectMask2D));
                RectTransform maskRect = maskObject.GetComponent<RectTransform>();
                maskRect.SetParent(playArea, false);
                maskRect.anchorMin = Vector2.zero;
                maskRect.anchorMax = Vector2.one;
                maskRect.offsetMin = new Vector2(14f, 14f);
                maskRect.offsetMax = new Vector2(-14f, -14f);
                maskRect.SetSiblingIndex(Mathf.Min(1, playArea.childCount - 1));

                RectTransform bulletRoot = FindDescendant(root.transform, "Bullet Root") as RectTransform;
                if (bulletRoot == null)
                    throw new MissingReferenceException("CombatBoard requires Bullet Root.");
                ConfigureStretchRoot(bulletRoot, maskRect);

                Transform existingLaserRoot = FindDirectChild(maskRect, "Laser Root");
                RectTransform laserRoot = existingLaserRoot as RectTransform;
                if (laserRoot == null)
                {
                    laserRoot = new GameObject("Laser Root", typeof(RectTransform)).GetComponent<RectTransform>();
                    laserRoot.SetParent(maskRect, false);
                }
                ConfigureStretchRoot(laserRoot, maskRect);
                laserRoot.SetAsLastSibling();

                SerializedObject serialized = new SerializedObject(boardView);
                SetObject(serialized, "bulletRoot", bulletRoot);
                SetObject(serialized, "laserRoot", laserRoot);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, BoardPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureStretchRoot(RectTransform root, RectTransform parent)
        {
            root.SetParent(parent, false);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.pivot = new Vector2(.5f, .5f);
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            root.localScale = Vector3.one;
            root.localRotation = Quaternion.identity;
        }

        private static Sprite LoadFirstSprite(string path, string spriteName = null)
        {
            Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
                .FirstOrDefault(candidate => spriteName == null || candidate.name == spriteName);
            if (sprite == null)
                throw new MissingReferenceException($"Sprite '{spriteName ?? "(first)"}' was not found at '{path}'.");
            return sprite;
        }

        private static void ConfigureFade(CanvasFadeStep step, CanvasGroup fade, float alpha, float duration)
        {
            SerializedObject serialized = new SerializedObject(step);
            SetObject(serialized, "canvasGroup", fade);
            serialized.FindProperty("targetAlpha").floatValue = alpha;
            serialized.FindProperty("duration").floatValue = duration;
            serialized.FindProperty("useUnscaledTime").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureSetActive(
            SetActiveStep step,
            GameObject[] enable,
            GameObject[] disable)
        {
            SerializedObject serialized = new SerializedObject(step);
            SetObjectArray(serialized.FindProperty("objectsToEnable"), enable);
            SetObjectArray(serialized.FindProperty("objectsToDisable"), disable);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureDialogue(DialogueStep step, DialogueData dialogue)
        {
            SerializedObject serialized = new SerializedObject(step);
            SetObject(serialized, "dialogueData", dialogue);
            SetObject(serialized, "dialogueController", null);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureAudio(PlayAudioStep step, AudioId id)
        {
            SerializedObject serialized = new SerializedObject(step);
            serialized.FindProperty("audioId").intValue = (int)id;
            serialized.FindProperty("failIfAudioServiceMissing").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureWait(WaitStep step, float duration)
        {
            SerializedObject serialized = new SerializedObject(step);
            serialized.FindProperty("duration").floatValue = duration;
            serialized.FindProperty("useUnscaledTime").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureChoice(
            StoryChoiceBranchStep step,
            StoryChoiceView view,
            string[] options,
            StoryEvent[] branches)
        {
            SerializedObject serialized = new SerializedObject(step);
            SetObject(serialized, "choiceView", view);
            SerializedProperty optionList = serialized.FindProperty("options");
            optionList.arraySize = options.Length;
            for (int index = 0; index < options.Length; index++)
                optionList.GetArrayElementAtIndex(index).stringValue = options[index];
            SerializedProperty branchList = serialized.FindProperty("branches");
            branchList.arraySize = branches.Length;
            for (int index = 0; index < branches.Length; index++)
                branchList.GetArrayElementAtIndex(index).objectReferenceValue = branches[index];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureMessageStatus(
            StoryMessageStatusStep step,
            CanvasGroup group,
            TMP_Text text)
        {
            SerializedObject serialized = new SerializedObject(step);
            SetObject(serialized, "canvasGroup", group);
            SetObject(serialized, "statusText", text);
            serialized.FindProperty("message").stringValue = "Đã gửi";
            serialized.FindProperty("fadeDuration").floatValue = .18f;
            serialized.FindProperty("holdDuration").floatValue = .72f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureTitleCard(
            StoryTitleCardStep step,
            CanvasGroup group,
            TMP_Text text)
        {
            SerializedObject serialized = new SerializedObject(step);
            SetObject(serialized, "overlay", group);
            SetObject(serialized, "titleText", text);
            serialized.FindProperty("title").stringValue = "Ngày 1 - Kết thúc";
            serialized.FindProperty("fadeDuration").floatValue = .55f;
            serialized.FindProperty("holdDuration").floatValue = 2f;
            serialized.FindProperty("leaveVisible").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureStartle(CharacterMotionStep step, Transform actor, Transform target, SpriteRenderer renderer, Transform shadow)
        {
            SerializedObject serialized = new SerializedObject(step);
            SetObject(serialized, "actor", actor);
            SetObject(serialized, "targetTransform", target);
            SetObject(serialized, "actorRenderer", renderer);
            SetObject(serialized, "groundedShadow", shadow);
            serialized.FindProperty("motionMode").intValue = (int)CharacterMotionMode.VerticalInPlace;
            serialized.FindProperty("duration").floatValue = .19f;
            serialized.FindProperty("arcHeight").floatValue = .09f;
            serialized.FindProperty("travelStretch").floatValue = .045f;
            serialized.FindProperty("landingDuration").floatValue = .08f;
            serialized.FindProperty("landingSquash").floatValue = .08f;
            serialized.FindProperty("landingWiden").floatValue = .05f;
            serialized.FindProperty("useUnscaledTime").boolValue = true;
            serialized.FindProperty("facingMode").intValue = (int)CharacterFacingMode.Preserve;
            serialized.FindProperty("sourceSpriteFacesLeft").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureFullscreen(FullscreenWorldModeTransitionStep step, FullscreenTransitionController fullscreen, WorldModeController mode, Renderer focus)
        {
            SerializedObject serialized = new SerializedObject(step);
            SetObject(serialized, "transitionController", fullscreen);
            SetObject(serialized, "worldModeController", mode);
            SetObject(serialized, "transitionProfile", LoadRequired<FullscreenTransitionProfile>(TransitionProfilePath));
            SetObject(serialized, "focusRenderer", focus);
            serialized.FindProperty("sourceMode").intValue = (int)WorldGameplayMode.Story;
            serialized.FindProperty("targetMode").intValue = (int)WorldGameplayMode.Combat;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureCombat(CombatStep step, CombatController controller, CombatEncounterData encounter)
        {
            SerializedObject serialized = new SerializedObject(step);
            SetObject(serialized, "combatController", controller);
            SetObject(serialized, "combatEncounterData", encounter);
            serialized.FindProperty("victoryBehaviour").intValue = (int)CombatResultBehaviour.Fail;
            serialized.FindProperty("defeatBehaviour").intValue = (int)CombatResultBehaviour.Complete;
            serialized.FindProperty("specialBehaviour").intValue = (int)CombatResultBehaviour.Fail;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureWorldMode(WorldModeStep step, WorldModeController mode)
        {
            SerializedObject serialized = new SerializedObject(step);
            SetObject(serialized, "worldModeController", mode);
            serialized.FindProperty("targetMode").intValue = (int)WorldGameplayMode.Story;
            serialized.FindProperty("waitUntilTransitionFinished").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static T CreateStep<T>(StoryEvent storyEvent, string name) where T : StoryStep
        {
            GameObject child = new GameObject(name, typeof(T));
            child.transform.SetParent(storyEvent.transform, false);
            return child.GetComponent<T>();
        }

        private static StoryEvent CreateBranch(Transform parent, string name, string eventId)
        {
            GameObject branchObject = new GameObject(name, typeof(StoryEvent));
            branchObject.transform.SetParent(parent, false);
            StoryEvent branch = branchObject.GetComponent<StoryEvent>();
            SerializedObject serialized = new SerializedObject(branch);
            serialized.FindProperty("eventId").stringValue = eventId;
            serialized.FindProperty("autoPlayNextEvent").boolValue = false;
            SetObject(serialized, "nextEvent", null);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return branch;
        }

        private static DialogueLine L(string text) => new DialogueLine(DialogueSpeakerSide.Left, text);
        private static DialogueLine R(string text) => new DialogueLine(DialogueSpeakerSide.Right, text);

        private static T EnsureAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;
            asset = ScriptableObject.CreateInstance<T>();
            asset.name = System.IO.Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static T LoadRequired<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                throw new MissingReferenceException($"Missing required asset '{path}'.");
            return asset;
        }

        private static GameObject CreateMessageIndicator()
        {
            Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(MessageIndicatorSpritePath)
                .OfType<Sprite>()
                .FirstOrDefault();
            if (sprite == null)
                throw new MissingReferenceException(
                    $"Message indicator sprite is missing at '{MessageIndicatorSpritePath}'.");

            GameObject indicator = new GameObject("dauchamthan", typeof(SpriteRenderer));
            SpriteRenderer renderer = indicator.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.red;
            return indicator;
        }

        private static GameObject EnsurePrefabChild(Transform parent, string name, GameObject prefab)
        {
            Transform existing = FindDirectChild(parent, name);
            if (existing != null)
            {
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(existing.gameObject);
                if (source == prefab)
                    return existing.gameObject;
                UnityEngine.Object.DestroyImmediate(existing.gameObject);
            }
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
            if (instance == null)
                instance = UnityEngine.Object.Instantiate(prefab, parent);
            instance.name = name;
            return instance;
        }

        private static GameObject EnsureRootPrefab(Scene scene, string name, GameObject prefab)
        {
            GameObject existing = FindRoot(scene, name);
            if (existing != null)
            {
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(existing);
                if (source == prefab)
                    return existing;
                UnityEngine.Object.DestroyImmediate(existing);
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
            if (instance == null)
                throw new InvalidOperationException($"Could not instantiate root prefab '{prefab.name}'.");
            instance.name = name;
            return instance;
        }

        private static Transform EnsureChild(Transform parent, string name)
        {
            Transform existing = FindDirectChild(parent, name);
            if (existing != null)
                return existing;
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static Transform FindDirectChild(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
                if (parent.GetChild(i).name == name) return parent.GetChild(i);
            return null;
        }

        private static Transform FindDescendant(Transform parent, string name)
        {
            if (parent == null)
                return null;
            for (int index = 0; index < parent.childCount; index++)
            {
                Transform child = parent.GetChild(index);
                if (child.name == name)
                    return child;
                Transform nested = FindDescendant(child, name);
                if (nested != null)
                    return nested;
            }
            return null;
        }

        private static GameObject FindRoot(Scene scene, string name) =>
            scene.GetRootGameObjects().FirstOrDefault(root => root.name == name);

        private static GameObject RequireRoot(Scene scene, string name) =>
            FindRoot(scene, name) ?? throw new MissingReferenceException($"Scene requires root '{name}'.");

        private static void DestroyRoot(Scene scene, string name)
        {
            GameObject root = FindRoot(scene, name);
            if (root != null)
                UnityEngine.Object.DestroyImmediate(root);
        }

        private static void ClearChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(root.GetChild(i).gameObject);
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static void SetObject(SerializedObject serialized, string field, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(field)
                ?? throw new MissingFieldException(serialized.targetObject.GetType().Name, field);
            property.objectReferenceValue = value;
        }

        private static void SetObject(SerializedProperty parent, string field, UnityEngine.Object value)
        {
            SerializedProperty property = parent.FindPropertyRelative(field)
                ?? throw new MissingFieldException(field);
            property.objectReferenceValue = value;
        }

        private static void SetObjectArray(SerializedProperty property, UnityEngine.Object[] values)
        {
            int length = values != null ? values.Length : 0;
            property.arraySize = length;
            for (int index = 0; index < length; index++)
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
        }

        private static T GetSerializedReference<T>(this Component component, string field) where T : UnityEngine.Object
        {
            return new SerializedObject(component).FindProperty(field)?.objectReferenceValue as T;
        }

        private readonly struct DialogueLine
        {
            public DialogueLine(DialogueSpeakerSide side, string text) { Side = side; Text = text; }
            public DialogueSpeakerSide Side { get; }
            public string Text { get; }
        }

        private readonly struct DialogueAssets
        {
            public DialogueAssets(
                DialogueData opening,
                DialogueData recognition,
                DialogueData message,
                DialogueData question,
                DialogueData conclusion,
                DialogueData[] barks,
                DialogueData defeat,
                DialogueData beforeChoice,
                DialogueData avoidTimor,
                DialogueData avoidAudere,
                DialogueData delay,
                DialogueData silence)
            {
                Opening = opening;
                Recognition = recognition;
                Message = message;
                Question = question;
                Conclusion = conclusion;
                Barks = barks;
                Defeat = defeat;
                BeforeChoice = beforeChoice;
                AvoidTimor = avoidTimor;
                AvoidAudere = avoidAudere;
                Delay = delay;
                Silence = silence;
            }
            public DialogueData Opening { get; }
            public DialogueData Recognition { get; }
            public DialogueData Message { get; }
            public DialogueData Question { get; }
            public DialogueData Conclusion { get; }
            public DialogueData[] Barks { get; }
            public DialogueData Defeat { get; }
            public DialogueData BeforeChoice { get; }
            public DialogueData AvoidTimor { get; }
            public DialogueData AvoidAudere { get; }
            public DialogueData Delay { get; }
            public DialogueData Silence { get; }
        }

        private readonly struct NightMessageUi
        {
            public NightMessageUi(
                StoryChoiceView choiceView,
                CanvasGroup statusGroup,
                TMP_Text statusText,
                CanvasGroup titleGroup,
                TMP_Text titleText)
            {
                ChoiceView = choiceView;
                StatusGroup = statusGroup;
                StatusText = statusText;
                TitleGroup = titleGroup;
                TitleText = titleText;
            }

            public StoryChoiceView ChoiceView { get; }
            public CanvasGroup StatusGroup { get; }
            public TMP_Text StatusText { get; }
            public CanvasGroup TitleGroup { get; }
            public TMP_Text TitleText { get; }
        }
    }
}
