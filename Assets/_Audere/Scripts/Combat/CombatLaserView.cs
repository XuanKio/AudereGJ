using UnityEngine;
using UnityEngine.UI;

namespace Audere.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(Image))]
    public sealed class CombatLaserView : MonoBehaviour
    {
        private static readonly Color TelegraphColor = new Color(1f, 1f, 1f, .72f);
        private static readonly Color ActiveColor = new Color(.98f, .48f, .56f, .98f);

        private RectTransform rectTransform;
        private Image image;
        private Vector2 startPosition;
        private Vector2 endPosition;
        private float telegraphDuration;
        private float activeDuration;
        private float elapsed;
        private Color fadeStartColor;

        public RectTransform RectTransform => rectTransform;
        public int OwnerSessionVersion { get; private set; }
        public int OwnerPhaseVersion { get; private set; }
        public bool CollisionActive { get; private set; }

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            image = GetComponent<Image>();
            image.raycastTarget = false;
        }

        public void Setup(
            Vector2 start,
            Vector2 end,
            Vector2 size,
            float rotationDegrees,
            float telegraphSeconds,
            float activeSeconds,
            int sessionVersion,
            int phaseVersion)
        {
            if (rectTransform == null)
                Awake();

            startPosition = start;
            endPosition = end;
            telegraphDuration = Mathf.Max(0f, telegraphSeconds);
            activeDuration = Mathf.Max(.05f, activeSeconds);
            elapsed = 0f;
            OwnerSessionVersion = sessionVersion;
            OwnerPhaseVersion = phaseVersion;
            CollisionActive = telegraphDuration <= 0f;
            rectTransform.anchoredPosition = start;
            rectTransform.sizeDelta = size;
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotationDegrees);
            rectTransform.localScale = CollisionActive ? Vector3.one : new Vector3(.08f, 1f, 1f);
            image.color = CollisionActive
                ? ActiveColor
                : new Color(TelegraphColor.r, TelegraphColor.g, TelegraphColor.b, 0f);
            gameObject.SetActive(true);
        }

        public void BeginPresentationFade()
        {
            if (rectTransform == null)
                Awake();
            CollisionActive = false;
            fadeStartColor = image != null ? image.color : Color.clear;
        }

        public void SetPresentationFade(float visibility)
        {
            if (image == null)
                return;
            visibility = Mathf.Clamp01(visibility);
            image.color = new Color(
                fadeStartColor.r,
                fadeStartColor.g,
                fadeStartColor.b,
                fadeStartColor.a * visibility);
        }

        public bool Tick(float deltaTime)
        {
            elapsed += Mathf.Max(0f, deltaTime);
            if (elapsed < telegraphDuration)
            {
                float telegraphProgress = Mathf.Clamp01(elapsed / Mathf.Max(.01f, telegraphDuration));
                float eased = telegraphProgress * telegraphProgress * (3f - 2f * telegraphProgress);
                rectTransform.localScale = new Vector3(Mathf.Lerp(.08f, 1f, eased), 1f, 1f);
                image.color = new Color(
                    TelegraphColor.r,
                    TelegraphColor.g,
                    TelegraphColor.b,
                    TelegraphColor.a * eased);
                return true;
            }

            CollisionActive = true;
            rectTransform.localScale = Vector3.one;
            float activeElapsed = elapsed - telegraphDuration;
            float t = Mathf.Clamp01(activeElapsed / activeDuration);
            rectTransform.anchoredPosition = Vector2.LerpUnclamped(startPosition, endPosition, t);
            image.color = ActiveColor;
            return activeElapsed < activeDuration;
        }

        public void ReturnToPool()
        {
            CollisionActive = false;
            OwnerSessionVersion = 0;
            OwnerPhaseVersion = 0;
            elapsed = 0f;
            if (rectTransform != null)
                rectTransform.localScale = Vector3.one;
            if (image != null)
                image.color = ActiveColor;
            gameObject.SetActive(false);
        }
    }
}
