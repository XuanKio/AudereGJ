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
        private float passiveDecayElapsed;
        private float moveLeadInRemaining;
        private readonly bool allowVictory;
        private readonly float healthMultiplier;
        private int capturedBatchesInPhase;
        private bool batchProgressionPending;
        private bool healthProgressionPending;
        private bool playingDamageReaction;
        private bool playingOpeningMove;
        private int queuedDamageReactions;
        private int recoveryMaxHealth;

        public CombatEnemyRuntime(
            CombatEnemyDefinition definition,
            CombatBoardView board,
            ICombatRandom random,
            int sessionVersion,
            bool allowVictory = true,
            float healthMultiplier = 1f)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            this.board = board ?? throw new ArgumentNullException(nameof(board));
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            SessionVersion = sessionVersion;
            this.allowVictory = allowVictory;
            if (float.IsNaN(healthMultiplier) || float.IsInfinity(healthMultiplier) || healthMultiplier <= 0f)
                throw new ArgumentOutOfRangeException(nameof(healthMultiplier), healthMultiplier, "Health multiplier must be finite and greater than zero.");
            this.healthMultiplier = healthMultiplier;
            if (!definition.Validate(out string error))
                throw new InvalidOperationException(error);
        }

        public CombatEnemyRuntimeState State { get; private set; } = CombatEnemyRuntimeState.Inactive;
        public int SessionVersion { get; }
        public int PhaseVersion { get; private set; }
        public int PhaseIndex { get; private set; } = -1;
        public int PhaseCount => definition.PhaseCount;
        public int CurrentHealth => definition.PhasePolicy == CombatPhasePolicy.SharedHealthThresholds ||
                                    definition.PhasePolicy == CombatPhasePolicy.CapturedDiceBatchSequence ||
                                    definition.PhasePolicy == CombatPhasePolicy.SharedHealthPlayerTime
            ? sharedHealth
            : currentHealth;
        public int CurrentMaxHealth => recoveryMaxHealth > 0 ? recoveryMaxHealth :
                                       definition.PhasePolicy == CombatPhasePolicy.SharedHealthThresholds ||
                                       definition.PhasePolicy == CombatPhasePolicy.CapturedDiceBatchSequence ||
                                    definition.PhasePolicy == CombatPhasePolicy.SharedHealthPlayerTime
            ? ScaleHealth(definition.SharedMaxHealth)
            : CurrentPhase != null ? ScaleHealth(CurrentPhase.MaxHealth) : 0;
        public float PhaseElapsed => phaseElapsed;
        public CombatPhaseDefinition CurrentPhase => definition.GetPhase(PhaseIndex);
        public CombatEnemyActor Actor => actor;
        public CombatMoveDefinition CurrentMove { get; private set; }
        public int MoveVersion { get; private set; }
        public CombatMoveDefinition LastCompletedMove { get; private set; }
        public int MoveCompletionVersion { get; private set; }
        public bool ShowsHealth => definition.PhasePolicy != CombatPhasePolicy.TimedSequence;
        public bool AcceptsDamage => State == CombatEnemyRuntimeState.Playing && ShowsHealth &&
            !CurrentPhase.AdvanceOnMoveComplete && !healthProgressionPending && !playingOpeningMove;
        public bool UsesCapturedBatchProgression => definition.PhasePolicy == CombatPhasePolicy.CapturedDiceBatchSequence;
        public bool IsBatchProgressionPending => batchProgressionPending;
        public bool ShouldSpawnDice => CurrentPhase != null && CurrentPhase.SpawnDice &&
            !playingOpeningMove && !(CurrentMove is ICombatExclusiveDiceMove);
        public bool IsOpeningMove => playingOpeningMove;
        public CombatDiceBatchDefinition CurrentDiceBatch => UsesCapturedBatchProgression ? CurrentPhase?.DiceBatch : null;
        public bool CanPlayerBeDefeated => CurrentPhase != null && CurrentPhase.AllowsPlayerDefeat && !HasUnresolvedPlayerDefeatGate();
        public float HealthMultiplier => healthMultiplier;
        public bool IsRecoveringPhaseHealth => State == CombatEnemyRuntimeState.TransitioningPhase && recoveryMaxHealth > 0;

        public bool TryBeginNextPhaseHealthRecovery()
        {
            if (State != CombatEnemyRuntimeState.TransitioningPhase ||
                definition.PhasePolicy != CombatPhasePolicy.PerPhaseHealth || PhaseIndex + 1 >= PhaseCount)
                return false;
            if (recoveryMaxHealth > 0) return true;
            recoveryMaxHealth = ScaleHealth(definition.GetPhase(PhaseIndex + 1).MaxHealth);
            currentHealth = 0;
            return true;
        }

        public int HealNextPhaseHealth(int amount)
        {
            if (!IsRecoveringPhaseHealth || amount <= 0) return 0;
            int applied = Mathf.Min(amount, recoveryMaxHealth - currentHealth);
            currentHealth += applied;
            return applied;
        }

        public float ScaleAuthoredHealthThreshold(float authoredThreshold)
        {
            return Mathf.Max(0f, authoredThreshold) * healthMultiplier;
        }

        public void Start() => Start(0);

        public void Start(int startPhaseIndex)
        {
            if (State != CombatEnemyRuntimeState.Inactive)
                throw new InvalidOperationException("Enemy runtime can only be started once.");
            if (startPhaseIndex < 0 || startPhaseIndex >= PhaseCount ||
                (startPhaseIndex > 0 && definition.PhasePolicy != CombatPhasePolicy.PerPhaseHealth))
                throw new ArgumentOutOfRangeException(nameof(startPhaseIndex), startPhaseIndex,
                    "Checkpoint start requires a valid phase with per-phase health.");
            actor = board.SpawnEnemyActor(definition.ActorPrefab, SessionVersion, definition.SuppressHitFlash);
            if (actor == null)
                throw new InvalidOperationException($"Could not spawn actor for enemy '{definition.EnemyId}'.");
            actor.Initialize(new CombatEnemyMechanicContext(board, SessionVersion));
            sharedHealth = ScaleHealth(definition.SharedMaxHealth);
            EnterPhase(startPhaseIndex);
        }

        // Controller supplies the real TIME meter, including hits/heals. Progress only forwards;
        // a later Heal cannot re-enable an earlier pressure phase.
        public void ObservePlayerTime(float remaining, float maximum)
        {
            if (State != CombatEnemyRuntimeState.Playing || definition.PhasePolicy != CombatPhasePolicy.SharedHealthPlayerTime ||
                PhaseIndex >= PhaseCount - 1 || maximum <= 0f) return;
            if (Mathf.Clamp01(remaining / maximum) <= CurrentPhase.PlayerTimeExitFraction)
                BeginProgression(CombatEnemyProgression.PhaseBreak);
        }

        public void Tick(float activeDeltaTime)
        {
            if (State != CombatEnemyRuntimeState.Playing)
                return;

            board.TickMoveRecovery(activeDeltaTime);

            // Resolve a threshold held by required dialogue on the first active tick after
            // the controller releases its dialogue pause. Do not require another hit or
            // carry overflow into the next phase. The controller owns result/phase cleanup.
            if (healthProgressionPending && !HasUnresolvedSharedHealthGate())
            {
                healthProgressionPending = false;
                bool resumesPassiveDecay = definition.PhasePolicy == CombatPhasePolicy.PerPhaseHealth &&
                    definition.PassiveHealthDecayInterval > 0f;
                if (!resumesPassiveDecay)
                {
                    if (definition.PhasePolicy == CombatPhasePolicy.PerPhaseHealth)
                        currentHealth = 0;
                    BeginProgression(PhaseIndex >= definition.PhaseCount - 1
                        ? CombatEnemyProgression.Victory
                        : CombatEnemyProgression.PhaseBreak);
                    return;
                }
            }

            float decayInterval = definition.PassiveHealthDecayInterval;
            if (decayInterval > 0f && AcceptsDamage)
            {
                passiveDecayElapsed += Mathf.Max(0f, activeDeltaTime);
                if (passiveDecayElapsed >= decayInterval)
                {
                    int ticks = Mathf.Min(CurrentHealth, Mathf.FloorToInt(passiveDecayElapsed / decayInterval));
                    ApplyDamage(ticks, out int applied);
                    // A blocked final hit does not accumulate a burst behind the dialogue gate.
                    passiveDecayElapsed = applied < ticks ? 0f : passiveDecayElapsed % decayInterval;
                    if (State != CombatEnemyRuntimeState.Playing) return;
                }
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
            if (activeMove != null && activeMove.IsComplete)
            {
                LastCompletedMove = CurrentMove;
                MoveCompletionVersion++;
            }
            if (activeMove != null && activeMove.IsComplete && CurrentPhase.AdvanceOnMoveComplete && !playingOpeningMove)
            {
                BeginProgression(CombatEnemyProgression.PhaseBreak);
                return;
            }
            if (activeMove == null || activeMove.IsComplete)
                StartNextMove();
        }

        public CombatEnemyProgression ApplyDamage(int amount, out int appliedDamage)
        {
            var progression = ApplyDamageCore(amount, out appliedDamage);
            if (progression == CombatEnemyProgression.None && appliedDamage > 0 &&
                State == CombatEnemyRuntimeState.Playing && !healthProgressionPending && CurrentPhase.DamageReactionMove != null)
            {
                if (playingDamageReaction) queuedDamageReactions++;
                else StartDamageReaction();
            }
            return progression;
        }

        private CombatEnemyProgression ApplyDamageCore(int amount, out int appliedDamage)
        {
            appliedDamage = 0;
            if (!AcceptsDamage || amount <= 0)
                return CombatEnemyProgression.None;

            if (definition.PhasePolicy == CombatPhasePolicy.PerPhaseHealth)
            {
                int previous = currentHealth;
                bool finalPhase = PhaseIndex >= definition.PhaseCount - 1;
                bool phaseGate = !finalPhase && HasUnresolvedPhaseAdvanceGate();
                bool victoryGate = finalPhase && (!allowVictory || HasUnresolvedVictoryGate());
                int minimumHealth = phaseGate || victoryGate ? 1 : 0;
                currentHealth = Mathf.Max(minimumHealth, currentHealth - amount);
                appliedDamage = previous - currentHealth;
                if (currentHealth > 0)
                {
                    if (currentHealth == 1 && previous - amount <= 0 && (phaseGate || victoryGate))
                        healthProgressionPending = true;
                    return CombatEnemyProgression.None;
                }
                return BeginProgression(finalPhase
                    ? CombatEnemyProgression.Victory
                    : CombatEnemyProgression.PhaseBreak);
            }

            if (definition.PhasePolicy == CombatPhasePolicy.SharedHealthPlayerTime)
            {
                int beforeTimeDamage = sharedHealth;
                bool final = PhaseIndex >= PhaseCount - 1;
                int floor = !final || !allowVictory || HasUnresolvedVictoryGate() ? 1 : 0;
                sharedHealth = Mathf.Max(floor, sharedHealth - amount);
                appliedDamage = beforeTimeDamage - sharedHealth;
                return sharedHealth == 0 ? BeginProgression(CombatEnemyProgression.Victory) : CombatEnemyProgression.None;
            }
            if (definition.PhasePolicy == CombatPhasePolicy.CapturedDiceBatchSequence)
            {
                int previous = sharedHealth;
                sharedHealth = Mathf.Max(1, sharedHealth - amount);
                appliedDamage = previous - sharedHealth;
                return CombatEnemyProgression.None;
            }

            int threshold = ScaleHealth(CurrentPhase.SharedExitThreshold);
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
            if (IsRecoveringPhaseHealth && currentHealth < recoveryMaxHealth)
                return;
            // The controller normally drains recovery before calling this. Direct runtime
            // callers still enter a settled board, without inheriting the old move's delay.
            board.TickMoveRecovery(.4f);
            EnterPhase(PhaseIndex + 1);
        }

        // A test shortcut still uses the normal phase-break cleanup and version boundary.
        public bool TryAdvanceToNextPhaseForTesting()
        {
            if (State != CombatEnemyRuntimeState.Playing || PhaseIndex >= PhaseCount - 1)
                return false;
            if (definition.PhasePolicy == CombatPhasePolicy.SharedHealthThresholds)
                sharedHealth = Mathf.Min(sharedHealth, ScaleHealth(CurrentPhase.SharedExitThreshold));
            else if (definition.PhasePolicy == CombatPhasePolicy.PerPhaseHealth)
                currentHealth = 0;
            return BeginProgression(CombatEnemyProgression.PhaseBreak) == CombatEnemyProgression.PhaseBreak;
        }

        public void RestartFromBeginning()
        {
            if (State == CombatEnemyRuntimeState.Cancelled || actor == null)
                return;

            CancelActiveMove();
            if (CurrentPhase != null)
                actor.ExitPhase(CurrentPhase, PhaseIndex);
            actor.SetPaused(false);
            sharedHealth = ScaleHealth(definition.SharedMaxHealth);
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
            recoveryMaxHealth = 0;
        }

        public void Cancel()
        {
            if (State == CombatEnemyRuntimeState.Cancelled)
                return;
            CancelActiveMove();
            actor?.Shutdown();
            State = CombatEnemyRuntimeState.Cancelled;
            recoveryMaxHealth = 0;
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
            passiveDecayElapsed = 0f;
            capturedBatchesInPhase = 0;
            batchProgressionPending = false;
            healthProgressionPending = false;
            // Reactions belong to the phase that received the hit, including queued ones.
            queuedDamageReactions = 0;
            playingDamageReaction = false;
            recoveryMaxHealth = 0;
            CombatPhaseDefinition phase = CurrentPhase;
            if (definition.PhasePolicy == CombatPhasePolicy.PerPhaseHealth)
                currentHealth = ScaleHealth(phase.MaxHealth);
            moveSelector = new CombatMoveSelector(phase.MoveSet, random);
            moveSelector.Reset();
            CurrentMove = null;
            MoveVersion = 0;
            actor.EnterPhase(phase, PhaseIndex);
            State = CombatEnemyRuntimeState.Playing;
            playingOpeningMove = phase.OpeningMove != null;
            if (playingOpeningMove)
            {
                CurrentMove = phase.OpeningMove;
                MoveVersion++;
                moveLeadInRemaining = Mathf.Max(.4f, CurrentMove.LeadInDuration);
            }
            else if (phase.DamageReactionOnEnter && phase.DamageReactionMove != null) StartDamageReaction();
            else StartNextMove();
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
            if (progression == CombatEnemyProgression.PhaseBreak) RetireActiveMove();
            else CancelActiveMove();
            if (progression == CombatEnemyProgression.PhaseBreak)
                actor.ExitPhase(CurrentPhase, PhaseIndex);
            return progression;
        }

        private void StartNextMove()
        {
            if (State != CombatEnemyRuntimeState.Playing)
                return;

            if (queuedDamageReactions > 0)
            {
                queuedDamageReactions--;
                StartDamageReaction();
                return;
            }
            playingDamageReaction = false;

            playingOpeningMove = false;
            CombatMoveDefinition followUp = activeMove != null && activeMove.IsComplete &&
                activeMove is ICombatMoveFollowUp redirect ? redirect.NextMove : null;
            if (CurrentMove != null) RetireActiveMove();

            CombatMoveDefinition move = moveSelector.Next(followUp);
            CurrentMove = move;
            MoveVersion++;

            moveLeadInRemaining = Mathf.Max(board.IsRecoveringMove ? .4f : 0f, move.LeadInDuration);
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

        public int ConsumeMoveDamageReward()
        {
            return activeMove is ICombatMoveDamageReward reward ? reward.ConsumePendingDamage() : 0;
        }

        private void CancelActiveMove()
        {
            activeMove?.Cancel();
            activeMove = null;
            CurrentMove = null;
            queuedDamageReactions = 0;
            playingDamageReaction = false;
            playingOpeningMove = false;
        }

        private void RetireActiveMove()
        {
            board.CaptureMoveExit();
            activeMove?.Cancel();
            activeMove = null;
            board.ClearRuntimeBullets(SessionVersion, PhaseVersion);
            board.RestoreMoveExitPose();
        }

        private void StartDamageReaction()
        {
            activeMove?.Cancel();
            board.ClearRuntimeBullets(SessionVersion, PhaseVersion);
            CurrentMove = CurrentPhase.DamageReactionMove;
            MoveVersion++;
            moveLeadInRemaining = 0f;
            playingDamageReaction = true;
            activeMove = CurrentMove.CreateExecution(new CombatMoveExecutionContext(board, actor, random, SessionVersion, PhaseVersion));
        }

        private int ScaleHealth(int authoredHealth)
        {
            if (authoredHealth <= 0)
                return 0;
            return Mathf.Max(1, Mathf.CeilToInt(authoredHealth * healthMultiplier));
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
