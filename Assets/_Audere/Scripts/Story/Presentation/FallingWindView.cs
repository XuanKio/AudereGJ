using UnityEngine;

namespace Audere.Story.Presentation
{
    /// <summary>Rising edge streaks keep a camera-framed falling pose in free fall through player-paced dialogue.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class FallingWindView : MonoBehaviour
    {
        [SerializeField] private StoryEvent owner;
        [SerializeField] private FallingRoomProfile profile;
        [SerializeField] private Camera worldCamera;
        [Tooltip("Top, Bottom, Left, Right: the visible opening, inside the viewport cover.")]
        [SerializeField] private SpriteRenderer[] apertureEdges;
        private SpriteRenderer[] streaks;
        private Sprite pixel;
        private float elapsed;
        public bool IsRunning => streaks != null;
        public int StreakCount => streaks == null ? 0 : streaks.Length;

        private void OnEnable() { if (owner != null) owner.Ended += OnEventEnded; }
        private void OnDisable()
        {
            if (owner != null) owner.Ended -= OnEventEnded;
            Stop();
        }
        private void OnEventEnded(StoryEventResult result) => Stop();

        public bool Begin()
        {
            Stop();
            if (owner == null || profile == null || worldCamera == null || apertureEdges == null || apertureEdges.Length != 4)
                return false;
            foreach (var edge in apertureEdges) if (edge == null) return false;
            pixel = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * .5f, 1f);
            pixel.name = "Falling wind pixel";
            streaks = new SpriteRenderer[profile.StreakCount];
            for (int i = 0; i < streaks.Length; i++)
            {
                var streak = new GameObject("Rising wind streak " + i).AddComponent<SpriteRenderer>();
                streak.transform.SetParent(transform, false);
                streak.sprite = pixel;
                streak.sortingLayerName = "Player";
                streak.sortingOrder = 0;
                streak.color = Color.clear;
                streaks[i] = streak;
            }
            return true;
        }

        private void LateUpdate()
        {
            if (!IsRunning) return;
            if (owner == null || !owner.IsPlaying) { Stop(); return; }
            elapsed += Time.unscaledDeltaTime;
            float left = apertureEdges[2].bounds.max.x, right = apertureEdges[3].bounds.min.x;
            float bottom = apertureEdges[1].bounds.max.y, top = apertureEdges[0].bounds.min.y;
            float height = Mathf.Max(.01f, top - bottom);
            float fade = Mathf.SmoothStep(0f, 1f, elapsed / profile.FractureDuration);
            float rise = profile.Rise(elapsed);
            for (int i = 0; i < streaks.Length; i++)
            {
                // Keep the center readable; use the same layered upward drift as the Crowd falling room.
                float edge = .035f + Mathf.Repeat(i * .618034f, 1f) * .19f;
                float x = i % 2 == 0 ? edge : 1f - edge;
                float depth = .5f + Mathf.Repeat(i * .317f, 1f);
                float y = Mathf.Repeat(i * .381966f + rise * depth / height, 1f);
                var streak = streaks[i];
                streak.transform.position = new Vector3(Mathf.Lerp(left, right, x), Mathf.Lerp(bottom, top, y), worldCamera.transform.position.z + 5f);
                streak.transform.localScale = new Vector3(.003f + i % 3 * .0015f, (.025f + i % 5 * .025f) * (1f + fade), 1f);
                Color color = profile.StreakColor;
                color.a *= fade * (.35f + depth * .45f);
                streak.color = color;
            }
        }

        public void Stop()
        {
            if (streaks != null)
                foreach (var streak in streaks)
                    if (streak != null) { streak.gameObject.SetActive(false); Destroy(streak.gameObject); }
            streaks = null;
            if (pixel != null) Destroy(pixel);
            pixel = null;
            elapsed = 0f;
        }
    }
}
