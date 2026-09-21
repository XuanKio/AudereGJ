using UnityEngine;
namespace Audere.Combat
{
    /// <summary>Compatibility entry point. Legacy batch caps no longer constrain random dice.</summary>
    public static class CombatDiceBatchBudget
    {
        public static CombatSymbol Roll(int maximumAttacks, int reservedAttacks, float value01)
        {
            return CombatDiceConstants.RollSymbol(value01);
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
