using System;
using UnityEngine;

namespace Audere.Combat
{
    [CreateAssetMenu(menuName = "Audere/Combat/Moves/Linear Projectile Pattern", fileName = "Move_LinearProjectile")]
    public sealed class LinearProjectilePatternMove : CombatMoveDefinition
    {
        [SerializeField] private CombatBulletView projectilePrefab;
        [SerializeField] private LinearProjectileSpawnMode spawnMode;
        [SerializeField] private LinearProjectileTargetMode targetMode;
        [SerializeField, Min(.08f)] private float shotInterval = 1f;
        [SerializeField, Min(1)] private int projectilesPerShot = 1;
        [SerializeField, Min(20f)] private float speed = 140f;
        [SerializeField, Range(0f, 90f)] private float spreadDegrees = 20f;
        [SerializeField, Min(0f)] private float spacing = 42f;
        public CombatBulletView ProjectilePrefab => projectilePrefab;
        public LinearProjectileSpawnMode SpawnMode => spawnMode;
        public LinearProjectileTargetMode TargetMode => targetMode;
        public float ShotInterval => shotInterval;
        public int ProjectilesPerShot => projectilesPerShot;
        public float Speed => speed;
        public float SpreadDegrees => spreadDegrees;
        public float Spacing => spacing;
        public override bool Validate(out string error)
        {
            if (!base.Validate(out error)) return false;
            if (projectilePrefab == null)
            {
                error = $"Move '{name}' requires a projectile prefab.";
                return false;
            }
            error = null;
            return true;
        }
        public override ICombatMoveExecution CreateExecution(CombatMoveExecutionContext context)
        {
            if (projectilePrefab == null) throw new InvalidOperationException($"Move '{name}' has no projectile prefab.");
            return new LinearProjectilePatternExecution(this, context);
        }
        private void OnValidate() { if (!Validate(out string error)) Debug.LogError($"[LinearProjectilePatternMove] {error}", this); }
    }
}
