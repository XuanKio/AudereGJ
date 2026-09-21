using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Audere.Combat
{
    public sealed partial class CombatBoardView
    {
        [Header("Exterior projectiles and catch-blocking trails")]
        [SerializeField] private RectTransform exteriorProjectileRoot;
        [SerializeField] private RectTransform stunTrailRoot;
        [SerializeField] private Material stunTrailMaterial;
        [SerializeField] private Color stunTrailTint = new Color(.9f, .84f, .94f, 1f);
        [SerializeField, Min(0f)] private float stunTrailInset = 14f;
        private const int MaximumTrailSegments = 384;
        private readonly List<TrailSegment> trailPool = new List<TrailSegment>();
        private readonly Vector3[] trailCorners = new Vector3[4];
        private CombatChalkTrailGraphic trailGraphic;

        private sealed class TrailSegment
        {
            public CombatStunZoneView View;
            public object Owner;
            public int Session, Phase;
            public float Age, Hold, Fade;
            public Vector2 From, To;
            public float Width, Alpha;
            public bool Active;
        }

        public bool HasExteriorTrailBindings => exteriorProjectileRoot != null && stunTrailRoot != null && stunTrailMaterial != null;
        public int ActiveStunTrailCount
        {
            get { int count = 0; foreach (var t in trailPool) if (t.Active) count++; return count; }
        }

        // Coordinates are always Dice Field-local, even while rendered outside its mask.
        public CombatBulletView SpawnExteriorEnemyBullet(CombatBulletView prefab, Vector2 position,
            int session, int phase, float telegraph)
        {
            if (!HasExteriorTrailBindings)
                throw new MissingReferenceException("Bind Exterior Projectile Root, Stun Trail Root and trail material on CombatBoard.");
            SyncExteriorProjectileRoot();
            var bullet = SpawnEnemyBullet(prefab, position, Vector2.zero, session, phase, telegraph);
            if (bullet != null)
            {
                bullet.transform.SetParent(exteriorProjectileRoot, false);
                bullet.RectTransform.anchoredPosition = position;
            }
            return bullet;
        }

        public void ReturnEnemyBullet(CombatBulletView bullet, int leaseVersion)
        {
            if (bullet == null || bullet.PoolLeaseVersion != leaseVersion) return;
            activeBullets.Remove(bullet);
            bullet.ReturnToPool();
        }

        private void SyncExteriorProjectileRoot()
        {
            if (exteriorProjectileRoot == null || playArea == null) return;
            var root = exteriorProjectileRoot;
            root.pivot = playArea.pivot;
            root.sizeDelta = playArea.rect.size;
            root.SetPositionAndRotation(playArea.position, playArea.rotation);
            Vector3 parentScale = root.parent.lossyScale;
            Vector3 fieldScale = playArea.lossyScale;
            root.localScale = new Vector3(fieldScale.x / parentScale.x, fieldScale.y / parentScale.y, 1f);
        }

        private CombatBulletView FindExteriorPooledBullet(CombatBulletView prefab)
        {
            if (exteriorProjectileRoot == null) return null;
            for (int i = 0; i < exteriorProjectileRoot.childCount; i++)
            {
                var b = exteriorProjectileRoot.GetChild(i).GetComponent<CombatBulletView>();
                if (b != null && !b.gameObject.activeSelf && b.SourcePrefab == prefab) return b;
            }
            return null;
        }

        private void ClearExteriorProjectiles()
        {
            if (exteriorProjectileRoot == null) return;
            for (int i = 0; i < exteriorProjectileRoot.childCount; i++)
            {
                var b = exteriorProjectileRoot.GetChild(i).GetComponent<CombatBulletView>();
                if (b == null) continue;
                activeBullets.Remove(b);
                b.ReturnToPool();
            }
        }

        public void EmitStunTrail(object owner, int session, int phase, Vector2 from, Vector2 to,
            float width, float hold, float fade)
        {
            if (!isActiveAndEnabled || stunTrailRoot == null || playArea == null || owner == null ||
                width <= 0f || hold <= 0f || fade <= 0f) return;
            if (!ClipVisibleTrail(ref from, ref to, width)) return;
            EnsureTrailGraphic();
            TrailSegment segment = null;
            foreach (var candidate in trailPool) if (!candidate.Active) { segment = candidate; break; }
            if (segment == null)
            {
                if (trailPool.Count >= MaximumTrailSegments) return;
                var go = new GameObject("Stun Trail (pooled)", typeof(RectTransform),
                    typeof(CanvasGroup), typeof(CombatStunZoneView));
                go.transform.SetParent(stunTrailRoot, false);
                segment = new TrailSegment { View = go.GetComponent<CombatStunZoneView>() };
                trailPool.Add(segment);
            }
            segment.Owner = owner; segment.Session = session; segment.Phase = phase;
            segment.Age = 0; segment.Hold = hold; segment.Fade = fade; segment.Active = true;
            segment.From = from; segment.To = to; segment.Width = width;
            segment.View.gameObject.SetActive(true);
            UpdateTrailPresentation(segment);
            trailGraphic.SetVerticesDirty();
        }

        public void ClearStunTrails(int session = -1, int phase = -1, object owner = null)
        {
            foreach (var t in trailPool)
                if (t.Active && (session < 0 || t.Session == session) && (phase < 0 || t.Phase == phase) &&
                    (owner == null || ReferenceEquals(owner, t.Owner))) HideTrail(t);
            trailGraphic?.SetVerticesDirty();
            catchCursorView?.SetStunned(OverlapsActiveStunZone(catchCursor));
        }

        private void TickStunTrails(float deltaTime)
        {
            bool changed = false;
            foreach (var t in trailPool)
            {
                if (!t.Active || t.View == null) continue;
                changed = true;
                t.Age += Mathf.Max(0f, deltaTime);
                if (t.Age >= t.Hold + t.Fade) { HideTrail(t); continue; }
                UpdateTrailPresentation(t);
            }
            if (changed) trailGraphic?.SetVerticesDirty();
            catchCursorView?.SetStunned(OverlapsActiveStunZone(catchCursor));
        }

        private void EnsureTrailGraphic()
        {
            if (trailGraphic != null) return;
            var go = new GameObject("Chalk Trail Surface", typeof(RectTransform), typeof(CanvasRenderer),
                typeof(CombatChalkTrailGraphic));
            go.transform.SetParent(stunTrailRoot, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            rect.pivot = playArea.pivot;
            trailGraphic = go.GetComponent<CombatChalkTrailGraphic>();
            trailGraphic.material = stunTrailMaterial;
            trailGraphic.raycastTarget = false;
            trailGraphic.Bind(this);
        }

        private bool ClipVisibleTrail(ref Vector2 from, ref Vector2 to, float width)
        {
            Rect bounds = playArea.rect;
            // Include the strip's half-width so its full rotated footprint stays visible.
            float inset = stunTrailInset + width * .5f;
            bounds = Rect.MinMaxRect(bounds.xMin + inset, bounds.yMin + inset, bounds.xMax - inset, bounds.yMax - inset);
            return ClipTrailSegment(bounds, ref from, ref to) && (to - from).sqrMagnitude >= .01f;
        }

        private bool UpdateTrailPresentation(TrailSegment trail)
        {
            Vector2 from = trail.From, to = trail.To;
            if (!ClipVisibleTrail(ref from, ref to, trail.Width))
            {
                trail.Alpha = 0f;
                trail.View.ForceHide();
                return false;
            }
            Vector2 direction = to - from;
            var rect = trail.View.RectTransform;
            rect.anchorMin = rect.anchorMax = playArea.pivot;
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            trail.Alpha = .8f * (1f - Mathf.SmoothStep(0f, 1f, (trail.Age - trail.Hold) / trail.Fade));
            trail.View.SetPresentation((from + to) * .5f, new Vector2(direction.magnitude, trail.Width),
                trail.Alpha, trail.Age < trail.Hold);
            // CombatStunZoneView clamps tiny generic zones to one unit; trail endpoints must
            // preserve their exact clipped length to avoid extending beyond the visible stroke.
            rect.sizeDelta = new Vector2(direction.magnitude, trail.Width);
            return true;
        }

        internal void PopulateStunTrailMesh(VertexHelper vertices, RectTransform surface)
        {
            vertices.Clear();
            foreach (var trail in trailPool)
            {
                if (!trail.Active || trail.View == null || !UpdateTrailPresentation(trail)) continue;
                trail.View.RectTransform.GetWorldCorners(trailCorners);
                Color tint = stunTrailTint;
                tint.a *= trail.Alpha;
                int start = vertices.currentVertCount;
                // One common surface keeps shader grain in board space across every strip.
                // Adjacent strips share endpoints; neither geometry nor collision adds caps.
                vertices.AddVert(surface.InverseTransformPoint(trailCorners[0]), tint, Vector2.zero);
                vertices.AddVert(surface.InverseTransformPoint(trailCorners[1]), tint, Vector2.up);
                vertices.AddVert(surface.InverseTransformPoint(trailCorners[2]), tint, Vector2.one);
                vertices.AddVert(surface.InverseTransformPoint(trailCorners[3]), tint, Vector2.right);
                vertices.AddTriangle(start, start + 1, start + 2);
                vertices.AddTriangle(start, start + 2, start + 3);
            }
        }

        private static void HideTrail(TrailSegment trail)
        {
            trail.Active = false; trail.Owner = null;
            if (trail.View == null) return;
            trail.View.ForceHide(); trail.View.gameObject.SetActive(false);
        }

        private bool OverlapsStunTrail(RectTransform target)
        {
            if (target == null || stunTrailRoot == null || !stunTrailRoot.gameObject.activeInHierarchy) return false;
            foreach (var t in trailPool)
                if (t.Active && t.View != null && UpdateTrailPresentation(t) && t.View.IsBlocking &&
                    CircleOverlapsRectTransform(target, t.View.RectTransform)) return true;
            return false;
        }

        public static bool ClipTrailSegment(Rect bounds, ref Vector2 from, ref Vector2 to)
        {
            if (bounds.width <= 0f || bounds.height <= 0f) return false;
            Vector2 delta = to - from;
            float enter = 0f, exit = 1f;
            if (!ClipEdge(-delta.x, from.x - bounds.xMin, ref enter, ref exit) ||
                !ClipEdge(delta.x, bounds.xMax - from.x, ref enter, ref exit) ||
                !ClipEdge(-delta.y, from.y - bounds.yMin, ref enter, ref exit) ||
                !ClipEdge(delta.y, bounds.yMax - from.y, ref enter, ref exit)) return false;
            to = from + delta * exit; from += delta * enter;
            return true;
        }

        private static bool ClipEdge(float p, float q, ref float enter, ref float exit)
        {
            if (Mathf.Abs(p) < .00001f) return q >= 0f;
            float t = q / p;
            if (p < 0) enter = Mathf.Max(enter, t); else exit = Mathf.Min(exit, t);
            return enter <= exit;
        }
    }
}
