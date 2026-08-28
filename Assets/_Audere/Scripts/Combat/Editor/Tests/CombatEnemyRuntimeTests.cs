#if UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Audere.Story.Presentation;
using Audere.Story.Steps;

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

        [TestCase(.01f)]
        [TestCase(.20f)]
        public void MoveLeadIn_SpendsRemainingActiveFrameInsteadOfDroppingIt(float leadIn)
        {
            var runtime = CreateRuntime(CombatPhasePolicy.PerPhaseHealth, 1, ("test", 2, 0, 5f));
            var data = new SerializedObject(runtime.CurrentMove);
            data.FindProperty("leadInDuration").floatValue = leadIn;
            data.ApplyModifiedPropertiesWithoutUndo();
            runtime.RestartFromBeginning();
            var active = typeof(CombatEnemyRuntime).GetField("activeMove", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNull(active.GetValue(runtime));
            runtime.PauseForDialogue();
            runtime.Tick(1f);
            Assert.IsNull(active.GetValue(runtime));
            runtime.ResumeFromDialogue();
            runtime.Tick(.25f);
            Assert.IsNotNull(active.GetValue(runtime));
            Assert.AreEqual(.25f, runtime.PhaseElapsed, .0001f);
            runtime.Cancel();
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
        public void SharedHealth_RequiredSpeechDefersProgressionWithoutExtraDamageOrPhaseSkip()
        {
            var runtime = CreateRuntime(CombatPhasePolicy.SharedHealthThresholds, 12,
                ("first", 1, 8, 5f), ("second", 1, 4, 5f), ("last", 1, 0, 5f));
            var cue = new CombatDialogueCue();
            SetField(cue, "cueId", "first-reply");
            SetField(cue, "requiredBeforePhaseAdvance", true);
            SetField(runtime.CurrentPhase, "dialogueCues", new[] { cue });
            Assert.AreEqual(CombatEnemyProgression.None, runtime.ApplyDamage(99, out int applied));
            Assert.AreEqual(4, applied);
            Assert.AreEqual(8, runtime.CurrentHealth);
            Assert.IsFalse(runtime.AcceptsDamage);
            runtime.Tick(.5f);
            Assert.AreEqual(CombatEnemyRuntimeState.Playing, runtime.State);
            Assert.AreEqual(.5f, runtime.PhaseElapsed, .001f);
            runtime.ApplyDamage(99, out applied);
            Assert.AreEqual(0, applied);
            runtime.MarkCueResolved(cue);
            runtime.PauseForDialogue();
            runtime.Tick(1f);
            Assert.AreEqual(CombatEnemyRuntimeState.PausedForDialogue, runtime.State);
            runtime.ResumeFromDialogue();
            runtime.Tick(.01f);
            Assert.AreEqual(CombatEnemyRuntimeState.TransitioningPhase, runtime.State);
            runtime.CompletePhaseBreak();
            Assert.AreEqual(1, runtime.PhaseIndex);
            Assert.AreEqual(8, runtime.CurrentHealth);
            Assert.IsTrue(runtime.AcceptsDamage);
            Assert.AreEqual(CombatEnemyProgression.PhaseBreak, runtime.ApplyDamage(4, out _));
            runtime.Cancel();
        }

        [Test]
        public void SharedHealth_FinalSpeechFinishesBeforeVictory_AndRestartResetsCueState()
        {
            var runtime = CreateRuntime(CombatPhasePolicy.SharedHealthThresholds, 4, ("last", 1, 0, 5f));
            var cue = new CombatDialogueCue();
            SetField(cue, "cueId", "final-reply");
            SetField(cue, "requiredBeforeVictory", true);
            SetField(runtime.CurrentPhase, "dialogueCues", new[] { cue });
            Assert.IsTrue(runtime.MarkCuePlayed(cue));
            Assert.AreEqual(CombatEnemyProgression.None, runtime.ApplyDamage(4, out _));
            runtime.Tick(1f);
            Assert.AreEqual(CombatEnemyRuntimeState.Playing, runtime.State);
            runtime.MarkCueResolved(cue);
            runtime.MarkCueResolved(cue);
            runtime.Tick(.01f);
            Assert.AreEqual(CombatEnemyRuntimeState.Completed, runtime.State);
            Assert.AreEqual(CombatEnemyProgression.None, runtime.ApplyDamage(4, out _));
            runtime.RestartFromBeginning();
            Assert.AreEqual(4, runtime.CurrentHealth);
            Assert.IsFalse(runtime.IsCueResolved(cue.CueId));
            Assert.IsTrue(runtime.MarkCuePlayed(cue));
            Assert.AreEqual(CombatEnemyProgression.None, runtime.ApplyDamage(4, out _));
            runtime.Cancel();
            runtime.MarkCueResolved(cue); // A late callback cannot resurrect a cancelled attempt.
            runtime.Tick(1f);
            Assert.AreEqual(CombatEnemyRuntimeState.Cancelled, runtime.State);
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
            Assert.AreEqual(1, productionEnemy.PhaseCount);
            Assert.AreEqual(6, productionEnemy.GetPhase(0).MaxHealth);
            Assert.AreEqual(3, productionEnemy.GetPhase(0).MoveSet.Count,
                "The single production phase must retain all three projectile patterns.");
            Assert.AreEqual(1, tutorial.EnemyDefinition.PhaseCount);
            Assert.AreEqual(1, tutorial.EnemyDefinition.GetPhase(0).MoveSet.Count,
                "The isolated tutorial should keep one predictable projectile pattern.");

            IReadOnlyList<CombatDialogueCue> productionCues = productionEnemy.GetPhase(0).DialogueCues;
            Assert.AreEqual(4, productionCues.Count,
                "The production phase must own opening, Side Sweep, anchor and 2 HP cues.");
            Assert.AreEqual(CombatDialoguePresentation.AutoCombatDialogue, productionCues[0].Presentation);
            Assert.AreEqual(CombatDialogueCueTrigger.MoveStarted, productionCues[1].Trigger);
            Assert.IsInstanceOf<ConvergingSideCorridorMove>(productionCues[1].TriggerMove);
            Assert.AreEqual(CombatDialogueCueTrigger.CueCompleted, productionCues[2].Trigger);
            Assert.IsTrue(productionCues[2].RequiredBeforeVictory);
            Assert.AreEqual(CombatDialoguePresentation.BackgroundTextField, productionCues[3].Presentation);
            Assert.AreEqual(2f, productionCues[3].TriggerValue);
            Audere.Dialogue.DialogueData openingDialogue = productionCues[0].Sequence[0];
            Assert.AreEqual(Audere.Dialogue.DialogueCharacterId.Audere, openingDialogue.LeftCharacter);
            Assert.AreEqual(Audere.Dialogue.DialogueCharacterId.KhoangLang, openingDialogue.RightCharacter);
            Assert.IsTrue(openingDialogue.Lines.All(line =>
                line.Speaker == Audere.Dialogue.DialogueSpeakerSide.Right));
            Audere.Dialogue.DialogueData anchorDialogue = productionCues[2].Sequence[0];
            Assert.AreEqual(Audere.Dialogue.DialogueCharacterId.Audere, anchorDialogue.LeftCharacter);
            Assert.AreEqual(Audere.Dialogue.DialogueCharacterId.Timor, anchorDialogue.RightCharacter);
            Assert.AreEqual(Audere.Dialogue.DialogueSpeakerSide.Left, anchorDialogue.Lines[0].Speaker);
            for (int cueIndex = 0; cueIndex < productionCues.Count; cueIndex++)
            for (int dialogueIndex = 0; dialogueIndex < productionCues[cueIndex].Sequence.Count; dialogueIndex++)
            for (int lineIndex = 0; lineIndex < productionCues[cueIndex].Sequence[dialogueIndex].Lines.Count; lineIndex++)
                Assert.LessOrEqual(productionCues[cueIndex].Sequence[dialogueIndex].Lines[lineIndex].Text.Length, 42,
                    $"Production cue '{productionCues[cueIndex].CueId}' exceeds the bubble budget.");

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
        public void ClassroomPreCombatDialogue_ShowsTremblingAndReturnsChoiceToAudere()
        {
            Audere.Dialogue.DialogueData dialogue = AssetDatabase.LoadAssetAtPath<Audere.Dialogue.DialogueData>(
                "Assets/_Audere/Data/Dialogue/Day1/Classroom/Dialogue_D1_CLASSROOM_TIMOR_INTERVENES.asset");
            Assert.IsNotNull(dialogue);
            Assert.AreEqual(Audere.Dialogue.DialogueCharacterId.Audere, dialogue.LeftCharacter);
            Assert.AreEqual(Audere.Dialogue.DialogueCharacterId.Timor, dialogue.RightCharacter);

            bool hasTremble = false;
            bool hasEscapeImpulse = false;
            bool hasSelfConfrontation = false;
            for (int lineIndex = 0; lineIndex < dialogue.Lines.Count; lineIndex++)
            {
                Audere.Dialogue.DialogueData.Line line = dialogue.Lines[lineIndex];
                Assert.LessOrEqual(line.Text.Length, 42,
                    $"Pre-combat line {lineIndex + 1} exceeds the dialogue bubble budget.");
                if (line.Speaker != Audere.Dialogue.DialogueSpeakerSide.Left) continue;
                hasTremble |= line.Text.Contains("run");
                hasEscapeImpulse |= line.Text.Contains("trốn");
                hasSelfConfrontation |= line.Text.Contains("tự đối diện");
            }

            Assert.IsTrue(hasTremble && hasEscapeImpulse && hasSelfConfrontation,
                "Audere's pre-combat beat must show her body, avoidance impulse, and own decision to face it.");
        }

        [Test]
        public void DialogueController_LegacyAudereRightDataIsMirroredToAudereLeft()
        {
            Audere.Dialogue.DialogueData data = ScriptableObject.CreateInstance<Audere.Dialogue.DialogueData>();
            cleanup.Add(data);
            Assert.AreEqual(Audere.Dialogue.DialogueCharacterId.Audere, data.LeftCharacter);
            Assert.AreEqual(Audere.Dialogue.DialogueCharacterId.Timor, data.RightCharacter);

            SerializedObject serialized = new SerializedObject(data);
            serialized.FindProperty("leftCharacter").enumValueIndex =
                (int)Audere.Dialogue.DialogueCharacterId.Timor;
            serialized.FindProperty("rightCharacter").enumValueIndex =
                (int)Audere.Dialogue.DialogueCharacterId.Audere;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            MethodInfo shouldMirror = typeof(Audere.Dialogue.DialogueController).GetMethod(
                "ShouldMirrorForAudere",
                BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo displaySide = typeof(Audere.Dialogue.DialogueController).GetMethod(
                "DisplaySide",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(shouldMirror);
            Assert.IsNotNull(displaySide);
            Assert.IsTrue((bool)shouldMirror.Invoke(null, new object[] { data }));
            Assert.AreEqual(
                Audere.Dialogue.DialogueSpeakerSide.Left,
                displaySide.Invoke(null, new object[]
                {
                    Audere.Dialogue.DialogueSpeakerSide.Right,
                    true,
                }));
        }

        [Test]
        public void AllAuthoredDialogueWithAudere_UsesLeftSlot()
        {
            string[] guids = AssetDatabase.FindAssets(
                "t:DialogueData",
                new[] { "Assets/_Audere/Data/Dialogue" });
            Assert.IsNotEmpty(guids);

            for (int index = 0; index < guids.Length; index++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[index]);
                Audere.Dialogue.DialogueData dialogue =
                    AssetDatabase.LoadAssetAtPath<Audere.Dialogue.DialogueData>(path);
                Assert.IsNotNull(dialogue, path);
                bool includesAudere =
                    dialogue.LeftCharacter == Audere.Dialogue.DialogueCharacterId.Audere ||
                    dialogue.RightCharacter == Audere.Dialogue.DialogueCharacterId.Audere;
                if (!includesAudere)
                    continue;

                Assert.AreEqual(
                    Audere.Dialogue.DialogueCharacterId.Audere,
                    dialogue.LeftCharacter,
                    $"'{path}' must author Audere on the Left slot.");
                Assert.AreNotEqual(
                    Audere.Dialogue.DialogueCharacterId.Audere,
                    dialogue.RightCharacter,
                    $"'{path}' must place Audere's counterpart on the Right slot.");
            }
        }

        [TestCase("Assets/_Audere/Prefabs/Puzzle/Actors/Player.prefab")]
        [TestCase("Assets/_Audere/Prefabs/Story/Characters/Bianca.prefab")]
        [TestCase("Assets/_Audere/Prefabs/Story/Characters/Teacher.prefab")]
        public void ActorPrefab_UsesPlayerFiveAndGroundedShadowFour(string prefabPath)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                SpriteRenderer body = root.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(body, $"'{prefabPath}' needs a root actor renderer.");
                Assert.AreEqual("Player", body.sortingLayerName, prefabPath);
                Assert.AreEqual(5, body.sortingOrder, prefabPath);

                SpriteRenderer shadow = root.GetComponentsInChildren<SpriteRenderer>(true)
                    .FirstOrDefault(candidate => candidate != body);
                Assert.IsNotNull(shadow, $"'{prefabPath}' needs a grounded shadow renderer.");
                Assert.AreEqual("Player", shadow.sortingLayerName, prefabPath);
                Assert.AreEqual(4, shadow.sortingOrder, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
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

                Assert.IsNotNull(ui.Dialogue,
                    "Auto combat dialogue must reuse the standard DialogueController.");
                Assert.IsTrue(Enum.IsDefined(
                    typeof(Audere.Dialogue.DialoguePlaybackMode),
                    Audere.Dialogue.DialoguePlaybackMode.AutoAdvanceNoInput));
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
            Assert.AreEqual(1, encounter.EnemyDefinition.PhaseCount);
            Assert.AreEqual(6, encounter.EnemyDefinition.GetPhase(0).MaxHealth);
            Assert.AreEqual(3, encounter.EnemyDefinition.GetPhase(0).MoveSet.Count);
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
        public void KhoangLang_RequiredAnchorPreventsOnlyEarlyLethalDamage()
        {
            CombatEnemyDefinition definition = AssetDatabase.LoadAssetAtPath<CombatEnemyDefinition>(
                "Assets/_Audere/Data/Combat/Enemies/Enemy_KhoangLang.asset");
            Assert.IsNotNull(definition);
            CombatDialogueCue required = null;
            for (int i = 0; i < definition.GetPhase(0).DialogueCues.Count; i++)
                if (definition.GetPhase(0).DialogueCues[i].RequiredBeforeVictory)
                    required = definition.GetPhase(0).DialogueCues[i];
            Assert.IsNotNull(required);

            GameObject boardObject = new GameObject("Narrative Gate Board", typeof(RectTransform), typeof(CombatBoardView));
            cleanup.Add(boardObject);
            GameObject mountObject = new GameObject("Enemy Mount", typeof(RectTransform));
            mountObject.transform.SetParent(boardObject.transform, false);
            SetObject(boardObject.GetComponent<CombatBoardView>(), "enemyMount", mountObject.transform);
            var runtime = new CombatEnemyRuntime(
                definition, boardObject.GetComponent<CombatBoardView>(), new FixedRandom(.5f), 19);
            runtime.Start();

            Assert.AreEqual(CombatEnemyProgression.None, runtime.ApplyDamage(99, out int guardedDamage));
            Assert.AreEqual(5, guardedDamage);
            Assert.AreEqual(1, runtime.CurrentHealth);
            runtime.MarkCueResolved(required);
            Assert.AreEqual(CombatEnemyProgression.Victory, runtime.ApplyDamage(1, out int finalDamage));
            Assert.AreEqual(1, finalDamage);
        }

        [Test]
        public void CombatBoard_AnxietyLayerIsBoundBehindGameplay()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(
                "Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab");
            try
            {
                CombatBoardView board = root.GetComponent<CombatBoardView>();
                SerializedObject serialized = new SerializedObject(board);
                RectTransform viewport = serialized.FindProperty("combatViewport")
                    .objectReferenceValue as RectTransform;
                CombatAnxietyTextFieldView field = serialized.FindProperty("anxietyTextField")
                    .objectReferenceValue as CombatAnxietyTextFieldView;
                Transform diceField = root.transform.Find("Dice Field");
                Transform airborneDice = root.transform.Find("Airborne Dice Overlay");
                Transform enemy = root.transform.Find("Enemy");
                Assert.IsNotNull(viewport);
                Assert.IsNotNull(field);
                Assert.IsNotNull(diceField);
                Assert.IsNotNull(airborneDice);
                Assert.IsNotNull(enemy);
                Assert.AreEqual(0, viewport.GetSiblingIndex(),
                    "Anxiety text must stay behind the entire combat board.");
                Assert.Less(viewport.GetSiblingIndex(), airborneDice.GetSiblingIndex(),
                    "Airborne dice must stay readable above anxiety text.");
                Assert.Less(viewport.GetSiblingIndex(), enemy.GetSiblingIndex(),
                    "Enemy presentation must stay readable above anxiety text.");
                Assert.AreSame(viewport, field.transform.parent);
                RectTransform fieldRect = field.transform as RectTransform;
                Assert.AreEqual(Vector2.zero, fieldRect.anchorMin);
                Assert.AreEqual(Vector2.one, fieldRect.anchorMax);
                Assert.IsFalse(field.gameObject.activeSelf);
                SerializedObject fieldSerialized = new SerializedObject(field);
                Assert.AreEqual(384, fieldSerialized.FindProperty("labelCount").intValue);
                Assert.AreEqual(12, fieldSerialized.FindProperty("simulationFramesPerSecond").intValue);
                Assert.AreEqual(.65f, fieldSerialized.FindProperty("fadeDuration").floatValue, .001f);
                field.Show(new[] { "Đừng nhìn xuống", "Mình vẫn ở đây" }, 41);
                Assert.AreEqual(384, field.transform.childCount,
                    "The anxiety field must densely cover the camera background.");
                foreach (TMP_Text label in field.GetComponentsInChildren<TMP_Text>(true))
                {
                    Assert.IsFalse(label.text.Contains(" ") || label.text.Contains("\n"),
                        "Each anxiety label must remain one isolated word, never a phrase or sentence.");
                    float z = Mathf.Repeat(label.rectTransform.localEulerAngles.z, 360f);
                    bool axisAligned = Mathf.Abs(z) < .01f || Mathf.Abs(z - 90f) < .01f ||
                                       Mathf.Abs(z - 270f) < .01f;
                    Assert.IsTrue(axisAligned, "Anxiety words must be horizontal or vertical only.");
                }
                field.Tick(.1f);
                field.ForceHide();
                Assert.IsFalse(field.gameObject.activeSelf);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void ClassroomScene_BindsPolishedEncounterWithoutMissingScripts()
        {
            const string path = "Assets/_Audere/Scenes/30_Classroom.unity";
            Scene scene = SceneManager.GetSceneByPath(path);
            bool openedForTest = !scene.IsValid() || !scene.isLoaded;
            if (openedForTest)
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                CombatStep step = null;
                int missingScripts = 0;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    missingScripts += CountMissingScripts(root);
                    if (step == null)
                        step = root.GetComponentsInChildren<CombatStep>(true)
                            .FirstOrDefault(candidate => candidate.name == "210_PlayKhoangLangPrototype");
                }
                Assert.AreEqual(0, missingScripts, "Scene 30 must not contain missing MonoBehaviours.");
                Assert.IsNotNull(step);
                Assert.AreEqual(
                    "Assets/_Audere/Data/Combat/CombatEncounter_D1_CLASSROOM_KHOANG_LANG.asset",
                    AssetDatabase.GetAssetPath(step.CombatEncounterData));
                Assert.IsNotNull(step.CombatController);
                Assert.IsNotNull(step.CombatController.BoardView);
            }
            finally
            {
                if (openedForTest && scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void ClassroomPostCombat_IsVictoryGatedAndActorStagingIsDirectlyBound()
        {
            const string path = "Assets/_Audere/Scenes/30_Classroom.unity";
            Scene scene = SceneManager.GetSceneByPath(path);
            bool openedForTest = !scene.IsValid() || !scene.isLoaded;
            if (openedForTest)
                scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                CombatStep combat = null;
                StoryIllustrationStep illustration = null;
                SpriteGroupFadeStep fade = null;
                List<CharacterMotionStep> departureHops = new List<CharacterMotionStep>();
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (CombatStep candidate in root.GetComponentsInChildren<CombatStep>(true))
                        if (candidate.name == "210_PlayKhoangLangPrototype") combat = candidate;
                    foreach (StoryIllustrationStep candidate in root.GetComponentsInChildren<StoryIllustrationStep>(true))
                        if (candidate.name == "280_ShowRegistrationSheet") illustration = candidate;
                    foreach (SpriteGroupFadeStep candidate in root.GetComponentsInChildren<SpriteGroupFadeStep>(true))
                        if (candidate.name == "390_BiancaFadesOut") fade = candidate;
                    departureHops.AddRange(root.GetComponentsInChildren<CharacterMotionStep>(true)
                        .Where(candidate => candidate.name.StartsWith("310_BiancaHopsDeparture") ||
                                            candidate.name.StartsWith("340_BiancaHopsDeparture") ||
                                            candidate.name.StartsWith("370_BiancaHopsDeparture")));
                }

                Assert.IsNotNull(combat);
                Assert.AreEqual(CombatResultBehaviour.Complete, combat.VictoryBehaviour);
                Assert.AreEqual(CombatResultBehaviour.Retry, combat.DefeatBehaviour);
                Assert.IsNotNull(illustration);
                Assert.IsNotNull(illustration.OverlayView);
                Assert.IsNotNull(fade);
                Assert.GreaterOrEqual(fade.Renderers.Length, 2);
                Assert.AreEqual(3, departureHops.Count);
                departureHops = departureHops.OrderBy(step => step.TargetTransform.position.x).ToList();
                for (int index = 0; index < departureHops.Count; index++)
                {
                    Assert.IsNotNull(departureHops[index].Actor);
                    Assert.IsNotNull(departureHops[index].ActorRenderer);
                    Assert.IsNotNull(departureHops[index].GroundedShadow);
                    if (index > 0)
                        Assert.Greater(
                            departureHops[index].TargetTransform.position.x,
                            departureHops[index - 1].TargetTransform.position.x);
                }
            }
            finally
            {
                if (openedForTest && scene.IsValid() && scene.isLoaded)
                    EditorSceneManager.CloseScene(scene, true);
            }
        }

        [Test]
        public void RegistrationOverlayAndEveningScene_AreReusableAndBuildListed()
        {
            const string overlayPath =
                "Assets/_Audere/Prefabs/Story/Overlays/StoryRegistrationOverlay.prefab";
            GameObject overlay = PrefabUtility.LoadPrefabContents(overlayPath);
            try
            {
                Assert.AreEqual(0, CountMissingScripts(overlay));
                StoryIllustrationOverlayView view = overlay.GetComponent<StoryIllustrationOverlayView>();
                Assert.IsNotNull(view);
                Assert.IsNotNull(view.Caption);
                Assert.AreEqual("Phiếu đăng ký hoàn thành", view.Caption.text);
                Canvas canvas = overlay.GetComponent<Canvas>();
                Assert.IsNotNull(canvas);
                Assert.AreEqual(RenderMode.ScreenSpaceOverlay, canvas.renderMode);
                Assert.AreEqual(1100, canvas.sortingOrder);

                GameObject owner = new GameObject("Illustration Test Owner");
                cleanup.Add(owner);
                int dismissed = 0;
                Assert.IsTrue(view.Show(owner, () => dismissed++));
                Button button = overlay.GetComponentInChildren<Button>(true);
                Assert.IsNotNull(button);
                button.onClick.Invoke();
                button.onClick.Invoke();
                Assert.AreEqual(1, dismissed, "Double click must resolve the overlay only once.");
                Assert.IsFalse(view.IsShowing);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(overlay);
            }

            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Audere/Prefabs/Story/Overlays/RegistrationSheet_PLACEHOLDER.prefab"));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(
                "Assets/_Audere/Scenes/40_Evening.unity"));
            Assert.IsTrue(EditorBuildSettings.scenes.Any(scene =>
                scene.enabled && scene.path == "Assets/_Audere/Scenes/40_Evening.unity"));
        }

        [Test]
        public void PolishedCombatPrefabs_HaveNoMissingScripts()
        {
            string[] paths =
            {
                "Assets/_Audere/Prefabs/UI/GameplayUIRoot.prefab",
                "Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab",
                "Assets/_Audere/Prefabs/Combat/Enemies/Enemy_KhoangLang_PLACEHOLDER.prefab",
            };
            for (int i = 0; i < paths.Length; i++)
            {
                GameObject root = PrefabUtility.LoadPrefabContents(paths[i]);
                try { Assert.AreEqual(0, CountMissingScripts(root), $"Missing script in {paths[i]}."); }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
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

        [Test]
        public void AttackHitVfx_ConvertsAsepritePixelsAndSortsAboveCombatCanvas()
        {
            GameObject canvasObject = new GameObject(
                "Combat Canvas",
                typeof(RectTransform),
                typeof(Canvas));
            cleanup.Add(canvasObject);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 20;

            GameObject boardObject = new GameObject(
                "Combat Board",
                typeof(RectTransform),
                typeof(CombatBoardView));
            boardObject.transform.SetParent(canvasObject.transform, false);
            CombatBoardView board = boardObject.GetComponent<CombatBoardView>();
            GameObject anchorObject = new GameObject("VFX Anchor", typeof(RectTransform));
            anchorObject.transform.SetParent(boardObject.transform, false);
            SetObject(board, "vfxRoot", anchorObject.transform);

            GameObject scratch = new GameObject(
                "Scratch",
                typeof(SpriteRenderer),
                typeof(UnityEngine.Rendering.SortingGroup));
            scratch.transform.SetParent(anchorObject.transform, false);
            MethodInfo configure = typeof(CombatBoardView).GetMethod(
                "ConfigureAttackHitVfxInstance",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.IsNotNull(configure);
            configure.Invoke(board, new object[] { scratch });

            Assert.AreEqual(300f, scratch.transform.localScale.x, .001f);
            Assert.AreEqual(21, scratch.GetComponent<SpriteRenderer>().sortingOrder);
            Assert.AreEqual(21, scratch.GetComponent<UnityEngine.Rendering.SortingGroup>().sortingOrder);
        }

        [Test]
        public void CombatBoard_AttackScratchUsesSharedEnemyVfxRootAndAnimatedAsset()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(
                "Assets/_Audere/Prefabs/Combat/World/CombatBoard.prefab");
            try
            {
                CombatBoardView board = root.GetComponent<CombatBoardView>();
                RectTransform enemyMount = root.transform.Find("Enemy/Enemy Mount") as RectTransform;
                RectTransform vfxRoot = root.transform.Find("Enemy/VFX") as RectTransform;
                SerializedObject serialized = new SerializedObject(board);
                Transform boundVfxRoot = serialized.FindProperty("vfxRoot").objectReferenceValue as Transform;
                GameObject scratchAsset = AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/_Audere/AssetGame/Vfx/scratch.aseprite");

                Assert.IsNotNull(enemyMount);
                Assert.IsNotNull(vfxRoot);
                Assert.AreSame(vfxRoot, boundVfxRoot);
                Assert.AreEqual(enemyMount.parent, vfxRoot.parent);
                Assert.AreEqual(enemyMount.anchoredPosition, vfxRoot.anchoredPosition);
                Assert.AreEqual(enemyMount.sizeDelta, vfxRoot.sizeDelta);
                Assert.IsNotNull(scratchAsset);

                Animator animator = scratchAsset.GetComponentInChildren<Animator>(true);
                Assert.IsNotNull(animator);
                Assert.IsNotNull(animator.runtimeAnimatorController);
                Assert.IsNotEmpty(animator.runtimeAnimatorController.animationClips);

                enemyMount.anchoredPosition += new Vector2(13f, -9f);
                CombatEnemyActor actorPrefab = AssetDatabase.LoadAssetAtPath<CombatEnemyActor>(
                    "Assets/_Audere/Prefabs/Combat/Enemies/Enemy_KhoangLang_PLACEHOLDER.prefab");
                Assert.IsNotNull(actorPrefab);
                CombatEnemyActor preview = UnityEngine.Object.Instantiate(actorPrefab, enemyMount);
                preview.name = actorPrefab.name + "__SCENE_AUTHORED";
                RectTransform previewRect = preview.transform as RectTransform;
                previewRect.anchoredPosition = new Vector2(17f, -11f);
                previewRect.sizeDelta = new Vector2(230f, 190f);
                previewRect.localRotation = Quaternion.Euler(0f, 0f, 6f);
                previewRect.localScale = Vector3.one * 1.15f;
                serialized.FindProperty("authoredEnemyActor").objectReferenceValue = preview;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                CombatEnemyActor runtimeActor = board.SpawnEnemyActor(actorPrefab, 31);
                Assert.IsNotNull(runtimeActor);
                Assert.AreSame(preview, runtimeActor,
                    "Combat must use the exact scene-authored actor instead of cloning a runtime copy.");
                RectTransform runtimeRect = runtimeActor.transform as RectTransform;
                Assert.AreEqual(previewRect.anchoredPosition, runtimeRect.anchoredPosition);
                Assert.AreEqual(previewRect.sizeDelta, runtimeRect.sizeDelta);
                Assert.AreEqual(previewRect.localRotation, runtimeRect.localRotation);
                Assert.AreEqual(previewRect.localScale, runtimeRect.localScale);

                board.ClearEnemyActor();
                Assert.IsFalse(preview.gameObject.activeSelf,
                    "Cleanup should hide, not destroy, the scene-authored actor.");

                Assert.AreEqual(
                    enemyMount.anchoredPosition,
                    vfxRoot.anchoredPosition,
                    "Runtime scene overrides on Enemy Mount must be copied to the shared VFX root.");
                serialized.Update();
                Assert.AreSame(
                    vfxRoot,
                    serialized.FindProperty("vfxRoot").objectReferenceValue as Transform,
                    "Spawning an enemy must not replace the board-owned scratch VFX root.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        [Test]
        public void CombatBoard_HorizontalLayoutStaysInsideFrameAndRestoresAuthoredLayout()
        {
            GameObject boardObject = new GameObject("Shifting Board", typeof(RectTransform), typeof(CombatBoardView));
            cleanup.Add(boardObject);
            GameObject frameObject = new GameObject("Frame", typeof(RectTransform));
            frameObject.transform.SetParent(boardObject.transform, false);
            RectTransform frame = frameObject.GetComponent<RectTransform>();
            frame.sizeDelta = new Vector2(620f, 500f);
            GameObject fieldObject = new GameObject("Dice Field", typeof(RectTransform));
            fieldObject.transform.SetParent(boardObject.transform, false);
            RectTransform field = fieldObject.GetComponent<RectTransform>();
            field.anchoredPosition = new Vector2(0f, 9f);
            field.sizeDelta = new Vector2(580f, 420f);
            GameObject airborneObject = new GameObject("Airborne Dice Overlay", typeof(RectTransform));
            airborneObject.transform.SetParent(boardObject.transform, false);
            RectTransform airborne = airborneObject.GetComponent<RectTransform>();
            airborne.anchoredPosition = new Vector2(0f, 20f);
            airborne.sizeDelta = new Vector2(580f, 420f);

            CombatBoardView board = boardObject.GetComponent<CombatBoardView>();
            SetObject(board, "battleBoxFrame", frame);
            SetObject(board, "playArea", field);
            SetObject(board, "airborneDiceRoot", airborne);

            board.SetBattleBoxHorizontalLayout(.5f, 1f);
            Assert.AreEqual(290f, field.rect.width, .001f);
            Assert.AreEqual(165f, field.anchoredPosition.x, .001f);
            Assert.AreEqual(310f, field.anchoredPosition.x + field.rect.width * .5f, .001f,
                "Dice Field right edge must never exceed Frame right edge.");
            Assert.AreEqual(290f, airborne.rect.width, .001f);
            Assert.AreEqual(165f, airborne.anchoredPosition.x, .001f);
            Assert.AreEqual(.5f, board.BattleBoxWidthFraction, .001f);
            Assert.AreEqual(1f, board.BattleBoxNormalizedX, .001f);

            board.ResetBattleBoxLayout();
            Assert.AreEqual(new Vector2(0f, 9f), field.anchoredPosition);
            Assert.AreEqual(new Vector2(580f, 420f), field.sizeDelta);
            Assert.AreEqual(new Vector2(0f, 20f), airborne.anchoredPosition);
            Assert.AreEqual(new Vector2(580f, 420f), airborne.sizeDelta);
            Assert.AreEqual(1f, board.BattleBoxWidthFraction, .001f);
            Assert.AreEqual(0f, board.BattleBoxNormalizedX, .001f);
        }

        [Test]
        public void ShiftingBattleBoxMove_CancelRestoresFieldDeterministically()
        {
            GameObject boardObject = new GameObject("Shifting Move Board", typeof(RectTransform), typeof(CombatBoardView));
            cleanup.Add(boardObject);
            GameObject frameObject = new GameObject("Frame", typeof(RectTransform));
            frameObject.transform.SetParent(boardObject.transform, false);
            RectTransform frame = frameObject.GetComponent<RectTransform>();
            frame.sizeDelta = new Vector2(620f, 500f);
            GameObject fieldObject = new GameObject("Dice Field", typeof(RectTransform));
            fieldObject.transform.SetParent(boardObject.transform, false);
            RectTransform field = fieldObject.GetComponent<RectTransform>();
            field.sizeDelta = new Vector2(580f, 420f);
            CombatBoardView board = boardObject.GetComponent<CombatBoardView>();
            SetObject(board, "battleBoxFrame", frame);
            SetObject(board, "playArea", field);

            ShiftingBattleBoxMove move = Create<ShiftingBattleBoxMove>();
            SerializedObject serialized = new SerializedObject(move);
            serialized.FindProperty("duration").floatValue = 4f;
            serialized.FindProperty("telegraphDuration").floatValue = .1f;
            serialized.FindProperty("squeezeDuration").floatValue = .4f;
            serialized.FindProperty("holdDuration").floatValue = .4f;
            serialized.FindProperty("returnDuration").floatValue = .4f;
            SerializedProperty poses = serialized.FindProperty("poses");
            poses.arraySize = 1;
            poses.GetArrayElementAtIndex(0).FindPropertyRelative("widthFraction").floatValue = .5f;
            poses.GetArrayElementAtIndex(0).FindPropertyRelative("normalizedX").floatValue = -1f;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            ICombatMoveExecution execution = move.CreateExecution(
                new CombatMoveExecutionContext(board, null, new FixedRandom(.5f), 3, 7));
            execution.Tick(.3f);
            Assert.Less(board.BattleBoxWidthFraction, 1f);
            Assert.Less(field.rect.width, 580f);
            Assert.GreaterOrEqual(field.anchoredPosition.x - field.rect.width * .5f, -310f - .001f);
            Assert.LessOrEqual(field.anchoredPosition.x + field.rect.width * .5f, 310f + .001f);

            execution.Cancel();
            Assert.AreEqual(1f, board.BattleBoxWidthFraction, .001f);
            Assert.AreEqual(580f, field.rect.width, .001f);
            Assert.AreEqual(Vector2.zero, field.anchoredPosition);
            Assert.IsTrue(execution.IsComplete);
        }



        private sealed class FiniteSpecialMove : CombatMoveDefinition
        {
            public override ICombatMoveExecution CreateExecution(CombatMoveExecutionContext context) => new Execution();
            private sealed class Execution : ICombatMoveExecution
            {
                private float elapsed;
                public bool IsComplete => elapsed >= .5f;
                public void Tick(float dt) { elapsed += dt; }
                public void Cancel() { elapsed = 1f; }
            }
        }

        [Test]
        public void DiceBatchBudget_CapturedAttacksAndRerollsCannotExceedTwo()
        {
            var active = new List<CombatSymbol>();
            for (int i = 0; i < 3; i++)
                active.Add(CombatDiceBatchBudget.Roll(2, active.Count(x => x == CombatSymbol.Attack), 0f));
            Assert.AreEqual(2, active.Count(x => x == CombatSymbol.Attack));
            int captured = 0;
            for (int choice = 0; choice < 3; choice++)
            {
                for (int reroll = 0; reroll < 40; reroll++)
                {
                    int selected = reroll % active.Count;
                    int reserved = captured + active.Where((_, i) => i != selected).Count(x => x == CombatSymbol.Attack);
                    active[selected] = CombatDiceBatchBudget.Roll(2, reserved, 0f);
                    Assert.LessOrEqual(captured + active.Count(x => x == CombatSymbol.Attack), 2);
                }
                if (active[0] == CombatSymbol.Attack) captured++;
                active.RemoveAt(0);
            }
            Assert.AreEqual(2, captured);
            Assert.AreEqual(CombatSymbol.Attack, CombatDiceBatchBudget.Roll(2, 0, 0f), "Fresh batch resets the budget.");
            Assert.AreEqual(CombatSymbol.Attack, CombatDiceBatchBudget.Roll(0, 99, 0f), "Legacy encounters remain unrestricted.");
        }

        [Test]
        public void WrongBox_SixtyPercentExplodes_AndTwoSuccessesNeedNotBeConsecutive()
        {
            var progress = new CombatChoiceRoundState();
            Assert.IsFalse(progress.Resolve(.59999f, .6f));
            Assert.IsTrue(progress.Resolve(.6f, .6f));
            Assert.IsFalse(progress.Resolve(.01f, .6f));
            Assert.AreEqual(1, progress.Successes);
            Assert.IsTrue(progress.Resolve(.99f, .6f));
            Assert.AreEqual(2, progress.Successes);
        }

        [Test]
        public void ReturningOrbit_TravelsBoardAndRetracesBeforePoolReset()
        {
            var rect = new Rect(-250, -170, 500, 340);
            var start = ReturningOrbitMove.EvaluatePosition(rect, 0f, .4f, 1f);
            Assert.Less(Vector2.Distance(start, ReturningOrbitMove.EvaluatePosition(rect, 1f, .4f, 1f)), .001f);
            for (int i = 0; i <= 100; i++)
            {
                float t = i / 100f;
                var point = ReturningOrbitMove.EvaluatePosition(rect, t, .4f, 1f);
                Assert.IsTrue(rect.Contains(point));
                Assert.Less(Vector2.Distance(point, ReturningOrbitMove.EvaluatePosition(rect, 1f - t, .4f, 1f)), .001f);
            }
            var go = new GameObject("Returning bullet", typeof(RectTransform), typeof(CombatBulletView));
            cleanup.Add(go);
            var bullet = go.GetComponent<CombatBulletView>();
            bullet.Setup(null, Vector2.zero, Vector2.zero, 4, 2, .5f);
            bullet.ConfigureReturningOrbit(rect, 2f, 0f, 1f);
            bullet.TickMovement(rect, .2f);
            Assert.IsFalse(bullet.CollisionActive);
            bullet.TickMovement(rect, .4f);
            Assert.IsTrue(bullet.CollisionActive);
            bullet.ReturnToPool();
            bullet.Setup(null, Vector2.zero, Vector2.right * 10f, 5, 1);
            bullet.TickMovement(rect, .1f);
            Assert.AreEqual(1f, bullet.RectTransform.anchoredPosition.x, .001f, "Pooling clears the curved trajectory.");
        }

        [Test]
        public void SharedHealth_SpecialHoldsHp_ThenResumesAndWins()
        {
            CombatEnemyRuntime runtime = CreateRuntime(CombatPhasePolicy.SharedHealthThresholds, 10,
                ("normal", 10, 6, 1f), ("special", 10, 4, 1f), ("normal2", 10, 2, 1f), ("final", 10, 0, 1f));
            var definition = (CombatEnemyDefinition)typeof(CombatEnemyRuntime).GetField("definition", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(runtime);
            var so = new SerializedObject(definition);
            var special = so.FindProperty("phases").GetArrayElementAtIndex(1);
            special.FindPropertyRelative("sharedExitThreshold").intValue = 6;
            special.FindPropertyRelative("advanceOnMoveComplete").boolValue = true;
            special.FindPropertyRelative("spawnDice").boolValue = false;
            special.FindPropertyRelative("moveSet").objectReferenceValue = CreateMoveSet(CombatMoveSelectionPolicy.OrderedLoop, (Create<FiniteSpecialMove>(), 1f));
            so.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsTrue(definition.Validate(out string error), error);
            Assert.AreEqual(CombatEnemyProgression.PhaseBreak, runtime.ApplyDamage(99, out _));
            runtime.CompletePhaseBreak();
            Assert.AreEqual(6, runtime.CurrentHealth);
            Assert.IsFalse(runtime.ShouldSpawnDice);
            Assert.AreEqual(CombatEnemyProgression.None, runtime.ApplyDamage(99, out int damage));
            Assert.AreEqual(0, damage);
            runtime.PauseForDialogue(); runtime.Tick(10f);
            Assert.AreEqual(CombatEnemyRuntimeState.PausedForDialogue, runtime.State);
            runtime.ResumeFromDialogue(); runtime.Tick(.6f);
            Assert.AreEqual(CombatEnemyRuntimeState.TransitioningPhase, runtime.State);
            runtime.CompletePhaseBreak();
            Assert.AreEqual(6, runtime.CurrentHealth);
            Assert.IsTrue(runtime.ShouldSpawnDice);
            Assert.AreEqual(CombatEnemyProgression.PhaseBreak, runtime.ApplyDamage(99, out _));
            runtime.CompletePhaseBreak();
            Assert.AreEqual(CombatEnemyProgression.Victory, runtime.ApplyDamage(99, out _));
            Assert.AreEqual(0, runtime.CurrentHealth);
        }

        [Test]
        public void RepeatedMoveCue_DoesNotConsumeOneShotIdentity()
        {
            var runtime = CreateRuntime(CombatPhasePolicy.PerPhaseHealth, 1, ("normal", 2, 0, 1f));
            var cue = JsonUtility.FromJson<CombatDialogueCue>("{\"cueId\":\"repeat\",\"repeatOnTrigger\":true}");
            Assert.IsTrue(runtime.MarkCuePlayed(cue));
            Assert.IsTrue(runtime.MarkCuePlayed(cue));
            var once = JsonUtility.FromJson<CombatDialogueCue>("{\"cueId\":\"once\"}");
            Assert.IsTrue(runtime.MarkCuePlayed(once));
            Assert.IsFalse(runtime.MarkCuePlayed(once));
        }


        [Test]
        public void BiancaProduction_UsesExactHpGatesArtAndVictoryContinuation()
        {
            var data = AssetDatabase.LoadAssetAtPath<CombatEncounterData>(Audere.EditorTools.BiancaCombatAuthoring.EncounterPath);
            Assert.IsNotNull(data);
            Assert.AreEqual(90f, data.EncounterDuration);
            Assert.AreEqual(3, data.DicePerBatch);
            Assert.AreEqual(2, data.MaximumAttacksPerBatch);
            var enemy = data.EnemyDefinition;
            Assert.IsTrue(enemy.Validate(out string error), error);
            CollectionAssert.AreEqual(new[] { 6, 6, 2, 2, 0 }, enemy.Phases.Select(p => p.SharedExitThreshold).ToArray());
            CollectionAssert.AreEqual(new[] { true, false, true, false, true }, enemy.Phases.Select(p => p.SpawnDice).ToArray());
            for (int p = 0; p < enemy.PhaseCount; p++)
            foreach (var cue in enemy.GetPhase(p).DialogueCues)
            foreach (var dialogue in cue.Sequence)
            {
                Assert.AreEqual(Audere.Dialogue.DialogueCharacterId.Audere, dialogue.LeftCharacter);
                Assert.IsNotNull(dialogue.LeftPortraitOverride);
                Assert.IsTrue(dialogue.Lines.All(l => l.Text.Length <= 42), dialogue.name);
            }
            var bullet = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Audere/Prefabs/Combat/Bullets/Bullet_Bianca_Returning.prefab");
            Assert.AreEqual("Assets/_Audere/AssetGame/Item/dan_bianca.aseprite",
                AssetDatabase.GetAssetPath(bullet.GetComponentInChildren<Image>(true).sprite));
            Assert.Greater(data.VictoryFadeDuration, 0f);
            var response = AssetDatabase.LoadAssetAtPath<Audere.Dialogue.DialogueData>(
                "Assets/_Audere/Data/Dialogue/Day2/School/PostCombat/Dialogue_D2_BIANCA_YOU_DONT_KNOW_EITHER.asset");
            Assert.IsTrue(response.Lines.Any(l => l.Text == "…Cậu cũng đâu biết."));
            const string scenePath = "Assets/_Audere/Scenes/60_D2_School_Morning.unity";
            Scene scene = SceneManager.GetSceneByPath(scenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                var step = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<CombatStep>(true)).Single();
                Assert.AreSame(data, step.CombatEncounterData);
                Assert.IsTrue(step.CombatController.BoardView.gameObject.activeSelf,
                    "Combat Root owns mode visibility; its Board must not remain locally disabled.");
                var mode = step.CombatController.GetComponentInParent<Audere.World.WorldModeController>();
                Assert.IsFalse(new SerializedObject(mode).FindProperty("allowChildFadeFallback").boolValue);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
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
                phase.FindPropertyRelative("spawnDice").boolValue = true;
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

        private static int CountMissingScripts(GameObject root)
        {
            int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root);
            for (int i = 0; i < root.transform.childCount; i++)
                count += CountMissingScripts(root.transform.GetChild(i).gameObject);
            return count;
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
