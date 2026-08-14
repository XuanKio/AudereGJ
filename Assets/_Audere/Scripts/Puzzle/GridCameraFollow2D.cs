using Audere.Puzzle.Board;
using UnityEngine;

namespace Audere.Puzzle
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class GridCameraFollow2D : MonoBehaviour
    {
        [SerializeField] private GridPlayer target;
        [SerializeField] private BoardManager board;
        [SerializeField] private Vector2 deadZone = Vector2.zero;
        [SerializeField, Min(.01f)] private float smoothTime = .18f;
        [Tooltip("Normalized part of the camera view left open by the screen-fixed gameplay mask.")]
        [SerializeField] private Vector2 framingCoverage = new Vector2(.56f, .60f);
        [Tooltip("Keep disabled when the camera should follow Player over the black void outside the board.")]
        [SerializeField] private bool clampToBoard;

        private Camera followCamera;
        private Vector3 velocity;
        private bool initialized;

        public void Configure(GridPlayer player, BoardManager boardManager)
        {
            target = player;
            board = boardManager;
            initialized = false;
        }

        public void ConfigureMotion(Vector2 deadZoneSize, float dampingTime)
        {
            deadZone = new Vector2(
                Mathf.Max(0f, deadZoneSize.x),
                Mathf.Max(0f, deadZoneSize.y));
            smoothTime = Mathf.Max(.01f, dampingTime);
            velocity = Vector3.zero;
            initialized = false;
        }

        public void ConfigureFramingCoverage(Vector2 normalizedCoverage)
        {
            framingCoverage = new Vector2(
                Mathf.Clamp(normalizedCoverage.x, .05f, 1f),
                Mathf.Clamp(normalizedCoverage.y, .05f, 1f));
            velocity = Vector3.zero;
            initialized = false;
        }

        public void ConfigureBoardClamping(bool enabled)
        {
            clampToBoard = enabled;
            velocity = Vector3.zero;
            initialized = false;
        }

        private void Awake()
        {
            followCamera = GetComponent<Camera>();
            if (target == null) target = FindFirstObjectByType<GridPlayer>();
            if (board == null) board = FindFirstObjectByType<BoardManager>();
        }

        private void LateUpdate()
        {
            if (target == null || followCamera == null)
                return;

            Vector3 current = transform.position;
            Vector3 desired = initialized
                ? ApplyDeadZone(current, target.transform.position)
                : target.transform.position;

            if (clampToBoard)
                desired = ClampToBoard(desired);
            desired.z = current.z;

            if (!initialized)
            {
                transform.position = desired;
                velocity = Vector3.zero;
                initialized = true;
                return;
            }

            transform.position = Vector3.SmoothDamp(
                current,
                desired,
                ref velocity,
                smoothTime,
                Mathf.Infinity,
                Time.unscaledDeltaTime);
        }

        private Vector3 ApplyDeadZone(Vector3 cameraPosition, Vector3 targetPosition)
        {
            Vector3 desired = cameraPosition;
            float deltaX = targetPosition.x - cameraPosition.x;
            float deltaY = targetPosition.y - cameraPosition.y;

            if (Mathf.Abs(deltaX) > deadZone.x)
                desired.x = targetPosition.x - Mathf.Sign(deltaX) * deadZone.x;
            if (Mathf.Abs(deltaY) > deadZone.y)
                desired.y = targetPosition.y - Mathf.Sign(deltaY) * deadZone.y;

            return desired;
        }

        private Vector3 ClampToBoard(Vector3 desired)
        {
            if (board == null || !board.TryGetWorldBounds(out Bounds bounds))
                return desired;

            float halfHeight = followCamera.orthographicSize *
                Mathf.Clamp(framingCoverage.y, .05f, 1f);
            float viewportAspect = followCamera.pixelHeight > 0
                ? (float)followCamera.pixelWidth / followCamera.pixelHeight
                : followCamera.aspect;
            float halfWidth = followCamera.orthographicSize * viewportAspect *
                Mathf.Clamp(framingCoverage.x, .05f, 1f);

            desired.x = ClampAxis(desired.x, bounds.min.x, bounds.max.x, halfWidth);
            desired.y = ClampAxis(desired.y, bounds.min.y, bounds.max.y, halfHeight);
            return desired;
        }

        private static float ClampAxis(float value, float minimum, float maximum, float halfView)
        {
            float usableMinimum = minimum + halfView;
            float usableMaximum = maximum - halfView;
            return usableMinimum > usableMaximum
                ? (minimum + maximum) * .5f
                : Mathf.Clamp(value, usableMinimum, usableMaximum);
        }
    }
}
