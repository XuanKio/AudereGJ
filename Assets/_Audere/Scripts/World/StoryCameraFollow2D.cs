using Audere.Story;
using UnityEngine;

namespace Audere.World
{
    /// <summary>Follows a scene-authored story actor after the covered combat hand-off.</summary>
    public sealed class StoryCameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Transform target;
        [SerializeField] private GameObject puzzleViewportMask;
        [SerializeField] private StoryEvent ownerEvent;
        [SerializeField] private Vector2 framingOffset = new Vector2(0f, .17f);
        [SerializeField, Min(.01f)] private float smoothTime = .24f;

        private Vector3 cameraStartPosition;
        private Vector3 velocity;

        public Transform Target => target;

        private void OnEnable()
        {
            if (worldCamera == null || target == null || puzzleViewportMask == null || ownerEvent == null)
            {
                Debug.LogError("[StoryCameraFollow2D] Assign camera, actor, mask, and StoryEvent.", this);
                enabled = false;
                return;
            }

            cameraStartPosition = worldCamera.transform.position;
            velocity = Vector3.zero;
            puzzleViewportMask.SetActive(false);
            worldCamera.transform.position = DesiredPosition();
            ownerEvent.Ended += HandleEventEnded;
        }

        private void LateUpdate()
        {
            if (worldCamera == null || target == null)
                return;

            worldCamera.transform.position = Vector3.SmoothDamp(
                worldCamera.transform.position, DesiredPosition(), ref velocity,
                smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
        }

        private Vector3 DesiredPosition()
        {
            Vector3 position = target.position + (Vector3)framingOffset;
            position.z = worldCamera.transform.position.z;
            return position;
        }

        private void HandleEventEnded(StoryEventResult result)
        {
            if (result == StoryEventResult.Completed)
                return;

            worldCamera.transform.position = cameraStartPosition;
            puzzleViewportMask.SetActive(true);
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            if (ownerEvent != null)
                ownerEvent.Ended -= HandleEventEnded;
        }
    }
}
