#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Audere.Combat;
using Audere.Dialogue;

using Audere.UI;
using Audere.Story.Steps;
using Audere.World;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Audere.Story.Editor
{
    public static class Day4TimorFinalSetupTool
    {
        public const string MoveFolder = Day4TimorEveningSetupTool.Folder + "/FinalMoves";
        public const string FinalDialogueFolder = "Assets/_Audere/Data/Dialogue/Day4/TimorFinal";
        public const string TailThrowPath = MoveFolder + "/Move_TimorTailThrow.asset";
        public const string TailHoldPath = MoveFolder + "/Move_TimorTailHold.asset";
        public const string BiancaProjectionPath = MoveFolder + "/Move_Projection_Bianca.asset";
        public const string TeacherProjectionPath = MoveFolder + "/Move_Projection_Teacher.asset";
        public const string CrowdProjectionPath = MoveFolder + "/Move_Projection_Crowd.asset";

        [MenuItem("Audere/Story/Author Active Day4 Timor Final Boss And Ending")]
        public static void AuthorActive()
        {
            Day4TimorEveningSetupTool.AuthorActive();
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != Day4TimorEveningSetupTool.ScenePath || scene.isDirty ||
                EditorApplication.isPlaying || EditorApplication.isCompiling)
                throw new InvalidOperationException("Open saved Scene150 in Edit Mode.");

            EnsureFolder(MoveFolder);
            EnsureFolder(FinalDialogueFolder);
            CombatEncounterData encounter = AuthorEncounter();
            CombatStep combatStep = All<CombatStep>(scene).Single();
            CombatEnemyActor actor = combatStep.EnemyActorOverride;

            ConfigureCombatLayout(scene, combatStep);
            ConfigureEnemyName(scene);
foreach (Image image in actor.Graphics.OfType<Image>())
                image.sprite = Sprite("Enemyy/timor.png");
            Set(combatStep, "combatEncounterData", encounter, "victoryBehaviour", 0, "defeatBehaviour", 2);

            StoryEvent story = All<StoryEvent>(scene).Single();
            RemoveTailAfter(story.transform, "160_TimorAgain");
            AuthorEnding(scene, story.transform, combatStep);
            foreach (DialogueStep dialogue in All<DialogueStep>(scene))
                Set(dialogue, "dialogueController", All<DialogueController>(scene).Single());

            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
        }

        private static CombatEncounterData AuthorEncounter()
        {
            Sprite tail = Sprite("Enemyy/đuôi.png");
            Sprite normal = Sprite("Enemyy/timor.png");
            Sprite noTail = Sprite("Enemyy/timor no tail.png");
            TimorTailThrowMove tailThrow = New<TimorTailThrowMove>(TailThrowPath);
            Set(tailThrow, "duration", 3.15f, "leadInDuration", .16f, "tailSprite", tail,
                "normalTimorSprite", normal, "noTailTimorSprite", noTail,
                "warningDuration", .72f, "lungeDuration", .36f, "holdDuration", .18f,
                "throwDuration", .52f, "stunDuration", 1f, "catchRadius", 62f,
                "visualHeight", 230f);
            Safe(tailThrow, 2,
                new Vector2(.24f, .28f), new Vector2(.5f, .5f), new Vector2(.76f, .72f));
            Save(tailThrow, TailThrowPath);

            TimorTailThrowMove tailHold = New<TimorTailThrowMove>(TailHoldPath);
            Set(tailHold, "duration", 3.75f, "leadInDuration", .12f, "tailSprite", tail,
                "normalTimorSprite", normal, "noTailTimorSprite", noTail,
                "warningDuration", .62f, "lungeDuration", .31f, "holdDuration", .88f,
                "throwDuration", .46f, "stunDuration", 1f, "catchRadius", 74f,
                "visualHeight", 250f);
            Safe(tailHold, 3,
                new Vector2(.2f, .5f), new Vector2(.5f, .28f), new Vector2(.8f, .5f));
            Save(tailHold, TailHoldPath);

            ReturningOrbitMove orbit = New<ReturningOrbitMove>(MoveFolder + "/Move_BiancaMemoryBoomerang.asset");
            EditorUtility.CopySerialized(AssetDatabase.LoadAssetAtPath<ReturningOrbitMove>(
                "Assets/_Audere/Data/Combat/BiancaSupplies/Moves/Move_Bianca_ReturningOrbit.asset"), orbit);
            Set(orbit, "duration", 5.8f, "leadInDuration", .08f, "maximumSimultaneous", 3,
                "initialFlightDuration", 1.28f, "finalFlightDuration", .82f,
                "telegraphDuration", .34f, "betweenWaves", .1f, "horizontalTraversal", true);
            Save(orbit, MoveFolder + "/Move_BiancaMemoryBoomerang.asset");

            ProjectionAssaultMove bianca = Projection(BiancaProjectionPath,
                Sprite("Enemyy/biancaenemy.png"), orbit, 5.8f, new Vector2(520f, 728f),
                new Color(.52f, .42f, 1f, .76f));
            ProjectionAssaultMove teacher = Projection(TeacherProjectionPath,
                Sprite("Enemyy/co giao enemy.png"),
                AssetDatabase.LoadAssetAtPath<CombatMoveDefinition>(
                    "Assets/_Audere/Data/Combat/Teacher/Moves/Move_TeacherRadialInwardTrails.asset"),
                7.1f, new Vector2(568f, 760f), new Color(.62f, .5f, 1f, .72f));
            ProjectionAssaultMove crowd = Projection(CrowdProjectionPath,
                Sprite("Enemyy/dam dong enemy.png"),
                AssetDatabase.LoadAssetAtPath<CombatMoveDefinition>(
                    "Assets/_Audere/Data/Combat/Crowd/Move_HandWaves.asset"),
                6.4f, new Vector2(680f, 600f), new Color(.45f, .35f, .9f, .76f));
            Safe(bianca, 3, new Vector2(.18f, .3f), new Vector2(.5f, .72f), new Vector2(.82f, .3f));
            Safe(teacher, 3, new Vector2(.18f, .72f), new Vector2(.5f, .28f), new Vector2(.82f, .72f));
            Safe(crowd, 3, new Vector2(.2f, .5f), new Vector2(.5f, .26f), new Vector2(.8f, .5f));

            NarrativePressurePatternMove p1Corridor = FinalPattern(1, "P1_Corridor", 1);
            NarrativePressurePatternMove p1Laser = FinalPattern(2, "P1_LaserColumns", 1);
            NarrativePressurePatternMove p1Rain = FinalPattern(3, "P1_SafeRain", 1);
            NarrativePressurePatternMove p1Gap = FinalPattern(4, "P1_MovingGap", 1);
            NarrativePressurePatternMove p1Fans = FinalPattern(5, "P1_SequentialFans", 1);
            NarrativePressurePatternMove p1Split = FinalPattern(6, "P1_SplitBurst", 1);
            Safe(p1Corridor, 1, new Vector2(.22f, .28f), new Vector2(.5f, .5f), new Vector2(.78f, .72f));
            Safe(p1Laser, 1, new Vector2(.24f, .5f), new Vector2(.5f, .5f), new Vector2(.76f, .5f));
            Safe(p1Rain, 1, new Vector2(.25f, .3f), new Vector2(.5f, .7f), new Vector2(.75f, .3f));
            Safe(p1Gap, 1, new Vector2(.22f, .68f), new Vector2(.5f, .32f), new Vector2(.78f, .68f));
            Safe(p1Fans, 1, new Vector2(.22f, .5f), new Vector2(.5f, .5f), new Vector2(.78f, .5f));
            Safe(p1Split, 1, new Vector2(.25f, .28f), new Vector2(.5f, .72f), new Vector2(.75f, .28f));

            NarrativePressurePatternMove p2Gap = FinalPattern(4, "P2_MovingGap", 2);
            NarrativePressurePatternMove p2Split = FinalPattern(6, "P2_SplitBurst", 2);
            NarrativePressurePatternMove p2Orbit = FinalPattern(7, "P2_OrbitRing", 2);
            NarrativePressurePatternMove p2Sweep = FinalPattern(8, "P2_SweepingLaser", 2);
            Safe(p2Gap, 2, new Vector2(.2f, .68f), new Vector2(.5f, .32f), new Vector2(.8f, .68f));
            Safe(p2Split, 2, new Vector2(.22f, .28f), new Vector2(.5f, .72f), new Vector2(.78f, .28f));
            Safe(p2Orbit, 2, new Vector2(.2f, .5f), new Vector2(.5f, .5f), new Vector2(.8f, .5f));
            Safe(p2Sweep, 2, new Vector2(.22f, .72f), new Vector2(.5f, .28f), new Vector2(.78f, .72f));

            NarrativePressurePatternMove p3Laser = FinalPattern(2, "P3_LaserColumns", 3);
            NarrativePressurePatternMove p3Gap = FinalPattern(4, "P3_MovingGap", 3);
            NarrativePressurePatternMove p3Fans = FinalPattern(5, "P3_SequentialFans", 3);
            NarrativePressurePatternMove p3Blades = FinalPattern(9, "P3_RotatingBlades", 3);
            NarrativePressurePatternMove p3Pendulum = FinalPattern(10, "P3_PendulumLaser", 3);
            Safe(p3Laser, 3, new Vector2(.18f, .5f), new Vector2(.5f, .5f), new Vector2(.82f, .5f));
            Safe(p3Gap, 3, new Vector2(.18f, .7f), new Vector2(.5f, .3f), new Vector2(.82f, .7f));
            Safe(p3Fans, 3, new Vector2(.18f, .28f), new Vector2(.5f, .72f), new Vector2(.82f, .28f));
            Safe(p3Blades, 3, new Vector2(.2f, .5f), new Vector2(.5f, .26f), new Vector2(.8f, .5f));
            Safe(p3Pendulum, 3, new Vector2(.18f, .72f), new Vector2(.5f, .28f), new Vector2(.82f, .72f));

            CompositeCombatMove crossRain = Composite(
                MoveFolder + "/Move_TimorFinal_CrossRain.asset", 5.1f, p1Corridor, p1Rain);
            ShiftingBattleBoxMove finalShift = FinalBattleBox();
            CompositeCombatMove orbitSqueeze = Composite(
                MoveFolder + "/Move_TimorFinal_OrbitSqueeze.asset", 5.05f, p2Orbit, finalShift);
            CompositeCombatMove fracturedWall = Composite(
                MoveFolder + "/Move_TimorFinal_FracturedWall.asset", 5.15f, p2Gap, p2Split);
            CompositeCombatMove bladePendulum = Composite(
                MoveFolder + "/Move_TimorFinal_BladePendulum.asset", 5.25f, p3Blades, p3Pendulum);
            CompositeCombatMove fanWall = Composite(
                MoveFolder + "/Move_TimorFinal_FanWall.asset", 5.15f, p3Fans, p3Gap);

            Safe(crossRain, 2, new Vector2(.2f, .28f), new Vector2(.5f, .72f), new Vector2(.8f, .28f));
            Safe(orbitSqueeze, 2, new Vector2(.2f, .5f), new Vector2(.5f, .28f), new Vector2(.8f, .5f));
            Safe(fracturedWall, 2, new Vector2(.2f, .72f), new Vector2(.5f, .28f), new Vector2(.8f, .72f));
            Safe(bladePendulum, 3, new Vector2(.18f, .3f), new Vector2(.5f, .72f), new Vector2(.82f, .3f));
            Safe(fanWall, 3, new Vector2(.18f, .72f), new Vector2(.5f, .28f), new Vector2(.82f, .72f));

            CombatMoveSet phase1 = MoveSet(MoveFolder + "/MoveSet_TimorFinal_P1.asset",
                p1Corridor, p1Rain, p1Fans, p1Laser, p1Split, p1Gap);
            CombatMoveSet phase2 = MoveSet(MoveFolder + "/MoveSet_TimorFinal_P2.asset",
                tailThrow, fracturedWall, p2Orbit, orbitSqueeze,
                p2Sweep, p2Split, crossRain, tailThrow);
            CombatMoveSet phase3 = MoveSet(MoveFolder + "/MoveSet_TimorFinal_P3.asset",
                tailHold, bianca, bladePendulum, teacher, fanWall, crowd,
                orbitSqueeze, tailThrow, p3Pendulum, p3Blades, p3Laser);

            DialogueData history = D("HISTORY", "Audere_Scared.png", "TimorLoLangKhongVui.png",
                "R|Tớ giúp cậu ra khỏi giường.",
                "R|Tớ nhắc cậu phải nói gì.",
                "R|Tớ ở đó khi chẳng còn ai khác.",
                "R|Không biết đi đâu, tớ luôn chỉ đường.");
            DialogueData fear = D("FEAR", "Audere_Scared.png", "TimorTucGian.png",
                "R|Một lần họ giúp…",
                "R|không có nghĩa lần sau họ cũng giúp.",
                "R|Một lần cậu nói được…",
                "R|không có nghĩa cậu sẽ không làm hỏng.",
                "R|Cậu sẽ lại nói sai.",
                "R|Cậu sẽ lại đứng im.",
                "R|Cậu sẽ lại khiến mọi người phải chờ.",
                "R|Rồi chính cậu sẽ bị tổn thương.");
            DialogueData rebellion = D("REBELLION", "Audere_Tired.png", "TimorTucGian.png",
                "R|Cậu chỉ đang ở tuổi nổi loạn thôi.",
                "R|Cô bé nào rồi cũng trải qua mà.",
                "R|Cậu chưa hiểu tấm lòng của tớ.");
            DialogueData avoid = D("AVOID", "Audere_Tired.png", "TimorLoLangKhongVui.png",
                "R|Nếu cứ tránh tất cả…",
                "R|cậu sẽ không bao giờ thất bại.",
                "L|Nhưng tớ cũng sẽ không bao giờ biết…",
                "L|điều gì có thể xảy ra.");
            DialogueData unknown = D("UNKNOWN", "Audere_Tired.png", "TimorLoLangKhongVui.png",
                "R|Cậu không thể biết trước được.",
                "L|Ừ.",
                "L|Tớ không biết họ sẽ nghĩ gì.",
                "L|Tớ không biết mình có nói sai không.");
            DialogueData tryAnyway = D("TRY_ANYWAY", "Audere_Tired.png", "TimorBuon.png",
                "L|Tớ cũng không biết ngày mai…",
                "L|có tốt hơn hôm nay không.",
                "L|Nhưng tớ không muốn vì không biết…",
                "L|mà không bao giờ thử.");
            DialogueData finalGate = D("FINAL_GATE", "Audere_smiled.png", "TimorBuon.png",
                "R|Cậu… vẫn muốn thử sao?",
                "L|Ừ. Dù tớ vẫn sợ.");

            CombatEnemyDefinition enemy = New<CombatEnemyDefinition>(
                Day4TimorEveningSetupTool.Folder + "/Enemy_TimorReturn.asset");
            CombatEnemyDefinition oldEnemy = AssetDatabase.LoadAssetAtPath<CombatEnemyDefinition>(
                "Assets/_Audere/Data/Combat/TimorNightPressure/Enemy_TimorNightPressure.asset");
            EditorUtility.CopySerialized(oldEnemy, enemy);
            Set(enemy, "enemyId", "d4-timor-final", "displayName", "TIMOR",
                "phasePolicy", (int)CombatPhasePolicy.PerPhaseHealth, "sharedMaxHealth", 33,
                "passiveHealthDecayInterval", 0f);
            SerializedObject enemySo = new SerializedObject(enemy);
            SerializedProperty phases = enemySo.FindProperty("phases");
            phases.arraySize = 3;
            Phase(phases.GetArrayElementAtIndex(0), "timor-final-protection", phase1,
                new CueSpec("timor-history", CombatDialogueCueTrigger.PhaseEnter, 0f, null,
                    new[] { history }, false, true));
            Phase(phases.GetArrayElementAtIndex(1), "timor-final-control", phase2,
                new CueSpec("timor-fear", CombatDialogueCueTrigger.PhaseEnter, 0f, null,
                    new[] { fear }, false, true));
            Phase(phases.GetArrayElementAtIndex(2), "timor-final-uncertainty", phase3,
                new CueSpec("timor-rebellion", CombatDialogueCueTrigger.PhaseEnter, 0f, null,
                    new[] { rebellion }, true, false),
                new CueSpec("memory-bianca", CombatDialogueCueTrigger.MoveStarted, 0f, bianca,
                    new[] { avoid }, true, false),
                new CueSpec("memory-teacher", CombatDialogueCueTrigger.MoveStarted, 0f, teacher,
                    new[] { unknown }, true, false),
                new CueSpec("memory-crowd", CombatDialogueCueTrigger.MoveStarted, 0f, crowd,
                    new[] { tryAnyway }, true, false),
                new CueSpec("timor-final-choice", CombatDialogueCueTrigger.HealthAtOrBelow, 2f, null,
                    new[] { finalGate }, true, false, true));
            enemySo.ApplyModifiedPropertiesWithoutUndo();
            Save(enemy, Day4TimorEveningSetupTool.Folder + "/Enemy_TimorReturn.asset");

            DialogueData victory = D("VICTORY_RECONCILIATION", "Audere_smiled.png", "TimorBuon.png",
                "L|Timor, tớ biết cậu từng giúp tớ.",
                "L|Cậu đã ở cạnh tớ rất lâu.",
                "L|Nhưng tớ không còn bé như ngày ấy.",
                "L|Hãy thử tin tớ một lần được không?",
                "L|Và thử tin người khác cũng có thể ở lại.",
                "R|Tớ sợ cậu không cần tớ nữa.",
                "L|Tớ vẫn cần cậu.",
                "L|Nhưng tớ cần được tự bước đi.",
                "R|…Được. Tớ sẽ thử.");

            CombatEncounterData encounter = New<CombatEncounterData>(Day4TimorEveningSetupTool.EncounterPath);
            Set(encounter, "encounterId", "d4-timor-final", "enemyDefinition", enemy,
                "music", 9004, "encounterDuration", 243.75f, "dicePerBatch", 3,
                "maximumAttacksPerBatch", 1, "additionalRerolledAttacksPerBatch", 1, "batchRespawnDelay", .14f,
                "minimumDiceSpeed", 166f, "maximumDiceSpeed", 252f,
                "playerHitInvulnerability", .4f, "bulletTimePenaltySeconds", 3.75f,
                "victoryFadeDuration", .8f);
            SerializedObject encounterSo = new SerializedObject(encounter);
            SerializedProperty rules = encounterSo.FindProperty("outcomeRules");
            rules.FindPropertyRelative("allowedOutcomes").intValue =
                (int)(CombatAllowedOutcome.Victory | CombatAllowedOutcome.Defeat);
            rules.FindPropertyRelative("playerDefeatGate").intValue =
                (int)CombatPlayerDefeatGate.CurrentPhaseAndRequiredCues;
            rules.FindPropertyRelative("showRetryOnDefeat").boolValue = true;
            encounterSo.FindProperty("defeatPresentation").FindPropertyRelative("dialogue").objectReferenceValue = null;
            SerializedProperty vp = encounterSo.FindProperty("victoryPresentation");
            vp.FindPropertyRelative("dialogue").objectReferenceValue = victory;
            vp.FindPropertyRelative("hazardFadeDuration").floatValue = .65f;
            encounterSo.ApplyModifiedPropertiesWithoutUndo();
            Save(encounter, Day4TimorEveningSetupTool.EncounterPath);
            return encounter;
        }

        [MenuItem("Audere/Story/Configure Active Scene150 Timor Combat Layout")]
        public static void ConfigureActiveCombatLayout()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.path != Day4TimorEveningSetupTool.ScenePath || EditorApplication.isPlaying ||
                EditorApplication.isCompiling)
                throw new InvalidOperationException("Open Scene150 in Edit Mode.");

            CombatStep combatStep = All<CombatStep>(scene).Single();
            ConfigureCombatLayout(scene, combatStep);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigureCombatLayout(Scene scene, CombatStep combatStep)
        {
            CombatBoardView board = All<CombatBoardView>(scene).Single();
            Place(RequiredRect(board, "Frame"), new Vector2(0f, -12f), new Vector2(620f, 500f));
            Place(RequiredRect(board, "Dice Field"), new Vector2(0f, 9f), new Vector2(580f, 420f));
            Place(RequiredRect(board, "Airborne Dice Overlay"), new Vector2(0f, 20f), new Vector2(580f, 420f));
            Place(RequiredRect(board, "Exterior Projectile Root"), new Vector2(0f, 20f), new Vector2(580f, 420f));
            Place(RequiredRect(board, "Timer Track"), new Vector2(0f, -236f), new Vector2(580f, 22f));

            Place(RequiredRect(board, "Enemy Mount"), new Vector2(0f, 421f), new Vector2(360f, 240f));
            Place(RequiredRect(board, "VFX"), new Vector2(0f, 421f), new Vector2(360f, 240f));
            Place(RequiredRect(board, "Name"), new Vector2(275f, 411f), new Vector2(220.2f, 94.9f));
            Place(RequiredRect(board, "Health"), new Vector2(-213f, 424f), new Vector2(100f, 100f));

            CombatEnemyActor actor = combatStep.EnemyActorOverride;
            if (actor == null)
                throw new MissingReferenceException("Scene150 CombatStep EnemyActorOverride");
            RectTransform actorRect = actor.transform as RectTransform;
            if (actorRect != null)
                actorRect.anchoredPosition = new Vector2(58f, 0f);
            else
                actor.transform.localPosition = new Vector3(58f, 0f, 0f);
            actor.transform.localScale = Vector3.one;
            EditorUtility.SetDirty(actor);
            EditorSceneManager.MarkSceneDirty(scene);
        }
        private static RectTransform RequiredRect(CombatBoardView board, string objectName)
        {
            RectTransform rect = board.GetComponentsInChildren<RectTransform>(true)
                .SingleOrDefault(x => x.name == objectName);
            if (rect == null)
                throw new MissingReferenceException("Combat Board/" + objectName);
            return rect;
        }

        private static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            EditorUtility.SetDirty(rect);
        }

private static void AuthorEnding(Scene scene, Transform parent, CombatStep combatStep)
        {
            WorldModeController mode = All<WorldModeController>(scene).Single();
            CanvasGroup cover = All<CanvasGroup>(scene).Single(x => x.name == "Fade" &&
                x.transform.parent != null && x.transform.parent.name == "Scene Transition Overlay");
            Transform stage = scene.GetRootGameObjects().Single(x => x.name == "WORLD")
                .transform.Find("Home Stage PLACEHOLDER_NO_ART");
            SpriteRenderer audere = stage.GetComponentsInChildren<SpriteRenderer>(true)
                .Single(x => x.name == "Audere");
            Transform groundedShadow = audere.GetComponentsInChildren<SpriteRenderer>(true)
                .Single(x => x != audere).transform;

            DialogueData promise = D("CONTINUE_PROMISE", "Audere_smiled.png", "TimorBuon.png",
                "L|Timor.",
                "R|…Ừ.");
            DialogueData firstStep = D("CONTINUE_STEP_01", "Audere_smiled.png", "TimorBuon.png",
                "L|Hãy cùng tớ đi tiếp nhé.");
            DialogueData secondStep = D("CONTINUE_STEP_02", "Audere_smiled.png", "TimorBuon.png",
                "L|Dù phía trước có khó đến đâu.");
            DialogueData thirdStep = D("CONTINUE_STEP_03", "Audere_smiled.png", "TimorBuon.png",
                "L|Tớ sẽ tự bước.");
            DialogueData finalStep = D("CONTINUE_STEP_04", "Audere_smiled.png", "TimorBuon.png",
                "L|Cậu có thể đi cùng.");

            Fade(parent, "170_CombatFallsAway", cover, 1f, .85f);
            Set(Step<WorldModeStep>(parent, "180_ReturnToTheQuietRoom"),
                "worldModeController", mode, "targetMode", (int)WorldGameplayMode.Story);
            List<Transform> endingTiles = AuthorEndingTiles(stage);
            Fade(parent, "190_AudereReturns", cover, 0f, 1.05f);

            CharacterMotionStep stand = Step<CharacterMotionStep>(parent, "200_AudereChoosesToStand");
            Set(stand, "actor", audere.transform, "targetTransform", audere.transform,
                "actorRenderer", audere, "groundedShadow", groundedShadow,
                "motionMode", (int)CharacterMotionMode.VerticalInPlace,
                "duration", .38f, "arcHeight", .09f, "travelStretch", .05f,
                "landingDuration", .11f, "landingSquash", .09f, "landingWiden", .065f,
                "useUnscaledTime", true, "facingMode", (int)CharacterFacingMode.Preserve);

            BoardTileTransitionStep reveal = Step<BoardTileTransitionStep>(parent, "210_ThePathOpensOutward");
            SetObjectArray(reveal, "objectsToReveal", endingTiles.Cast<Object>().ToArray());
            Set(reveal, "transitionDuration", .27f, "staggerDelay", .035f,
                "revealWaveDuration", 1.55f, "verticalOffset", .075f,
                "revealOvershoot", .016f, "useUnscaledTime", true);
            Talk(parent, "220_TimorAnswers", promise);
            Wait(parent, "225_TheOpenPathSettles", .32f);

            Transform anchorRight = CreateEndingAnchor(stage, audere.transform,
                endingTiles.Single(x => x.name == "Ending Tile 02"), "Audere_Path_01");
            Transform anchorUpperRight = CreateEndingAnchor(stage, audere.transform,
                endingTiles.Single(x => x.name == "Ending Tile 06"), "Audere_Path_02");
            Transform anchorTop = CreateEndingAnchor(stage, audere.transform,
                endingTiles.Single(x => x.name == "Ending Tile 03"), "Audere_Path_03");
            Transform anchorUpperLeft = CreateEndingAnchor(stage, audere.transform,
                endingTiles.Single(x => x.name == "Ending Tile 05"), "Audere_Path_04");

            ConfigureTravel(Step<CharacterMotionStep>(parent, "230_AudereStepsOntoThePath"),
                audere, groundedShadow, anchorRight, CharacterFacingMode.FollowHorizontalTravel);
            Talk(parent, "240_ContinueTogether", firstStep);
            ConfigureTravel(Step<CharacterMotionStep>(parent, "250_AudereKeepsWalking"),
                audere, groundedShadow, anchorUpperRight, CharacterFacingMode.FollowHorizontalTravel);
            Talk(parent, "260_WhateverComes", secondStep);
            ConfigureTravel(Step<CharacterMotionStep>(parent, "270_AudereWalksWithoutAChoiceArrow"),
                audere, groundedShadow, anchorTop, CharacterFacingMode.Preserve);
            Talk(parent, "280_HerOwnStep", thirdStep);
            ConfigureTravel(Step<CharacterMotionStep>(parent, "290_AudereCrossesTheOpenTiles"),
                audere, groundedShadow, anchorUpperLeft, CharacterFacingMode.FollowHorizontalTravel);
            Talk(parent, "300_TimorMayComeAlong", finalStep);
            Wait(parent, "310_HoldTheChoice", .9f);

            GameObject finalCanvas;
            CanvasGroup finalGroup;
            GameObject creditsCanvas;
            CanvasGroup creditsGroup;
            AuthorEndingPresentation(out finalCanvas, out finalGroup, out creditsCanvas, out creditsGroup);

            Fade(parent, "320_FadeToFinalImage", cover, 1f, 1.05f);
            Active(parent, "330_ShowFinalCutscene", new[] { finalCanvas }, new GameObject[0]);
            Fade(parent, "340_FinalImageAppears", finalGroup, 1f, 1.35f);
            Wait(parent, "350_HoldFinalImage", 4.2f);
            Fade(parent, "360_FinalImageFades", finalGroup, 0f, 1.25f);
            Active(parent, "370_OpenCredits", new[] { creditsCanvas }, new[] { finalCanvas });
            Fade(parent, "380_CreditsAppear", creditsGroup, 1f, 1.6f);
            Wait(parent, "390_ThankYou", 10.5f);
            Fade(parent, "400_EndGameFade", creditsGroup, 0f, 2.2f);
        }

private static List<Transform> AuthorEndingTiles(Transform stage)
        {
            Transform old = stage.Find("ENDING TILES");
            if (old != null) Object.DestroyImmediate(old.gameObject);
            Transform oldAnchors = stage.Find("ENDING STAGING");
            if (oldAnchors != null) Object.DestroyImmediate(oldAnchors.gameObject);

            Transform root = new GameObject("ENDING TILES").transform;
            root.SetParent(stage, false);
            Transform source = stage.Find("Night Tile PLACEHOLDER/Visual Root");
            const float worldCellSize = .25f; // Scene80 Dream spacing: .24 tile + .01 breathing room.
            float localCellX = worldCellSize / Mathf.Max(.0001f, Mathf.Abs(root.lossyScale.x));
            float localCellY = worldCellSize / Mathf.Max(.0001f, Mathf.Abs(root.lossyScale.y));
            Vector3[] positions = {
                new Vector3(-localCellX, 0f, 0f), new Vector3(localCellX, 0f, 0f),
                new Vector3(0f, localCellY, 0f), new Vector3(0f, -localCellY, 0f),
                new Vector3(-localCellX, localCellY, 0f), new Vector3(localCellX, localCellY, 0f),
                new Vector3(-localCellX, -localCellY, 0f), new Vector3(localCellX, -localCellY, 0f)
            };
            List<Transform> result = new List<Transform>();
            for (int i = 0; i < positions.Length; i++)
            {
                GameObject tile = Object.Instantiate(source.gameObject, root);
                tile.name = "Ending Tile " + (i + 1).ToString("00");
                tile.transform.localPosition = positions[i];
                tile.transform.localRotation = source.localRotation;
                tile.transform.localScale = source.localScale;
                foreach (SpriteRenderer renderer in tile.GetComponentsInChildren<SpriteRenderer>(true))
                    renderer.sortingOrder = positions[i].y > .01f ? -1 :
                        positions[i].y < -.01f ? 1 : 0;
                tile.SetActive(false);
                result.Add(tile.transform);
            }

            return result.OrderBy(x => x.localPosition.sqrMagnitude).ToList();
        }

private static Transform CreateEndingAnchor(
            Transform stage,
            Transform audere,
            Transform tile,
            string name)
        {
            Transform root = stage.Find("ENDING STAGING");
            if (root == null)
            {
                root = new GameObject("ENDING STAGING").transform;
                root.SetParent(stage, false);
                root.gameObject.SetActive(false);
            }

            Transform anchor = new GameObject(name).transform;
            anchor.SetParent(root, false);
            Vector3 tileOffset = tile.localPosition;
            anchor.localPosition = audere.localPosition +
                new Vector3(tileOffset.x, tileOffset.y, 0f);
            anchor.localRotation = audere.localRotation;
            anchor.localScale = Vector3.one;
            return anchor;
        }

        private static void ConfigureTravel(
            CharacterMotionStep step,
            SpriteRenderer audere,
            Transform groundedShadow,
            Transform target,
            CharacterFacingMode facing)
        {
            Set(step, "actor", audere.transform, "targetTransform", target,
                "actorRenderer", audere, "groundedShadow", groundedShadow,
                "motionMode", (int)CharacterMotionMode.TravelToTarget,
                "duration", .34f, "arcHeight", .068f, "travelStretch", .06f,
                "landingDuration", .105f, "landingSquash", .095f, "landingWiden", .07f,
                "useUnscaledTime", true, "facingMode", (int)facing,
                "sourceSpriteFacesLeft", true);
        }


private static void AuthorEndingPresentation(out GameObject finalCanvas, out CanvasGroup finalGroup,
            out GameObject creditsCanvas, out CanvasGroup creditsGroup)
        {
            Scene activeScene = SceneManager.GetActiveScene();
            foreach (GameObject existing in activeScene.GetRootGameObjects()
                .Where(x => x.name == "DAY FOUR FINAL PRESENTATION" ||
                    x.name == "FINAL CUTSCENE" || x.name == "CREDITS"))
                Object.DestroyImmediate(existing);

            GameObject root = new GameObject("DAY FOUR FINAL PRESENTATION");
            root.transform.localScale = Vector3.one;

            // These canvases intentionally sit above the shared scene Fade (order 1000).
            // The Fade remains the neutral cover; it must not hide the finale underneath it.
            finalCanvas = CanvasScreen("FINAL CUTSCENE", null, out finalGroup, 1300);
            Image finalBackground = ImageRect("Black", finalCanvas.transform, Color.black,
                Vector2.zero, Vector2.one);
            finalBackground.transform.SetAsFirstSibling();
            Image cutscene = ImageRect("final_cutscene", finalCanvas.transform, Color.white,
                Vector2.zero, Vector2.one);
            cutscene.sprite = Sprite("Enemyy/final_cutscene.png");
            cutscene.rectTransform.localScale = Vector3.one * 1.025f;
            MainMenuBackgroundParallax parallax = cutscene.gameObject.AddComponent<MainMenuBackgroundParallax>();
            Set(parallax, "background", cutscene.rectTransform,
                "maxPointerOffset", new Vector2(20f, 12f), "followSharpness", 2.2f,
                "idleDriftAmount", new Vector2(7f, 4f), "idleDriftSpeed", .08f,
                "invertPointer", true);
            cutscene.preserveAspect = true;
            finalGroup.alpha = 0f;
            finalCanvas.SetActive(false);
            finalCanvas.transform.localScale = Vector3.one;

            creditsCanvas = CanvasScreen("CREDITS", null, out creditsGroup, 1310);
            ImageRect("Credits Black", creditsCanvas.transform, new Color(.012f, .008f, .025f, 1f),
                Vector2.zero, Vector2.one);
            GameObject textGo = new GameObject("Credits Text", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            RectTransform rect = (RectTransform)textGo.transform;
            rect.SetParent(creditsCanvas.transform, false);
            rect.anchorMin = new Vector2(.12f, .055f);
            rect.anchorMax = new Vector2(.88f, .945f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            TextMeshProUGUI text = textGo.GetComponent<TextMeshProUGUI>();
            text.text = "<size=82><b>AUDERE</b></size>\n" +
                "<size=36><i>Một câu chuyện về tuổi nổi loạn, nỗi sợ,\n" +
                "và khoảnh khắc ta chọn tự bước đi.</i></size>\n\n" +
                "Dev : Xuân Kio\n\n" +
                "Artist : nlinn\nArtist : Naga\nArtist : Paiiyin\n\n" +
                "Composer : toigay\n\n" +
                "<size=38>Cảm ơn mọi người đã hoàn thành trò chơi!</size>";
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(.98f, .94f, 1f, 1f);
            text.fontSize = 43f;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/_Audere/AssetGame/Font/Mynerve-Regular SDF.asset");
            if (font != null) text.font = font;
            creditsGroup.alpha = 0f;
            creditsCanvas.SetActive(false);
            creditsCanvas.transform.localScale = Vector3.one;
        }

        private static GameObject CanvasScreen(string name, Transform parent, out CanvasGroup group, int order)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
            if (parent != null)
                go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one;
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            group = go.GetComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            return go;
        }

        private static Image ImageRect(string name, Transform parent, Color color, Vector2 min, Vector2 max)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            Image image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static ProjectionAssaultMove Projection(string path, Sprite sprite,
            CombatMoveDefinition child, float duration, Vector2 size, Color tint)
        {
            ProjectionAssaultMove move = New<ProjectionAssaultMove>(path);
            Set(move, "duration", duration, "leadInDuration", .12f,
                "projectionSprite", sprite, "childMove", child, "copies", 2,
                "visualSize", size, "tint", tint, "boardSideOffset", 48f);
            Save(move, path);
            return move;
        }

        private static NarrativePressurePatternMove FinalPattern(
            int sourceNumber,
            string variantId,
            int phase)
        {
            string sourcePath =
                "Assets/_Audere/Data/Combat/TimorNightPressure/Moves/Move_TimorNightPressure_" +
                sourceNumber.ToString("00") + ".asset";
            NarrativePressurePatternMove source =
                AssetDatabase.LoadAssetAtPath<NarrativePressurePatternMove>(sourcePath);
            if (source == null)
                throw new MissingReferenceException(sourcePath);

            string path = MoveFolder + "/Move_TimorFinal_" + variantId + ".asset";
            NarrativePressurePatternMove move = New<NarrativePressurePatternMove>(path);
            EditorUtility.CopySerialized(source, move);

            float speedMultiplier = phase == 1 ? 1.28f : phase == 2 ? 1.50f : 1.72f;
            float beatMultiplier = phase == 1 ? .68f : phase == 2 ? .54f : .44f;
            float minimumBeats = phase == 3 ? .65f : .9f;
            float beats = Mathf.Max(minimumBeats, source.WaveBeats * beatMultiplier);
            bool laserPattern =
                source.Pattern == NarrativePressurePatternKind.VerticalLaserColumns ||
                source.Pattern == NarrativePressurePatternKind.SweepingLaser ||
                source.Pattern == NarrativePressurePatternKind.PendulumLaser;
            float telegraphMultiplier = phase == 1 ? .92f : phase == 2 ? .82f : .76f;
            float telegraph = Mathf.Max(
                laserPattern ? .38f : .16f,
                source.TelegraphDuration * telegraphMultiplier);
            float duration = phase == 1 ? 5.05f : phase == 2 ? 5.15f : 5.25f;

            SerializedObject so = new SerializedObject(move);
            so.FindProperty("duration").floatValue = duration;
            so.FindProperty("leadInDuration").floatValue = 0f;
            so.FindProperty("waveInterval").floatValue = beats * 60f / 110f;
            so.FindProperty("speed").floatValue = source.Speed * speedMultiplier;
            so.FindProperty("telegraphDuration").floatValue = telegraph;
            so.FindProperty("safeGapFraction").floatValue = phase == 1 ? .34f : phase == 2 ? .32f : .30f;
            so.FindProperty("intensity").intValue = source.Intensity + phase + 1;
            so.FindProperty("wavesPerBurst").intValue = phase == 1 ? 2 : 3;
            so.FindProperty("breatherDuration").floatValue = phase == 1 ? .58f : phase == 2 ? .48f : .44f;
            so.FindProperty("breatherGridPulses").intValue = 1;
            so.FindProperty("rhythmMusic").intValue = (int)Audere.Audio.AudioId.Music_TimorCombat;
            so.FindProperty("rhythmBpm").floatValue = 110f;
            so.FindProperty("rhythmBeatOffset").floatValue = .013f;
            so.FindProperty("waveBeats").floatValue = beats;
            so.ApplyModifiedPropertiesWithoutUndo();
            Save(move, path);
            return move;
        }

        private static ShiftingBattleBoxMove FinalBattleBox()
        {
            const string sourcePath =
                "Assets/_Audere/Data/Combat/TimorNightPressure/Moves/Move_TimorNightPressure_06_ShiftingBattleBox.asset";
            const string path =
                MoveFolder + "/Move_TimorFinal_ShiftingBattleBox.asset";
            ShiftingBattleBoxMove source =
                AssetDatabase.LoadAssetAtPath<ShiftingBattleBoxMove>(sourcePath);
            if (source == null)
                throw new MissingReferenceException(sourcePath);
            ShiftingBattleBoxMove move = New<ShiftingBattleBoxMove>(path);
            EditorUtility.CopySerialized(source, move);
            Set(move, "duration", 5.05f, "leadInDuration", 0f,
                "telegraphDuration", .28f, "squeezeDuration", .34f,
                "holdDuration", .42f, "returnDuration", .34f,
                "telegraphWidthPulse", .045f);
            Save(move, path);
            return move;
        }

        private static CompositeCombatMove Composite(
            string path,
            float duration,
            params CombatMoveDefinition[] children)
        {
            CompositeCombatMove move = New<CompositeCombatMove>(path);
            SerializedObject so = new SerializedObject(move);
            so.FindProperty("duration").floatValue = duration;
            so.FindProperty("leadInDuration").floatValue = 0f;
            SerializedProperty serializedChildren = so.FindProperty("children");
            serializedChildren.arraySize = children.Length;
            for (int i = 0; i < children.Length; i++)
                serializedChildren.GetArrayElementAtIndex(i).objectReferenceValue = children[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            Save(move, path);
            return move;
        }

        private static CombatMoveDefinition Old(int number, bool composite = false)
        {
            string suffix = composite ? "_WithStunZone" : "";
            string path = "Assets/_Audere/Data/Combat/TimorNightPressure/Moves/Move_TimorNightPressure_" +
                number.ToString("00") + suffix + ".asset";
            CombatMoveDefinition move = AssetDatabase.LoadAssetAtPath<CombatMoveDefinition>(path);
            if (move == null) throw new MissingReferenceException(path);
            return move;
        }

        private static void RemovedChalkMoveAuthoring()
        {
        }

        private static void Safe(CombatMoveDefinition move, int phase, params Vector2[] centers)
        {
            if (move == null || centers == null || centers.Length == 0)
                throw new ArgumentException("Every Timor move requires a readable lead-in contract.");

            float leadIn = phase == 1 ? .62f : phase == 2 ? .52f : .46f;

            SerializedObject so = new SerializedObject(move);
            SerializedProperty leadInProperty = so.FindProperty("leadInDuration");
            leadInProperty.floatValue = Mathf.Max(leadInProperty.floatValue, leadIn);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(move);
        }

        private static CombatMoveSet MoveSet(string path, params CombatMoveDefinition[] moves)
        {
            CombatMoveSet set = New<CombatMoveSet>(path);
            SerializedObject so = new SerializedObject(set);
            so.FindProperty("selectionPolicy").intValue = (int)CombatMoveSelectionPolicy.OrderedLoop;
            SerializedProperty entries = so.FindProperty("entries");
            entries.arraySize = moves.Length;
            for (int i = 0; i < moves.Length; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("move").objectReferenceValue = moves[i];
                entry.FindPropertyRelative("weight").floatValue = 1f;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            Save(set, path);
            return set;
        }

        private sealed class CueSpec
        {
            public CueSpec(string id, CombatDialogueCueTrigger trigger, float value,
                CombatMoveDefinition move, DialogueData[] sequence, bool victory, bool phase,
                bool interrupt = false)
            { Id = id; Trigger = trigger; Value = value; Move = move; Sequence = sequence;
              Victory = victory; Phase = phase; Interrupt = interrupt; }
            public string Id;
            public CombatDialogueCueTrigger Trigger;
            public float Value;
            public CombatMoveDefinition Move;
            public DialogueData[] Sequence;
            public bool Victory;
            public bool Phase;
            public bool Interrupt;
        }

        private static void Phase(SerializedProperty phase, string id, CombatMoveSet moveSet,
            params CueSpec[] specs)
        {
            phase.FindPropertyRelative("phaseId").stringValue = id;
            phase.FindPropertyRelative("maxHealth").intValue = 11;
            phase.FindPropertyRelative("sharedExitThreshold").intValue = 0;
            phase.FindPropertyRelative("duration").floatValue = 1f;
            phase.FindPropertyRelative("moveSet").objectReferenceValue = moveSet;
            phase.FindPropertyRelative("diceBatch").objectReferenceValue = null;
            phase.FindPropertyRelative("requiredCapturedBatches").intValue = 1;
            phase.FindPropertyRelative("spawnDice").boolValue = true;
            phase.FindPropertyRelative("advanceOnMoveComplete").boolValue = false;
            phase.FindPropertyRelative("allowsPlayerDefeat").boolValue = true;
            phase.FindPropertyRelative("minimumPlayerTimeOnEnter").floatValue = 0f;
            phase.FindPropertyRelative("playerTimeExitFraction").floatValue = 0f;
            SerializedProperty cues = phase.FindPropertyRelative("dialogueCues");
            cues.arraySize = specs.Length;
            for (int i = 0; i < specs.Length; i++) Cue(cues.GetArrayElementAtIndex(i), specs[i]);
        }

        private static void Cue(SerializedProperty cue, CueSpec spec)
        {
            cue.FindPropertyRelative("cueId").stringValue = spec.Id;
            cue.FindPropertyRelative("oneShotKey").stringValue = spec.Id;
            cue.FindPropertyRelative("trigger").intValue = (int)spec.Trigger;
            cue.FindPropertyRelative("triggerValue").floatValue = spec.Value;
            cue.FindPropertyRelative("triggerMove").objectReferenceValue = spec.Move;
            cue.FindPropertyRelative("triggerCueId").stringValue = "";
            cue.FindPropertyRelative("filterBySymbol").boolValue = false;
            SerializedProperty sequence = cue.FindPropertyRelative("sequence");
            sequence.arraySize = spec.Sequence.Length;
            for (int i = 0; i < spec.Sequence.Length; i++)
                sequence.GetArrayElementAtIndex(i).objectReferenceValue = spec.Sequence[i];
            cue.FindPropertyRelative("instruction").stringValue = "";
            cue.FindPropertyRelative("instructionDuration").floatValue = 0f;
            cue.FindPropertyRelative("tutorialFocus").intValue = 0;
            cue.FindPropertyRelative("isTutorial").boolValue = false;
            cue.FindPropertyRelative("presentation").intValue = (int)CombatDialoguePresentation.AutoCombatDialogue;
            cue.FindPropertyRelative("minimumLineDuration").floatValue = 1.35f;
            cue.FindPropertyRelative("charactersPerSecond").floatValue = 21f;
            cue.FindPropertyRelative("interLineGap").floatValue = .2f;
            cue.FindPropertyRelative("repeatOnTrigger").boolValue = false;
            cue.FindPropertyRelative("interruptsAutoDialogue").boolValue = spec.Interrupt;
            cue.FindPropertyRelative("playLoseRhythmOnComplete").boolValue = false;
            cue.FindPropertyRelative("requiredBeforeVictory").boolValue = spec.Victory;
            cue.FindPropertyRelative("requiredBeforePhaseAdvance").boolValue = spec.Phase;
            cue.FindPropertyRelative("requiredBeforePlayerDefeat").boolValue = false;
        }

        private static DialogueData D(string id, string auderePortrait, string timorPortrait,
            params string[] lines)
        {
            string path = FinalDialogueFolder + "/Dialogue_D4_TIMOR_FINAL_" + id + ".asset";
            DialogueData data = New<DialogueData>(path);
            Set(data, "dialogueId", "D4_TIMOR_FINAL_" + id,
                "leftCharacter", (int)DialogueCharacterId.Audere,
                "rightCharacter", (int)DialogueCharacterId.Timor,
                "leftPortraitOverride", Sprite("Audere/" + auderePortrait),
                "rightPortraitOverride", Sprite("Timor/" + timorPortrait));
            SerializedObject so = new SerializedObject(data);
            SerializedProperty serializedLines = so.FindProperty("lines");
            serializedLines.arraySize = lines.Length;
            for (int i = 0; i < lines.Length; i++)
            {
                SerializedProperty line = serializedLines.GetArrayElementAtIndex(i);
                line.FindPropertyRelative("speaker").intValue =
                    lines[i][0] == 'L' ? (int)DialogueSpeakerSide.Left : (int)DialogueSpeakerSide.Right;
                line.FindPropertyRelative("text").stringValue = lines[i].Substring(2);
                line.FindPropertyRelative("characterOverride").intValue = 0;
                line.FindPropertyRelative("portraitOverride").objectReferenceValue = null;
                line.FindPropertyRelative("glitchPortraitTransition").boolValue = false;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            Save(data, path);
            return data;
        }

        private static void RemoveTailAfter(Transform parent, string keepName)
        {
            Transform keep = parent.Find(keepName);
            if (keep == null) throw new MissingReferenceException(keepName);
            int index = keep.GetSiblingIndex();
            for (int i = parent.childCount - 1; i > index; i--)
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }

        private static void Active(Transform parent, string name, GameObject[] enable, GameObject[] disable)
        {
            SetActiveStep step = Step<SetActiveStep>(parent, name);
            SetObjectArray(step, "objectsToEnable", enable.Cast<Object>().ToArray());
            SetObjectArray(step, "objectsToDisable", disable.Cast<Object>().ToArray());
        }

        private static void Fade(Transform parent, string name, CanvasGroup group, float alpha, float duration) =>
            Set(Step<CanvasFadeStep>(parent, name), "canvasGroup", group, "targetAlpha", alpha, "duration", duration);
        private static void Wait(Transform parent, string name, float duration) =>
            Set(Step<WaitStep>(parent, name), "duration", duration);
        private static void Talk(Transform parent, string name, DialogueData data) =>
            Set(Step<DialogueStep>(parent, name), "dialogueData", data);

        private static void SetObjectArray(Object target, string property, Object[] values)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty array = so.FindProperty(property);
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static T New<T>(string path) where T : ScriptableObject =>
            AssetDatabase.LoadAssetAtPath<T>(path) ?? ScriptableObject.CreateInstance<T>();
        private static void Save(Object asset, string path)
        {
            if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(asset))) AssetDatabase.CreateAsset(asset, path);
            else EditorUtility.SetDirty(asset);
        }
        private static Sprite Sprite(string path)
        {
            Sprite sprite = AssetDatabase.LoadAllAssetsAtPath("Assets/_Audere/AssetGame/" + path)
                .OfType<Sprite>().FirstOrDefault();
            if (sprite == null) throw new MissingReferenceException(path);
            return sprite;
        }
        private static T Step<T>(Transform parent, string name) where T : StoryStep
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<T>();
        }
        private static T[] All<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<T>(true)).ToArray();
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int split = path.LastIndexOf('/');
            EnsureFolder(path.Substring(0, split));
            AssetDatabase.CreateFolder(path.Substring(0, split), path.Substring(split + 1));
        }
        private static void Set(Object target, params object[] pairs)
        {
            SerializedObject so = new SerializedObject(target);
            for (int i = 0; i < pairs.Length; i += 2)
            {
                SerializedProperty property = so.FindProperty((string)pairs[i]);
                object value = pairs[i + 1];
                if (property == null) throw new InvalidOperationException(target.name + ":" + pairs[i]);
                if (value is string) property.stringValue = (string)value;
                else if (value is int) property.intValue = (int)value;
                else if (value is bool) property.boolValue = (bool)value;
                else if (value is float) property.floatValue = (float)value;
                else if (value is Vector2) property.vector2Value = (Vector2)value;
                else if (value is Color) property.colorValue = (Color)value;
                else property.objectReferenceValue = value as Object;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }



        private static void ConfigureEnemyName(Scene scene)
        {
            TMP_Text label = All<TMP_Text>(scene).Single(text => text.name == "Enemy Name");
            label.enableAutoSizing = true;
            label.fontSizeMin = 36f;
            label.fontSizeMax = 57f;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Truncate;
            label.alignment = TextAlignmentOptions.Center;
            EditorUtility.SetDirty(label);
        }
}
}
#endif
