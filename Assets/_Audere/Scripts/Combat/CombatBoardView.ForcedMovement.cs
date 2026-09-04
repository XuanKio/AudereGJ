using UnityEngine;

namespace Audere.Combat
{
    public sealed partial class CombatBoardView
    {
        private object forcedPlayerControlOwner;
        private float forcedMovementGrace;
        public bool HasForcedPlayerControl => forcedPlayerControlOwner != null;
        public bool HasForcedMovementProtection => HasForcedPlayerControl || forcedMovementGrace > 0f;

        // Finite, owner-scoped involuntary movement. Mouse cannot counteract the pull.
        // The player is protected while input cannot dodge, plus a short release grace.
        public void SetForcedPlayerControl(object owner, Vector2 normalizedTarget, float speed, float deltaTime)
        {
            if (owner == null || catchCursor == null || playArea == null ||
                (forcedPlayerControlOwner != null && !ReferenceEquals(owner, forcedPlayerControlOwner))) return;
            forcedPlayerControlOwner = owner;
            forcedMovementGrace = .35f;
            Rect r = playArea.rect;
            Vector2 target = new Vector2(Mathf.Lerp(r.xMin, r.xMax, Mathf.Clamp01(normalizedTarget.x)),
                Mathf.Lerp(r.yMin, r.yMax, Mathf.Clamp01(normalizedTarget.y)));
            catchCursor.anchoredPosition = Vector2.MoveTowards(catchCursor.anchoredPosition,
                ClampCursorToBattleBox(target), Mathf.Max(0f, speed) * Mathf.Max(0f, deltaTime));
        }

        public void ReleaseForcedPlayerControl(object owner)
        {
            if (ReferenceEquals(owner, forcedPlayerControlOwner)) forcedPlayerControlOwner = null;
        }
    }
}
