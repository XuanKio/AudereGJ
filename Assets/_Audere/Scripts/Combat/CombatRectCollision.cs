using UnityEngine;

namespace Audere.Combat
{
    /// <summary>Oriented rectangle SAT: rotating chalk/lasers must not hit their empty AABB corners.</summary>
    public static class CombatRectCollision
    {
        // Called only on Unity's main thread; reusable arrays avoid per-projectile allocations.
        private static readonly Vector3[] aCorners = new Vector3[4];
        private static readonly Vector3[] bCorners = new Vector3[4];
        public static bool Overlaps(RectTransform a, RectTransform b)
        {
            if (a == null || b == null) return false;
            a.GetWorldCorners(aCorners); b.GetWorldCorners(bCorners);
            return !Separated(aCorners, bCorners) && !Separated(bCorners, aCorners);
        }
        private static bool Separated(Vector3[] a, Vector3[] b)
        {
            for (int edge = 0; edge < 2; edge++)
            {
                Vector2 side = a[edge + 1] - a[edge];
                Vector2 axis = new Vector2(-side.y, side.x);
                if (axis.sqrMagnitude < .0000001f) return true;
                float minA = float.PositiveInfinity, maxA = float.NegativeInfinity;
                float minB = float.PositiveInfinity, maxB = float.NegativeInfinity;
                for (int i = 0; i < 4; i++)
                {
                    float pa = Vector2.Dot(a[i], axis), pb = Vector2.Dot(b[i], axis);
                    minA = Mathf.Min(minA, pa); maxA = Mathf.Max(maxA, pa);
                    minB = Mathf.Min(minB, pb); maxB = Mathf.Max(maxB, pb);
                }
                if (maxA < minB || maxB < minA) return true;
            }
            return false;
        }
    }
}
