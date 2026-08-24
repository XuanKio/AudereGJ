using System;
using System.Collections;
using System.Collections.Generic;
using Audere.Audio;
using Audere.Dialogue;
using Audere.GameplayInput;
using UnityEngine;

namespace Audere.Combat
{
    public enum CombatResult { Victory, Defeat, Cancelled, Special }

    [DisallowMultipleComponent]
    public sealed class CombatController : MonoBehaviour
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

        private void Awake()
        {
            if (boardView == null)
                Debug.LogError("[CombatController] Assign Combat Board View directly; scene search is not supported.", this);
        }

        private void Start() { if (playOnStart) BeginEncounter(); }
        private void OnDisable() { if (isPlaying) Cancel(); else ResetRuntimeState(); }

        private void Update()
        {
            if (tutorialInstructionAwaitingInteraction)
            {
                if (HasCombatInput() && (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)))
                    DismissTutorialOnPlayerInteraction();
                return;
            }
            if (CurrentState != State.Playing || encounterData == null || boardView == null || enemyRuntime == null) return;
            float deltaTime = Time.deltaTime;
            float timeScale = tutorialActive ? tutorialTimeScale : 1f;
            encounterTimeRemaining = Mathf.Max(0f, encounterTimeRemaining - deltaTime * timeScale);
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
            TickDice(deltaTime);
            enemyRuntime.Tick(deltaTime);
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
                if (catchPressed) TryCatchUnderCursor();
                else if (rerollPressed) TryRerollUnderCursor();
            }
            if (CurrentState == State.Playing)
                TryStartPendingCombatCue();
        }

        public bool Play(CombatEncounterData data, Action<CombatResult> onEnded = null)
        {
            if (data == null || boardView == null || data.EnemyDefinition == null)
            {
                Debug.LogError("[CombatController] Assign Encounter Data, Enemy Definition and Combat Board View.", this);
                return false;
            }
            if (!data.EnemyDefinition.Validate(out string error)) { Debug.LogError(error, data.EnemyDefinition); return false; }
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
            completionCallback = onEnded;
            isPlaying = true;
            StartEncounterRuntime(requestVersion);
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

        private void StartEncounterRuntime(int sessionVersion)
        {
            ResetRuntimeState(false);
            tutorialActive = encounterData.TutorialData != null;
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
            try
            {
                CombatEnemyDefinition runtimeDefinition = tutorialActive
                    ? encounterData.TutorialData.EnemyDefinition
                    : encounterData.EnemyDefinition;
                enemyRuntime = new CombatEnemyRuntime(runtimeDefinition, boardView, new SystemCombatRandom(sessionVersion), sessionVersion);
                enemyRuntime.Start();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                Complete(CombatResult.Cancelled);
                return;
            }
            boardView.SetEnemyHealthVisible(enemyRuntime.ShowsHealth);
            UpdateEnemyHealthImmediate();
            CurrentState = State.EncounterIntro;
            StartCoroutine(EncounterIntro(sessionVersion));
        }

        private IEnumerator EncounterIntro(int sessionVersion)
        {
            yield return boardView.PlayEnemyIntro();
            yield return new WaitForSecondsRealtime(.12f);
            if (!SessionIsCurrent(sessionVersion) || CurrentState != State.EncounterIntro) yield break;
            boardView.ResetPlayer();
            CombatDialogueCue cue = FindTriggeredCue(CombatDialogueCueTrigger.PhaseEnter);
            if (cue != null)
            {
                if (cue.TutorialFocus == CombatTutorialFocus.Time)
                    yield return PreviewEncounterTimeDrain(sessionVersion, enemyRuntime.PhaseVersion);
                if (!SessionIsCurrent(sessionVersion) || CurrentState != State.EncounterIntro) yield break;
                HideTutorialInstruction();
                CurrentState = State.DialoguePause;
                enemyRuntime.PauseForDialogue();
                yield return PlayDialogueSequence(cue, sessionVersion, enemyRuntime.PhaseVersion);
                if (!SessionIsCurrent(sessionVersion)) yield break;
                yield return PresentPausedCueInstruction(cue, sessionVersion, enemyRuntime.PhaseVersion);
                if (!SessionIsCurrent(sessionVersion)) yield break;
                enemyRuntime.ResumeFromDialogue();
            }
            CurrentState = State.Playing;
            ScheduleNextBatch(0f);
        }

        private void TickDice(float deltaTime)
        {
            Rect playRect = boardView.PlayArea != null ? boardView.PlayArea.rect : default;
            for (int i = activeDice.Count - 1; i >= 0; i--)
            {
                CombatDieView die = activeDice[i];
                if (die == null || die.IsCaptured || !die.gameObject.activeInHierarchy) { activeDice.RemoveAt(i); continue; }
                die.TickMovement(playRect, deltaTime);
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
                if (die != null && !die.IsCaptured && !die.IsRerolling && die.gameObject.activeInHierarchy) die.ConstrainToBounds(playRect);
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
                activeDice[i] = boardView.RerollDie(die, encounterData.RollSymbol());
                AudioService.Instance?.Play(AudioId.Dice_Roll);
                QueueCurrentPhaseCue(CombatDialogueCueTrigger.DiceRerolled);
                return;
            }
        }

        private void CatchDie(CombatDieView die)
        {
            if (die == null || die.IsCaptured || !activeDice.Remove(die)) return;
            CombatSymbol symbol = die.Symbol;
            die.PlayCaptured();
            AudioService.Instance?.Play(AudioId.Dice_Catch);
            ApplyImmediateDiceEffect(symbol);
            if (CurrentState == State.Playing)
            {
                QueueCurrentPhaseCue(CombatDialogueCueTrigger.DiceCaught, symbol, true);
                if (tutorialActive && caughtTutorialSymbols.Add(symbol) && caughtTutorialSymbols.Count >= 3)
                    QueueCurrentPhaseCue(CombatDialogueCueTrigger.AllDiceTypesCaught);
            }
            if (activeDice.Count == 0 && !isBatchSpawning && CurrentState == State.Playing) ScheduleNextBatch(encounterData.BatchRespawnDelay);
        }

        private void ApplyImmediateDiceEffect(CombatSymbol symbol)
        {
            CombatDiceDefinition definition = CombatDiceConstants.GetDefinition(symbol);
            switch (definition.Ability)
            {
                case CombatDiceAbility.DamageEnemy:
                    int previous = enemyRuntime.CurrentHealth;
                    int maximum = Mathf.Max(1, enemyRuntime.CurrentMaxHealth);
                    CombatEnemyProgression progression = enemyRuntime.ApplyDamage(Mathf.Max(1, Mathf.RoundToInt(definition.EffectAmount)), out int applied);
                    if (applied > 0)
                    {
                        boardView.PlayEnemyDamageFeedback(previous / (float)maximum, enemyRuntime.CurrentHealth / (float)maximum);
                        boardView.PlayEnemyDamageNumber(applied);
                        boardView.PlayAttackHitVfx();
                        boardView.TriggerEnemyHitFeedback();
                        AudioService.Instance?.Play(AudioId.Dice_Hit);
                    }
                    if (progression == CombatEnemyProgression.Victory) EndCombat(State.Victory);
                    else if (progression == CombatEnemyProgression.PhaseBreak) StartCoroutine(PhaseBreakRoutine(enemyRuntime.SessionVersion, enemyRuntime.PhaseVersion));
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

        private IEnumerator PhaseBreakRoutine(int sessionVersion, int oldPhaseVersion)
        {
            CurrentState = State.PhaseTransition;
            pendingCombatCues.Clear();
            HideTutorialInstruction();
            StopBatchAndClearDice();
            boardView.ClearRuntimeBullets(sessionVersion, oldPhaseVersion);
            CombatDialogueCue exitCue = FindTriggeredCue(CombatDialogueCueTrigger.PhaseExit);
            if (exitCue != null) yield return PlayDialogueSequence(exitCue, sessionVersion, oldPhaseVersion);
            if (!PhaseIsCurrent(sessionVersion, oldPhaseVersion)) yield break;
            yield return PresentPausedCueInstruction(exitCue, sessionVersion, oldPhaseVersion);
            if (!PhaseIsCurrent(sessionVersion, oldPhaseVersion)) yield break;
            enemyRuntime.CompletePhaseBreak();
            UpdateEnemyHealthImmediate();
            int newPhaseVersion = enemyRuntime.PhaseVersion;
            CombatDialogueCue enterCue = FindTriggeredCue(CombatDialogueCueTrigger.PhaseEnter);
            if (enterCue != null)
            {
                HideTutorialInstruction();
                enemyRuntime.PauseForDialogue();
                yield return PlayDialogueSequence(enterCue, sessionVersion, newPhaseVersion);
                if (!PhaseIsCurrent(sessionVersion, newPhaseVersion)) yield break;
                yield return PresentPausedCueInstruction(enterCue, sessionVersion, newPhaseVersion);
                if (!PhaseIsCurrent(sessionVersion, newPhaseVersion)) yield break;
                enemyRuntime.ResumeFromDialogue();
            }
            CurrentState = State.Playing;
            ScheduleNextBatch(0f);
        }

        private bool TryStartMidPhaseDialogue()
        {
            if (tutorialOpeningBatchPending)
                return false;
            CombatDialogueCue cue = FindTriggeredCue(CombatDialogueCueTrigger.HealthAtOrBelow) ?? FindTriggeredCue(CombatDialogueCueTrigger.ElapsedActiveTime);
            if (cue == null) return false;
            StartCoroutine(MidPhaseDialogueRoutine(cue, enemyRuntime.SessionVersion, enemyRuntime.PhaseVersion));
            return true;
        }

        private bool TryStartPendingCombatCue()
        {
            while (pendingCombatCues.Count > 0)
            {
                PendingCombatCue pending = pendingCombatCues.Dequeue();
                if (!PhaseIsCurrent(pending.SessionVersion, pending.PhaseVersion))
                    continue;

                StartCoroutine(MidPhaseDialogueRoutine(
                    pending.Cue,
                    pending.SessionVersion,
                    pending.PhaseVersion));
                return true;
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

            pendingCombatCues.Enqueue(new PendingCombatCue(
                cue,
                enemyRuntime.SessionVersion,
                enemyRuntime.PhaseVersion));
        }

        private IEnumerator MidPhaseDialogueRoutine(CombatDialogueCue cue, int sessionVersion, int phaseVersion)
        {
            HideTutorialInstruction();
            CurrentState = State.DialoguePause;
            enemyRuntime.PauseForDialogue();
            yield return PlayDialogueSequence(cue, sessionVersion, phaseVersion);
            if (!PhaseIsCurrent(sessionVersion, phaseVersion)) yield break;
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
            bool hasSymbol = false)
        {
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
                    trigger == CombatDialogueCueTrigger.AllDiceTypesCaught;
                bool triggered = isImmediateEvent ||
                    (trigger == CombatDialogueCueTrigger.HealthAtOrBelow && enemyRuntime.CurrentHealth <= cue.TriggerValue) ||
                    (trigger == CombatDialogueCueTrigger.ElapsedActiveTime && enemyRuntime.PhaseElapsed >= cue.TriggerValue);
                if (triggered && cue.FilterBySymbol && (!hasSymbol || !cue.MatchesSymbol(symbol)))
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
                actualSessionVersion);
            enemyRuntime.Start();
            encounterTimeRemaining = encounterData.EncounterDuration;
            batchIndex = 0;
            caughtTutorialSymbols.Clear();
            cursorWasStunned = false;

            boardView.PrepareEncounter(encounterData.EnemyDisplayName);
            boardView.ResetPlayer();
            boardView.UpdateTimer(1f);
            boardView.SetEnemyHealthVisible(enemyRuntime.ShowsHealth);
            UpdateEnemyHealthImmediate();

            CurrentState = State.Playing;
            ScheduleNextBatch(0f);
        }

        private void UpdateEnemyHealthImmediate()
        {
            if (enemyRuntime == null || !enemyRuntime.ShowsHealth) return;
            float normalized = enemyRuntime.CurrentHealth / (float)Mathf.Max(1, enemyRuntime.CurrentMaxHealth);
            boardView.PlayEnemyDamageFeedback(normalized, normalized);
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
                !tutorialActive);
            float maximumTime = ResolveActiveMaximumTime();
            boardView.PlayPlayerDamageFeedback(previous / maximumTime, encounterTimeRemaining / maximumTime);
            AudioService.Instance?.Play(AudioId.Nilah_Hurt);
            if (encounterTimeRemaining <= 0f) EndCombat(State.Defeat);
            else QueueCurrentPhaseCue(CombatDialogueCueTrigger.PlayerHit);
        }

        private static float ClampPlayerTimeAfterDamage(float current, float penalty, bool tutorialDone)
        {
            // The onboarding must reach Timor's reset beat even if the player is
            // still learning to dodge. Damage keeps its authored size so the
            // TIME rule remains truthful, but cannot end the tutorial attempt.
            float minimum = tutorialDone ? 0f : 1f;
            return Mathf.Max(minimum, current - Mathf.Max(0f, penalty));
        }

        private void ScheduleNextBatch(float delay)
        {
            if (batchRoutine != null || CurrentState != State.Playing) return;
            batchRoutine = StartCoroutine(SpawnBatchAfterDelay(delay, enemyRuntime.SessionVersion, enemyRuntime.PhaseVersion));
        }

        private IEnumerator SpawnBatchAfterDelay(float delay, int sessionVersion, int phaseVersion)
        {
            if (delay > 0f)
                yield return WaitForCombatActiveDelay(delay, sessionVersion, phaseVersion);
            if (!PhaseIsCurrent(sessionVersion, phaseVersion) || CurrentState != State.Playing) { batchRoutine = null; yield break; }
            isBatchSpawning = true;
            bool isOpeningTutorialBatch = tutorialActive && batchIndex == 0;
            batchIndex++;
            IReadOnlyList<CombatSymbol> openingDice = isOpeningTutorialBatch
                ? encounterData.TutorialData.OpeningDice
                : null;
            int diceCount = openingDice != null ? openingDice.Count : encounterData.DicePerBatch;
            for (int i = 0; i < diceCount; i++)
            {
                if (!PhaseIsCurrent(sessionVersion, phaseVersion) || CurrentState != State.Playing) break;
                float speed = UnityEngine.Random.Range(encounterData.MinimumDiceSpeed, encounterData.MaximumDiceSpeed);
                CombatSymbol symbol = openingDice != null ? openingDice[i] : encounterData.RollSymbol();
                CombatDieView die = boardView.SpawnDie(symbol, speed);
                if (die != null) activeDice.Add(die);
                yield return WaitForCombatActiveDelay(spawnStagger, sessionVersion, phaseVersion);
            }
            isBatchSpawning = false;
            batchRoutine = null;
            if (PhaseIsCurrent(sessionVersion, phaseVersion) && CurrentState == State.Playing)
                QueueCurrentPhaseCue(CombatDialogueCueTrigger.DiceBatchReady);
            if (activeDice.Count == 0 && CurrentState == State.Playing) ScheduleNextBatch(encounterData.BatchRespawnDelay);
        }

        private IEnumerator WaitForCombatActiveDelay(float duration, int sessionVersion, int phaseVersion)
        {
            float elapsed = 0f;
            while (elapsed < duration && PhaseIsCurrent(sessionVersion, phaseVersion))
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
            if (batchRoutine != null) StopCoroutine(batchRoutine);
            batchRoutine = null;
            isBatchSpawning = false;
            activeDice.Clear();
            boardView.ClearRuntimeDice();
        }

        private void EndCombat(State result)
        {
            if (!isPlaying || (result != State.Victory && result != State.Defeat && result != State.Special)) return;
            CurrentState = result;
            if (result == State.Victory) enemyRuntime?.CompleteVictory();
            StopEncounterRuntime();
            Complete(result == State.Victory ? CombatResult.Victory : result == State.Defeat ? CombatResult.Defeat : CombatResult.Special);
        }

        private void Complete(CombatResult result)
        {
            if (!isPlaying) return;
            isPlaying = false;
            Action<CombatResult> callback = completionCallback;
            completionCallback = null;
            ReleaseInputClaim();
            callback?.Invoke(result);
        }

        private void ResetRuntimeState(bool resetLifecycleState = true)
        {
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

        private void StopEncounterRuntime()
        {
            StopAllCoroutines();
            batchRoutine = null;
            activeDice.Clear();
            isBatchSpawning = false;
            pendingCombatCues.Clear();
            caughtTutorialSymbols.Clear();
            cursorWasStunned = false;
            tutorialActive = false;
            tutorialOpeningBatchPending = false;
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
            if (tutorialActive && encounterData != null && encounterData.TutorialData != null)
                return Mathf.Max(1f, encounterData.TutorialData.PlayerTime);
            return encounterData != null ? Mathf.Max(1f, encounterData.EncounterDuration) : 1f;
        }
    }
}
