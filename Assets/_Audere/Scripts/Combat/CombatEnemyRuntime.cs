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
        private CombatMoveSelector moveSelector;
        private ICombatMoveExecution activeMove;
        private CombatEnemyActor actor;
        private int currentHealth;
        private int sharedHealth;
        private float phaseElapsed;

        public CombatEnemyRuntime(
            CombatEnemyDefinition definition,
            CombatBoardView board,
            ICombatRandom random,
            int sessionVersion)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            this.board = board ?? throw new ArgumentNullException(nameof(board));
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            SessionVersion = sessionVersion;
            if (!definition.Validate(out string error))
                throw new InvalidOperationException(error);
        }

        public CombatEnemyRuntimeState State { get; private set; } = CombatEnemyRuntimeState.Inactive;
        public int SessionVersion { get; }
        public int PhaseVersion { get; private set; }
        public int PhaseIndex { get; private set; } = -1;
        public int PhaseCount => definition.PhaseCount;
        public int CurrentHealth => definition.PhasePolicy == CombatPhasePolicy.SharedHealthThresholds
            ? sharedHealth
            : currentHealth;
        public int CurrentMaxHealth => definition.PhasePolicy == CombatPhasePolicy.SharedHealthThresholds
            ? definition.SharedMaxHealth
            : CurrentPhase != null ? CurrentPhase.MaxHealth : 0;
        public float PhaseElapsed => phaseElapsed;
        public CombatPhaseDefinition CurrentPhase => definition.GetPhase(PhaseIndex);
        public CombatEnemyActor Actor => actor;
        public bool ShowsHealth => definition.PhasePolicy != CombatPhasePolicy.TimedSequence;
        public bool AcceptsDamage => State == CombatEnemyRuntimeState.Playing && ShowsHealth;

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

            phaseElapsed += Mathf.Max(0f, activeDeltaTime);
            if (definition.PhasePolicy == CombatPhasePolicy.TimedSequence &&
                phaseElapsed >= CurrentPhase.Duration)
            {
                BeginProgression(PhaseIndex >= definition.PhaseCount - 1
                    ? CombatEnemyProgression.Victory
                    : CombatEnemyProgression.PhaseBreak);
                return;
            }

            activeMove?.Tick(activeDeltaTime);
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
                currentHealth = Mathf.Max(0, currentHealth - amount);
                appliedDamage = previous - currentHealth;
                if (currentHealth > 0)
                    return CombatEnemyProgression.None;
                return BeginProgression(PhaseIndex >= definition.PhaseCount - 1
                    ? CombatEnemyProgression.Victory
                    : CombatEnemyProgression.PhaseBreak);
            }

            int threshold = CurrentPhase.SharedExitThreshold;
            int before = sharedHealth;
            sharedHealth = Mathf.Max(threshold, sharedHealth - amount);
            appliedDamage = before - sharedHealth;
            if (sharedHealth > threshold)
                return CombatEnemyProgression.None;
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
            return cue != null && !string.IsNullOrWhiteSpace(cue.OneShotKey) && playedCueIds.Add(cue.OneShotKey);
        }

        private void EnterPhase(int phaseIndex)
        {
            PhaseIndex = phaseIndex;
            PhaseVersion++;
            State = CombatEnemyRuntimeState.EnteringPhase;
            phaseElapsed = 0f;
            CombatPhaseDefinition phase = CurrentPhase;
            if (definition.PhasePolicy == CombatPhasePolicy.PerPhaseHealth)
                currentHealth = phase.MaxHealth;
            moveSelector = new CombatMoveSelector(phase.MoveSet, random);
            moveSelector.Reset();
            actor.EnterPhase(phase, PhaseIndex);
            State = CombatEnemyRuntimeState.Playing;
            StartNextMove();
        }

        private CombatEnemyProgression BeginProgression(CombatEnemyProgression progression)
        {
            if (State != CombatEnemyRuntimeState.Playing)
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
            activeMove = move.CreateExecution(new CombatMoveExecutionContext(
                board, actor, random, SessionVersion, PhaseVersion));
        }

        private void CancelActiveMove()
        {
            activeMove?.Cancel();
            activeMove = null;
        }
    }
}
