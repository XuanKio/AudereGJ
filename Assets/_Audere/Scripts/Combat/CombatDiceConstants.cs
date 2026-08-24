using UnityEngine;

namespace Audere.Combat
{
    public enum CombatDiceRarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
    }

    public enum CombatDiceAbility
    {
        DamageEnemy = 0,
        DestroyNearbyBullets = 1,
        RestoreEncounterTime = 2,
    }

    public readonly struct CombatDiceDefinition
    {
        public CombatDiceDefinition(
            CombatSymbol symbol,
            string displayName,
            string shortLabel,
            string abilityName,
            string usageDescription,
            CombatDiceAbility ability,
            CombatDiceRarity rarity,
            int rollWeight,
            float effectAmount,
            float effectRadius = 0f)
        {
            Symbol = symbol;
            DisplayName = displayName;
            ShortLabel = shortLabel;
            AbilityName = abilityName;
            UsageDescription = usageDescription;
            Ability = ability;
            Rarity = rarity;
            RollWeight = Mathf.Max(0, rollWeight);
            EffectAmount = Mathf.Max(0f, effectAmount);
            EffectRadius = Mathf.Max(0f, effectRadius);
        }

        public CombatSymbol Symbol { get; }
        public string DisplayName { get; }
        public string ShortLabel { get; }
        public string AbilityName { get; }
        public string UsageDescription { get; }
        public CombatDiceAbility Ability { get; }
        public CombatDiceRarity Rarity { get; }
        public int RollWeight { get; }
        public float EffectAmount { get; }
        public float EffectRadius { get; }
    }

    /// <summary>
    /// Single source of truth for combat-dice identity, ability, balance and rarity.
    /// Encounter data continues to own enemy health, duration, bullet patterns and batch pacing.
    /// </summary>
    public static class CombatDiceConstants
    {
        public const float DefaultVisualSize = 72f;

        // Catcher reroll: the board-plane impulse is derived from the die's
        // normalized offset from the catcher center. Height uses constant-gravity
        // projectile motion; walls reflect the planar path inside the Dice Field.
        public const float RerollCenterDeadZoneNormalized = .06f;
        public const float RerollDistanceExponent = 1.25f;
        public const float RerollMaximumTravelDistance = 118f;
        public const float RerollMinimumFlightDuration = .38f;
        public const float RerollMaximumFlightDuration = .48f;
        public const float RerollGravity = 3200f;
        public const float RerollBoardEdgePadding = 0f;

        public const int AttackDamage = 1;
        public const int AttackRollWeight = 5;

        // The catch cursor is 100 wide (50 radius). Shield clears roughly three
        // catcher radii around Audere so it reads as a deliberate breathing-space tool.
        public const float ShieldBulletClearRadius = 150f;
        public const int ShieldRollWeight = 3;

        public const float HealTimeSeconds = 3f;
        public const int HealRollWeight = 2;

        public const int TotalRollWeight =
            AttackRollWeight + ShieldRollWeight + HealRollWeight;

        private static readonly CombatDiceDefinition AttackDefinition = new CombatDiceDefinition(
            CombatSymbol.Attack,
            "Tấn công",
            "ATK",
            "Đòn đánh",
            "Bắt để gây sát thương trực tiếp lên đối thủ.",
            CombatDiceAbility.DamageEnemy,
            CombatDiceRarity.Common,
            AttackRollWeight,
            AttackDamage);

        private static readonly CombatDiceDefinition ShieldDefinition = new CombatDiceDefinition(
            CombatSymbol.Shield,
            "Khiên",
            "SHD",
            "Xung khiên",
            "Bắt để phá các viên đạn đang bay gần Audere.",
            CombatDiceAbility.DestroyNearbyBullets,
            CombatDiceRarity.Uncommon,
            ShieldRollWeight,
            0f,
            ShieldBulletClearRadius);

        private static readonly CombatDiceDefinition HealDefinition = new CombatDiceDefinition(
            CombatSymbol.Heal,
            "Hồi nhịp",
            "TIME",
            "Lấy lại nhịp",
            "Bắt để hồi lại một phần thời gian chiến đấu.",
            CombatDiceAbility.RestoreEncounterTime,
            CombatDiceRarity.Rare,
            HealRollWeight,
            HealTimeSeconds);

        public static CombatDiceDefinition GetDefinition(CombatSymbol symbol)
        {
            return symbol switch
            {
                CombatSymbol.Shield => ShieldDefinition,
                CombatSymbol.Heal => HealDefinition,
                _ => AttackDefinition,
            };
        }

        public static float GetRollChance(CombatSymbol symbol)
        {
            return TotalRollWeight > 0
                ? GetDefinition(symbol).RollWeight / (float)TotalRollWeight
                : 0f;
        }

        public static CombatSymbol RollSymbol()
        {
            int roll = Random.Range(0, TotalRollWeight);
            if (roll < AttackRollWeight)
                return CombatSymbol.Attack;

            roll -= AttackRollWeight;
            if (roll < ShieldRollWeight)
                return CombatSymbol.Shield;

            return CombatSymbol.Heal;
        }
    }
}
