using System;
using System.Collections.Generic;
using UnityEngine;

namespace Audere.Combat
{
    [CreateAssetMenu(menuName = "Audere/Combat/Enemy Definition", fileName = "Enemy_New")]
    public sealed class CombatEnemyDefinition : ScriptableObject
    {
        [SerializeField] private string enemyId;
        [SerializeField] private string displayName;
        [SerializeField] private CombatEnemyActor actorPrefab;
        [SerializeField] private CombatPhasePolicy phasePolicy;
        [SerializeField, Min(1)] private int sharedMaxHealth = 1;
        [SerializeField] private CombatPhaseDefinition[] phases;
        public string EnemyId => enemyId;
        public string DisplayName => displayName;
        public CombatEnemyActor ActorPrefab => actorPrefab;
        public CombatPhasePolicy PhasePolicy => phasePolicy;
        public int SharedMaxHealth => sharedMaxHealth;
        public IReadOnlyList<CombatPhaseDefinition> Phases => phases;
        public int PhaseCount => phases != null ? phases.Length : 0;
        public CombatPhaseDefinition GetPhase(int index) => index >= 0 && phases != null && index < phases.Length ? phases[index] : null;

        public bool Validate(out string error)
        {
            if (string.IsNullOrWhiteSpace(enemyId)) { error = $"Enemy definition '{name}' has an empty stable Enemy ID."; return false; }
            if (actorPrefab == null) { error = $"Enemy '{enemyId}' has no CombatEnemyActor prefab."; return false; }
            if (phases == null || phases.Length == 0) { error = $"Enemy '{enemyId}' has no authored phase."; return false; }
            if ((phasePolicy == CombatPhasePolicy.SharedHealthThresholds ||
                 phasePolicy == CombatPhasePolicy.CapturedDiceBatchSequence) && sharedMaxHealth <= 0)
            { error = $"Enemy '{enemyId}' requires Shared Max Health greater than zero."; return false; }
            int previousThreshold = sharedMaxHealth + 1;
            var phaseIds = new HashSet<string>(StringComparer.Ordinal);
            var cueIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < phases.Length; i++)
            {
                CombatPhaseDefinition phase = phases[i];
                if (phase == null) { error = $"Enemy '{enemyId}' has a null phase at index {i}."; return false; }
                if (string.IsNullOrWhiteSpace(phase.PhaseId) || !phaseIds.Add(phase.PhaseId)) { error = $"Enemy '{enemyId}' has an empty or duplicate Phase ID at index {i}."; return false; }
                if (phasePolicy == CombatPhasePolicy.PerPhaseHealth && phase.MaxHealth <= 0) { error = $"Enemy '{enemyId}' phase '{phase.PhaseId}' requires Max Health greater than zero."; return false; }
                if (phasePolicy == CombatPhasePolicy.SharedHealthThresholds)
                {
                    if (phase.SharedExitThreshold < 0 || (phase.AdvanceOnMoveComplete ? phase.SharedExitThreshold > previousThreshold : phase.SharedExitThreshold >= previousThreshold)) { error = $"Enemy '{enemyId}' health thresholds must descend; move-completion specials may hold the preceding threshold."; return false; }
                    previousThreshold = phase.SharedExitThreshold;
                }
                if (phasePolicy == CombatPhasePolicy.TimedSequence && phase.Duration <= 0f) { error = $"Enemy '{enemyId}' phase '{phase.PhaseId}' requires Duration greater than zero."; return false; }
                if (phasePolicy == CombatPhasePolicy.CapturedDiceBatchSequence)
                {
                    bool isFinal = i == phases.Length - 1;
                    if (!isFinal && !phase.SpawnDice)
                    { error = $"Enemy '{enemyId}' phase '{phase.PhaseId}' must spawn a scripted dice batch before the final phase."; return false; }
                    if (phase.SpawnDice && phase.DiceBatch == null)
                    { error = $"Enemy '{enemyId}' phase '{phase.PhaseId}' requires Scripted Dice Batch data."; return false; }
                    if (phase.DiceBatch != null && !phase.DiceBatch.Validate(out string batchError))
                    { error = $"Enemy '{enemyId}' phase '{phase.PhaseId}': {batchError}"; return false; }
                }
                if (phase.AdvanceOnMoveComplete && (phase.SpawnDice || i == phases.Length - 1))
                { error = $"Enemy '{enemyId}' special '{phase.PhaseId}' must disable regular dice and have a following phase."; return false; }
                if (phase.MoveSet == null) { error = $"Enemy '{enemyId}' phase '{phase.PhaseId}' has no moveset."; return false; }
                if (!phase.MoveSet.Validate(out string moveError)) { error = $"Enemy '{enemyId}' phase '{phase.PhaseId}': {moveError}"; return false; }
                IReadOnlyList<CombatDialogueCue> cues = phase.DialogueCues;
                if (cues == null) continue;
                for (int cueIndex = 0; cueIndex < cues.Count; cueIndex++)
                {
                    CombatDialogueCue cue = cues[cueIndex];
                    if (cue == null) { error = $"Enemy '{enemyId}' phase '{phase.PhaseId}' has a null cue at index {cueIndex}."; return false; }
                    if (string.IsNullOrWhiteSpace(cue.CueId) || !cueIds.Add(cue.CueId)) { error = $"Enemy '{enemyId}' has an empty or duplicate Cue ID in phase '{phase.PhaseId}'."; return false; }
                    if (!cue.HasContent) { error = $"Enemy '{enemyId}' cue '{cue.CueId}' has no dialogue or instruction."; return false; }
                    if (cue.Trigger == CombatDialogueCueTrigger.MoveStarted && cue.TriggerMove == null)
                    { error = $"Enemy '{enemyId}' cue '{cue.CueId}' requires a Move reference."; return false; }
                    if (cue.Trigger == CombatDialogueCueTrigger.CueCompleted && string.IsNullOrWhiteSpace(cue.TriggerCueId))
                    { error = $"Enemy '{enemyId}' cue '{cue.CueId}' requires a completed Cue ID."; return false; }
                    if (cue.Presentation != CombatDialoguePresentation.ModalDialogue && !cue.HasDialogue)
                    { error = $"Enemy '{enemyId}' cue '{cue.CueId}' requires DialogueData for its non-modal presentation."; return false; }
                    if (cue.Sequence == null) continue;
                    for (int dialogueIndex = 0; dialogueIndex < cue.Sequence.Count; dialogueIndex++)
                    {
                        if (cue.Sequence[dialogueIndex] == null) { error = $"Enemy '{enemyId}' cue '{cue.CueId}' has a null DialogueData reference."; return false; }
                    }
                }
            }
            for (int phaseIndex = 0; phaseIndex < phases.Length; phaseIndex++)
            {
                IReadOnlyList<CombatDialogueCue> cues = phases[phaseIndex].DialogueCues;
                if (cues == null) continue;
                for (int cueIndex = 0; cueIndex < cues.Count; cueIndex++)
                {
                    CombatDialogueCue cue = cues[cueIndex];
                    if (cue.Trigger != CombatDialogueCueTrigger.CueCompleted) continue;
                    if (!cueIds.Contains(cue.TriggerCueId) || cue.TriggerCueId == cue.CueId)
                    { error = $"Enemy '{enemyId}' cue '{cue.CueId}' references a missing or self-referential completed cue '{cue.TriggerCueId}'."; return false; }
                }
            }
            if (phasePolicy == CombatPhasePolicy.SharedHealthThresholds && phases[phases.Length - 1].SharedExitThreshold != 0)
            { error = $"Enemy '{enemyId}' final shared threshold must be 0."; return false; }
            error = null;
            return true;
        }
        private void OnValidate() { if (!Validate(out string error)) Debug.LogError($"[CombatEnemyDefinition] {error}", this); }
    }
}
