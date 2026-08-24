using System;
using UnityEngine;

namespace Audere.Combat
{
    public enum LinearProjectileSpawnMode
    {
        ActorAnchor = 0,
        AlternatingSides = 1,
        RandomTop = 2,
    }

    public enum LinearProjectileTargetMode
    {
        AimAtHeart = 0,
        HorizontalIntoBoard = 1,
        Down = 2,
    }

    [Serializable]
    public struct CombatWeightedMove
    {
        [SerializeField] private CombatMoveDefinition move;
        [SerializeField, Min(0f)] private float weight;

        public CombatMoveDefinition Move => move;
        public float Weight => weight;
    }

}
