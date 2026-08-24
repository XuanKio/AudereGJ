using UnityEngine;
using UnityEngine.Serialization;

namespace Audere.Combat
{
    [CreateAssetMenu(menuName = "Audere/Combat/Encounter Data", fileName = "CombatEncounter_New")]
    public sealed class CombatEncounterData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string encounterId = "combat-sample";
        [SerializeField] private CombatEnemyDefinition enemyDefinition;
        [SerializeField] private CombatTutorialData tutorialData;

        [Header("Win / Lose")]
        [SerializeField, Min(1f)] private float encounterDuration = 40f;

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
            if (tutorialData != null && !tutorialData.Validate(out string tutorialError))
                Debug.LogError($"[CombatEncounterData] '{name}' has invalid tutorial data: {tutorialError}", this);
        }
    }
}
