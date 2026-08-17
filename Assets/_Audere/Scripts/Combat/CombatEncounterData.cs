using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Audere.Combat
{
    public enum EnemyAttackPatternKind
    {
        AimedFan = 0,
        SideSweep = 1,
        Rain = 2,
    }

    [Serializable]
    public struct CombatSymbolWeight
    {
        public CombatSymbol Symbol;
        [Min(0)] public int Weight;
    }

    [Serializable]
    public struct EnemyAttackPatternDefinition
    {
        public EnemyAttackPatternKind Kind;
        [Min(.5f)] public float Duration;
        [Min(.08f)] public float ShotInterval;
        [Min(1)] public int BulletsPerShot;
        [Min(20f)] public float BulletSpeed;
        [Range(0f, 90f)] public float SpreadDegrees;
    }

    [CreateAssetMenu(menuName = "Audere/Combat/Encounter Data", fileName = "CombatEncounter_New")]
    public sealed class CombatEncounterData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string encounterId = "combat-sample";
        [SerializeField] private string enemyDisplayName = "Audere";

        [Header("Win / Lose")]
        [SerializeField, Min(1)] private int enemyMaxHealth = 12;
        [SerializeField, Min(1f)] private float encounterDuration = 40f;

        [Header("Immediate Dice Effects")]
        [FormerlySerializedAs("swordDamage")]
        [SerializeField, Min(1)] private int attackDamage = 1;
        [SerializeField, Min(1)] private int armorPerDie = 1;
        [FormerlySerializedAs("healPerDie")]
        [SerializeField, Min(.1f)] private float healTimeSeconds = 3f;

        [Header("Continuous Dice Batches")]
        [FormerlySerializedAs("dicePerWave")]
        [SerializeField, Min(1)] private int dicePerBatch = 5;
        [SerializeField, Range(.05f, 1f)] private float batchRespawnDelay = .3f;
        [SerializeField, Min(20f)] private float minimumDiceSpeed = 115f;
        [SerializeField, Min(20f)] private float maximumDiceSpeed = 185f;
        [SerializeField] private CombatSymbolWeight[] symbolWeights =
        {
            new CombatSymbolWeight { Symbol = CombatSymbol.Attack, Weight = 5 },
            new CombatSymbolWeight { Symbol = CombatSymbol.Armor, Weight = 3 },
            new CombatSymbolWeight { Symbol = CombatSymbol.Heal, Weight = 2 },
        };

        [Header("Heart Marker")]
        [SerializeField, Min(.05f)] private float playerHitInvulnerability = .55f;

        [Header("Enemy Bullets")]
        [FormerlySerializedAs("bulletDamage")]
        [SerializeField, Min(.1f)] private float bulletTimePenaltySeconds = 3f;
        [SerializeField] private EnemyAttackPatternDefinition[] attackPatterns =
        {
            new EnemyAttackPatternDefinition
            {
                Kind = EnemyAttackPatternKind.AimedFan,
                Duration = 8f,
                ShotInterval = 1.15f,
                BulletsPerShot = 3,
                BulletSpeed = 145f,
                SpreadDegrees = 24f,
            },
            new EnemyAttackPatternDefinition
            {
                Kind = EnemyAttackPatternKind.SideSweep,
                Duration = 9f,
                ShotInterval = .85f,
                BulletsPerShot = 2,
                BulletSpeed = 165f,
                SpreadDegrees = 0f,
            },
            new EnemyAttackPatternDefinition
            {
                Kind = EnemyAttackPatternKind.Rain,
                Duration = 8f,
                ShotInterval = .72f,
                BulletsPerShot = 3,
                BulletSpeed = 155f,
                SpreadDegrees = 16f,
            },
        };

        public string EncounterId => encounterId;
        public string EnemyDisplayName => enemyDisplayName;
        public int EnemyMaxHealth => enemyMaxHealth;
        public float EncounterDuration => encounterDuration;
        public int AttackDamage => attackDamage;
        public int ArmorPerDie => armorPerDie;
        public float HealTimeSeconds => healTimeSeconds;
        public int DicePerBatch => dicePerBatch;
        public float BatchRespawnDelay => batchRespawnDelay;
        public float MinimumDiceSpeed => minimumDiceSpeed;
        public float MaximumDiceSpeed => Mathf.Max(minimumDiceSpeed, maximumDiceSpeed);
        public float PlayerHitInvulnerability => playerHitInvulnerability;
        public float BulletTimePenaltySeconds => bulletTimePenaltySeconds;
        public int AttackPatternCount => attackPatterns != null ? attackPatterns.Length : 0;

        public EnemyAttackPatternDefinition GetAttackPattern(int index)
        {
            if (attackPatterns == null || attackPatterns.Length == 0)
            {
                return new EnemyAttackPatternDefinition
                {
                    Kind = EnemyAttackPatternKind.AimedFan,
                    Duration = 8f,
                    ShotInterval = 1.1f,
                    BulletsPerShot = 3,
                    BulletSpeed = 145f,
                    SpreadDegrees = 24f,
                };
            }

            return attackPatterns[Mathf.Abs(index) % attackPatterns.Length];
        }

        public CombatSymbol RollSymbol()
        {
            int totalWeight = 0;
            if (symbolWeights != null)
            {
                for (int i = 0; i < symbolWeights.Length; i++)
                    totalWeight += Mathf.Max(0, symbolWeights[i].Weight);
            }

            if (totalWeight <= 0)
                return (CombatSymbol)UnityEngine.Random.Range(0, 3);

            int roll = UnityEngine.Random.Range(0, totalWeight);
            for (int i = 0; i < symbolWeights.Length; i++)
            {
                roll -= Mathf.Max(0, symbolWeights[i].Weight);
                if (roll < 0)
                    return symbolWeights[i].Symbol;
            }

            return CombatSymbol.Attack;
        }

        private void OnValidate()
        {
            maximumDiceSpeed = Mathf.Max(minimumDiceSpeed, maximumDiceSpeed);
        }
    }
}
