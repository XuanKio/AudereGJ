using UnityEngine;
namespace Audere.Combat
{
    /// <summary>Encounter-scoped cap; reservations include caught attacks and live dice, excluding the die being rerolled.</summary>
    public static class CombatDiceBatchBudget
    {
        public static CombatSymbol Roll(int maximumAttacks, int reservedAttacks, float value01)
        {
            int attackWeight = maximumAttacks > 0 && reservedAttacks >= maximumAttacks ? 0 : CombatDiceConstants.AttackRollWeight;
            int total = attackWeight + CombatDiceConstants.ShieldRollWeight + CombatDiceConstants.HealRollWeight;
            float roll = Mathf.Clamp(value01, 0f, .999999f) * total;
            if (roll < attackWeight) return CombatSymbol.Attack;
            return roll < attackWeight + CombatDiceConstants.ShieldRollWeight ? CombatSymbol.Shield : CombatSymbol.Heal;
        }
    }

    public interface ICombatMoveInputHandler
    {
        void HandleInput(bool catchPressed, bool rerollPressed);
    }

    public sealed class CombatChoiceRoundState
    {
        public int Successes { get; private set; }
        public bool Resolve(float value01, float explosionChance)
        {
            bool success = value01 >= Mathf.Clamp01(explosionChance);
            if (success) Successes++;
            return success;
        }
    }
}
