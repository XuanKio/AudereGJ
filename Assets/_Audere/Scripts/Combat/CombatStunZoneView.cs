using UnityEngine;

namespace Audere.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform), typeof(CanvasGroup))]
    public sealed class CombatStunZoneView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup group;

        private RectTransform rectTransform;
        private Vector2 authoredPosition;
        private Vector2 authoredSize;
        private bool authoredLayoutCaptured;

        public RectTransform RectTransform
        {
            get
            {
                ResolveReferences();
                return rectTransform;
            }
        }

        public bool IsVisible => group != null && group.alpha > .001f;
        public bool IsBlocking { get; private set; }

        private void Awake()
        {
            ResolveReferences();
            CaptureAuthoredLayout();
        }

        public void SetPresentation(Vector2 position, Vector2 size, float alpha, bool blocking)
        {
            ResolveReferences();
            CaptureAuthoredLayout();
            if (rectTransform == null || group == null)
                return;

            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = new Vector2(Mathf.Max(1f, size.x), Mathf.Max(1f, size.y));
            group.alpha = Mathf.Clamp01(alpha);
            group.interactable = false;
            group.blocksRaycasts = false;
            IsBlocking = blocking && group.alpha > .001f;
        }

        public void ShowAuthored(bool blocking)
        {
            ResolveReferences();
            CaptureAuthoredLayout();
            SetPresentation(authoredPosition, authoredSize, 1f, blocking);
        }

        public void ForceHide()
        {
            ResolveReferences();
            if (group != null)
            {
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;
            }
            IsBlocking = false;
        }

        private void ResolveReferences()
        {
            if (rectTransform == null)
                rectTransform = transform as RectTransform;
            if (group == null)
                group = GetComponent<CanvasGroup>();
        }

        private void CaptureAuthoredLayout()
        {
            if (authoredLayoutCaptured || rectTransform == null)
                return;
            authoredPosition = rectTransform.anchoredPosition;
            authoredSize = rectTransform.sizeDelta;
            authoredLayoutCaptured = true;
        }

        private void OnDisable()
        {
            IsBlocking = false;
        }
    }
}
