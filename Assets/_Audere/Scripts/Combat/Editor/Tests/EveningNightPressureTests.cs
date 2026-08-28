using System;
using System.Linq;
using Audere.Dialogue;
using Audere.Story;
using Audere.Story.Presentation;
using Audere.Story.Steps;
using Audere.World;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Audere.Combat.Editor.Tests
{
    public sealed class EveningNightPressureTests
    {
        private const string EncounterPath =
            "Assets/_Audere/Data/Combat/TimorNightPressure/CombatEncounter_D1_TIMOR_NIGHT_PRESSURE.asset";
        private const string ScenePath = "Assets/_Audere/Scenes/40_Evening.unity";
        private const string EveningDialoguePath = "Assets/_Audere/Data/Dialogue/Day1/Evening/";

        [Test]
        public void TimorEncounter_AuthorsScriptedDefeatContract()
        {
            CombatEncounterData encounter = AssetDatabase.LoadAssetAtPath<CombatEncounterData>(EncounterPath);
            Assert.IsNotNull(encounter);
            Assert.AreEqual("d1-timor-night-pressure", encounter.EncounterId);
            Assert.AreEqual(66f, encounter.EncounterDuration);
            Assert.AreEqual(3, encounter.DicePerBatch);
            Assert.AreEqual(CombatAllowedOutcome.Defeat, encounter.OutcomeRules.AllowedOutcomes);
            Assert.AreEqual(CombatPlayerDefeatGate.CurrentPhaseAndRequiredCues,
                encounter.OutcomeRules.PlayerDefeatGate);
            Assert.IsFalse(encounter.OutcomeRules.ShowRetryOnDefeat);
            Assert.IsFalse(encounter.OutcomeRules.Allows(CombatResult.Victory));
            Assert.IsTrue(encounter.OutcomeRules.Allows(CombatResult.Defeat));
            Assert.IsNotNull(encounter.DefeatPresentation);
            Assert.IsTrue(encounter.DefeatPresentation.IsConfigured);
            Assert.AreEqual(.62f, encounter.DefeatPresentation.HazardFadeDuration, .0001f);
            CollectionAssert.AreEqual(new[]
            {
                "…",
                "Thấy chưa.",
                "Cậu mệt rồi.",
                "Hôm nay cậu đã cố đủ nhiều rồi.",
                "…Ừ.",
            }, encounter.DefeatPresentation.Dialogue.Lines.Select(line => line.Text).ToArray());
            Assert.AreEqual("TimorBuon_0", encounter.DefeatPresentation.Dialogue.RightPortraitOverride.name);

            CombatEnemyDefinition enemy = encounter.EnemyDefinition;
            Assert.IsNotNull(enemy);
            Assert.AreEqual("d1-evening-timor-night-pressure", enemy.EnemyId);
            Assert.AreEqual("Timor", enemy.DisplayName);
            Assert.AreEqual(CombatPhasePolicy.CapturedDiceBatchSequence, enemy.PhasePolicy);
            Assert.AreEqual(36, enemy.SharedMaxHealth);
            Assert.AreEqual(11, enemy.PhaseCount);
            Assert.IsTrue(enemy.ActorPrefab.name.Contains("PLACEHOLDER"));
            Assert.IsTrue(enemy.Validate(out string error), error);

            for (int i = 0; i < 10; i++)
            {
                CombatPhaseDefinition phase = enemy.GetPhase(i);
                Assert.IsTrue(phase.SpawnDice, $"Phase {i + 1}");
                Assert.IsFalse(phase.AllowsPlayerDefeat, $"Phase {i + 1}");
                Assert.IsNotNull(phase.DiceBatch, $"Phase {i + 1}");
                Assert.AreEqual(3, phase.DiceBatch.Count, $"Phase {i + 1}");
                CollectionAssert.AreEquivalent(
                    new[] { CombatSymbol.Attack, CombatSymbol.Shield, CombatSymbol.Heal },
                    phase.DiceBatch.Dice.Select(die => die.Symbol).ToArray(),
                    $"Phase {i + 1}");
                Assert.IsTrue(phase.DialogueCues.Single().RequiredBeforePhaseAdvance);
                Assert.AreEqual(CombatDialoguePresentation.AutoCombatDialogue,
                    phase.DialogueCues.Single().Presentation);
                CombatMoveDefinition authoredMove = phase.MoveSet.Entries.Single().Move;
                bool expectsStunZone = i == 3 || i == 5 || i == 8;
                if (expectsStunZone)
                {
                    CompositeCombatMove composite = authoredMove as CompositeCombatMove;
                    Assert.IsNotNull(composite, $"Phase {i + 1} must compose its primary attack with Stun Zone pressure.");
                    Assert.AreEqual(2, composite.Children.Length);
                    Assert.IsTrue(composite.Children.Any(child => child is StunZonePressureMove));
                    Assert.IsTrue(composite.Validate(out string compositeError), compositeError);
                }
                else
                {
                    Assert.IsFalse(authoredMove is CompositeCombatMove, $"Phase {i + 1} should not add Stun Zone pressure.");
                }

                CombatMoveDefinition move = ResolvePrimaryMove(authoredMove);
                if (i == 5)
                {
                    ShiftingBattleBoxMove shifting = move as ShiftingBattleBoxMove;
                    Assert.IsNotNull(shifting, "Phase 6 must resize and reposition Dice Field instead of adding another bullet-only pattern.");
                    Assert.AreEqual(3, shifting.Poses.Length);
                    Assert.AreEqual(.72f, shifting.Poses[0].WidthFraction, .001f);
                    Assert.AreEqual(-.70f, shifting.Poses[0].NormalizedX, .001f);
                    Assert.AreEqual(.58f, shifting.Poses[1].WidthFraction, .001f);
                    Assert.AreEqual(.75f, shifting.Poses[1].NormalizedX, .001f);
                    Assert.AreEqual(.46f, shifting.Poses[2].WidthFraction, .001f);
                    Assert.AreEqual(0f, shifting.Poses[2].NormalizedX, .001f);
                }
                else
                {
                    Assert.AreEqual((NarrativePressurePatternKind)i,
                        ((NarrativePressurePatternMove)move).Pattern);
                }
            }

            CombatPhaseDefinition finale = enemy.GetPhase(10);
            Assert.IsFalse(finale.SpawnDice);
            Assert.IsNull(finale.DiceBatch);
            Assert.IsTrue(finale.AllowsPlayerDefeat);
            Assert.AreEqual(30f, finale.MinimumPlayerTimeOnEnter);
            Assert.IsTrue(finale.DialogueCues.Single().RequiredBeforePlayerDefeat);
            Assert.AreEqual(NarrativePressurePatternKind.ClosingFinale,
                ((NarrativePressurePatternMove)finale.MoveSet.Entries.Single().Move).Pattern);
            Assert.AreEqual(NarrativePressurePatternKind.VerticalLaserColumns,
                ((NarrativePressurePatternMove)enemy.GetPhase(1).MoveSet.Entries.Single().Move).Pattern);
            Assert.AreEqual(NarrativePressurePatternKind.SweepingLaser,
                ((NarrativePressurePatternMove)enemy.GetPhase(7).MoveSet.Entries.Single().Move).Pattern);
            Assert.AreEqual(NarrativePressurePatternKind.PendulumLaser,
                ((NarrativePressurePatternMove)enemy.GetPhase(9).MoveSet.Entries.Single().Move).Pattern);
        }

        [Test]
        public void StunZoneMove_TelegraphsBeforeBlockingAndCancelHides()
        {
            StunZonePressureMove move = AssetDatabase.LoadAssetAtPath<StunZonePressureMove>(
                "Assets/_Audere/Data/Combat/TimorNightPressure/Moves/Move_TimorNightPressure_04_StunZone.asset");
            Assert.IsNotNull(move);

            GameObject boardObject = new GameObject("Stun Zone Test Board", typeof(RectTransform), typeof(CombatBoardView));
            GameObject fieldObject = new GameObject("Dice Field", typeof(RectTransform));
            GameObject rootObject = new GameObject("Stun Zone Root", typeof(RectTransform));
            GameObject zoneObject = new GameObject("Stun Zone", typeof(RectTransform), typeof(CanvasGroup), typeof(CombatStunZoneView));
            fieldObject.transform.SetParent(boardObject.transform, false);
            rootObject.transform.SetParent(fieldObject.transform, false);
            zoneObject.transform.SetParent(rootObject.transform, false);
            RectTransform field = fieldObject.GetComponent<RectTransform>();
            field.sizeDelta = new Vector2(620f, 360f);
            CombatStunZoneView zone = zoneObject.GetComponent<CombatStunZoneView>();
            CombatBoardView board = boardObject.GetComponent<CombatBoardView>();
            SerializedObject serialized = new SerializedObject(board);
            serialized.FindProperty("playArea").objectReferenceValue = field;
            serialized.FindProperty("stunZoneRoot").objectReferenceValue = rootObject.GetComponent<RectTransform>();
            SerializedProperty zones = serialized.FindProperty("stunZones");
            zones.arraySize = 1;
            zones.GetArrayElementAtIndex(0).objectReferenceValue = zone;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            try
            {
                ICombatMoveExecution execution = move.CreateExecution(
                    new CombatMoveExecutionContext(board, null, new FixedRandom(), 4, 1));
                execution.Tick(.10f);
                Assert.IsTrue(zone.IsVisible);
                Assert.IsFalse(zone.IsBlocking, "Telegraph must be readable before catch is blocked.");
                execution.Tick(.40f);
                Assert.IsTrue(zone.IsVisible);
                Assert.IsTrue(zone.IsBlocking);
                execution.Cancel();
                Assert.IsFalse(zone.IsVisible);
                Assert.IsFalse(zone.IsBlocking);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(boardObject);
            }
        }

        [Test]
        public void CapturedBatchRuntime_GatesEveryPhaseAndReleasesFinalConstraint()
        {
            CombatEncounterData encounter = AssetDatabase.LoadAssetAtPath<CombatEncounterData>(EncounterPath);
            GameObject boardObject = new GameObject(
                "Timor Runtime Test Board",
                typeof(RectTransform),
                typeof(CombatBoardView));
            GameObject mountObject = new GameObject("Enemy Mount", typeof(RectTransform));
            mountObject.transform.SetParent(boardObject.transform, false);
            GameObject playAreaObject = new GameObject("Play Area", typeof(RectTransform));
            playAreaObject.transform.SetParent(boardObject.transform, false);
            playAreaObject.GetComponent<RectTransform>().sizeDelta = new Vector2(620f, 360f);
            CombatBoardView board = boardObject.GetComponent<CombatBoardView>();
            SerializedObject boardSerialized = new SerializedObject(board);
            boardSerialized.FindProperty("enemyMount").objectReferenceValue = mountObject.transform;
            boardSerialized.FindProperty("playArea").objectReferenceValue = playAreaObject.GetComponent<RectTransform>();
            boardSerialized.ApplyModifiedPropertiesWithoutUndo();

            try
            {
                var runtime = new CombatEnemyRuntime(
                    encounter.EnemyDefinition,
                    board,
                    new FixedRandom(),
                    44,
                    false);
                runtime.Start();
                Assert.AreEqual(36, runtime.CurrentHealth);

                runtime.ApplyDamage(999, out int applied);
                Assert.AreEqual(35, applied);
                Assert.AreEqual(1, runtime.CurrentHealth);
                Assert.AreEqual(CombatEnemyRuntimeState.Playing, runtime.State);

                for (int phaseIndex = 0; phaseIndex < 10; phaseIndex++)
                {
                    Assert.AreEqual(phaseIndex, runtime.PhaseIndex);
                    CombatDialogueCue cue = runtime.CurrentPhase.DialogueCues.Single();
                    Assert.AreEqual(CombatEnemyProgression.None, runtime.NotifyCapturedDiceBatch());
                    Assert.IsTrue(runtime.IsBatchProgressionPending);
                    runtime.MarkCueResolved(cue);
                    Assert.AreEqual(CombatEnemyProgression.PhaseBreak,
                        runtime.TryReleasePendingBatchProgression());
                    runtime.CompletePhaseBreak();
                }

                Assert.AreEqual(10, runtime.PhaseIndex);
                Assert.IsFalse(runtime.ShouldSpawnDice);
                Assert.IsFalse(runtime.CanPlayerBeDefeated);
                runtime.Tick(.1f);
                Assert.IsTrue(board.HasPlayerConstraint);
                runtime.MarkCueResolved(runtime.CurrentPhase.DialogueCues.Single());
                Assert.IsTrue(runtime.CanPlayerBeDefeated);
                runtime.Cancel();
                Assert.IsFalse(board.HasPlayerConstraint);
                Assert.AreEqual(CombatEnemyRuntimeState.Cancelled, runtime.State);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(boardObject);
            }
        }

        [Test]
        public void NightDialogue_BuildsPressureBeforeCombatAndDistortsPositiveOptions()
        {
            DialogueData question = LoadDialogue("Dialogue_D1_HOME_NIGHT_TIMOR_QUESTIONS");
            DialogueData conclusion = LoadDialogue("Dialogue_D1_HOME_NIGHT_CONCLUSION");

            CollectionAssert.AreEqual(new[]
            {
                "Audere... cậu không thấy sợ à?",
                "Sợ gì?",
                "Nếu Bianca chỉ tìm cậu",
                "vì muốn nhờ vả thì sao?",
                "Tớ nghĩ cậu ấy chỉ đang hỏi thôi.",
                "Tớ cũng muốn tin như vậy.",
                "Nhưng tớ sợ lắm.",
                "Mẹ cậu cũng từng tin người khác.",
                "Rồi cậu đã mất bà ấy.",
                "Timor... chuyện đó không giống nhau.",
                "Bianca chỉ nhắn cho tớ thôi.",
            }, question.Lines.Select(line => line.Text).ToArray());
            CollectionAssert.AreEqual(new[]
            {
                "Cậu không biết cô ấy muốn gì đâu.",
                "Tớ có thể hỏi lại.",
                "Rồi nếu cô ấy tiếp tục nhờ thì sao?",
                "Nếu nhiều quá... tớ sẽ nói là không.",
                "Audere.",
                "Tớ biết cậu đang lo.",
                "Nhưng tớ vẫn muốn trả lời.",
                "Không được đâu, Audere.",
                "Tớ đã thấy chuyện gì xảy ra rồi.",
                "Timor, đừng lo.",
                "Đừng bảo tớ đừng lo!",
                "Cậu phải nghe tớ lần này.",
                "Tin tớ đi.",
                "Giữ khoảng cách với cô ấy.",
                "...Tớ không muốn.",
                "Audere.",
                "Lần này, để tớ tự trả lời.",
                "Tớ không thể để cậu làm vậy.",
            }, conclusion.Lines.Select(line => line.Text).ToArray());

            foreach (DialogueData storyDialogue in new[] { question, conclusion })
            {
                Assert.AreEqual(DialogueCharacterId.Audere, storyDialogue.LeftCharacter, storyDialogue.name);
                Assert.AreEqual(DialogueCharacterId.Timor, storyDialogue.RightCharacter, storyDialogue.name);
                foreach (DialogueData.Line line in storyDialogue.Lines)
                    Assert.LessOrEqual(line.Text.Length, 42, $"{storyDialogue.name}: {line.Text}");
            }

            DialogueData[] barks = Enumerable.Range(1, 11)
                .Select(index => LoadDialogue($"Dialogue_D1_TIMOR_NIGHT_PRESSURE_BARK_{index:00}"))
                .ToArray();
            foreach (DialogueData bark in barks)
            {
                Assert.AreEqual(DialogueCharacterId.Audere, bark.LeftCharacter, bark.name);
                Assert.AreEqual(DialogueCharacterId.Timor, bark.RightCharacter, bark.name);
                Assert.That(bark.Lines, Is.Not.Empty, bark.name);
                foreach (DialogueData.Line line in bark.Lines)
                    Assert.LessOrEqual(line.Text.Length, 42, $"{bark.name}: {line.Text}");
            }

            CollectionAssert.AreEqual(new[]
            {
                "Cậu nghĩ mình sẽ nói được 'không' à?",
                "Tớ có thể thử.",
            }, barks[2].Lines.Select(line => line.Text).ToArray());
            CollectionAssert.AreEqual(new[]
            {
                "Tớ không bỏ cậu.",
                "Vậy sao cậu vẫn chọn cô ấy?",
            }, barks[8].Lines.Select(line => line.Text).ToArray());
            CollectionAssert.AreEqual(new[]
            {
                "Cậu không cần thêm bất kỳ ai.",
                "Có tớ là đủ rồi.",
            }, barks[9].Lines.Select(line => line.Text).ToArray());
        }

        [Test]
        public void EveningScene_HasCenteredStagingAndDirectStoryFlow()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject world = scene.GetRootGameObjects().Single(root => root.name == "WORLD");
            Assert.IsNotNull(scene.GetRootGameObjects().Single(root => root.name == "GameplayUIRoot")
                .GetComponent<Audere.Dialogue.GameplayUIRoot>());
            Transform stage = world.transform.Find("Story Root/Evening Stage PLACEHOLDER_NO_ART");
            Assert.IsNotNull(stage);
            Transform storyRoot = stage.parent;
            Assert.AreEqual(new Vector3(0f, -.3275f, 0f), storyRoot.localPosition);
            Assert.AreEqual(Vector3.one * .25f, storyRoot.localScale);
            Transform tile = stage.Find("Night Tile PLACEHOLDER");
            Transform audere = stage.Find("Audere");
            Assert.IsNotNull(tile);
            Assert.IsNotNull(audere);
            Assert.AreEqual(new Vector3(0f, 1.15f, 0f), tile.localPosition);
            Assert.AreEqual(new Vector3(0f, 1.8475f, -1f), audere.localPosition);
            Assert.AreEqual(Vector3.one * 1.5f, audere.localScale);
            SpriteRenderer body = audere.GetComponent<SpriteRenderer>();
            Assert.AreEqual("Player", body.sortingLayerName);
            Assert.AreEqual(5, body.sortingOrder);
            SpriteRenderer shadow = audere.GetComponentsInChildren<SpriteRenderer>(true)
                .Single(renderer => renderer.transform.name.StartsWith("shadow", StringComparison.OrdinalIgnoreCase));
            Assert.AreEqual("Player", shadow.sortingLayerName);
            Assert.AreEqual(4, shadow.sortingOrder);
            Transform messageIndicator = audere.Find("dauchamthan");
            Assert.IsNotNull(messageIndicator);
            Assert.IsFalse(messageIndicator.gameObject.activeSelf);
            SpriteRenderer indicatorRenderer = messageIndicator.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(indicatorRenderer);
            Assert.AreEqual("Player", indicatorRenderer.sortingLayerName);
            Assert.AreEqual(6, indicatorRenderer.sortingOrder);

            Camera camera = scene.GetRootGameObjects().Single(root => root.name == "Main Camera")
                .GetComponent<Camera>();
            Assert.IsNotNull(camera.GetComponent<AudioListener>());
            Transform viewport = camera.transform.Find("PuzzleViewportMask");
            Assert.IsNotNull(viewport);
            Assert.AreEqual(new Vector3(0f, 0f, 9f), viewport.localPosition);
            Assert.AreEqual(Vector3.one * .488376f, viewport.localScale);
            WorldModeController worldMode = world.GetComponent<WorldModeController>();
            Assert.IsNotNull(worldMode);
            SerializedObject worldModeSerialized = new SerializedObject(worldMode);
            Assert.AreEqual((int)WorldGameplayMode.Story,
                worldModeSerialized.FindProperty("startingMode").enumValueIndex);
            Vector3 storyCameraPosition = worldModeSerialized.FindProperty("storyCameraPosition").vector3Value;
            Assert.AreEqual(tile.position.x, storyCameraPosition.x, .0001f);
            Assert.AreEqual(tile.position.y, storyCameraPosition.y, .0001f);
            Assert.AreEqual(new Vector3(0f, 0f, -10f),
                worldModeSerialized.FindProperty("combatCameraPosition").vector3Value);

            StoryEvent storyEvent = scene.GetRootGameObjects().Single(root => root.name == "STORY")
                .GetComponentInChildren<StoryEvent>(true);
            Assert.AreEqual("D1_HOME_NIGHT_MESSAGE", storyEvent.EventId);
            string[] expectedSteps =
            {
                "00_NormalizeMessageAlert", "10_FadeIn", "20_AudereAfterLongDay",
                "30_PlayMessageArrival", "35_HoldForMessage", "40_ShowMessageAlert",
                "45_HoldMessageAlert", "50_AudereStartles", "55_HoldAfterStartle",
                "60_AudereRecognizesBianca", "65_HideMessageAlert",
                "70_BiancaNightMessage", "80_TimorQuestionsHer", "90_KeepSilence",
                "100_AudereAndTimorConclude", "110_HoldBeforePressure",
                "120_EnterNightPressure", "130_PlayTimorNightPressure",
                "140_ReturnToEvening", "145_HoldAfterReturn",
                "150_TimorNarrowsTheReply", "160_ChooseBiancaReply",
                "170_HoldAfterReply", "180_LightsOut", "190_DayOneEnds",
            };
            CollectionAssert.AreEqual(expectedSteps,
                storyEvent.transform.Cast<Transform>().Select(child => child.name).ToArray());

            SetActiveStep normalizeAlert = storyEvent.transform.Find("00_NormalizeMessageAlert")
                .GetComponent<SetActiveStep>();
            CollectionAssert.Contains(normalizeAlert.ObjectsToDisable.ToArray(), messageIndicator.gameObject);
            SetActiveStep showAlert = storyEvent.transform.Find("40_ShowMessageAlert")
                .GetComponent<SetActiveStep>();
            CollectionAssert.Contains(showAlert.ObjectsToEnable.ToArray(), messageIndicator.gameObject);
            SetActiveStep hideAlert = storyEvent.transform.Find("65_HideMessageAlert")
                .GetComponent<SetActiveStep>();
            CollectionAssert.Contains(hideAlert.ObjectsToDisable.ToArray(), messageIndicator.gameObject);

            DialogueStep recognition = storyEvent.transform.Find("60_AudereRecognizesBianca")
                .GetComponent<DialogueStep>();
            Assert.AreEqual(DialogueCharacterId.Audere, recognition.DialogueData.LeftCharacter);
            Assert.AreEqual(DialogueCharacterId.Timor, recognition.DialogueData.RightCharacter);
            Assert.AreEqual(DialogueSpeakerSide.Left, recognition.DialogueData.Lines.Single().Speaker);
            Assert.AreEqual("Bianca nhắn cho tớ này.", recognition.DialogueData.Lines.Single().Text);

            CharacterMotionStep startle = storyEvent.transform.Find("50_AudereStartles")
                .GetComponent<CharacterMotionStep>();
            Assert.AreEqual(CharacterMotionMode.VerticalInPlace, startle.MotionMode);
            Assert.AreEqual(.19f, startle.Duration, .0001f);
            Assert.AreEqual(.09f, startle.ArcHeight, .0001f);
            Assert.AreEqual(audere, startle.Actor);
            Assert.IsNotNull(startle.GroundedShadow);

            CombatStep combat = storyEvent.transform.Find("130_PlayTimorNightPressure")
                .GetComponent<CombatStep>();
            Assert.AreEqual(CombatResultBehaviour.Fail, combat.VictoryBehaviour);
            Assert.AreEqual(CombatResultBehaviour.Complete, combat.DefeatBehaviour);
            Assert.AreEqual(CombatResultBehaviour.Fail, combat.SpecialBehaviour);
            Assert.AreEqual(EncounterPath, AssetDatabase.GetAssetPath(combat.CombatEncounterData));
            Assert.IsNotNull(combat.CombatController);
            Assert.IsNotNull(combat.CombatController.BoardView);

            SerializedObject boardSerialized = new SerializedObject(combat.CombatController.BoardView);
            CombatEnemyActor authoredPreview = boardSerialized.FindProperty("authoredEnemyActor")
                .objectReferenceValue as CombatEnemyActor;
            Assert.IsNotNull(authoredPreview);
            Assert.AreEqual(
                "Assets/_Audere/Prefabs/Combat/Enemies/Enemy_TimorNightPressure_PLACEHOLDER.prefab",
                PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(authoredPreview.gameObject));
            Assert.AreEqual(
                boardSerialized.FindProperty("enemyMount").objectReferenceValue,
                authoredPreview.transform.parent);

            Transform choiceTransform = storyEvent.transform.Find("160_ChooseBiancaReply");
            StoryChoiceBranchStep choice = choiceTransform.GetComponent<StoryChoiceBranchStep>();
            Assert.IsNotNull(choice);
            CollectionAssert.AreEqual(new[]
            {
                "Tớ xin lỗi, nhưng mai tớ có việc bận.",
                "Tớ chưa biết nữa.",
                "…",
            }, choice.Options.ToArray());
            CollectionAssert.AreEqual(new[] { "00_Avoid", "10_Delay", "20_NoReply" },
                choice.Branches.Select(branch => branch.name).ToArray());
            Assert.IsTrue(choice.Branches.All(branch => branch.transform.parent == choiceTransform));
            Assert.IsNotNull(choice.ChoiceView);
            Assert.AreEqual("NIGHT MESSAGE UI/Reply Choices", GetHierarchyPath(choice.ChoiceView.transform));

            GameObject nightUi = scene.GetRootGameObjects().Single(root => root.name == "NIGHT MESSAGE UI");
            Canvas choiceCanvas = nightUi.GetComponent<Canvas>();
            Assert.AreEqual(RenderMode.ScreenSpaceOverlay, choiceCanvas.renderMode);
            Assert.IsTrue(choiceCanvas.isRootCanvas);
            Assert.AreEqual(1300, choiceCanvas.sortingOrder);
            Assert.AreEqual(3, nightUi.GetComponentsInChildren<StoryChoiceOptionView>(true).Length);
            CanvasScaler choiceScaler = nightUi.GetComponent<CanvasScaler>();
            Assert.AreEqual(1f, choiceScaler.matchWidthOrHeight, .0001f);
            RectTransform choiceRect = choice.ChoiceView.GetComponent<RectTransform>();
            Assert.AreEqual(new Vector2(1320f, 210f), choiceRect.sizeDelta);
            Assert.AreEqual(new Vector2(0f, 12f), choiceRect.anchoredPosition);
            RectTransform[] optionRects = choice.ChoiceView
                .GetComponentsInChildren<StoryChoiceOptionView>(true)
                .Select(option => option.GetComponent<RectTransform>())
                .OrderBy(option => option.anchoredPosition.y * -1f)
                .ToArray();
            Assert.AreEqual(3, optionRects.Length);
            for (int optionIndex = 0; optionIndex < optionRects.Length; optionIndex++)
            {
                Assert.AreEqual(new Vector2(1260f, 62f), optionRects[optionIndex].sizeDelta);
                Assert.AreEqual(-optionIndex * 70f, optionRects[optionIndex].anchoredPosition.y, .0001f);
            }

            RectTransform messageStatus = nightUi.transform.Find("Message Status")
                .GetComponent<RectTransform>();
            Assert.AreEqual(new Vector2(.5f, .107795f), messageStatus.anchorMin);
            Assert.AreEqual(new Vector2(.5f, .107795f), messageStatus.anchorMax);
            Assert.AreEqual(Vector2.zero, messageStatus.anchoredPosition);
        }

        [Test]
        public void NightReplyBranches_KeepAudereLeftAndUseAcceptedLines()
        {
            DialogueData beforeChoice = LoadDialogue("Dialogue_D1_HOME_NIGHT_BEFORE_REPLY_CHOICE");
            DialogueData avoidTimor = LoadDialogue("Dialogue_D1_HOME_NIGHT_REPLY_AVOID_TIMOR");
            DialogueData avoidAudere = LoadDialogue("Dialogue_D1_HOME_NIGHT_REPLY_AVOID_AUDERE");
            DialogueData delay = LoadDialogue("Dialogue_D1_HOME_NIGHT_REPLY_DELAY");
            DialogueData silence = LoadDialogue("Dialogue_D1_HOME_NIGHT_REPLY_SILENCE");

            foreach (DialogueData dialogue in new[] { beforeChoice, avoidTimor, avoidAudere, delay, silence })
            {
                Assert.AreEqual(DialogueCharacterId.Audere, dialogue.LeftCharacter, dialogue.name);
                Assert.AreEqual(DialogueCharacterId.Timor, dialogue.RightCharacter, dialogue.name);
                Assert.AreEqual("TimorBuon_0", dialogue.RightPortraitOverride.name, dialogue.name);
                foreach (DialogueData.Line line in dialogue.Lines)
                    Assert.LessOrEqual(line.Text.Length, 42, $"{dialogue.name}: {line.Text}");
            }

            CollectionAssert.AreEqual(new[] { "Không cần ép mình.", "Chọn câu dễ nhất thôi." },
                beforeChoice.Lines.Select(line => line.Text).ToArray());
            CollectionAssert.AreEqual(new[] { "Ừ.", "Thế là ngày mai cậu không phải lo nữa." },
                avoidTimor.Lines.Select(line => line.Text).ToArray());
            CollectionAssert.AreEqual(new[] { "…Ừ." },
                avoidAudere.Lines.Select(line => line.Text).ToArray());
            CollectionAssert.AreEqual(new[] { "Tốt rồi.", "Mai nếu cậu thấy ổn thì tính tiếp." },
                delay.Lines.Select(line => line.Text).ToArray());
            CollectionAssert.AreEqual(new[]
            {
                "Không trả lời cũng là một câu trả lời.",
                "Bianca sẽ nghĩ gì nhỉ?",
                "Ngày mai rồi tính.",
                "Nghỉ thôi.",
            }, silence.Lines.Select(line => line.Text).ToArray());
        }

        [Test]
        public void CombatBoard_ClipsProjectilesInsideTheFrame()
        {
            GameObject board = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab");
            Assert.IsNotNull(board);
            CombatBoardView view = board.GetComponent<CombatBoardView>();
            Assert.IsNotNull(view);
            RectMask2D mask = board.GetComponentsInChildren<RectMask2D>(true)
                .Single(component => component.name == "Projectile Mask");
            RectTransform maskRect = mask.GetComponent<RectTransform>();
            Assert.AreEqual(new Vector2(14f, 14f), maskRect.offsetMin);
            Assert.AreEqual(new Vector2(-14f, -14f), maskRect.offsetMax);

            SerializedObject serialized = new SerializedObject(view);
            RectTransform bulletRoot = serialized.FindProperty("bulletRoot").objectReferenceValue as RectTransform;
            RectTransform laserRoot = serialized.FindProperty("laserRoot").objectReferenceValue as RectTransform;
            Assert.IsNotNull(bulletRoot);
            Assert.IsNotNull(laserRoot);
            Assert.AreEqual(maskRect, bulletRoot.parent);
            Assert.AreEqual(maskRect, laserRoot.parent);
        }

        [Test]
        public void EnemyBullet_UsesAuthoredDanSpriteWithoutLegacyTintOrShape()
        {
            GameObject bullet = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Audere/Prefabs/Combat/Bullets/EnemyBullet.prefab");
            Assert.IsNotNull(bullet);

            RectTransform rect = bullet.GetComponent<RectTransform>();
            Image image = bullet.GetComponent<Image>();
            Assert.IsNotNull(rect);
            Assert.IsNotNull(image);
            Assert.AreEqual(new Vector2(24f, 24f), rect.sizeDelta);
            Assert.AreEqual(Quaternion.identity, rect.localRotation);
            Assert.AreEqual(Color.white, image.color);
            Assert.IsTrue(image.preserveAspect);
            Assert.IsNotNull(image.sprite);
            Assert.AreEqual("Assets/_Audere/AssetGame/Item/dan.aseprite",
                AssetDatabase.GetAssetPath(image.sprite));
            Assert.IsNull(bullet.transform.Find("Core"));
        }

        [Test]
        public void TimorDialoguePortraitAndLaserPresentation_HaveDeterministicLifecycle()
        {
            DialogueData question = LoadDialogue("Dialogue_D1_HOME_NIGHT_TIMOR_QUESTIONS");
            DialogueData conclusion = LoadDialogue("Dialogue_D1_HOME_NIGHT_CONCLUSION");
            Assert.AreEqual("TimorLolang_0", question.RightPortraitOverride.name);
            Assert.AreEqual("TimorLoLangKhongVui_0", question.Lines[6].PortraitOverride.name);
            Assert.AreEqual("TimorLoLangKhongVui_0", conclusion.RightPortraitOverride.name);
            Assert.AreEqual("TimorTucGian_0", conclusion.Lines[7].PortraitOverride.name);
            Assert.AreEqual("TimorLolang_0",
                LoadDialogue("Dialogue_D1_TIMOR_NIGHT_PRESSURE_BARK_01").RightPortraitOverride.name);
            Assert.AreEqual("TimorTucGian_0",
                LoadDialogue("Dialogue_D1_TIMOR_NIGHT_PRESSURE_BARK_07").RightPortraitOverride.name);
            Assert.AreEqual("TimorBuon_0",
                LoadDialogue("Dialogue_D1_TIMOR_NIGHT_PRESSURE_BARK_11").RightPortraitOverride.name);

            GameObject laserObject = new GameObject(
                "Laser Lifecycle Test",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CombatLaserView));
            try
            {
                CombatLaserView laser = laserObject.GetComponent<CombatLaserView>();
                laser.Setup(Vector2.zero, Vector2.right * 100f, new Vector2(20f, 420f), 0f, .2f, .3f, 7, 9);
                Assert.IsFalse(laser.CollisionActive);
                Assert.AreEqual(.08f, laser.RectTransform.localScale.x, .0001f);
                Assert.AreEqual(0f, laserObject.GetComponent<Image>().color.a, .0001f);
                Assert.IsTrue(laser.Tick(.1f));
                Assert.IsFalse(laser.CollisionActive);
                Assert.Greater(laser.RectTransform.localScale.x, .08f);
                Assert.Less(laser.RectTransform.localScale.x, 1f);
                Assert.Greater(laserObject.GetComponent<Image>().color.a, 0f);
                Assert.AreEqual(laserObject.GetComponent<Image>().color.r,
                    laserObject.GetComponent<Image>().color.g, .0001f);
                Assert.AreEqual(laserObject.GetComponent<Image>().color.g,
                    laserObject.GetComponent<Image>().color.b, .0001f);
                Assert.IsTrue(laser.Tick(.11f));
                Assert.IsTrue(laser.CollisionActive);
                Assert.AreEqual(1f, laser.RectTransform.localScale.x, .0001f);
                Assert.Greater(laserObject.GetComponent<Image>().color.r,
                    laserObject.GetComponent<Image>().color.g);
                float activeAlpha = laserObject.GetComponent<Image>().color.a;
                laser.BeginPresentationFade();
                Assert.IsFalse(laser.CollisionActive);
                laser.SetPresentationFade(.5f);
                Assert.AreEqual(activeAlpha * .5f, laserObject.GetComponent<Image>().color.a, .0001f);
                laser.ReturnToPool();
                Assert.IsFalse(laser.gameObject.activeSelf);
                Assert.AreEqual(0, laser.OwnerSessionVersion);
                Assert.AreEqual(0, laser.OwnerPhaseVersion);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(laserObject);
            }
        }

        private sealed class FixedRandom : ICombatRandom
        {
            public float Value01() => .5f;
            public float Range(float minimum, float maximum) => (minimum + maximum) * .5f;
        }

        private static CombatMoveDefinition ResolvePrimaryMove(CombatMoveDefinition move)
        {
            if (!(move is CompositeCombatMove composite))
                return move;
            return composite.Children.Single(child => !(child is StunZonePressureMove));
        }

        private static DialogueData LoadDialogue(string assetName)
        {
            DialogueData dialogue = AssetDatabase.LoadAssetAtPath<DialogueData>(
                $"{EveningDialoguePath}{assetName}.asset");
            Assert.IsNotNull(dialogue, assetName);
            return dialogue;
        }

        private static string GetHierarchyPath(Transform target)
        {
            string path = target.name;
            while (target.parent != null)
            {
                target = target.parent;
                path = target.name + "/" + path;
            }
            return path;
        }
    }
}
