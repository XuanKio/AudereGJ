using UnityEngine;

namespace Audere.Combat
{
    [CreateAssetMenu(menuName = "Audere/Combat/Phase Presentation")]
    public sealed class CombatPhasePresentationProfile : ScriptableObject
    {
        [SerializeField, Range(.5f, 1f)] private float boardWidth = 1f;
        [SerializeField] private Vector2 playerOffset;
        [SerializeField] private Vector2 enemyOffset;
        [SerializeField] private Vector2 enemyLabelOffset;
        [SerializeField, Min(.1f)] private float duration = 1.1f;
        public float BoardWidth => boardWidth;
        public Vector2 PlayerOffset => playerOffset;
        public Vector2 EnemyOffset => enemyOffset;
        public Vector2 EnemyLabelOffset => enemyLabelOffset;
        public float Duration => duration;
    }
}
