using UnityEngine;

namespace Audere.Story.Presentation
{
    [CreateAssetMenu(menuName = "Audere/Story/Falling Room")]
    public sealed class FallingRoomProfile : ScriptableObject
    {
        [SerializeField] private Color voidColor = Color.black;
        [SerializeField, Min(.1f)] private float fractureDuration = 1.4f;
        [SerializeField, Min(0f)] private float initialSpeed = .16f;
        [SerializeField, Min(0f)] private float acceleration = .18f;
        [SerializeField, Range(8, 80)] private int streakCount = 38;
        [SerializeField] private Color streakColor = new Color(.62f, .61f, .7f, .42f);

        public Color VoidColor => voidColor;
        public float FractureDuration => Mathf.Max(.1f, fractureDuration);
        public int StreakCount => Mathf.Clamp(streakCount, 8, 80);
        public Color StreakColor => streakColor;
        public float Rise(float time)
        {
            float t = Mathf.Max(0, time);
            // Keep long, player-paced dialogue from producing unbounded speeds.
            float ramp = Mathf.Min(t, 4f);
            return initialSpeed * t + acceleration * (ramp * ramp * .5f + Mathf.Max(0, t - 4f) * 4f);
        }
    }
}
