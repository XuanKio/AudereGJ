using System.Collections.Generic;
using UnityEngine;

namespace Audere.Combat
{
    [CreateAssetMenu(menuName = "Audere/Combat/Move Set", fileName = "MoveSet_New")]
    public sealed class CombatMoveSet : ScriptableObject
    {
        [SerializeField] private CombatMoveSelectionPolicy selectionPolicy;
        [SerializeField] private CombatWeightedMove[] entries;
        public CombatMoveSelectionPolicy SelectionPolicy => selectionPolicy;
        public IReadOnlyList<CombatWeightedMove> Entries => entries;
        public int Count => entries != null ? entries.Length : 0;

        public bool Validate(out string error)
        {
            if (entries == null || entries.Length == 0) { error = $"Moveset '{name}' is empty."; return false; }
            bool hasPositiveWeight = false;
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].Move == null) { error = $"Moveset '{name}' has a null move at index {i}."; return false; }
                if (!entries[i].Move.Validate(out string moveError))
                { error = $"Moveset '{name}' entry {i}: {moveError}"; return false; }
                if (selectionPolicy == CombatMoveSelectionPolicy.WeightedRandom) hasPositiveWeight |= entries[i].Weight > 0f;
            }
            if (selectionPolicy == CombatMoveSelectionPolicy.WeightedRandom && !hasPositiveWeight)
            { error = $"Weighted moveset '{name}' has no entry with positive weight."; return false; }
            error = null;
            return true;
        }

        private void OnValidate() { if (!Validate(out string error)) Debug.LogError($"[CombatMoveSet] {error}", this); }
    }
}
