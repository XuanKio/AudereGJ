#if UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Audere.Combat.Editor.Tests
{
    public sealed class CombatEnemyRuntimeTests
    {
        private readonly List<UnityEngine.Object> cleanup = new List<UnityEngine.Object>();

        private sealed class NoopMove : CombatMoveDefinition
        {
            public override ICombatMoveExecution CreateExecution(CombatMoveExecutionContext context) => new Execution();
            private sealed class Execution : ICombatMoveExecution
            {
                public bool IsComplete => false;
                public void Tick(float activeDeltaTime) { }
                public void Cancel() { }
            }
        }

        private sealed class FixedRandom : ICombatRandom
        {
            private readonly float value;
            public FixedRandom(float value) => this.value = value;
            public float Value01() => value;
            public float Range(float minimum, float maximum) => Mathf.Lerp(minimum, maximum, value);
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = cleanup.Count - 1; i >= 0; i--)
                if (cleanup[i] != null) UnityEngine.Object.DestroyImmediate(cleanup[i]);
            cleanup.Clear();
        }

        [Test]
        public void OrderedLoop_WrapsAndNewSelectorResets()
        {
            NoopMove first = Create<NoopMove>();
            NoopMove second = Create<NoopMove>();
            CombatMoveSet set = CreateMoveSet(CombatMoveSelectionPolicy.OrderedLoop,
                (first, 1f), (second, 1f));
            var selector = new CombatMoveSelector(set, new FixedRandom(.5f));
            Assert.AreSame(first, selector.Next());
            Assert.AreSame(second, selector.Next());
            Assert.AreSame(first, selector.Next());
            Assert.AreSame(first, new CombatMoveSelector(set, new FixedRandom(.5f)).Next());
        }

        [Test]
        public void WeightedRandom_DoesNotSelectZeroWeight_AndIsDeterministic()
        {
            NoopMove zero = Create<NoopMove>();
            NoopMove valid = Create<NoopMove>();
            CombatMoveSet set = CreateMoveSet(CombatMoveSelectionPolicy.WeightedRandom,
                (zero, 0f), (valid, 3f));
            Assert.AreSame(valid, new CombatMoveSelector(set, new FixedRandom(0f)).Next());
            Assert.AreSame(valid, new CombatMoveSelector(set, new FixedRandom(.99f)).Next());
        }

        [Test]
        public void PerPhaseHealth_DiscardsOverflow_ResetsHealth_AndVictoriesOnce()
        {
            CombatEnemyRuntime runtime = CreateRuntime(CombatPhasePolicy.PerPhaseHealth, 4,
                ("p1", 2, 0, 5f), ("p2", 2, 0, 5f));
            Assert.AreEqual(CombatEnemyProgression.PhaseBreak, runtime.ApplyDamage(99, out int firstDamage));
            Assert.AreEqual(2, firstDamage);
            runtime.CompletePhaseBreak();
            Assert.AreEqual(2, runtime.CurrentHealth);
            Assert.AreEqual(CombatEnemyProgression.Victory, runtime.ApplyDamage(99, out int secondDamage));
            Assert.AreEqual(2, secondDamage);
            Assert.AreEqual(CombatEnemyProgression.None, runtime.ApplyDamage(1, out _));
        }

        [Test]
        public void SharedHealth_ClampsAtCurrentThreshold_AndCannotSkipPhase()
        {
            CombatEnemyRuntime runtime = CreateRuntime(CombatPhasePolicy.SharedHealthThresholds, 6,
                ("p1", 1, 4, 5f), ("p2", 1, 0, 5f));
            Assert.AreEqual(CombatEnemyProgression.PhaseBreak, runtime.ApplyDamage(99, out int applied));
            Assert.AreEqual(2, applied);
            Assert.AreEqual(4, runtime.CurrentHealth);
            runtime.CompletePhaseBreak();
            Assert.AreEqual(CombatEnemyProgression.Victory, runtime.ApplyDamage(99, out applied));
            Assert.AreEqual(4, applied);
        }

        [Test]
        public void TimedSequence_OnlyTicksWhilePlaying()
        {
            CombatEnemyRuntime runtime = CreateRuntime(CombatPhasePolicy.TimedSequence, 1,
                ("p1", 1, 0, 1f), ("p2", 1, 0, 1f));
            runtime.Tick(.5f);
            runtime.PauseForDialogue();
            runtime.Tick(10f);
            Assert.AreEqual(.5f, runtime.PhaseElapsed, .001f);
            runtime.ResumeFromDialogue();
            runtime.Tick(.51f);
            Assert.AreEqual(CombatEnemyRuntimeState.TransitioningPhase, runtime.State);
            runtime.CompletePhaseBreak();
            runtime.Tick(1.01f);
            Assert.AreEqual(CombatEnemyRuntimeState.Completed, runtime.State);
        }

        [Test]
        public void RetryView_DoubleClickInvokesCallbackOnce()
        {
            CombatRetryView view = CreateRetryView(out Button button);
            int calls = 0;
            Assert.IsTrue(view.Show(view, () => calls++));
            MethodInfo click = typeof(CombatRetryView).GetMethod(
                "HandleRetryClicked", BindingFlags.Instance | BindingFlags.NonPublic);
            click.Invoke(view, null);
            click.Invoke(view, null);
            Assert.AreEqual(1, calls);
            Assert.IsFalse(view.IsShowing);
        }

        [Test]
        public void RetryView_ForceHideIsIdempotentAndDoesNotInvokeCallback()
        {
            CombatRetryView view = CreateRetryView(out _);
            int calls = 0;
            Assert.IsTrue(view.Show(view, () => calls++));
            view.ForceHide();
            view.ForceHide();
            Assert.AreEqual(0, calls);
            Assert.IsFalse(view.IsShowing);
            Assert.IsNull(view.ActiveOwner);
        }

        [Test]
        public void TutorialCue_SymbolFilterAndInstruction_AreDataDriven()
        {
            var cue = new CombatDialogueCue();
            SetField(cue, "cueId", "test-shield-cue");
            SetField(cue, "filterBySymbol", true);
            SetField(cue, "symbol", CombatSymbol.Shield);
            SetField(cue, "instruction", "KHIÊN");
            SetField(cue, "tutorialFocus", CombatTutorialFocus.Dice);
            SetField(cue, "showcasedSymbol", CombatSymbol.Shield);
            SetField(cue, "isTutorial", true);

            Assert.IsTrue(cue.HasContent);
            Assert.IsTrue(cue.HasInstruction);
            Assert.IsTrue(cue.MatchesSymbol(CombatSymbol.Shield));
            Assert.IsFalse(cue.MatchesSymbol(CombatSymbol.Attack));
            Assert.IsTrue(cue.PausesCombatForPresentation);
            Assert.AreEqual(CombatTutorialFocus.Dice, cue.TutorialFocus);
            Assert.AreEqual(CombatSymbol.Shield, cue.ShowcasedSymbol);
            Assert.IsTrue(cue.IsTutorial);
        }

        [Test]
        public void TutorialCue_PlayedState_IsPerRuntimeAttempt()
        {
            var cue = new CombatDialogueCue();
            SetField(cue, "cueId", "attempt-local-cue");
            SetField(cue, "instruction", "Instruction");

            CombatEnemyRuntime first = CreateRuntime(CombatPhasePolicy.PerPhaseHealth, 1,
                ("p1", 1, 0, 5f));
            Assert.IsTrue(first.MarkCuePlayed(cue));
            Assert.IsFalse(first.MarkCuePlayed(cue));

            CombatEnemyRuntime retry = CreateRuntime(CombatPhasePolicy.PerPhaseHealth, 1,
                ("p1", 1, 0, 5f));
            Assert.IsTrue(retry.MarkCuePlayed(cue));
        }

        [Test]
        public void TutorialCue_SharedOneShotKey_DoesNotRepeatAcrossPhaseEntries()
        {
            var phaseOneEntry = new CombatDialogueCue();
            SetField(phaseOneEntry, "cueId", "shield-p1");
            SetField(phaseOneEntry, "oneShotKey", "shield-tutorial");
            SetField(phaseOneEntry, "instruction", "Shield");
            var phaseTwoEntry = new CombatDialogueCue();
            SetField(phaseTwoEntry, "cueId", "shield-p2");
            SetField(phaseTwoEntry, "oneShotKey", "shield-tutorial");
            SetField(phaseTwoEntry, "instruction", "Shield");

            CombatEnemyRuntime runtime = CreateRuntime(CombatPhasePolicy.PerPhaseHealth, 1,
                ("p1", 1, 0, 5f));
            Assert.IsTrue(runtime.MarkCuePlayed(phaseOneEntry));
            Assert.IsFalse(runtime.MarkCuePlayed(phaseTwoEntry));
        }

        [Test]
        public void ProductionTutorial_AllCuesAreValid_AndDialogueFitsBubbleBudget()
        {
            CombatEnemyDefinition productionEnemy = AssetDatabase.LoadAssetAtPath<CombatEnemyDefinition>(
                "Assets/_Audere/Data/Combat/Enemies/Enemy_KhoangLang.asset");
            CombatTutorialData tutorial = AssetDatabase.LoadAssetAtPath<CombatTutorialData>(
                "Assets/_Audere/Data/Combat/Tutorials/CombatTutorial_D1_CLASSROOM.asset");
            Assert.IsNotNull(productionEnemy);
            Assert.IsNotNull(tutorial);
            Assert.IsTrue(productionEnemy.Validate(out string productionError), productionError);
            Assert.IsTrue(tutorial.Validate(out string tutorialError), tutorialError);
            Assert.AreEqual(3, productionEnemy.PhaseCount);
            Assert.AreEqual(1, tutorial.EnemyDefinition.PhaseCount);

            for (int phaseIndex = 0; phaseIndex < productionEnemy.PhaseCount; phaseIndex++)
                Assert.AreEqual(0, productionEnemy.GetPhase(phaseIndex).DialogueCues.Count,
                    "Tutorial cues must not leak into the production enemy phases.");

            var cueIds = new HashSet<string>(StringComparer.Ordinal);
            bool hasTime = false;
            bool hasOverview = false;
            bool hasAttack = false;
            bool hasStun = false;
            bool hasReroll = false;
            bool hasShield = false;
            bool hasHit = false;
            bool hasHeal = false;
            bool hasIntroComplete = false;

            for (int cueIndex = 0; cueIndex < tutorial.Cues.Count; cueIndex++)
            {
                CombatDialogueCue cue = tutorial.Cues[cueIndex];
                Assert.IsTrue(cueIds.Add(cue.CueId), $"Duplicate cue '{cue.CueId}'.");
                Assert.IsTrue(cue.IsTutorial, $"Cue '{cue.CueId}' must be marked as tutorial content.");
                Assert.AreEqual(0f, cue.InstructionDuration,
                    $"Cue '{cue.CueId}' must wait for player interaction instead of a timeout.");
                hasTime |= cue.CueId == "tutorial-player-hit";
                hasOverview |= cue.CueId == "tutorial-overview" &&
                    cue.Trigger == CombatDialogueCueTrigger.DiceBatchReady &&
                    cue.TutorialFocus == CombatTutorialFocus.DiceAll;
                hasAttack |= cue.OneShotKey == "tutorial-attack";
                hasStun |= cue.CueId == "tutorial-stun-zone";
                hasReroll |= cue.CueId == "tutorial-reroll";
                hasShield |= cue.OneShotKey == "tutorial-shield";
                hasHit |= cue.CueId == "tutorial-player-hit";
                hasHeal |= cue.OneShotKey == "tutorial-heal";
                hasIntroComplete |= cue.OneShotKey == "tutorial-intro-complete" &&
                    cue.Trigger == CombatDialogueCueTrigger.AllDiceTypesCaught;
                if (cue.CueId == "tutorial-player-hit")
                    Assert.AreEqual(CombatTutorialFocus.Time, cue.TutorialFocus);
                if (cue.CueId == "tutorial-stun-zone")
                {
                    Assert.AreEqual(CombatTutorialFocus.StunZone, cue.TutorialFocus);
                    StringAssert.Contains("gieo lại", cue.Instruction.ToLowerInvariant());
                }
                if (cue.OneShotKey == "tutorial-attack" || cue.OneShotKey == "tutorial-shield" ||
                    cue.OneShotKey == "tutorial-heal")
                {
                    Assert.AreEqual(CombatTutorialFocus.Dice, cue.TutorialFocus);
                    Assert.AreEqual(CombatDialogueCueTrigger.DiceCaught, cue.Trigger,
                        $"{cue.OneShotKey} must be introduced only after that die is caught.");
                }
                for (int dialogueIndex = 0; dialogueIndex < cue.Sequence.Count; dialogueIndex++)
                {
                    Audere.Dialogue.DialogueData dialogue = cue.Sequence[dialogueIndex];
                    for (int lineIndex = 0; lineIndex < dialogue.Lines.Count; lineIndex++)
                        Assert.LessOrEqual(dialogue.Lines[lineIndex].Text.Length, 42,
                            $"{dialogue.name} line {lineIndex + 1} exceeds the bubble budget.");
                }
            }

            Assert.IsTrue(hasTime && hasOverview && hasAttack && hasStun && hasReroll &&
                          hasShield && hasHit && hasHeal && hasIntroComplete,
                "Production tutorial is missing a required combat concept.");
        }

        [Test]
        public void GameplayUiPrefab_TutorialIsDirectlyBound_BelowRetryOverlay()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(
                "Assets/_Audere/Prefabs/UI/GameplayUIRoot.prefab");
            try
            {
                Audere.Dialogue.GameplayUIRoot ui = root.GetComponent<Audere.Dialogue.GameplayUIRoot>();
                SerializedObject serialized = new SerializedObject(ui);
                CombatTutorialView tutorial = serialized.FindProperty("combatTutorial")
                    .objectReferenceValue as CombatTutorialView;
                Assert.IsNotNull(tutorial);
                Assert.IsTrue(tutorial.gameObject.activeSelf,
                    "CombatTutorialUI must stay active so it can run presentation coroutines.");
                Transform retry = root.transform.Find("CombatRetryUI");
                Assert.IsNotNull(retry);
                Assert.Less(tutorial.transform.GetSiblingIndex(), retry.GetSiblingIndex());
                RectTransform rect = tutorial.GetComponent<RectTransform>();
                Assert.AreEqual(Vector2.zero, rect.anchorMin);
                Assert.AreEqual(Vector2.one, rect.anchorMax);

                SerializedObject tutorialSerialized = new SerializedObject(tutorial);
                Assert.IsNotNull(tutorialSerialized.FindProperty("spotlightRoot").objectReferenceValue);
                Assert.IsNotNull(tutorialSerialized.FindProperty("dimTop").objectReferenceValue);
                Assert.IsNotNull(tutorialSerialized.FindProperty("dimBottom").objectReferenceValue);
                Assert.IsNotNull(tutorialSerialized.FindProperty("dimLeft").objectReferenceValue);
                Assert.IsNotNull(tutorialSerialized.FindProperty("dimRight").objectReferenceValue);
                Assert.IsNotNull(tutorialSerialized.FindProperty("attackDicePrefab").objectReferenceValue);
                Assert.IsNotNull(tutorialSerialized.FindProperty("shieldDicePrefab").objectReferenceValue);
                Assert.IsNotNull(tutorialSerialized.FindProperty("healDicePrefab").objectReferenceValue);
                RectTransform showcase = tutorial.transform.Find("Dice Showcase") as RectTransform;
                Assert.IsNotNull(showcase);
                Assert.AreEqual(new Vector2(520f, 180f), showcase.sizeDelta);

                Transform instructionTransform = tutorial.transform.Find("Tutorial Instruction");
                Assert.IsNotNull(instructionTransform);
                TMP_Text instruction = instructionTransform.GetComponent<TMP_Text>();
                Assert.IsNotNull(instruction);
                Assert.AreEqual("Assets/_Audere/AssetGame/Font/Mynerve-Regular SDF.asset",
                    AssetDatabase.GetAssetPath(instruction.font));
                Assert.AreEqual(40f, instruction.fontSize);
                Assert.AreEqual(FontStyles.Bold, instruction.fontStyle);
                Assert.IsNull(instructionTransform.GetComponent<Image>(),
                    "Tutorial instruction must remain a plain Scene-20-style text line.");
                Assert.IsNull(instructionTransform.GetComponent<Outline>(),
                    "Tutorial instruction must not recreate the old framed background.");

                tutorial.ForceHide();
                Assert.IsTrue(tutorial.gameObject.activeSelf,
                    "ForceHide must hide through CanvasGroup without disabling the coroutine host.");
                CanvasGroup group = tutorial.GetComponent<CanvasGroup>();
                Assert.AreEqual(0f, group.alpha);
                Assert.IsFalse(group.interactable);
                Assert.IsFalse(group.blocksRaycasts);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void KhoangLangPlaceholder_UsesAudereSpriteAsset()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(
                "Assets/_Audere/Prefabs/Combat/Enemies/Enemy_KhoangLang_PLACEHOLDER.prefab");
            try
            {
                Image renderer = root.GetComponentInChildren<Image>(true);
                Assert.IsNotNull(renderer);
                Assert.IsNotNull(renderer.sprite);
                Assert.AreEqual("Assets/_Audere/AssetGame/Audere/audere mid.aseprite",
                    AssetDatabase.GetAssetPath(renderer.sprite));
                CombatEnemyActor actor = root.GetComponent<CombatEnemyActor>();
                Assert.IsNotNull(actor);
                Assert.IsNotNull(actor.DamageAnchor);
                Assert.Greater(renderer.rectTransform.rect.width, 45f,
                    "UI placeholder must be authored at a visible canvas size.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }


        [Test]
        public void Shield_ClearRadius_IsThreeCatchCursorRadii()
        {
            Assert.AreEqual(150f, CombatDiceConstants.ShieldBulletClearRadius);
        }

        [Test]
        public void RestartFromBeginning_ResetsPhaseAndHealthWithoutReplacingAttempt()
        {
            CombatEnemyRuntime runtime = CreateRuntime(CombatPhasePolicy.PerPhaseHealth, 4,
                ("p1", 2, 0, 5f), ("p2", 3, 0, 5f));
            Assert.AreEqual(CombatEnemyProgression.PhaseBreak, runtime.ApplyDamage(99, out _));
            runtime.CompletePhaseBreak();
            int previousPhaseVersion = runtime.PhaseVersion;
            runtime.PauseForDialogue();

            runtime.RestartFromBeginning();

            Assert.AreEqual(0, runtime.PhaseIndex);
            Assert.AreEqual(2, runtime.CurrentHealth);
            Assert.AreEqual(CombatEnemyRuntimeState.Playing, runtime.State);
            Assert.Greater(runtime.PhaseVersion, previousPhaseVersion);
            Assert.AreEqual(7, runtime.SessionVersion);
        }

        [Test]
        public void CombatBoard_EnemyNameWrapsInsideItsImageAtFontSize57()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(
                "Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab");
            try
            {
                RectTransform nameRoot = root.transform.Find("Enemy/Name") as RectTransform;
                RectTransform image = nameRoot != null ? nameRoot.Find("Image") as RectTransform : null;
                TMP_Text text = nameRoot != null ? nameRoot.Find("Enemy Name")?.GetComponent<TMP_Text>() : null;
                Assert.IsNotNull(nameRoot);
                Assert.IsNotNull(image);
                Assert.IsNotNull(text);
                Assert.AreEqual(Vector2.zero, image.anchorMin);
                Assert.AreEqual(Vector2.one, image.anchorMax);
                Assert.AreEqual(new Vector2(420f, 120f), nameRoot.sizeDelta);
                Assert.AreEqual(57f, text.fontSize);
                Assert.AreEqual(TextWrappingModes.Normal, text.textWrappingMode);
                Assert.AreEqual(TextOverflowModes.Truncate, text.overflowMode);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void ClassroomTutorial_UsesSlowTimeAndComfortableFullAttemptDuration()
        {
            GameObject controllerObject = new GameObject("Tutorial Time Controller", typeof(CombatController));
            cleanup.Add(controllerObject);
            SerializedObject controller = new SerializedObject(controllerObject.GetComponent<CombatController>());
            Assert.AreEqual(.25f, controller.FindProperty("tutorialTimeScale").floatValue, .001f);

            CombatEncounterData encounter = AssetDatabase.LoadAssetAtPath<CombatEncounterData>(
                "Assets/_Audere/Data/Combat/CombatEncounter_D1_CLASSROOM_KHOANG_LANG.asset");
            Assert.IsNotNull(encounter);
            Assert.AreEqual(45f, encounter.EncounterDuration, .001f);
            Assert.IsNotNull(encounter.TutorialData);
            Assert.AreEqual(120f, encounter.TutorialData.PlayerTime, .001f);
            Assert.AreEqual(1, encounter.TutorialData.EnemyDefinition.PhaseCount);
            Assert.GreaterOrEqual(encounter.TutorialData.EnemyDefinition.GetPhase(0).MaxHealth, 30);
            Assert.AreEqual(3, encounter.TutorialData.OpeningDice.Count);
            Assert.AreEqual(CombatSymbol.Attack, encounter.TutorialData.OpeningDice[0]);
            Assert.AreEqual(CombatSymbol.Shield, encounter.TutorialData.OpeningDice[1]);
            Assert.AreEqual(CombatSymbol.Heal, encounter.TutorialData.OpeningDice[2]);
            Assert.AreEqual(3, encounter.EnemyDefinition.PhaseCount);
            Assert.AreEqual(2, encounter.EnemyDefinition.GetPhase(0).MaxHealth);
        }

        [Test]
        public void ClassroomTutorial_DamageCannotDefeatBeforeFullAttemptStarts()
        {
            MethodInfo clamp = typeof(CombatController).GetMethod(
                "ClampPlayerTimeAfterDamage",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(clamp);

            float tutorialResult = (float)clamp.Invoke(null, new object[] { 2f, 3f, false });
            float fullAttemptResult = (float)clamp.Invoke(null, new object[] { 2f, 3f, true });

            Assert.AreEqual(1f, tutorialResult, .001f);
            Assert.AreEqual(0f, fullAttemptResult, .001f);
        }

        [Test]
        public void LinearProjectileMove_FirstShotHasNoStartupDelay()
        {
            GameObject projectileObject = new GameObject("Immediate Projectile", typeof(RectTransform), typeof(CombatBulletView));
            cleanup.Add(projectileObject);
            LinearProjectilePatternMove move = Create<LinearProjectilePatternMove>();
            SerializedObject serialized = new SerializedObject(move);
            serialized.FindProperty("projectilePrefab").objectReferenceValue = projectileObject.GetComponent<CombatBulletView>();
            serialized.ApplyModifiedPropertiesWithoutUndo();

            ICombatMoveExecution execution = move.CreateExecution(
                new CombatMoveExecutionContext(null, null, new FixedRandom(.5f), 1, 1));
            FieldInfo cooldown = execution.GetType().GetField(
                "cooldown",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsNotNull(cooldown);
            Assert.AreEqual(0f, (float)cooldown.GetValue(execution), .001f);
        }

        [Test]
        public void LinearProjectileMove_ActorAnchorIsClampedInsideBattleBox()
        {
            Type executionType = typeof(LinearProjectilePatternMove).Assembly.GetType(
                "Audere.Combat.LinearProjectilePatternExecution");
            Assert.IsNotNull(executionType);
            MethodInfo clamp = executionType.GetMethod(
                "ClampActorAnchorToPlayArea",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(clamp);

            var rect = new Rect(-200f, -100f, 400f, 200f);
            Vector2 result = (Vector2)clamp.Invoke(null, new object[]
            {
                new Vector2(35f, 280f),
                rect,
                10f,
            });

            Assert.AreEqual(35f, result.x, .001f);
            Assert.AreEqual(90f, result.y, .001f);
        }

        [Test]
        public void EnemyHitRoutine_ToleratesActorBeingClearedMidFeedback()
        {
            GameObject boardObject = new GameObject("Hit Feedback Board", typeof(RectTransform), typeof(CombatBoardView));
            cleanup.Add(boardObject);
            CombatBoardView board = boardObject.GetComponent<CombatBoardView>();
            GameObject target = new GameObject("Enemy Visual", typeof(RectTransform));
            cleanup.Add(target);
            SetObject(board, "enemyVisual", target.transform);

            System.Collections.IEnumerator routine = board.PlayEnemyHit();
            Assert.IsTrue(routine.MoveNext());
            SetObject(board, "enemyVisual", null);
            Assert.DoesNotThrow(() => routine.MoveNext());
        }

        private CombatEnemyRuntime CreateRuntime(
            CombatPhasePolicy policy,
            int sharedHealth,
            params (string id, int hp, int threshold, float duration)[] phaseData)
        {
            GameObject boardObject = new GameObject("Test Board", typeof(RectTransform), typeof(CombatBoardView));
            cleanup.Add(boardObject);
            GameObject mountObject = new GameObject("Enemy Mount", typeof(RectTransform));
            mountObject.transform.SetParent(boardObject.transform, false);
            SetObject(boardObject.GetComponent<CombatBoardView>(), "enemyMount", mountObject.transform);

            GameObject actorObject = new GameObject("Test Actor", typeof(RectTransform), typeof(CombatEnemyActor));
            cleanup.Add(actorObject);
            CombatEnemyActor actor = actorObject.GetComponent<CombatEnemyActor>();
            SetObject(actor, "visualRoot", actorObject.transform);
            SetObject(actor, "projectileOrigin", actorObject.transform);
            SetObject(actor, "vfxAnchor", actorObject.transform);

            CombatMoveSet moveSet = CreateMoveSet(CombatMoveSelectionPolicy.OrderedLoop, (Create<NoopMove>(), 1f));
            CombatEnemyDefinition definition = Create<CombatEnemyDefinition>();
            SerializedObject serialized = new SerializedObject(definition);
            serialized.FindProperty("enemyId").stringValue = "test-enemy";
            serialized.FindProperty("displayName").stringValue = "Test";
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
                phase.FindPropertyRelative("duration").floatValue = phaseData[i].duration;
                phase.FindPropertyRelative("moveSet").objectReferenceValue = moveSet;
                phase.FindPropertyRelative("dialogueCues").arraySize = 0;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var runtime = new CombatEnemyRuntime(definition, boardObject.GetComponent<CombatBoardView>(), new FixedRandom(.5f), 7);
            runtime.Start();
            return runtime;
        }

        private CombatMoveSet CreateMoveSet(CombatMoveSelectionPolicy policy, params (CombatMoveDefinition move, float weight)[] data)
        {
            CombatMoveSet set = Create<CombatMoveSet>();
            SerializedObject serialized = new SerializedObject(set);
            serialized.FindProperty("selectionPolicy").enumValueIndex = (int)policy;
            SerializedProperty entries = serialized.FindProperty("entries");
            entries.arraySize = data.Length;
            for (int i = 0; i < data.Length; i++)
            {
                entries.GetArrayElementAtIndex(i).FindPropertyRelative("move").objectReferenceValue = data[i].move;
                entries.GetArrayElementAtIndex(i).FindPropertyRelative("weight").floatValue = data[i].weight;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return set;
        }

        private T Create<T>() where T : ScriptableObject
        {
            T value = ScriptableObject.CreateInstance<T>();
            cleanup.Add(value);
            return value;
        }

        private CombatRetryView CreateRetryView(out Button button)
        {
            GameObject root = new GameObject("Retry Test Root", typeof(RectTransform));
            cleanup.Add(root);
            root.SetActive(false);
            CombatRetryView view = root.AddComponent<CombatRetryView>();
            GameObject panel = new GameObject("Retry Panel", typeof(RectTransform));
            panel.transform.SetParent(root.transform, false);
            GameObject textObject = new GameObject("Retry Message", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(panel.transform, false);
            GameObject buttonObject = new GameObject("Retry Button", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(panel.transform, false);
            button = buttonObject.GetComponent<Button>();
            SetObject(view, "retryRoot", panel);
            SetObject(view, "messageText", textObject.GetComponent<TextMeshProUGUI>());
            SetObject(view, "retryButton", button);
            root.SetActive(true);
            return view;
        }

        private static void SetObject(UnityEngine.Object target, string property, UnityEngine.Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Missing field '{fieldName}'.");
            field.SetValue(target, value);
        }
    }
}
#endif
