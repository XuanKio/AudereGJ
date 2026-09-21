using UnityEngine;
using UnityEngine.UI;

namespace Audere.Combat
{
    [CreateAssetMenu(menuName = "Audere/Combat/Moves/Orbiting Clone Volley")]
    public sealed class OrbitingCloneVolleyMove : CombatMoveDefinition
    {
        [SerializeField] private Sprite cloneSprite;
        [SerializeField] private Material silhouetteMaterial;
        [SerializeField] private CombatBulletView projectile;
        [SerializeField, Range(.45f, 1f)] private float fieldWidth = .5f;
        [SerializeField, Min(.55f)] private float telegraph = .65f;
        [SerializeField, Min(.3f)] private float betweenShots = .4f;
        [SerializeField, Min(50f)] private float speed = 180f;
        [SerializeField, Range(3, 7)] private int projectilesPerFan = 5;
        [SerializeField, Range(10f, 18f)] private float fanSpacing = 13f;
        [SerializeField, Min(.1f)] private float followupDelay = .14f;
        [SerializeField, Range(.5f, 1.2f)] private float orbitAngularSpeed = .8f;
        public static int ShooterIndex(int shot) => shot < 4 ? shot : 7 - shot;
        public static Vector2 ClonePosition(int index, Rect field, float orbitAngle, Vector2 size)
        {
            float radius = new Vector2(field.width * .5f + size.x*.5f + 20f, field.height * .5f + size.y*.5f + 20f).magnitude;
            float angle = Mathf.PI * .75f - index * Mathf.PI * .5f - orbitAngle;
            return field.center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
        public static Vector2 MuzzlePosition(Vector2 center, Vector2 size, Vector2 aim)
        {
            Vector2 direction = (aim - center).normalized;
            float reachX = Mathf.Abs(direction.x) > .001f ? size.x * .5f / Mathf.Abs(direction.x) : float.MaxValue;
            float reachY = Mathf.Abs(direction.y) > .001f ? size.y * .5f / Mathf.Abs(direction.y) : float.MaxValue;
            return center + direction * Mathf.Max(0f, Mathf.Min(reachX, reachY) - 12f);
        }
        public override bool Validate(out string error)
        {
            if (!base.Validate(out error)) return false;
            if (cloneSprite == null || projectile == null || fieldWidth < .45f || telegraph < .55f ||
                betweenShots < .3f || followupDelay >= betweenShots || projectilesPerFan < 3 ||
                projectilesPerFan % 2 == 0 || Duration < 2.1f + 8f * (telegraph + betweenShots))
            { error = "Clone volley needs eight telegraphed shots, a reversal pause and a recovery window."; return false; }
            return true;
        }
        public override ICombatMoveExecution CreateExecution(CombatMoveExecutionContext ctx) => new Execution(this, ctx);
        private sealed class Execution : ICombatMoveExecution
        {
            private readonly OrbitingCloneVolleyMove data;
            private readonly CombatMoveExecutionContext ctx;
            private readonly CombatMoveStage stage;
            private readonly Image[] clones = new Image[4];
            private readonly CombatMountRainbowEcho[] trails = new CombatMountRainbowEcho[4];
            private readonly Vector2 cloneSize;
            private readonly RectTransform root;
            private readonly GameObject originalActorVisual;
            private readonly bool originalActorVisible;
            private float elapsed, shotStart = .7f;
            private float orbitAngle, orbitDirection = 1f, firedAt = -10f;
            private int lastShooter = -1;
            private int shot;
            private bool locked, fired, followupFired, cancelled;
            private Vector2 origin, aim;
            public Execution(OrbitingCloneVolleyMove data, CombatMoveExecutionContext ctx)
            {
                this.data = data; this.ctx = ctx; stage = new CombatMoveStage(ctx.Board, Cancel);
                cloneSize=CombatMoveStage.OriginalActorSize(ctx,data.cloneSprite);
                originalActorVisual = ctx.Actor != null && ctx.Actor.VisualRoot != null ? ctx.Actor.VisualRoot.gameObject : null;
                if (originalActorVisual != null)
                { originalActorVisible = originalActorVisual.activeSelf; originalActorVisual.SetActive(false); }
                root = stage.Root("Exterior Timor silhouettes", true);
                for (int i = 0; i < clones.Length; i++)
                {
                    clones[i] = CombatMoveStage.Picture(root, "Timor echo " + i, data.cloneSprite,
                        cloneSize, Color.white);
                    trails[i] = new CombatMountRainbowEcho(clones[i], data.silhouetteMaterial, true);
                }
            }
            public bool IsComplete => cancelled || elapsed >= data.Duration;
            public void Tick(float dt)
            {
                if (cancelled || dt <= 0f) return;
                elapsed += dt;
                orbitDirection = Mathf.MoveTowards(orbitDirection, shot < 4 ? 1f : -1f, dt * 6f);
                orbitAngle += dt * data.orbitAngularSpeed * orbitDirection;
                float enter = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / .8f));
                float exit = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((data.Duration - elapsed) / .9f));
                float breathing = .5f + .5f * Mathf.Sin(elapsed * Mathf.PI * 2f / (data.telegraph + data.betweenShots));
                ctx.Board.SetBattleBoxSizeLayout(Mathf.Lerp(1f, data.fieldWidth + breathing * .055f, enter * exit),
                    Mathf.Lerp(1f, .45f + breathing * .035f, enter * exit), 0f);
                Rect r = ctx.Board.PlayArea.rect;
                root.anchoredPosition = ctx.Board.PlayArea.anchoredPosition;
                int shooter = shot < 8 ? ShooterIndex(shot) : -1;
                for (int i = 0; i < 4; i++)
                {
                    Vector2 p = ClonePosition(i, r, orbitAngle, cloneSize);
                    p *= 1f + (1f - enter) * .12f;
                    float recoil = i == lastShooter ? Mathf.Sin(Mathf.Clamp01((elapsed - firedAt) / .3f) * Mathf.PI) * 12f : 0f;
                    clones[i].rectTransform.anchoredPosition = p + p.normalized * recoil;
                    clones[i].color = new Color(1f,1f,1f,enter * exit);
                    trails[i].Tick(dt, enter > .2f && exit > .2f);
                }
                if (shot >= 8 || elapsed < shotStart) return;
                if (!locked)
                {
                    locked = true; origin = clones[shooter].rectTransform.anchoredPosition;
                    aim = ctx.Board.PlayerPosition;
                    ctx.Board.ShowAttackWarnings(this, new[] { aim }, elapsed);
                    ctx.Board.SetMechanicHint(shot < 4 ? "Theo chiều kim đồng hồ" : "Đổi chiều");
                }
                if (!fired && elapsed >= shotStart + data.telegraph)
                {
                    fired = true;
                    origin = MuzzlePosition(clones[shooter].rectTransform.anchoredPosition, cloneSize, aim);
                    firedAt = elapsed; lastShooter = shooter;
                    ctx.Board.HideAttackWarning(this);
                    FireFan();
                }
                if (fired && !followupFired && elapsed >= shotStart + data.telegraph + data.followupDelay)
                {
                    followupFired = true; FireFan();
                }
                if (elapsed >= shotStart + data.telegraph + data.betweenShots)
                {
                    shot++; locked = fired = followupFired = false;
                    shotStart = elapsed + (shot == 4 ? .65f : 0f);
                    // The last clockwise volley still needs to cross the field during this breather.
                    // Its owned path expires naturally; reversal must not erase it outside the box.
                    if (shot == 4) ctx.Board.SetMechanicHint("Đổi chiều");
                }
            }
            private void FireFan()
            {
                Vector2 direction = (aim - origin).normalized;
                int half = data.projectilesPerFan / 2;
                for (int i = -half; i <= half; i++)
                {
                    float a = i * data.fanSpacing * Mathf.Deg2Rad;
                    Vector2 d = new Vector2(direction.x * Mathf.Cos(a) - direction.y * Mathf.Sin(a),
                        direction.x * Mathf.Sin(a) + direction.y * Mathf.Cos(a));
                    stage.ExteriorBullet(ctx, data.projectile, origin, d * data.speed);
                }
            }
            public void Cancel()
            {
                if (cancelled) return; cancelled = true;
                foreach (var trail in trails) trail?.Dispose();
                if (originalActorVisual != null) originalActorVisual.SetActive(originalActorVisible);
                ctx.Board.HideAttackWarning(this);
                stage?.Dispose(); ctx.Board.ResetBattleBoxLayout(); ctx.Board.SetMechanicHint(null);
            }
        }
    }
}

