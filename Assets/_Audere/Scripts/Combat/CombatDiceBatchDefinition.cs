using System;
using System.Collections.Generic;
using UnityEngine;

namespace Audere.Combat
{
    [Serializable]
    public struct CombatScriptedDieSpawn
    {
        [SerializeField] private CombatSymbol symbol;
        [SerializeField] private Vector2 normalizedPosition;
        [SerializeField] private Vector2 normalizedDirection;
        [SerializeField, Min(.1f)] private float speedMultiplier;

        public CombatSymbol Symbol => symbol;
        public Vector2 NormalizedPosition => normalizedPosition;
        public Vector2 NormalizedDirection => normalizedDirection.sqrMagnitude > .001f
            ? normalizedDirection.normalized
            : Vector2.right;
        public float SpeedMultiplier => Mathf.Max(.1f, speedMultiplier);
    }

    [CreateAssetMenu(
        menuName = "Audere/Combat/Scripted Dice Batch",
        fileName = "DiceBatch_New")]
    public sealed class CombatDiceBatchDefinition : ScriptableObject
    {
        [SerializeField, Min(0f)] private float spawnDelay;
        [SerializeField] private CombatScriptedDieSpawn[] dice = Array.Empty<CombatScriptedDieSpawn>();

        public float SpawnDelay => Mathf.Max(0f, spawnDelay);
        public IReadOnlyList<CombatScriptedDieSpawn> Dice => dice;
        public int Count => dice != null ? dice.Length : 0;

        public bool Validate(out string error)
        {
            if (dice == null || dice.Length == 0)
            {
                error = $"Scripted dice batch '{name}' is empty.";
                return false;
            }

            for (int i = 0; i < dice.Length; i++)
            {
                Vector2 position = dice[i].NormalizedPosition;
                if (position.x < 0f || position.x > 1f || position.y < 0f || position.y > 1f)
                {
                    error = $"Scripted dice batch '{name}' entry {i} must use a normalized position inside 0..1.";
                    return false;
                }
            }

            error = null;
            return true;
        }

        private void OnValidate()
        {
            if (!Validate(out string error))
                Debug.LogError($"[CombatDiceBatchDefinition] {error}", this);
        }
    }
}
