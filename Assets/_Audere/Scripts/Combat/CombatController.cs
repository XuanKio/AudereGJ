using System;
using System.Collections;
using System.Collections.Generic;
using Audere.Audio;
using Audere.Core;
using Audere.Dialogue;
using Audere.GameplayInput;
using UnityEngine;

namespace Audere.Combat
{
    public enum CombatResult { Victory, Defeat, Cancelled, Special }

    [DisallowMultipleComponent]
    public sealed partial class CombatController : MonoBehaviour
    {
        public enum State { Idle, EncounterIntro, Playing, PhaseTransition, DialoguePause, Victory, Defeat, Special }

        [Header("Lifecycle")]
        [SerializeField] private bool playOnStart;
        [SerializeField] private CombatEncounterData encounterData;
        [SerializeField] private CombatBoardView boardView;
        [SerializeField, Min(.01f)] private float spawnStagger = .065f;
        [SerializeField, Range(.05f, 1f)] private float tutorialTimeScale = .25f;
        [Header("Dice Collision")]
        [SerializeField, Range(0f, 1f)] private float diceCollisionBounciness = .92f;
        [SerializeField, Min(0f)] private float diceCollisionSeparationPadding = .5f;
        [SerializeField, Range(1, 4)] private int diceCollisionIterations = 2;

        private readonly List<CombatDieView> activeDice = new List<CombatDieView>();
        private readonly Queue<PendingCombatCue> pendingCombatCues = new Queue<PendingCombatCue>();
        private readonly HashSet<CombatSymbol> caughtTutorialSymbols = new HashSet<CombatSymbol>();
        private int batchIndex;
        private float encounterTimeRemaining;
        private bool isPlaying;
        private bool isBatchSpawning;
        private Coroutine batchRoutine;
        private Action<CombatResult> completionCallback;
        private int playRequestVersion;
        private GameplayInputGate inputGate;
        private GameplayInputToken inputToken;
        private CombatEnemyRuntime enemyRuntime;
        private bool cursorWasStunned;
        private bool tutorialInstructionAwaitingInteraction;
        private bool tutorialActive;
        private bool tutorialOpeningBatchPending;
        private int tutorialInstructionShownFrame = -1;
        private Coroutine activeAutoDialogueRoutine;
        private Coroutine resultPresentationRoutine;
        private int observedMoveVersion;
        private bool exclusiveDiceMoveActive;
        private bool isRecoveringPlayerTime;
        private float pendingRecoveryTime;
        private readonly Dictionary<CombatDieView, float> recoveryDiceAges = new Dictionary<CombatDieView, float>();
        private float recoveryDropRemaining;
        private int recoveryDropIndex;
        private DiceCatchSabotage diceSabotage;
        private int retryCheckpointPhaseIndex;
        private GameDifficulty activeDifficulty = GameDifficulty.Easy;
        public CombatHeartScreenPose LastDefeatHeartPose { get; private set; }
        private Texture2D defeatBackdrop;
        public Texture2D TakeDefeatBackdrop()
        {
            Texture2D snapshot = defeatBackdrop;
            defeatBackdrop = null;
            return snapshot;
        }
        private int debugVictoryKeyPressCount;
        private int debugNextPhaseKeyPressCount;

        private readonly struct PendingCombatCue
        {
            public PendingCombatCue(CombatDialogueCue cue, int sessionVersion, int phaseVersion)
            {
                Cue = cue;
                SessionVersion = sessionVersion;
                PhaseVersion = phaseVersion;
            }

            public CombatDialogueCue Cue { get; }
            public int SessionVersion { get; }
            public int PhaseVersion { get; }
        }

        public State CurrentState { get; private set; } = State.Idle;
        public bool IsPlaying => isPlaying;
        public CombatEncounterData CurrentEncounter => encounterData;
        public CombatBoardView BoardView => boardView;
        public float PlayerTime => encounterTimeRemaining;
        public int EnemyHealth => enemyRuntime != null ? enemyRuntime.CurrentHealth : 0;
        public int BatchIndex => batchIndex;
        public float EncounterTimeRemaining => encounterTimeRemaining;
        public CombatEnemyRuntime EnemyRuntime => enemyRuntime;
        public bool IsTutorialActive => tutorialActive;
        public bool IsInstructionPauseActive => tutorialInstructionAwaitingInteraction;
        public float ActiveMaximumTime => ResolveActiveMaximumTime();
        public GameDifficulty ActiveDifficulty => activeDifficulty;
        public int RetryCheckpointPhaseIndex => retryCheckpointPhaseIndex;
        public bool IsRecoveringPlayerTime => isRecoveringPlayerTime;

        private void Awake()
        {
            if (boardView == null)
                Debug.LogError("[CombatController] Assign Combat Board View directly; scene search is not supported.", this);
        }

        private void Start() { if (playOnStart) BeginEncounter(); }
        private void OnDisable() { if (isPlaying) Cancel(); else ResetRuntimeState(); }

        private void LateUpdate()
        {
            boardView?.SetAttackAudioPaused(tutorialInstructionAwaitingInteraction ||
                CurrentState != State.Playing || Time.timeScale <= 0f);
        }

        private void Update()
        {
            if (guidedTutorialRunning)
            {
                TickGuidedTutorial(Time.deltaTime);
                return;
            }
            if (tutorialInstructionAwaitingInteraction)
            {
                if (HasCombatInput() && (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)))
                    DismissTutorialOnPlayerInteraction();
                return;
            }
            if (CurrentState != State.Playing || encounterData == null || boardView == null || enemyRuntime == null) return;
            if (!tutorialActive && encounterData.OutcomeRules.Allows(CombatResult.Victory) &&
                HasCombatInput() && Input.GetKeyDown(KeyCode.K) && ++debugVictoryKeyPressCount >= 5)
            {
                EndCombat(State.Victory);
                return;
            }
            if (!tutorialActive && HasCombatInput() && Input.GetKeyDown(KeyCode.P) &&
                ++debugNextPhaseKeyPressCount >= 5)
            {
                debugNextPhaseKeyPressCount = 0;
                if (enemyRuntime.TryAdvanceToNextPhaseForTesting())
                {
                    StartCoroutine(PhaseBreakRoutine(enemyRuntime.SessionVersion, enemyRuntime.PhaseVersion, true));
                    return;
                }
            }
            boardView.SetAttackAudioPaused(Time.timeScale <= 0f);
            float deltaTime = Time.deltaTime;
            float timeScale = tutorialActive ? tutorialTimeScale : 1f;
            encounterTimeRemaining = ClampPlayerTimeAfterDamage(
                encounterTimeRemaining,
                deltaTime * timeScale,
                CanPlayerBeDefeatedNow());
            boardView.UpdateTimer(encounterTimeRemaining / ResolveActiveMaximumTime());
            bool hasCombatInput = HasCombatInput();
            if (hasCombatInput)
            {
                boardView.UpdateCursor(Input.mousePosition);
                bool cursorIsStunned = boardView.IsCursorStunned;
                if (cursorIsStunned && !cursorWasStunned)
                    QueueCurrentPhaseCue(CombatDialogueCueTrigger.CursorEnteredStunZone);
                cursorWasStunned = cursorIsStunned;
            }
            boardView.TickHeartFeedback(deltaTime);
            diceSabotage?.Tick(deltaTime);
            enemyRuntime.ObservePlayerTime(encounterTimeRemaining, ResolveActiveMaximumTime());
            int healthBeforeTick = enemyRuntime.CurrentHealth;
            bool wasMountDiveActive=boardView.IsMountDiveActive;
            bool wasOpeningMove = enemyRuntime.IsOpeningMove;
            int completionBeforeTick = enemyRuntime.MoveCompletionVersion;
            enemyRuntime.Tick(deltaTime);
            if (completionBeforeTick != enemyRuntime.MoveCompletionVersion)
                EnqueueCue(FindTriggeredCue(CombatDialogueCueTrigger.MoveCompleted,
                    triggerMove: enemyRuntime.LastCompletedMove), enemyRuntime.SessionVersion, enemyRuntime.PhaseVersion);
            SyncExclusiveMoveDice();
            if (wasOpeningMove && !enemyRuntime.IsOpeningMove && enemyRuntime.ShouldSpawnDice && enemyRuntime.State == CombatEnemyRuntimeState.Playing)
                ScheduleNextBatch(encounterData.BatchRespawnDelay);
            if(hasCombatInput && (wasMountDiveActive || boardView.IsMountDiveActive))boardView.UpdateCursor(Input.mousePosition);
            TickDice(deltaTime);
            if (healthBeforeTick != enemyRuntime.CurrentHealth) UpdateEnemyHealthImmediate();
            boardView.TickAnxietyText(deltaTime);
            QueueMoveStartCueIfNeeded();
            if (enemyRuntime.State == CombatEnemyRuntimeState.TransitioningPhase)
            {
                StartCoroutine(PhaseBreakRoutine(enemyRuntime.SessionVersion, enemyRuntime.PhaseVersion));
                return;
            }
            if (enemyRuntime.State == CombatEnemyRuntimeState.Completed) { EndCombat(State.Victory); return; }
            if (encounterTimeRemaining <= 0f) { EndCombat(State.Defeat); return; }

            int bulletHits = boardView.TickBullets(deltaTime, encounterData.PlayerHitInvulnerability);
            for (int i = 0; i < bulletHits && CurrentState == State.Playing; i++) ApplyPlayerHit();
            if (CurrentState != State.Playing) return;
            if (TryStartPendingCombatCue()) return;
            if (TryStartMidPhaseDialogue()) return;
            if (hasCombatInput && !tutorialOpeningBatchPending)
            {
                bool catchPressed = Input.GetMouseButtonDown(0);
                bool rerollPressed = Input.GetMouseButtonDown(1);
                if (enemyRuntime.HandleMoveInput(catchPressed, rerollPressed))
                {
                    int reward = enemyRuntime.ConsumeMoveDamageReward();
                    if (reward > 0) ApplyEnemyDamage(reward);
                }
                else if (catchPressed) TryCatchUnderCursor();
                else if (rerollPressed) TryRerollUnderCursor();
            }
            if (CurrentState == State.Playing)
                TryStartPendingCombatCue();
        }

        public bool Play(CombatEncounterData data, Action<CombatResult> onEnded = null) =>
            Play(data, onEnded, 0);

        public bool Play(CombatEncounterData data, Action<CombatResult> onEnded, int startPhaseIndex)
        {
            if (data == null || boardView == null || data.EnemyDefinition == null)
            {
                Debug.LogError("[CombatController] Assign Encounter Data, Enemy Definition and Combat Board View.", this);
                return false;
            }
            if (!data.EnemyDefinition.Validate(out string error)) { Debug.LogError(error, data.EnemyDefinition); return false; }
            if (startPhaseIndex < 0 || startPhaseIndex >= data.EnemyDefinition.PhaseCount ||
                (startPhaseIndex > 0 && (!data.PhaseRecovery.Enabled ||
                    data.EnemyDefinition.PhasePolicy != CombatPhasePolicy.PerPhaseHealth)) ||
                (data.PhaseRecovery.Enabled && data.EnemyDefinition.PhasePolicy != CombatPhasePolicy.PerPhaseHealth))
            {
                Debug.LogError("[CombatController] Invalid phase checkpoint or phase recovery policy.", data);
                return false;
            }
            if (data.TutorialData != null && !data.TutorialData.Validate(out string tutorialError))
            {
                Debug.LogError(tutorialError, data.TutorialData);
                return false;
            }
            if (!isActiveAndEnabled) { Debug.LogError("[CombatController] Enable the controller before Play.", this); return false; }
            int requestVersion = ++playRequestVersion;
            if (isPlaying) { Cancel(); if (requestVersion != playRequestVersion) return false; }
            GameplayInputGate gate = ResolveInputGate();
            if (gate == null) { Debug.LogError("[CombatController] GameplayInputGate is not available.", this); return false; }
            GameplayInputToken token = gate.PushMode(this, GameplayInputMode.Combat);
            if (!token.IsValid) return false;
            inputGate = gate;
            inputToken = token;
            encounterData = data;
            activeDifficulty = GameplayDifficultySettings.Current;
            completionCallback = onEnded;
            isPlaying = true;
            StartEncounterRuntime(requestVersion, startPhaseIndex);
            return isPlaying;
        }

        public void BeginEncounter() => Play(encounterData);
        public bool Cancel()
        {
            if (!isPlaying) return false;
            ResetRuntimeState(false);
            Complete(CombatResult.Cancelled);
            return true;
        }
        public void ResetEncounter() { if (isPlaying) Cancel(); else ResetRuntimeState(); }
        public void CompleteSpecial() => EndCombat(State.Special);
        public void DebugApplyDiceEffect(CombatSymbol symbol) { if (CurrentState == State.Playing) ApplyImmediateDiceEffect(symbol); }
        public void DebugExpireTimer() { encounterTimeRemaining = 0f; boardView?.UpdateTimer(0f); if (CurrentState == State.Playing) EndCombat(State.Defeat); }
        public void DebugSetTimerHalf() { if (encounterData == null || boardView == null) return; encounterTimeRemaining = ResolveActiveMaximumTime() * .5f; boardView.UpdateTimer(.5f); }
        public void DebugTakePlayerHit() { if (CurrentState == State.Playing) ApplyPlayerHit(); }

        private void StartEncounterRuntime(int sessionVersion, int startPhaseIndex)
        {
            ResetRuntimeState(false);
            retryCheckpointPhaseIndex = startPhaseIndex;
            AudioService.Instance?.SetCombatMusicOwner(this, true, encounterData.Music, 1);
            tutorialActive = encounterData.TutorialData != null && !completedGuidedTutorials.Contains(encounterData.TutorialData);
            tutorialOpeningBatchPending = tutorialActive;
            encounterTimeRemaining = ResolveActiveMaximumTime();
            batchIndex = 0;
            pendingCombatCues.Clear();
            caughtTutorialSymbols.Clear();
            cursorWasStunned = false;
            tutorialInstructionAwaitingInteraction = false;
            tutorialInstructionShownFrame = -1;
            HideTutorialInstruction();
            boardView.ClearCombatRuntime();
            boardView.PrepareEncounter(encounterData.EnemyDisplayName);
            if (tutorialActive && !encounterData.TutorialData.UseGuidedLessons)
                boardView.ShowTutorialStunZone();
            try
            {
                CombatEnemyDefinition runtimeDefinition = tutorialActive
                    ? encounterData.TutorialData.EnemyDefinition
                    : encounterData.EnemyDefinition;
                enemyRuntime = new CombatEnemyRuntime(
                    runtimeDefinition,
                    boardView,
                    new SystemCombatRandom(sessionVersion),
                    sessionVersion,
                    encounterData.OutcomeRules.Allows(CombatResult.Victory),
                    GameplayDifficultySettings.GetEnemyHealthMultiplier(activeDifficulty));
                enemyRuntime.Start(startPhaseIndex);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                Complete(CombatResult.Cancelled);
                return;
            }
            boardView.SetEnemyHealthVisible(enemyRuntime.ShowsHealth);
            UpdateEnemyHealthImmediate();
            boardView.SetEncounterPresentationVisible(true);
            if (tutorialActive && encounterData.TutorialData.UseGuidedLessons)
            {
                StartCoroutine(RunGuidedTutorial(sessionVersion));
                return;
            }
            observedMoveVersion = enemyRuntime.CurrentMove != null && enemyRuntime.CurrentMove.LeadInDuration > 0f ? 0 : enemyRuntime.MoveVersion;
            CurrentState = State.EncounterIntro;
            StartCoroutine(EncounterIntro(sessionVersion));
        }

        private IEnumerator EncounterIntro(int sessionVersion)
        {
            yield return boardView.TransitionPhasePresentation(enemyRuntime.CurrentPhase.Presentation);
            if (!SessionIsCurrent(sessionVersion)) yield break;
            yield return boardView.PlayEnemyIntro();
            yield return new WaitForSecondsRealtime(.12f);
            if (!SessionIsCurrent(sessionVersion) || CurrentState != State.EncounterIntro) yield break;
            boardView.ResetPlayer();
            CombatDialogueCue cue = FindTriggeredCue(CombatDialogueCueTrigger.PhaseEnter);
            bool deferCueUntilPlaying = cue != null && !cue.PausesCombatForPresentation;
            if (cue != null && !deferCueUntilPlaying)
            {
                if (cue.TutorialFocus == CombatTutorialFocus.Time)
                    yield return PreviewEncounterTimeDrain(sessionVersion, enemyRuntime.PhaseVersion);
                if (!SessionIsCurrent(sessionVersion) || CurrentState != State.EncounterIntro) yield break;
                HideTutorialInstruction();
                CurrentState = State.DialoguePause;
                enemyRuntime.PauseForDialogue();
                bool dialogueCompleted = false;
                yield return PlayCueDialogueSequence(
                    cue,
                    sessionVersion,
                    enemyRuntime.PhaseVersion,
                    completed => dialogueCompleted = completed);
                if (!SessionIsCurrent(sessionVersion)) yield break;
                if (dialogueCompleted)
                    enemyRuntime.MarkCueResolved(cue);
                yield return PresentPausedCueInstruction(cue, sessionVersion, enemyRuntime.PhaseVersion);
                if (!SessionIsCurrent(sessionVersion)) yield break;
                enemyRuntime.ResumeFromDialogue();
            }
            CurrentState = State.Playing;
            if (deferCueUntilPlaying)
                EnqueueCue(cue, sessionVersion, enemyRuntime.PhaseVersion);
            else if (cue != null)
                QueueCueCompleted(cue, sessionVersion, enemyRuntime.PhaseVersion);
            if (enemyRuntime.ShouldSpawnDice)
                ScheduleNextBatch(0f);
        }

        private void TickDice(float deltaTime)
        {
            for (int i = activeDice.Count - 1; i >= 0; i--)
            {
                CombatDieView die = activeDice[i];
                if (die == null || die.IsCaptured || !die.gameObject.activeInHierarchy) { activeDice.RemoveAt(i); continue; }
                die.TickMovement(boardView.GetDiceMovementBounds(die.RectTransform.anchoredPosition), deltaTime);
            }
            int iterations = Mathf.Clamp(diceCollisionIterations, 1, 4);
            for (int iteration = 0; iteration < iterations; iteration++)
            for (int i = 0; i < activeDice.Count - 1; i++)
            {
                CombatDieView first = activeDice[i];
                if (first == null || first.IsCaptured || !first.gameObject.activeInHierarchy) continue;
                for (int j = i + 1; j < activeDice.Count; j++)
                {
                    CombatDieView second = activeDice[j];
                    if (second == null || second.IsCaptured || !second.gameObject.activeInHierarchy) continue;
                    first.ResolveCollisionWith(second, diceCollisionBounciness, diceCollisionSeparationPadding);
                }
            }
            for (int i = 0; i < activeDice.Count; i++)
            {
                CombatDieView die = activeDice[i];
                if (die != null && !die.IsCaptured && !die.IsRerolling && die.gameObject.activeInHierarchy)
                    die.ConstrainToBounds(boardView.GetDiceMovementBounds(die.RectTransform.anchoredPosition));
            }
        }

        private void TryCatchUnderCursor()
        {
            for (int i = activeDice.Count - 1; i >= 0; i--)
            {
                CombatDieView die = activeDice[i];
                if (die == null || !die.CanInteract || !boardView.CursorOverlaps(die)) continue;
                if (boardView.IsCursorStunned) { boardView.PlayBlockedCursorFeedback(); return; }
                CatchDie(die);
                return;
            }
        }

        private void TryRerollUnderCursor()
        {
            for (int i = activeDice.Count - 1; i >= 0; i--)
            {
                CombatDieView die = activeDice[i];
                if (die == null || !die.CanInteract || !boardView.CursorOverlaps(die)) continue;
                CombatSymbol rerolledSymbol = RollRerollSymbol(die);
                activeDice[i] = boardView.RerollDie(die, rerolledSymbol);
                AudioService.Instance?.Play(AudioId.Dice_Roll);
                QueueCurrentPhaseCue(CombatDialogueCueTrigger.DiceRerolled);
                return;
            }
        }

        private void CatchDie(CombatDieView die)
        {
            if (TrySabotageDie(die)) return;
            if (die == null || die.IsCaptured || !activeDice.Remove(die)) return;
            recoveryDiceAges.Remove(die);
            CombatSymbol symbol = die.Symbol;
            boardView.PlayDiceCatchVfx(die);
            die.PlayCaptured();
            AudioService.Instance?.Play(AudioId.Dice_Catch);
            if (isRecoveringPlayerTime && symbol == CombatSymbol.Heal)
            {
                pendingRecoveryTime = Mathf.Min(ResolveActiveMaximumTime() - encounterTimeRemaining,
                    pendingRecoveryTime + encounterData.PhaseRecovery.TimePerHealDie);
            }
            else ApplyImmediateDiceEffect(symbol);
            if (CurrentState == State.Playing)
            {
                QueueCurrentPhaseCue(CombatDialogueCueTrigger.DiceCaught, symbol, true);
                if (tutorialActive && caughtTutorialSymbols.Add(symbol) && caughtTutorialSymbols.Count >= 3)
                    QueueCurrentPhaseCue(CombatDialogueCueTrigger.AllDiceTypesCaught);
            }
            if (activeDice.Count == 0 && !isBatchSpawning && CurrentState == State.Playing)
            {
                CombatEnemyProgression progression = enemyRuntime.NotifyCapturedDiceBatch();
                if (progression == CombatEnemyProgression.PhaseBreak)
                    StartCoroutine(PhaseBreakRoutine(enemyRuntime.SessionVersion, enemyRuntime.PhaseVersion));
                else if (!enemyRuntime.IsBatchProgressionPending && enemyRuntime.ShouldSpawnDice)
                    ScheduleNextBatch(encounterData.BatchRespawnDelay);
            }
        }

        private void ApplyImmediateDiceEffect(CombatSymbol symbol)
        {
            CombatDiceDefinition definition = CombatDiceConstants.GetDefinition(symbol);
            switch (definition.Ability)
            {
                case CombatDiceAbility.DamageEnemy:
                    ApplyEnemyDamage(Mathf.Max(1, Mathf.RoundToInt(definition.EffectAmount)));
                    break;
                case CombatDiceAbility.DestroyNearbyBullets:
                    boardView.DestroyBulletsNearPlayer(definition.EffectRadius);
                    break;
                case CombatDiceAbility.RestoreEncounterTime:
                    float maximumTime = ResolveActiveMaximumTime();
                    encounterTimeRemaining = Mathf.Min(maximumTime, encounterTimeRemaining + definition.EffectAmount);
                    boardView.UpdateTimer(encounterTimeRemaining / maximumTime);
                    break;
            }
        }

        private void ApplyEnemyDamage(int amount)
        {
            int previous = enemyRuntime.CurrentHealth;
            int maximum = Mathf.Max(1, enemyRuntime.CurrentMaxHealth);
            CombatEnemyProgression progression = enemyRuntime.ApplyDamage(amount, out int applied);
            if (applied > 0)
            {
                boardView.PlayEnemyDamageFeedback(previous / (float)maximum, enemyRuntime.CurrentHealth / (float)maximum);
                boardView.PlayEnemyDamageNumber(applied);
                boardView.PlayAttackHitVfx();
                boardView.TriggerEnemyHitFeedback();
                AudioService.Instance?.Play(AudioId.Enemy_Hurt);
            }
            if (progression == CombatEnemyProgression.Victory) EndCombat(State.Victory);
            else if (progression == CombatEnemyProgression.PhaseBreak)
                StartCoroutine(PhaseBreakRoutine(enemyRuntime.SessionVersion, enemyRuntime.PhaseVersion));
        }

        private void SyncExclusiveMoveDice()
        {
            if (enemyRuntime == null || enemyRuntime.State != CombatEnemyRuntimeState.Playing) return;
            bool exclusive = enemyRuntime.CurrentMove is ICombatExclusiveDiceMove;
            if (exclusive == exclusiveDiceMoveActive) return;
            exclusiveDiceMoveActive = exclusive;
            if (exclusive)
            {
                boardView.CaptureDiceExit();
                StopBatchAndClearDice();
            }
            else if (enemyRuntime.ShouldSpawnDice && CurrentState == State.Playing)
                ScheduleNextBatch(encounterData.BatchRespawnDelay);
        }

        private IEnumerator PhaseBreakRoutine(int sessionVersion, int oldPhaseVersion, bool skipForTesting = false)
        {
            CurrentState = State.PhaseTransition;
            debugNextPhaseKeyPressCount = 0;
            pendingCombatCues.Clear();
            StopAutoDialogue();
            HideTutorialInstruction();
            boardView.CaptureDiceExit();
            StopBatchAndClearDice();
            boardView.ClearRuntimeBullets(sessionVersion, oldPhaseVersion);
            while (boardView.IsRecoveringMove)
            {
                if (!PhaseIsCurrent(sessionVersion, oldPhaseVersion)) yield break;
                boardView.TickMoveRecovery(Time.unscaledDeltaTime);
                yield return null;
            }
            CombatDialogueCue exitCue = skipForTesting ? null : FindTriggeredCue(CombatDialogueCueTrigger.PhaseExit);
            if (exitCue != null && exitCue.PausesCombatForPresentation)
            {
                bool dialogueCompleted = false;
                yield return PlayCueDialogueSequence(
                    exitCue,
                    sessionVersion,
                    oldPhaseVersion,
                    completed => dialogueCompleted = completed);
                if (dialogueCompleted && PhaseIsCurrent(sessionVersion, oldPhaseVersion))
                    enemyRuntime.MarkCueResolved(exitCue);
            }
            else if (exitCue != null && exitCue.Presentation == CombatDialoguePresentation.BackgroundTextField)
            {
                PresentBackgroundText(exitCue, sessionVersion);
                enemyRuntime.MarkCueResolved(exitCue);
            }
            if (!PhaseIsCurrent(sessionVersion, oldPhaseVersion)) yield break;
            if (!skipForTesting)
                yield return PresentPausedCueInstruction(exitCue, sessionVersion, oldPhaseVersion);
            if (!PhaseIsCurrent(sessionVersion, oldPhaseVersion)) yield break;
            if (!skipForTesting && encounterData.PhaseRecovery.Enabled)
            {
                yield return RecoverPlayerTimeWithHealDice(sessionVersion, oldPhaseVersion);
                if (!PhaseIsCurrent(sessionVersion, oldPhaseVersion) || CurrentState != State.PhaseTransition)
                    yield break;
            }
            if (skipForTesting && encounterData.PhaseRecovery.Enabled)
            {
                encounterTimeRemaining = ResolveActiveMaximumTime();
                boardView.UpdateTimer(1f);
            }
            enemyRuntime.CompletePhaseBreak();
            if (encounterData.PhaseRecovery.Enabled)
                retryCheckpointPhaseIndex = enemyRuntime.PhaseIndex;
            observedMoveVersion = enemyRuntime.CurrentMove != null && enemyRuntime.CurrentMove.LeadInDuration > 0f ? 0 : enemyRuntime.MoveVersion;
            ApplyCurrentPhaseTimeFloor();
            UpdateEnemyHealthImmediate();
            int newPhaseVersion = enemyRuntime.PhaseVersion;
            yield return boardView.TransitionPhasePresentation(enemyRuntime.CurrentPhase.Presentation);
            if (!PhaseIsCurrent(sessionVersion, newPhaseVersion)) yield break;
            CombatDialogueCue enterCue = FindTriggeredCue(CombatDialogueCueTrigger.PhaseEnter);
            if (skipForTesting)
            {
                enemyRuntime.MarkCueResolved(enterCue);
                enterCue = null;
            }
            if (enterCue != null && enterCue.PausesCombatForPresentation)
            {
                HideTutorialInstruction();
                enemyRuntime.PauseForDialogue();
                bool dialogueCompleted = false;
                yield return PlayCueDialogueSequence(
                    enterCue,
                    sessionVersion,
                    newPhaseVersion,
                    completed => dialogueCompleted = completed);
                if (!PhaseIsCurrent(sessionVersion, newPhaseVersion)) yield break;
                if (dialogueCompleted)
                    enemyRuntime.MarkCueResolved(enterCue);
                yield return PresentPausedCueInstruction(enterCue, sessionVersion, newPhaseVersion);
                if (!PhaseIsCurrent(sessionVersion, newPhaseVersion)) yield break;
                enemyRuntime.ResumeFromDialogue();
            }
            CurrentState = State.Playing;
            if (enterCue != null && !enterCue.PausesCombatForPresentation)
                EnqueueCue(enterCue, sessionVersion, newPhaseVersion);
            else if (enterCue != null)
                QueueCueCompleted(enterCue, sessionVersion, newPhaseVersion);
            if (enemyRuntime.ShouldSpawnDice)
                ScheduleNextBatch(0f);
        }

        private IEnumerator RecoverPlayerTimeWithHealDice(int sessionVersion, int phaseVersion)
        {
            BeginPhaseRecovery();
            while (PhaseIsCurrent(sessionVersion, phaseVersion) && CurrentState == State.PhaseTransition &&
                !IsPhaseRecoveryComplete())
            {
                float deltaTime = Time.deltaTime;
                bool hasInput = HasCombatInput();
                if (hasInput)
                    boardView.UpdateCursor(Input.mousePosition);
                boardView.TickHeartFeedback(deltaTime);
                TickPhaseRecovery(deltaTime);
                if (hasInput && Input.GetMouseButtonDown(0))
                    TryCatchUnderCursor();
                yield return null;
            }
            isRecoveringPlayerTime = false;
            pendingRecoveryTime = 0f;
            recoveryDiceAges.Clear();
            if (PhaseIsCurrent(sessionVersion, phaseVersion))
            {
                boardView.CaptureMoveExit();
                boardView.CaptureDiceExit();
                StopBatchAndClearDice();
                while (boardView.IsRecoveringMove && PhaseIsCurrent(sessionVersion, phaseVersion))
                {
                    boardView.TickMoveRecovery(Time.unscaledDeltaTime);
                    yield return null;
                }
            }
        }

        private void BeginPhaseRecovery()
        {
            if (encounterData.PhaseRecovery.HealEnemyOnDrop &&
                !enemyRuntime.TryBeginNextPhaseHealthRecovery())
                throw new InvalidOperationException("Enemy healing between phases requires a pending per-phase HP transition.");
            isRecoveringPlayerTime = true;
            pendingRecoveryTime = 0f;
            recoveryDropRemaining = 0f;
            recoveryDropIndex = 0;
            recoveryDiceAges.Clear();
            UpdateEnemyHealthImmediate();
        }

        private bool IsPhaseRecoveryComplete()
        {
            return encounterData.PhaseRecovery.HealEnemyOnDrop
                ? enemyRuntime.CurrentHealth >= enemyRuntime.CurrentMaxHealth && activeDice.Count == 0 && pendingRecoveryTime <= .01f
                : encounterTimeRemaining >= ResolveActiveMaximumTime() - .01f;
        }

        private void TickPhaseRecovery(float deltaTime)
        {
            if (!isRecoveringPlayerTime || CurrentState != State.PhaseTransition || deltaTime <= 0f) return;
            CombatPhaseRecoverySettings settings = encounterData.PhaseRecovery;
            TickRecoveryHealing(deltaTime);
            // Missed recovery dice expire, so reaching the active-dice cap never requires a catch.
            if (settings.HealEnemyOnDrop)
            {
                for (int i = activeDice.Count - 1; i >= 0; i--)
                {
                    CombatDieView die = activeDice[i];
                    if (die == null || !recoveryDiceAges.TryGetValue(die, out float age)) continue;
                    age += deltaTime;
                    if (age >= settings.DroppedDiceLifetime)
                    {
                        recoveryDiceAges.Remove(die);
                        activeDice.RemoveAt(i);
                        die.ReturnToPool();
                    }
                    else
                    {
                        recoveryDiceAges[die] = age;
                        die.SetPresentationAlpha((settings.DroppedDiceLifetime - age) / .4f);
                    }
                }
            }
            TickDice(deltaTime);
            recoveryDropRemaining -= deltaTime;
            bool needsDrop = settings.HealEnemyOnDrop
                ? enemyRuntime.CurrentHealth < enemyRuntime.CurrentMaxHealth
                : encounterTimeRemaining + pendingRecoveryTime < ResolveActiveMaximumTime() - .01f;
            if (!needsDrop || recoveryDropRemaining > 0f || activeDice.Count >= settings.MaximumActiveDice) return;
            float x = .5f + (recoveryDropIndex % 3 - 1) * .075f;
            float drift = (recoveryDropIndex % 2 == 0 ? 1f : -1f) * .18f;
            var spawn = new CombatScriptedDieSpawn(CombatSymbol.Heal,
                new Vector2(x, .91f), new Vector2(drift, -1f));
            CombatDieView spawned = boardView.SpawnDie(spawn, settings.DiceSpeed);
            if (spawned == null) return;
            activeDice.Add(spawned);
            if (settings.HealEnemyOnDrop)
            {
                recoveryDiceAges[spawned] = 0f;
                enemyRuntime.HealNextPhaseHealth(settings.EnemyHealthPerDie);
                UpdateEnemyHealthImmediate();
            }
            recoveryDropIndex++;
            recoveryDropRemaining = settings.DropInterval;
        }

        private void TickRecoveryHealing(float dt)
        {
            if (!isRecoveringPlayerTime || dt <= 0f) return;
            var settings = encounterData.PhaseRecovery;
            float healing = Mathf.Min(pendingRecoveryTime,
                settings.TimePerHealDie / settings.HealAnimationDuration * dt);
            pendingRecoveryTime -= healing;
            encounterTimeRemaining = Mathf.Min(ResolveActiveMaximumTime(), encounterTimeRemaining + healing);
            boardView.UpdateTimer(encounterTimeRemaining / ResolveActiveMaximumTime());
        }

        private bool TryStartMidPhaseDialogue()
        {
            if (tutorialOpeningBatchPending)
                return false;
            CombatDialogueCue cue = FindTriggeredCue(CombatDialogueCueTrigger.HealthAtOrBelow) ?? FindTriggeredCue(CombatDialogueCueTrigger.ElapsedActiveTime);
            if (cue == null) return false;
            return PresentCue(cue, enemyRuntime.SessionVersion, enemyRuntime.PhaseVersion);
        }

        private bool TryStartPendingCombatCue()
        {
            int remaining = pendingCombatCues.Count;
            while (remaining-- > 0 && pendingCombatCues.Count > 0)
            {
                PendingCombatCue pending = pendingCombatCues.Dequeue();
                if (!PhaseIsCurrent(pending.SessionVersion, pending.PhaseVersion))
                    continue;
                if (pending.Cue.Presentation == CombatDialoguePresentation.AutoCombatDialogue && activeAutoDialogueRoutine != null)
                {
                    pendingCombatCues.Enqueue(pending);
                    continue;
                }
                return PresentCue(pending.Cue, pending.SessionVersion, pending.PhaseVersion);
            }

            return false;
        }

        private void QueueCurrentPhaseCue(
            CombatDialogueCueTrigger trigger,
            CombatSymbol symbol = CombatSymbol.Attack,
            bool hasSymbol = false)
        {
            if (CurrentState != State.Playing || enemyRuntime == null)
                return;

            CombatDialogueCue cue = FindTriggeredCue(trigger, symbol, hasSymbol);
            if (cue == null)
                return;

            if (cue.InterruptsAutoDialogue)
            {
                StopAutoDialogue();
                pendingCombatCues.Clear();
            }
            EnqueueCue(cue, enemyRuntime.SessionVersion, enemyRuntime.PhaseVersion);
        }

        private void EnqueueCue(CombatDialogueCue cue, int sessionVersion, int phaseVersion)
        {
            if (cue != null)
                pendingCombatCues.Enqueue(new PendingCombatCue(cue, sessionVersion, phaseVersion));
        }

        private bool PresentCue(CombatDialogueCue cue, int sessionVersion, int phaseVersion)
        {
            if (cue == null || !PhaseIsCurrent(sessionVersion, phaseVersion))
                return false;
            if (cue.Presentation == CombatDialoguePresentation.BackgroundTextField)
            {
                PresentBackgroundText(cue, sessionVersion);
                ResolveCueAndPendingProgression(cue, sessionVersion, phaseVersion);
                QueueCueCompleted(cue, sessionVersion, phaseVersion);
                return false;
            }
            if (cue.Presentation == CombatDialoguePresentation.AutoCombatDialogue)
            {
                if (activeAutoDialogueRoutine != null)
                {
                    EnqueueCue(cue, sessionVersion, phaseVersion);
                    return false;
                }
                activeAutoDialogueRoutine = StartCoroutine(AutoCombatDialogueRoutine(cue, sessionVersion, phaseVersion));
                return false;
            }
            StartCoroutine(MidPhaseDialogueRoutine(cue, sessionVersion, phaseVersion));
            return true;
        }

        private IEnumerator MidPhaseDialogueRoutine(CombatDialogueCue cue, int sessionVersion, int phaseVersion)
        {
            HideTutorialInstruction();
            CurrentState = State.DialoguePause;
            enemyRuntime.PauseForDialogue();
            yield return PlayDialogueSequence(cue, sessionVersion, phaseVersion);
            if (!PhaseIsCurrent(sessionVersion, phaseVersion)) yield break;
            enemyRuntime.MarkCueResolved(cue);
            if (tutorialActive && cue.IsTutorial && cue.Trigger == CombatDialogueCueTrigger.AllDiceTypesCaught)
            {
                StartActualCombatAfterTutorial(sessionVersion, phaseVersion);
                yield break;
            }
            yield return PresentPausedCueInstruction(cue, sessionVersion, phaseVersion);
            if (!PhaseIsCurrent(sessionVersion, phaseVersion)) yield break;
            if (tutorialActive && cue.Trigger == CombatDialogueCueTrigger.DiceBatchReady &&
                cue.TutorialFocus == CombatTutorialFocus.DiceAll)
                tutorialOpeningBatchPending = false;
            enemyRuntime.ResumeFromDialogue();
            CurrentState = State.Playing;
            TryReleasePendingBatchProgression(sessionVersion, phaseVersion);
            QueueCueCompleted(cue, sessionVersion, phaseVersion);
        }

        private IEnumerator AutoCombatDialogueRoutine(CombatDialogueCue cue, int sessionVersion, int phaseVersion)
        {
            HideTutorialInstruction();
            CurrentState = State.DialoguePause;
            enemyRuntime.PauseForDialogue();
            bool dialogueCompleted = false;
            yield return PlayAutoDialogueSequence(
                cue,
                sessionVersion,
                phaseVersion,
                completed => dialogueCompleted = completed);
            activeAutoDialogueRoutine = null;
            if (!PhaseIsCurrent(sessionVersion, phaseVersion)) yield break;
            enemyRuntime.ResumeFromDialogue();
            CurrentState = State.Playing;
            if (!dialogueCompleted) yield break;
            enemyRuntime.MarkCueResolved(cue);
            if (cue.PlayLoseRhythmOnComplete)
            {
                boardView.PlayPlayerLoseRhythm(.3f);
                yield return WaitForCombatActiveDelay(.3f, sessionVersion, phaseVersion);
            }
            if (PhaseIsCurrent(sessionVersion, phaseVersion))
            {
                TryReleasePendingBatchProgression(sessionVersion, phaseVersion);
                QueueCueCompleted(cue, sessionVersion, phaseVersion);
            }
        }

        private void PresentBackgroundText(CombatDialogueCue cue, int sessionVersion)
        {
            var lines = new List<string>();
            if (cue.Sequence != null)
            {
                for (int dataIndex = 0; dataIndex < cue.Sequence.Count; dataIndex++)
                {
                    DialogueData data = cue.Sequence[dataIndex];
                    if (data == null) continue;
                    for (int lineIndex = 0; lineIndex < data.Lines.Count; lineIndex++)
                        if (!string.IsNullOrWhiteSpace(data.Lines[lineIndex].Text))
                            lines.Add(data.Lines[lineIndex].Text);
                }
            }
            boardView.ShowAnxietyText(lines, sessionVersion);
        }

        private void QueueCueCompleted(CombatDialogueCue completed, int sessionVersion, int phaseVersion)
        {
            if (completed == null || !PhaseIsCurrent(sessionVersion, phaseVersion))
                return;
            CombatDialogueCue followUp = FindTriggeredCue(
                CombatDialogueCueTrigger.CueCompleted,
                completedCueId: completed.CueId);
            EnqueueCue(followUp, sessionVersion, phaseVersion);
        }

        private void ResolveCueAndPendingProgression(
            CombatDialogueCue cue,
            int sessionVersion,
            int phaseVersion)
        {
            if (!PhaseIsCurrent(sessionVersion, phaseVersion))
                return;
            enemyRuntime.MarkCueResolved(cue);
            TryReleasePendingBatchProgression(sessionVersion, phaseVersion);
        }

        private void TryReleasePendingBatchProgression(int sessionVersion, int phaseVersion)
        {
            if (!PhaseIsCurrent(sessionVersion, phaseVersion) || CurrentState != State.Playing)
                return;
            CombatEnemyProgression progression = enemyRuntime.TryReleasePendingBatchProgression();
            if (progression == CombatEnemyProgression.PhaseBreak)
                StartCoroutine(PhaseBreakRoutine(sessionVersion, phaseVersion));
        }

        private void QueueMoveStartCueIfNeeded()
        {
            if (enemyRuntime == null || boardView.IsRecoveringMove || observedMoveVersion == enemyRuntime.MoveVersion)
                return;
            observedMoveVersion = enemyRuntime.MoveVersion;
            CombatDialogueCue cue = FindTriggeredCue(
                CombatDialogueCueTrigger.MoveStarted,
                triggerMove: enemyRuntime.CurrentMove);
            if (cue != null && cue.InterruptsAutoDialogue)
            {
                StopAutoDialogue();
                pendingCombatCues.Clear();
            }
            EnqueueCue(cue, enemyRuntime.SessionVersion, enemyRuntime.PhaseVersion);
        }

        private IEnumerator PresentPausedCueInstruction(
            CombatDialogueCue cue,
            int sessionVersion,
            int phaseVersion)
        {
            if (cue == null || !cue.HasInstruction)
                yield break;

            ShowCueInstruction(cue);
            while (tutorialInstructionAwaitingInteraction && PhaseIsCurrent(sessionVersion, phaseVersion))
                yield return null;
        }

        private CombatDialogueCue FindTriggeredCue(
            CombatDialogueCueTrigger trigger,
            CombatSymbol symbol = CombatSymbol.Attack,
            bool hasSymbol = false,
            CombatMoveDefinition triggerMove = null,
            string completedCueId = null)
        {
            if (guidedTutorialRunning) return null;
            IReadOnlyList<CombatDialogueCue> cues = tutorialActive
                ? encounterData?.TutorialData?.Cues
                : enemyRuntime?.CurrentPhase?.DialogueCues;
            if (cues == null) return null;
            for (int i = 0; i < cues.Count; i++)
            {
                CombatDialogueCue cue = cues[i];
                if (cue == null || cue.Trigger != trigger || !cue.HasContent) continue;
                if (!tutorialActive && cue.IsTutorial) continue;
                bool isImmediateEvent = trigger == CombatDialogueCueTrigger.PhaseEnter ||
                    trigger == CombatDialogueCueTrigger.PhaseExit ||
                    trigger == CombatDialogueCueTrigger.DiceBatchReady ||
                    trigger == CombatDialogueCueTrigger.DiceCaught ||
                    trigger == CombatDialogueCueTrigger.DiceRerolled ||
                    trigger == CombatDialogueCueTrigger.CursorEnteredStunZone ||
                    trigger == CombatDialogueCueTrigger.PlayerHit ||
                    trigger == CombatDialogueCueTrigger.AllDiceTypesCaught ||
                    trigger == CombatDialogueCueTrigger.MoveStarted ||
                    trigger == CombatDialogueCueTrigger.MoveCompleted ||
                    trigger == CombatDialogueCueTrigger.CueCompleted;
                bool triggered = isImmediateEvent ||
                    (trigger == CombatDialogueCueTrigger.HealthAtOrBelow &&
                     enemyRuntime.CurrentHealth <= enemyRuntime.ScaleAuthoredHealthThreshold(cue.TriggerValue)) ||
                    (trigger == CombatDialogueCueTrigger.ElapsedActiveTime && enemyRuntime.PhaseElapsed >= cue.TriggerValue);
                if (triggered && cue.FilterBySymbol && (!hasSymbol || !cue.MatchesSymbol(symbol)))
                    triggered = false;
                if (triggered && (trigger == CombatDialogueCueTrigger.MoveStarted ||
                    trigger == CombatDialogueCueTrigger.MoveCompleted) && cue.TriggerMove != triggerMove)
                    triggered = false;
                if (triggered && trigger == CombatDialogueCueTrigger.CueCompleted &&
                    !string.Equals(cue.TriggerCueId, completedCueId, StringComparison.Ordinal))
                    triggered = false;
                if (triggered && enemyRuntime.MarkCuePlayed(cue)) return cue;
            }
            return null;
        }

        private void ShowCueInstruction(CombatDialogueCue cue)
        {
            CombatTutorialView tutorial = GameplayUIRoot.Instance != null
                ? GameplayUIRoot.Instance.CombatTutorial
                : null;
            if (cue == null || !cue.HasInstruction || tutorial == null)
                return;
            tutorial.ShowInstruction(
                cue.Instruction,
                0f,
                cue.TutorialFocus,
                cue.ShowcasedSymbol,
                ResolveTutorialFocusTarget(cue.TutorialFocus));
            tutorialInstructionAwaitingInteraction = true;
            tutorialInstructionShownFrame = Time.frameCount;
        }

        private void DismissTutorialOnPlayerInteraction()
        {
            if (!tutorialInstructionAwaitingInteraction || Time.frameCount <= tutorialInstructionShownFrame)
                return;
            HideTutorialInstruction();
        }

        private void HideTutorialInstruction()
        {
            tutorialInstructionAwaitingInteraction = false;
            tutorialInstructionShownFrame = -1;
            GameplayUIRoot.Instance?.CombatTutorial?.ForceHide();
        }

        private RectTransform ResolveTutorialFocusTarget(CombatTutorialFocus focus)
        {
            if (boardView == null)
                return null;
            switch (focus)
            {
                case CombatTutorialFocus.Time:
                    return boardView.TimerFocusTarget;
                case CombatTutorialFocus.StunZone:
                    return boardView.StunZoneFocusTarget;
                default:
                    return null;
            }
        }

        private IEnumerator PlayCueDialogueSequence(
            CombatDialogueCue cue,
            int sessionVersion,
            int phaseVersion,
            Action<bool> onCompleted)
        {
            if (cue != null && cue.Presentation == CombatDialoguePresentation.AutoCombatDialogue)
            {
                yield return PlayAutoDialogueSequence(cue, sessionVersion, phaseVersion, onCompleted);
                yield break;
            }

            yield return PlayDialogueSequence(cue, sessionVersion, phaseVersion);
            onCompleted?.Invoke(PhaseIsCurrent(sessionVersion, phaseVersion));
        }

        private IEnumerator PlayAutoDialogueSequence(
            CombatDialogueCue cue,
            int sessionVersion,
            int phaseVersion,
            Action<bool> onCompleted)
        {
            DialogueController dialogue = GameplayUIRoot.Instance != null ? GameplayUIRoot.Instance.Dialogue : null;
            if (dialogue == null)
            {
                Debug.LogWarning($"[CombatController] Auto dialogue cue '{cue?.CueId}' has no DialogueController.", this);
                onCompleted?.Invoke(false);
                yield break;
            }

            if (cue?.Sequence != null)
            {
                for (int dataIndex = 0; dataIndex < cue.Sequence.Count; dataIndex++)
                {
                    DialogueData data = cue.Sequence[dataIndex];
                    if (data == null) continue;
                    if (!PhaseIsCurrent(sessionVersion, phaseVersion))
                    {
                        dialogue.ForceClose();
                        onCompleted?.Invoke(false);
                        yield break;
                    }

                    bool finished = false;
                    DialogueResult result = DialogueResult.Cancelled;
                    bool started = dialogue.PlayAuto(
                        data,
                        value => { result = value; finished = true; },
                        cue.MinimumLineDuration,
                        cue.CharactersPerSecond,
                        cue.InterLineGap,
                        false);
                    if (!started)
                    {
                        Debug.LogWarning($"[CombatController] Auto dialogue cue '{cue.CueId}' could not start.", this);
                        onCompleted?.Invoke(false);
                        yield break;
                    }

                    while (!finished && PhaseIsCurrent(sessionVersion, phaseVersion))
                        yield return null;
                    if (!PhaseIsCurrent(sessionVersion, phaseVersion))
                    {
                        dialogue.ForceClose();
                        onCompleted?.Invoke(false);
                        yield break;
                    }
                    if (result != DialogueResult.Completed)
                    {
                        onCompleted?.Invoke(false);
                        yield break;
                    }
                }
            }

            onCompleted?.Invoke(PhaseIsCurrent(sessionVersion, phaseVersion));
        }

        private IEnumerator PlayDialogueSequence(CombatDialogueCue cue, int sessionVersion, int phaseVersion)
        {
            DialogueController dialogue = GameplayUIRoot.Instance != null ? GameplayUIRoot.Instance.Dialogue : null;
            if (dialogue == null || cue?.Sequence == null) yield break;
            for (int i = 0; i < cue.Sequence.Count; i++)
            {
                DialogueData data = cue.Sequence[i];
                if (data == null) continue;
                bool completed = false;
                bool started = dialogue.Play(data, _ => completed = true, DialoguePlaybackMode.CallerOwnedPause, false);
                if (!started) continue;
                while (!completed && PhaseIsCurrent(sessionVersion, phaseVersion)) yield return null;
                if (!PhaseIsCurrent(sessionVersion, phaseVersion)) { dialogue.ForceClose(); yield break; }
            }
        }

        private void StartActualCombatAfterTutorial(int sessionVersion, int phaseVersion)
        {
            if (!PhaseIsCurrent(sessionVersion, phaseVersion) || encounterData == null || boardView == null)
                return;

            if (encounterData.TutorialData.UseGuidedLessons) completedGuidedTutorials.Add(encounterData.TutorialData);
            ResetGuidedTutorial();
            CurrentState = State.PhaseTransition;
            pendingCombatCues.Clear();
            HideTutorialInstruction();
            StopBatchAndClearDice();
            boardView.ClearRuntimeBullets(sessionVersion);
            enemyRuntime.Cancel();
            boardView.ClearCombatRuntime();

            tutorialActive = false;
            tutorialOpeningBatchPending = false;
            int actualSessionVersion = ++playRequestVersion;
            enemyRuntime = new CombatEnemyRuntime(
                encounterData.EnemyDefinition,
                boardView,
                new SystemCombatRandom(actualSessionVersion),
                actualSessionVersion,
                encounterData.OutcomeRules.Allows(CombatResult.Victory),
                GameplayDifficultySettings.GetEnemyHealthMultiplier(activeDifficulty));
            enemyRuntime.Start();
            observedMoveVersion = enemyRuntime.CurrentMove != null && enemyRuntime.CurrentMove.LeadInDuration > 0f ? 0 : enemyRuntime.MoveVersion;
            encounterTimeRemaining = ResolveActiveMaximumTime();
            batchIndex = 0;
            caughtTutorialSymbols.Clear();
            cursorWasStunned = false;

            boardView.PrepareEncounter(encounterData.EnemyDisplayName);
            boardView.ResetPlayer();
            boardView.UpdateTimer(1f);
            boardView.SetEnemyHealthVisible(enemyRuntime.ShowsHealth);
            UpdateEnemyHealthImmediate();
            boardView.SetEncounterPresentationVisible(true);

            CurrentState = State.Playing;
            QueueCurrentPhaseCue(CombatDialogueCueTrigger.PhaseEnter);
            if (enemyRuntime.ShouldSpawnDice)
                ScheduleNextBatch(0f);
        }

        private void UpdateEnemyHealthImmediate()
        {
            if (enemyRuntime == null || !enemyRuntime.ShowsHealth) return;
            float normalized = enemyRuntime.CurrentHealth / (float)Mathf.Max(1, enemyRuntime.CurrentMaxHealth);
            boardView.PlayEnemyDamageFeedback(normalized, normalized);
        }

        private void ApplyCurrentPhaseTimeFloor()
        {
            if (enemyRuntime?.CurrentPhase == null || boardView == null)
                return;

            float floor = Mathf.Min(
                ResolveActiveMaximumTime(),
                GameplayDifficultySettings.ScalePlayerTime(
                    enemyRuntime.CurrentPhase.MinimumPlayerTimeOnEnter,
                    activeDifficulty));
            if (floor <= encounterTimeRemaining)
                return;

            encounterTimeRemaining = floor;
            boardView.UpdateTimer(encounterTimeRemaining / ResolveActiveMaximumTime());
        }

        private IEnumerator PreviewEncounterTimeDrain(int sessionVersion, int phaseVersion)
        {
            const float previewDuration = 1.1f;
            float elapsed = 0f;
            while (elapsed < previewDuration &&
                   PhaseIsCurrent(sessionVersion, phaseVersion) &&
                   CurrentState == State.EncounterIntro)
            {
                float frameTime = Mathf.Min(Time.unscaledDeltaTime, .1f);
                elapsed += frameTime;
                encounterTimeRemaining = Mathf.Max(0f,
                    encounterTimeRemaining - frameTime * tutorialTimeScale);
                boardView.UpdateTimer(encounterTimeRemaining / ResolveActiveMaximumTime());
                yield return null;
            }
        }

        private void ApplyPlayerHit()
        {
            float previous = encounterTimeRemaining;
            encounterTimeRemaining = ClampPlayerTimeAfterDamage(
                encounterTimeRemaining,
                encounterData.BulletTimePenaltySeconds,
                CanPlayerBeDefeatedNow());
            float maximumTime = ResolveActiveMaximumTime();
            boardView.PlayPlayerDamageFeedback(previous / maximumTime, encounterTimeRemaining / maximumTime);
            AudioService.Instance?.Play(AudioId.Player_Hurt);
            if (encounterTimeRemaining <= 0f) EndCombat(State.Defeat);
            else QueueCurrentPhaseCue(CombatDialogueCueTrigger.PlayerHit);
        }

        private static float ClampPlayerTimeAfterDamage(float current, float penalty, bool canDefeatPlayer)
        {
            float minimum = canDefeatPlayer ? 0f : 1f;
            return Mathf.Max(minimum, current - Mathf.Max(0f, penalty));
        }

        private bool CanPlayerBeDefeatedNow()
        {
            if (tutorialActive || encounterData == null)
                return false;
            if (encounterData.OutcomeRules.PlayerDefeatGate == CombatPlayerDefeatGate.Always)
                return true;
            return enemyRuntime != null && enemyRuntime.CanPlayerBeDefeated;
        }

        private bool UseLowTimeDiceAssist() =>
            activeDifficulty == GameDifficulty.Easy &&
            encounterData != null &&
            encounterTimeRemaining < ResolveActiveMaximumTime() * CombatDiceConstants.LowTimeAssistThreshold;

        private CombatSymbol RollBatchSymbol() =>
            CombatDiceConstants.RollSymbol(UnityEngine.Random.value, UseLowTimeDiceAssist());

        private CombatSymbol RollRerollSymbol(CombatDieView replaced)
        {
            return CombatDiceConstants.RerollSymbol(replaced.Symbol, UnityEngine.Random.value, UseLowTimeDiceAssist());
        }

        private void ScheduleNextBatch(float delay)
        {
            if (batchRoutine != null || CurrentState != State.Playing ||
                enemyRuntime == null || !enemyRuntime.ShouldSpawnDice) return;
            batchRoutine = StartCoroutine(SpawnBatchAfterDelay(delay, enemyRuntime.SessionVersion, enemyRuntime.PhaseVersion));
        }

        private IEnumerator SpawnBatchAfterDelay(float delay, int sessionVersion, int phaseVersion)
        {
            CombatDiceBatchDefinition scriptedBatch = enemyRuntime != null
                ? enemyRuntime.CurrentDiceBatch
                : null;
            if (scriptedBatch != null)
                delay += scriptedBatch.SpawnDelay;
            if (delay > 0f)
                yield return WaitForCombatActiveDelay(delay, sessionVersion, phaseVersion);
            if (!PhaseIsCurrent(sessionVersion, phaseVersion) || CurrentState != State.Playing ||
                !enemyRuntime.ShouldSpawnDice) { batchRoutine = null; yield break; }
            isBatchSpawning = true;
            bool isOpeningTutorialBatch = tutorialActive && batchIndex == 0;
            batchIndex++;
            IReadOnlyList<CombatSymbol> openingDice = isOpeningTutorialBatch
                ? encounterData.TutorialData.OpeningDice
                : null;
            int diceCount = openingDice != null
                ? openingDice.Count
                : scriptedBatch != null ? scriptedBatch.Count : encounterData.DicePerBatch;
            for (int i = 0; i < diceCount; i++)
            {
                if (!PhaseIsCurrent(sessionVersion, phaseVersion) || CurrentState != State.Playing ||
                    !enemyRuntime.ShouldSpawnDice) break;
                float speed = UnityEngine.Random.Range(encounterData.MinimumDiceSpeed, encounterData.MaximumDiceSpeed);
                CombatDieView die;
                if (openingDice != null)
                    die = boardView.SpawnDie(openingDice[i], speed);
                else if (scriptedBatch != null)
                    die = boardView.SpawnDie(scriptedBatch.Dice[i], speed);
                else
                    die = boardView.SpawnDie(RollBatchSymbol(), speed);
                if (die != null) activeDice.Add(die);
                yield return WaitForCombatActiveDelay(spawnStagger, sessionVersion, phaseVersion);
            }
            isBatchSpawning = false;
            batchRoutine = null;
            if (PhaseIsCurrent(sessionVersion, phaseVersion) && CurrentState == State.Playing)
                QueueCurrentPhaseCue(CombatDialogueCueTrigger.DiceBatchReady);
            if (activeDice.Count == 0 && CurrentState == State.Playing && enemyRuntime.ShouldSpawnDice)
                ScheduleNextBatch(encounterData.BatchRespawnDelay);
        }

        private IEnumerator WaitForCombatActiveDelay(float duration, int sessionVersion, int phaseVersion)
        {
            float elapsed = 0f;
            while ((elapsed < duration || CurrentState == State.DialoguePause) && PhaseIsCurrent(sessionVersion, phaseVersion))
            {
                if (CurrentState == State.Playing)
                    elapsed += Time.deltaTime;
                else if (CurrentState != State.DialoguePause)
                    yield break;
                yield return null;
            }
        }

        private void StopBatchAndClearDice()
        {
            diceSabotage?.Cancel(); diceSabotage = null;
            if (batchRoutine != null) StopCoroutine(batchRoutine);
            batchRoutine = null;
            isBatchSpawning = false;
            activeDice.Clear();
            boardView.ClearRuntimeDice();
        }

        private void EndCombat(State result)
        {
            if (!isPlaying || (result != State.Victory && result != State.Defeat && result != State.Special)) return;
            CombatResult combatResult = result == State.Victory
                ? CombatResult.Victory
                : result == State.Defeat ? CombatResult.Defeat : CombatResult.Special;
            if (encounterData != null && !encounterData.OutcomeRules.Allows(combatResult))
                return;
            LastDefeatHeartPose = result == State.Defeat && boardView != null
                ? boardView.CapturePlayerHeartPose() : default;
            CurrentState = result;
            if (result == State.Defeat && encounterData.DefeatPresentation != null &&
                encounterData.DefeatPresentation.IsConfigured)
            {
                BeginResultPresentation(combatResult, encounterData.DefeatPresentation.Dialogue,
                    encounterData.DefeatPresentation.HazardFadeDuration);
                return;
            }
            if (result == State.Victory && encounterData.VictoryPresentation != null &&
                encounterData.VictoryPresentation.IsConfigured)
            {
                enemyRuntime?.CompleteVictory();
                BeginResultPresentation(combatResult, encounterData.VictoryPresentation.Dialogue,
                    encounterData.VictoryPresentation.HazardFadeDuration);
                return;
            }
            if (result == State.Victory && encounterData.VictoryFadeDuration > 0f)
            {
                enemyRuntime?.CompleteVictory();
                StopBatchAndClearDice();
                StopAutoDialogue();
                pendingCombatCues.Clear();
                boardView.ClearRuntimeBullets();
                boardView.SetCursorVisible(false);
                StartCoroutine(VictoryFadeRoutine(playRequestVersion));
                return;
            }
            if (result == State.Victory) enemyRuntime?.CompleteVictory();
            if (result == State.Defeat && encounterData.OutcomeRules.ShowRetryOnDefeat)
            {
                StartCoroutine(CaptureDefeatBackdropAndComplete(playRequestVersion));
                return;
            }
            StopEncounterRuntime();
            Complete(combatResult);
        }

        private IEnumerator CaptureDefeatBackdropAndComplete(int request)
        {
            // Keep the rendered fight until capture; the real Heart is replaced by Retry's
            // split sprite, so it must not remain baked into the frozen background.
            boardView.ActiveEnemyActor?.SetPaused(true);
            boardView.SetCursorVisible(false);
            boardView.SetAttackAudioPaused(true);
            yield return new WaitForEndOfFrame();
            if (!isPlaying || request != playRequestVersion || CurrentState != State.Defeat) yield break;
            defeatBackdrop = ScreenCapture.CaptureScreenshotAsTexture();
            StopEncounterRuntime(false);
            Complete(CombatResult.Defeat);
            ReleaseDefeatBackdrop(); // A Retry callback takes ownership synchronously.
        }

        private void ReleaseDefeatBackdrop()
        {
            if (defeatBackdrop == null) return;
            if (Application.isPlaying) Destroy(defeatBackdrop);
            else DestroyImmediate(defeatBackdrop);
            defeatBackdrop = null;
        }

        private void StopAutoDialogue()
        {
            if (activeAutoDialogueRoutine == null) return;
            StopCoroutine(activeAutoDialogueRoutine);
            activeAutoDialogueRoutine = null;
            GameplayUIRoot.Instance?.Dialogue?.ForceClose();
        }

        private IEnumerator VictoryFadeRoutine(int request)
        {
            yield return boardView.FadeEnemyPresentation(encounterData.VictoryFadeDuration);
            if (!isPlaying || request != playRequestVersion || CurrentState != State.Victory) yield break;
            StopEncounterRuntime();
            Complete(CombatResult.Victory);
        }

        private void BeginResultPresentation(CombatResult result, DialogueData dialogueData, float hazardFadeDuration)
        {
            if (resultPresentationRoutine != null)
                return;

            StopBatchAndClearDice();
            pendingCombatCues.Clear();
            if (activeAutoDialogueRoutine != null)
            {
                StopCoroutine(activeAutoDialogueRoutine);
                activeAutoDialogueRoutine = null;
            }
            GameplayUIRoot.Instance?.Dialogue?.ForceClose();
            if (result == CombatResult.Defeat) enemyRuntime?.Cancel();
            else boardView.ActiveEnemyActor?.SetPaused(true);
            boardView.ClearPlayerConstraint();
            boardView.SetCursorVisible(false);
            resultPresentationRoutine = StartCoroutine(PlayResultPresentation(result, playRequestVersion, dialogueData, hazardFadeDuration));
        }

        private IEnumerator PlayResultPresentation(CombatResult result, int requestVersion, DialogueData dialogueData, float hazardFadeDuration)
        {
            State expectedState = result == CombatResult.Victory ? State.Victory : State.Defeat;
            yield return boardView.FadeRuntimeHazards(hazardFadeDuration);

            if (!isPlaying || requestVersion != playRequestVersion || CurrentState != expectedState)
                yield break;

            DialogueController dialogue = GameplayUIRoot.Instance != null
                ? GameplayUIRoot.Instance.Dialogue
                : null;
            bool dialogueEnded = false;
            if (dialogue == null || !dialogue.Play(
                    dialogueData,
                    _ => dialogueEnded = true,
                    DialoguePlaybackMode.CallerOwnedPause,
                    false))
            {
                Debug.LogError("[CombatController] Result presentation requires an available DialogueController.", this);
                dialogueEnded = true;
            }

            while (isPlaying && requestVersion == playRequestVersion && !dialogueEnded)
                yield return null;

            if (!isPlaying || requestVersion != playRequestVersion)
                yield break;

            if (result == CombatResult.Victory && encounterData.VictoryFadeDuration > 0f)
                yield return boardView.FadeEnemyPresentation(encounterData.VictoryFadeDuration);
            if (!isPlaying || requestVersion != playRequestVersion || CurrentState != expectedState)
                yield break;
            resultPresentationRoutine = null;
            if (result == CombatResult.Defeat && encounterData.OutcomeRules.ShowRetryOnDefeat)
            {
                yield return CaptureDefeatBackdropAndComplete(requestVersion);
                yield break;
            }
            StopEncounterRuntime(false);
            Complete(result);
        }

        private void Complete(CombatResult result)
        {
            if (!isPlaying) return;
            isPlaying = false;
            AudioService.Instance?.ReleaseMusicOwner(this);
            Action<CombatResult> callback = completionCallback;
            completionCallback = null;
            ReleaseInputClaim();
            callback?.Invoke(result);
        }

        private void ResetRuntimeState(bool resetLifecycleState = true)
        {
            ReleaseDefeatBackdrop();
            LastDefeatHeartPose = default;
            AudioService.Instance?.ReleaseMusicOwner(this);
            StopEncounterRuntime();
            encounterTimeRemaining = 0f;
            batchIndex = 0;
            CurrentState = State.Idle;
            if (!resetLifecycleState) return;
            isPlaying = false;
            completionCallback = null;
            ReleaseInputClaim();
        }

        private GameplayInputGate ResolveInputGate() => GameplayUIRoot.Instance != null ? GameplayUIRoot.Instance.InputGate : null;
        private void ReleaseInputClaim()
        {
            GameplayInputGate gate = inputGate;
            GameplayInputToken token = inputToken;
            inputGate = null;
            inputToken = default;
            if (gate != null && token.IsValid) gate.Release(token);
        }
        private bool HasCombatInput() => inputGate != null && inputGate.IsActive(inputToken) && inputGate.Allows(GameplayInputMode.Combat);
        private bool SessionIsCurrent(int version) => isPlaying && enemyRuntime != null && enemyRuntime.SessionVersion == version;
        private bool PhaseIsCurrent(int session, int phase) => SessionIsCurrent(session) && enemyRuntime.PhaseVersion == phase;

        private void StopEncounterRuntime(bool stopCoroutines = true)
        {
            ResetGuidedTutorial();
            if (stopCoroutines)
                StopAllCoroutines();
            resultPresentationRoutine = null;
            batchRoutine = null;
            activeDice.Clear();
            isBatchSpawning = false;
            pendingCombatCues.Clear();
            caughtTutorialSymbols.Clear();
            cursorWasStunned = false;
            tutorialActive = false;
            tutorialOpeningBatchPending = false;
            activeAutoDialogueRoutine = null;
            observedMoveVersion = 0;
            exclusiveDiceMoveActive = false;
            isRecoveringPlayerTime = false;
            pendingRecoveryTime = 0f;
            recoveryDiceAges.Clear();
            recoveryDropRemaining = 0f;
            recoveryDropIndex = 0;
            diceSabotage?.Cancel(); diceSabotage = null;
            debugVictoryKeyPressCount = 0;
            debugNextPhaseKeyPressCount = 0;
            HideTutorialInstruction();
            GameplayUIRoot.Instance?.Dialogue?.ForceClose();
            enemyRuntime?.Cancel();
            enemyRuntime = null;
            if (boardView == null) return;
            boardView.ClearCombatRuntime();
            boardView.SetCursorVisible(false);
        }

        private float ResolveActiveMaximumTime()
        {
            if (encounterData == null)
                return 1f;

            float authoredMaximum = tutorialActive && encounterData.TutorialData != null
                ? encounterData.TutorialData.PlayerTime
                : encounterData.EncounterDuration;
            return Mathf.Max(1f, GameplayDifficultySettings.ScalePlayerTime(authoredMaximum, activeDifficulty));
        }
    }
}
