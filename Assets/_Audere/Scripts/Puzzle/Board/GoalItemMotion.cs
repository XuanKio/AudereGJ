using UnityEngine;

namespace Audere.Puzzle.Board
{
    public enum GoalItemMotionMode
    {
        Stationary,
        Floating
    }

    /// <summary>
    /// Presentation-only motion for the Item child on a scene-authored Goal.
    /// It never changes the goal tile's gameplay position.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Audere/Puzzle/Goal Item Motion")]
    public sealed class GoalItemMotion : MonoBehaviour
    {
        [SerializeField] private GoalItemMotionMode motion = GoalItemMotionMode.Stationary;
        [SerializeField, Min(0f)] private float floatDistance = .06f;
        [SerializeField, Min(0f)] private float floatCyclesPerSecond = 1f;
        [SerializeField, Range(0f, 1f)] private float phaseOffset;

        private Vector3 restingLocalPosition;
        private bool hasRestingPosition;

        public GoalItemMotionMode Motion
        {
            get => motion;
            set
            {
                motion = value;
                if (motion == GoalItemMotionMode.Stationary)
                    RestoreRestingPosition();
            }
        }

        private void OnEnable()
        {
            CaptureRestingPosition();
        }

        private void Update()
        {
            if (motion == GoalItemMotionMode.Stationary)
            {
                RestoreRestingPosition();
                return;
            }

            CaptureRestingPosition();
            float phase = (Time.time * floatCyclesPerSecond + phaseOffset) * Mathf.PI * 2f;
            transform.localPosition = restingLocalPosition + Vector3.up * (Mathf.Sin(phase) * floatDistance);
        }

        private void OnDisable()
        {
            RestoreRestingPosition();
        }

        private void CaptureRestingPosition()
        {
            if (hasRestingPosition)
                return;

            restingLocalPosition = transform.localPosition;
            hasRestingPosition = true;
        }

        private void RestoreRestingPosition()
        {
            if (hasRestingPosition)
                transform.localPosition = restingLocalPosition;
        }

        private void OnValidate()
        {
            floatDistance = Mathf.Max(0f, floatDistance);
            floatCyclesPerSecond = Mathf.Max(0f, floatCyclesPerSecond);
        }
    }
}
