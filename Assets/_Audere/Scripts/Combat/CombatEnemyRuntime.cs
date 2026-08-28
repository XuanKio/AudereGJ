using System;
using System.Collections.Generic;
using UnityEngine;

namespace Audere.Combat
{
    public enum CombatEnemyRuntimeState
    {
        Inactive = 0,
        EnteringPhase = 1,
        Playing = 2,
        PausedForDialogue = 3,
        TransitioningPhase = 4,
        Completed = 5,
        Cancelled = 6,
    }

    public enum CombatEnemyProgression
    {
        None = 0,
        PhaseBreak = 1,
        Victory = 2,
    }

    public sealed class CombatEnemyRuntime
    {
        private readonly CombatEnemyDefinition definition;
        private readonly CombatBoardView board;
        private readonly ICombatRandom random;
        private readonly HashSet<string> playedCueIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> resolvedCueIds = new HashSet<string>(StringComparer.Ordinal);
        private CombatMoveSelector moveSelector;
        private ICombatMoveExecution activeMove;
        private CombatEnemyActor actor;
        private int currentHealth;
        private int sharedHealth;
        private float phaseElapsed;
        private float moveLeadInRemaining;
        private readonly bool allowVictory;
        private int capturedBatchesInPhase;
        private bool batchProgressionPending;
        private bool healthProgressionPending;

        public CombatEnemyRuntime(
            CombatEnemyDefinition definition,
            CombatBoardView board,
            ICombatRandom random,
            int sessionVersion,
            bool allowVictory = true)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            this.board = board ?? throw new ArgumentNullException(nameof(board));
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            SessionVersion = sessionVersion;
            this.allowVictory = allowVictory;
            if (!definition.Validate(out string error))
                throw new InvalidOperationException(error);
        }

        public CombatEnemyRuntimeState State { get; private set; } = CombatEnemyRuntimeState.Inactive;
        public int SessionVersion { get; }
        public int PhaseVersion { get; private set; }
        public int PhaseIndex { get; private set; } = -1;
        public int PhaseCount => definition.PhaseCount;
        public int CurrentHealth => definition.PhasePolicy == CombatPhasePolicy.SharedHealthThresholds ||
                                    definition.PhasePolicy == CombatPhasePolicy.CapturedDiceBatchSequence
            ? sharedHealth
            : currentHealth;
        public int CurrentMaxHealth => definition.PhasePolicy == CombatPhasePolicy.SharedHealthThresholds ||
                                       definition.PhasePolicy == CombatPhasePolicy.CapturedDiceBatchSequence
            ? definition.SharedMaxHealth
            : CurrentPhase != null ? CurrentPhase.MaxHealth : 0;
        public float PhaseElapsed => phaseElapsed;
        public CombatPhaseDefinition CurrentPhase => definition.GetPhase(PhaseIndex);
        public CombatEnemyActor Actor => actor;
        public CombatMoveDefinition CurrentMove { get; private set; }
        public int MoveVersion { get; private set; }
        public bool ShowsHealth => definition.PhasePolicy != CombatPhasePolicy.TimedSequence;
        public bool AcceptsDamage => State == CombatEnemyRuntimeState.Playing && ShowsHealth &&
            !CurrentPhase.AdvanceOnMoveComplete && !healthProgressionPending;
        public bool UsesCapturedBatchProgression => definition.PhasePolicy == CombatPhasePolicy.CapturedDiceBatchSequence;
        public bool IsBatchProgressionPending => batchProgressionPending;
        public bool ShouldSpawnDice => CurrentPhase != null && CurrentPhase.SpawnDice;
        public CombatDiceBatchDefinition CurrentDiceBatch => UsesCapturedBatchProgression ? CurrentPhase?.DiceBatch : null;
        public bool CanPlayerBeDefeated => CurrentPhase != null && CurrentPhase.AllowsPlayerDefeat && !HasUnresolvedPlayerDefeatGate();

        public void Start()
        {
            if (State != CombatEnemyRuntimeState.Inactive)
                throw new InvalidOperationException("Enemy runtime can only be started once.");
            actor = board.SpawnEnemyActor(definition.ActorPrefab, SessionVersion);
            if (actor == null)
                throw new InvalidOperationException($"Could not spawn actor for enemy '{definition.EnemyId}'.");
            actor.Initialize(new CombatEnemyMechanicContext(board, SessionVersion));
            sharedHealth = definition.SharedMaxHealth;
            EnterPhase(0);
        }

        public void Tick(float activeDeltaTime)
        {
            if (State != CombatEnemyRuntimeState.Playing)
                return;

            // Keep the current simulation alive while a required auto-dialogue finishes.
            // Damage has already reached this threshold; do not require another hit or
            // carry overflow into the next phase. The controller owns result/phase cleanup.
            if (healthProgressionPending && !HasUnresolvedSharedHealthGate())
            {
                healthProgressionPending = false;
                BeginProgression(PhaseIndex >= definition.PhaseCount - 1
                    ? CombatEnemyProgression.Victory
                    : CombatEnemyProgression.PhaseBreak);
                return;
            }

            phaseElapsed += Mathf.Max(0f, activeDeltaTime);
            if (definition.PhasePolicy == CombatPhasePolicy.TimedSequence &&
                phaseElapsed >= CurrentPhase.Duration)
            {
                BeginProgression(PhaseIndex >= definition.PhaseCount - 1
                    ? CombatEnemyProgression.Victory
                    : CombatEnemyProgression.PhaseBreak);
                return;
            }

            if (moveLeadInRemaining > 0f)
            {
                float heldTime = Mathf.Min(moveLeadInRemaining, Mathf.Max(0f, activeDeltaTime));
                moveLeadInRemaining -= heldTime;
                activeDeltaTime -= heldTime;
                // Spend the remainder of the frame on the move. A slow first
                // frame must not turn a tiny authored lead-in into an empty beat.
                if (moveLeadInRemaining > 0f || activeDeltaTime <= 0f)
                    return;
            }
            if (activeMove == null && CurrentMove != null)
                activeMove = CurrentMove.CreateExecution(new CombatMoveExecutionContext(
                    board, actor, random, SessionVersion, PhaseVersion));
            activeMove?.Tick(activeDeltaTime);
            if (activeMove != null && activeMove.IsComplete && CurrentPhase.AdvanceOnMoveComplete)
            {
                BeginProgression(CombatEnemyProgression.PhaseBreak);
                return;
            }
            if (activeMove == null || activeMove.IsComplete)
                StartNextMove();
        }

        public CombatEnemyProgression ApplyDamage(int amount, out int appliedDamage)
        {
            appliedDamage = 0;
            if (!AcceptsDamage || amount <= 0)
                return CombatEnemyProgression.None;

            if (definition.PhasePolicy == CombatPhasePolicy.PerPhaseHealth)
            {
                int previous = currentHealth;
                int minimumHealth = (!allowVictory || HasUnresolvedVictoryGate()) &&
                                    PhaseIndex >= definition.PhaseCount - 1 ? 1 : 0;
                currentHealth = Mathf.Max(minimumHealth, currentHealth - amount);
                appliedDamage = previous - currentHealth;
                if (currentHealth > 0)
                    return CombatEnemyProgression.None;
                return BeginProgression(PhaseIndex >= definition.PhaseCount - 1
                    ? CombatEnemyProgression.Victory
                    : CombatEnemyProgression.PhaseBreak);
            }

            if (definition.PhasePolicy == CombatPhasePolicy.CapturedDiceBatchSequence)
            {
                int previous = sharedHealth;
                sharedHealth = Mathf.Max(1, sharedHealth - amount);
                appliedDamage = previous - sharedHealth;
                return CombatEnemyProgression.None;
            }

            int threshold = CurrentPhase.SharedExitThreshold;
            int before = sharedHealth;
            sharedHealth = Mathf.Max(threshold, sharedHealth - amount);
            appliedDamage = before - sharedHealth;
            if (sharedHealth > threshold)
                return CombatEnemyProgression.None;
            if (HasUnresolvedSharedHealthGate())
            {
                healthProgressionPending = true;
                return CombatEnemyProgression.None;
            }
            return BeginProgression(PhaseIndex >= definition.PhaseCount - 1
                ? CombatEnemyProgression.Victory
                : CombatEnemyProgression.PhaseBreak);
        }

        public void CompletePhaseBreak()
        {
            if (State != CombatEnemyRuntimeState.TransitioningPhase)
                return;
            EnterPhase(PhaseIndex + 1);
        }

        public void RestartFromBeginning()
        {
            if (State == CombatEnemyRuntimeState.Cancelled || actor == null)
                return;

            CancelActiveMove();
            if (CurrentPhase != null)
                actor.ExitPhase(CurrentPhase, PhaseIndex);
            actor.SetPaused(false);
            sharedHealth = definition.SharedMaxHealth;
            playedCueIds.Clear();
            resolvedCueIds.Clear();
            EnterPhase(0);
        }

        public void PauseForDialogue()
        {
            if (State != CombatEnemyRuntimeState.Playing)
                return;
            State = CombatEnemyRuntimeState.PausedForDialogue;
            actor?.SetPaused(true);
        }

        public void ResumeFromDialogue()
        {
            if (State != CombatEnemyRuntimeState.PausedForDialogue)
                return;
            actor?.SetPaused(false);
            State = CombatEnemyRuntimeState.Playing;
        }

        public void CompleteVictory()
        {
            if (State == CombatEnemyRuntimeState.Cancelled)
                return;
            CancelActiveMove();
            State = CombatEnemyRuntimeState.Completed;
        }

        public void Cancel()
        {
            if (State == CombatEnemyRuntimeState.Cancelled)
                return;
            CancelActiveMove();
            actor?.Shutdown();
            State = CombatEnemyRuntimeState.Cancelled;
        }

        public bool MarkCuePlayed(CombatDialogueCue cue)
        {
            return cue != null && !string.IsNullOrWhiteSpace(cue.OneShotKey) &&
                (cue.RepeatOnTrigger || playedCueIds.Add(cue.OneShotKey));
        }

        public void MarkCueResolved(CombatDialogueCue cue)
        {
            if (cue != null && !string.IsNullOrWhiteSpace(cue.CueId))
                resolvedCueIds.Add(cue.CueId);
        }

        public CombatEnemyProgression NotifyCapturedDiceBatch()
        {
            if (!UsesCapturedBatchProgression || State != CombatEnemyRuntimeState.Playing || CurrentPhase == null)
                return CombatEnemyProgression.None;

            capturedBatchesInPhase++;
            if (capturedBatchesInPhase < CurrentPhase.RequiredCapturedBatches)
                return CombatEnemyProgression.None;

            if (HasUnresolvedPhaseAdvanceGate())
            {
                batchProgressionPending = true;
                return CombatEnemyProgression.None;
            }

            return PhaseIndex >= definition.PhaseCount - 1
                ? CombatEnemyProgression.None
                : BeginProgression(CombatEnemyProgression.PhaseBreak);
        }

        public CombatEnemyProgression TryReleasePendingBatchProgression()
        {
            if (!batchProgressionPending || State != CombatEnemyRuntimeState.Playing || HasUnresolvedPhaseAdvanceGate())
                return CombatEnemyProgression.None;
            batchProgressionPending = false;
            return PhaseIndex >= definition.PhaseCount - 1
                ? CombatEnemyProgression.None
                : BeginProgression(CombatEnemyProgression.PhaseBreak);
        }

        public bool IsCueResolved(string cueId)
        {
            return !string.IsNullOrWhiteSpace(cueId) && resolvedCueIds.Contains(cueId);
        }

        private void EnterPhase(int phaseIndex)
        {
            PhaseIndex = phaseIndex;
            PhaseVersion++;
            State = CombatEnemyRuntimeState.EnteringPhase;
            phaseElapsed = 0f;
            capturedBatchesInPhase = 0;
            batchProgressionPending = false;
            healthProgressionPending = false;
            CombatPhaseDefinition phase = CurrentPhase;
            if (definition.PhasePolicy == CombatPhasePolicy.PerPhaseHealth)
                currentHealth = phase.MaxHealth;
            moveSelector = new CombatMoveSelector(phase.MoveSet, random);
            moveSelector.Reset();
            CurrentMove = null;
            MoveVersion = 0;
            actor.EnterPhase(phase, PhaseIndex);
            State = CombatEnemyRuntimeState.Playing;
            StartNextMove();
        }

        private CombatEnemyProgression BeginProgression(CombatEnemyProgression progression)
        {
            if (State != CombatEnemyRuntimeState.Playing)
                return CombatEnemyProgression.None;
            if (progression == CombatEnemyProgression.Victory && !allowVictory)
                return CombatEnemyProgression.None;
            State = progression == CombatEnemyProgression.Victory
                ? CombatEnemyRuntimeState.Completed
                : CombatEnemyRuntimeState.TransitioningPhase;
            CancelActiveMove();
            if (progression == CombatEnemyProgression.PhaseBreak)
                actor.ExitPhase(CurrentPhase, PhaseIndex);
            return progression;
        }

        private void StartNextMove()
        {
            if (State != CombatEnemyRuntimeState.Playing)
                return;
            activeMove?.Cancel();
            CombatMoveDefinition move = moveSelector.Next();
            CurrentMove = move;
            MoveVersion++;
            moveLeadInRemaining = move.LeadInDuration;
            activeMove = moveLeadInRemaining > 0f ? null : move.CreateExecution(new CombatMoveExecutionContext(
                board, actor, random, SessionVersion, PhaseVersion));
        }

        public bool HandleMoveInput(bool catchPressed, bool rerollPressed)
        {
            if (State != CombatEnemyRuntimeState.Playing || moveLeadInRemaining > 0f) return false;
            if (activeMove is ICombatMoveInputHandler handler)
            {
                handler.HandleInput(catchPressed, rerollPressed);
                return true;
            }
            return false;
        }

        private void CancelActiveMove()
        {
            activeMove?.Cancel();
            activeMove = null;
            CurrentMove = null;
        }

        private bool HasUnresolvedSharedHealthGate()
        {
            return HasUnresolvedPhaseAdvanceGate() ||
                (PhaseIndex >= definition.PhaseCount - 1 && (!allowVictory || HasUnresolvedVictoryGate()));
        }

        private bool HasUnresolvedVictoryGate()
        {
            IReadOnlyList<CombatDialogueCue> cues = CurrentPhase?.DialogueCues;
            if (cues == null)
                return false;
            for (int i = 0; i < cues.Count; i++)
            {
                CombatDialogueCue cue = cues[i];
                if (cue != null && cue.RequiredBeforeVictory && !IsCueResolved(cue.CueId))
                    return true;
            }
            return false;
        }

        private bool HasUnresolvedPhaseAdvanceGate()
        {
            IReadOnlyList<CombatDialogueCue> cues = CurrentPhase?.DialogueCues;
            if (cues == null)
                return false;
            for (int i = 0; i < cues.Count; i++)
            {
                CombatDialogueCue cue = cues[i];
                if (cue != null && cue.RequiredBeforePhaseAdvance && !IsCueResolved(cue.CueId))
                    return true;
            }
            return false;
        }

        private bool HasUnresolvedPlayerDefeatGate()
        {
            IReadOnlyList<CombatDialogueCue> cues = CurrentPhase?.DialogueCues;
            if (cues == null)
                return false;
            for (int i = 0; i < cues.Count; i++)
            {
                CombatDialogueCue cue = cues[i];
                if (cue != null && cue.RequiredBeforePlayerDefeat && !IsCueResolved(cue.CueId))
                    return true;
            }
            return false;
        }
    }
}
