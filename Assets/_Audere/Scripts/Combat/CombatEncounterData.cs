using System;
using Audere.Dialogue;
using UnityEngine;
using UnityEngine.Serialization;

namespace Audere.Combat
{
    [Serializable]
    public sealed class CombatDefeatPresentation
    {
        [SerializeField] private DialogueData dialogue;
        [SerializeField, Min(0f)] private float hazardFadeDuration = .55f;

        public DialogueData Dialogue => dialogue;
        public float HazardFadeDuration => Mathf.Max(0f, hazardFadeDuration);
        public bool IsConfigured => dialogue != null && dialogue.HasLines;
    }

    [Serializable]
    public sealed class CombatVictoryPresentation
    {
        [SerializeField] private DialogueData dialogue;
        [SerializeField, Min(0f)] private float hazardFadeDuration = .45f;
        public DialogueData Dialogue => dialogue;
        public float HazardFadeDuration => Mathf.Max(0f, hazardFadeDuration);
        public bool IsConfigured => dialogue != null && dialogue.HasLines;
    }

    [Serializable]
    public sealed class CombatPhaseRecoverySettings
    {
        [SerializeField] private bool enabled;
        [SerializeField, Min(1f)] private float timePerHealDie = 18f;
        [SerializeField, Min(.1f)] private float dropInterval = .48f;
        [SerializeField, Range(1, 8)] private int maximumActiveDice = 4;
        [SerializeField, Min(20f)] private float diceSpeed = 145f;
        [SerializeField, Min(.05f)] private float healAnimationDuration = .24f;
        [SerializeField] private bool healEnemyOnDrop;
        [SerializeField, Min(1)] private int enemyHealthPerDie = 1;
        [SerializeField, Min(1f)] private float droppedDiceLifetime = 3f;

        public bool Enabled => enabled;
        public float TimePerHealDie => Mathf.Max(1f, timePerHealDie);
        public float DropInterval => Mathf.Max(.1f, dropInterval);
        public int MaximumActiveDice => Mathf.Clamp(maximumActiveDice, 1, 8);
        public float DiceSpeed => Mathf.Max(20f, diceSpeed);
        public float HealAnimationDuration => Mathf.Max(.05f, healAnimationDuration);
        public bool HealEnemyOnDrop => healEnemyOnDrop;
        public int EnemyHealthPerDie => Mathf.Max(1, enemyHealthPerDie);
        public float DroppedDiceLifetime => Mathf.Max(1f, droppedDiceLifetime);
    }

    [CreateAssetMenu(menuName = "Audere/Combat/Encounter Data", fileName = "CombatEncounter_New")]
    public sealed class CombatEncounterData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string encounterId = "combat-sample";
        [SerializeField] private CombatEnemyDefinition enemyDefinition;
        [Header("Optional dice interception")]
        [SerializeField, Range(0f, 1f)] private float diceBreakChance;
        [SerializeField] private Sprite diceBreakTailSprite;
        public float DiceBreakChance => Mathf.Clamp01(diceBreakChance);
        public Sprite DiceBreakTailSprite => diceBreakTailSprite;
        [SerializeField] private CombatTutorialData tutorialData;

        [Header("Music")]
        [SerializeField] private Audere.Audio.AudioId music = Audere.Audio.AudioId.Music_Combat;
        public Audere.Audio.AudioId Music => music;

        [Header("Win / Lose")]
        [SerializeField, Min(1f)] private float encounterDuration = 40f;
        [SerializeField] private CombatEncounterOutcomeRules outcomeRules = new CombatEncounterOutcomeRules();
        [SerializeField] private CombatDefeatPresentation defeatPresentation = new CombatDefeatPresentation();
        [Header("Between Phases")]
        [SerializeField] private CombatPhaseRecoverySettings phaseRecovery = new CombatPhaseRecoverySettings();

        [SerializeField, Min(0f)] private float victoryFadeDuration;
        [SerializeField] private CombatVictoryPresentation victoryPresentation = new CombatVictoryPresentation();
        public CombatVictoryPresentation VictoryPresentation => victoryPresentation;
        public float VictoryFadeDuration => Mathf.Max(0f, victoryFadeDuration);

        [Header("Continuous Dice Batches")]
        [FormerlySerializedAs("dicePerWave")]
        [SerializeField, Min(1)] private int dicePerBatch = 5;
        [SerializeField, Range(.05f, 1f)] private float batchRespawnDelay = .3f;
        [SerializeField, Min(20f)] private float minimumDiceSpeed = 115f;
        [SerializeField, Min(20f)] private float maximumDiceSpeed = 185f;
        [Header("Heart Marker")]
        [SerializeField, Min(.05f)] private float playerHitInvulnerability = .55f;

        [Header("Enemy Hits")]
        [FormerlySerializedAs("bulletDamage")]
        [SerializeField, Min(.1f)] private float bulletTimePenaltySeconds = 3f;

        public string EncounterId => encounterId;
        public CombatEnemyDefinition EnemyDefinition => enemyDefinition;
        public CombatTutorialData TutorialData => tutorialData;
        public bool HasTutorial => tutorialData != null;
        public string EnemyDisplayName => enemyDefinition != null ? enemyDefinition.DisplayName : string.Empty;
        public float EncounterDuration => encounterDuration;
        public CombatEncounterOutcomeRules OutcomeRules => outcomeRules ??= new CombatEncounterOutcomeRules();
        public CombatDefeatPresentation DefeatPresentation => defeatPresentation;
        public CombatPhaseRecoverySettings PhaseRecovery => phaseRecovery ??= new CombatPhaseRecoverySettings();
        [Tooltip("Legacy serialized value retained for asset compatibility. Normal random dice no longer use an Attack quota.")]
        [SerializeField, HideInInspector, Min(0)] private int maximumAttacksPerBatch;
        public int MaximumAttacksPerBatch => Mathf.Max(0, maximumAttacksPerBatch);
        [Tooltip("Legacy serialized value retained for asset compatibility. Rerolls now choose equally between the other two faces.")]
        [SerializeField, HideInInspector, Min(0)] private int additionalRerolledAttacksPerBatch;
        public int AdditionalRerolledAttacksPerBatch => Mathf.Max(0, additionalRerolledAttacksPerBatch);
        public int DicePerBatch => dicePerBatch;
        public float BatchRespawnDelay => batchRespawnDelay;
        public float MinimumDiceSpeed => minimumDiceSpeed;
        public float MaximumDiceSpeed => Mathf.Max(minimumDiceSpeed, maximumDiceSpeed);
        public float PlayerHitInvulnerability => playerHitInvulnerability;
        public float BulletTimePenaltySeconds => bulletTimePenaltySeconds;
        public CombatSymbol RollSymbol()
        {
            return CombatDiceConstants.RollSymbol();
        }

        private void OnValidate()
        {
            maximumDiceSpeed = Mathf.Max(minimumDiceSpeed, maximumDiceSpeed);
            if (string.IsNullOrWhiteSpace(encounterId))
                Debug.LogError($"[CombatEncounterData] '{name}' requires a stable Encounter ID.", this);
            if (enemyDefinition == null)
                Debug.LogError($"[CombatEncounterData] '{name}' requires an Enemy Definition.", this);
            if (outcomeRules == null)
                Debug.LogError($"[CombatEncounterData] '{name}' requires Outcome Rules.", this);
            if (tutorialData != null && !tutorialData.Validate(out string tutorialError))
                Debug.LogError($"[CombatEncounterData] '{name}' has invalid tutorial data: {tutorialError}", this);
            if (PhaseRecovery.Enabled && enemyDefinition != null &&
                enemyDefinition.PhasePolicy != CombatPhasePolicy.PerPhaseHealth)
                Debug.LogError($"[CombatEncounterData] '{name}' phase recovery requires per-phase enemy health.", this);
        }
    }
}
