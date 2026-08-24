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
            if (phasePolicy == CombatPhasePolicy.SharedHealthThresholds && sharedMaxHealth <= 0) { error = $"Enemy '{enemyId}' requires Shared Max Health greater than zero."; return false; }
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
                    if (phase.SharedExitThreshold < 0 || phase.SharedExitThreshold >= previousThreshold) { error = $"Enemy '{enemyId}' shared thresholds must be strict descending."; return false; }
                    previousThreshold = phase.SharedExitThreshold;
                }
                if (phasePolicy == CombatPhasePolicy.TimedSequence && phase.Duration <= 0f) { error = $"Enemy '{enemyId}' phase '{phase.PhaseId}' requires Duration greater than zero."; return false; }
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
                    if (cue.Sequence == null) continue;
                    for (int dialogueIndex = 0; dialogueIndex < cue.Sequence.Count; dialogueIndex++)
                    {
                        if (cue.Sequence[dialogueIndex] == null) { error = $"Enemy '{enemyId}' cue '{cue.CueId}' has a null DialogueData reference."; return false; }
                    }
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
