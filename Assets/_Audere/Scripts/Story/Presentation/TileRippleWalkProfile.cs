using UnityEngine;

namespace Audere.Story.Presentation
{
    [CreateAssetMenu(menuName = "Audere/Story/Tile Ripple Walk")]
    public sealed class TileRippleWalkProfile : ScriptableObject
    {
        [SerializeField, Min(.1f)] private float duration = 15.4f;
        [SerializeField, Min(0f)] private float initialHold = 1.2f;
        [SerializeField, Min(.1f)] private float pathRevealDuration = 2.2f;
        [SerializeField, Min(.1f)] private float pathLeadDistance = 1.5f;
        [SerializeField, Min(.01f)] private float tileRevealDistance = .25f;
        [SerializeField, Min(0f)] private float acceleration = 3f;
        [SerializeField, Min(0f)] private float strideLift = .032f;
        [SerializeField] private Color background = new Color32(252, 225, 194, 255);
        [SerializeField, Range(0f, .95f)] private float whiteStart = .7f;

        public float Duration => Mathf.Max(.1f, duration);
        public float StrideLift => strideLift;
        public Color Background => background;
        public float InitialHold => Mathf.Max(0f, initialHold);
        public float WalkStart => InitialHold + Mathf.Max(.1f, pathRevealDuration);
        public float PathLeadDistance => Mathf.Max(.1f, pathLeadDistance);
        public float TileRevealDistance => Mathf.Max(.01f, tileRevealDistance);
        public float RevealProgress(float time) => Mathf.InverseLerp(InitialHold, WalkStart, time);
        public float DistanceProgress(float time)
        {
            float t = Mathf.InverseLerp(WalkStart, Mathf.Max(WalkStart + .1f, Duration), time);
            float a = Mathf.Max(0f, acceleration);
            return (t + a * t * t) / (1f + a);
        }
        public float WhiteAlpha(float time) => Mathf.SmoothStep(0f, 1f,
            Mathf.InverseLerp(whiteStart, 1f, time / Duration));
    }
}
