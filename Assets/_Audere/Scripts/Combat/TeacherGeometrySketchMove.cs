using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Audere.Combat
{
    [CreateAssetMenu(menuName = "Audere/Combat/Moves/Geometry Sketch")]
    public sealed class TeacherGeometrySketchMove : CombatMoveDefinition, ICombatExclusiveDiceMove
    {
        [SerializeField] private CombatBulletView chalkPrefab;
        [SerializeField] private Sprite chalkTip;
        [SerializeField] private Material chalkMaterial;
        [SerializeField, Min(.06f)] private float strokeInterval = .13f;
        [SerializeField, Min(.2f)] private float warningDuration = .26f;
        [SerializeField, Min(.5f)] private float flightDuration = 1.7f;
        [SerializeField, Min(.15f)] private float beatDuration = .3f;
        [SerializeField, Min(30f)] private float outsideDistance = 90f;
        [SerializeField, Range(2,32)] private int shapeCount = 20;
        [SerializeField, Min(48f)] private float safeRadius = 66f;
        private const float StrokeWidth = 7f;

        public float LaunchTime(int shapeIndex) => (shapeIndex % 2 == 0 ? 3 : 4) * strokeInterval + warningDuration;
        public float BeatDuration => beatDuration;
        public int ShapeCount => shapeCount;
        public float SafeRadius => safeRadius;

        public override bool Validate(out string error)
        {
            if (!base.Validate(out error)) return false;
            if (chalkPrefab == null || chalkTip == null || chalkMaterial == null || strokeInterval <= 0 ||
                warningDuration < .2f || flightDuration < .5f || beatDuration < .15f || shapeCount < 2 ||
                Duration < (shapeCount-1)*beatDuration + LaunchTime(shapeCount-1) + flightDuration ||
                outsideDistance < 30 || safeRadius < 48f)
            { error = "Geometry needs chalk art, a reachable gap and time for every drawing and flight."; return false; }
            error = null;
            return true;
        }

        public static Vector2[] Vertices(int shape, float side)
        {
            float h = side * Mathf.Sqrt(3f) * .5f;
            return shape % 2 == 0
                ? new[] { new Vector2(0,h*2/3), new Vector2(-side/2,-h/3), new Vector2(side/2,-h/3) }
                : new[] { new Vector2(-side/2,side/2), new Vector2(side/2,side/2),
                    new Vector2(side/2,-side/2), new Vector2(-side/2,-side/2) };
        }

        public Vector2 GetSafeCenter(Rect field, float absoluteMoveAge)
        {
            float age = Mathf.Max(0f, absoluteMoveAge);
            float ramp = Mathf.SmoothStep(0f, 1f, age / 1.2f);
            // Keep the complete pocket inside the field even for a narrower authored layout.
            float horizontal = Mathf.Min(128f, Mathf.Min(field.width * .23f,
                Mathf.Max(0f, field.width * .5f - safeRadius - 8f)));
            float vertical = Mathf.Min(64f, Mathf.Min(field.height * .16f,
                Mathf.Max(0f, field.height * .5f - safeRadius - 8f)));
            return field.center + new Vector2(horizontal * Mathf.Sin(age * 1.1f),
                vertical * Mathf.Sin(age * 2.2f)) * ramp;
        }

        public Vector2 GetFlightOffset(Rect field, int shapeIndex, float absoluteMoveAge)
        {
            Vector2 gap = GetSafeCenter(field, absoluteMoveAge) - field.center;
            // Translating the line's intercept keeps its perpendicular clearance from
            // the moving pocket constant while both launch points stay outside the sides.
            return Vector2.up * (gap.y - FlightSlope(shapeIndex) * gap.x);
        }

        private static float FlightSlope(int shapeIndex)
        {
            bool fromLeft = shapeIndex % 2 == 0;
            float vertical = shapeIndex % 4 == 0 || shapeIndex % 4 == 3 ? 1f : -1f;
            return vertical * (fromLeft ? 1f : -1f) * (.38f + .06f * (shapeIndex % 3));
        }

        // Base lines for the four approaches. GetFlightOffset moves every live outline
        // around the same pocket, with its complete stroke footprint outside that pocket.
        public void GetFlight(Rect field, int shapeIndex, out Vector2 start, out Vector2 end)
        {
            float side = chalkPrefab.GetComponent<RectTransform>().rect.width;
            float outlineRadius = side / Mathf.Sqrt(2f) + StrokeWidth * .5f;
            bool fromLeft = shapeIndex % 2 == 0;
            float vertical = shapeIndex % 4 == 0 || shapeIndex % 4 == 3 ? 1f : -1f;
            float slope = FlightSlope(shapeIndex);
            float clearance = safeRadius + outlineRadius + 8f + (shapeIndex / 4 % 2) * 14f;
            float intercept = vertical * clearance * Mathf.Sqrt(1f + slope*slope);
            float x = (field.width * .5f + outsideDistance) * (fromLeft ? -1f : 1f);
            start = field.center + new Vector2(x, intercept + slope*x);
            end = field.center + new Vector2(-x, intercept - slope*x);
        }

        public override ICombatMoveExecution CreateExecution(CombatMoveExecutionContext context)
        {
            if (!Validate(out string error)) throw new InvalidOperationException(error);
            if (context.Board == null || !context.Board.HasExteriorTrailBindings)
                throw new MissingReferenceException("Geometry requires the exterior projectile root.");
            return new Execution(this, context);
        }

        private sealed class Execution : ICombatMoveExecution
        {
            private sealed class Drawing
            {
                public CombatChalkSketchGraphic Graphic;
                public Image Chalk;
                public Vector2[] Vertices;
                public Vector2 Start, End, Warning;
                public Rect Field;
                public int Index;
                public bool Active;
            }

            private readonly TeacherGeometrySketchMove data;
            private readonly CombatMoveExecutionContext context;
            private readonly List<(CombatBulletView bullet,int lease)> bullets = new List<(CombatBulletView,int)>();
            private readonly List<Drawing> drawings = new List<Drawing>();
            private readonly List<Vector2> warningPoints = new List<Vector2>();
            private float elapsed;
            private int nextShape;
            private bool cancelled;

            public Execution(TeacherGeometrySketchMove data, CombatMoveExecutionContext context)
            { this.data = data; this.context = context; }
            public bool IsComplete => cancelled || elapsed >= data.Duration;

            public void Tick(float dt)
            {
                if (IsComplete || dt <= 0) return;
                if (context.Board == null || !context.Board.isActiveAndEnabled) { Cancel(); return; }
                float frameStart = elapsed;
                elapsed = Mathf.Min(elapsed + dt, data.Duration);
                // Each chalk keeps its own clock; the next never erases or
                // prematurely launches an unfinished neighboring outline.
                while (nextShape < data.shapeCount && elapsed >= nextShape*data.beatDuration)
                    BeginDrawing(nextShape++);

                warningPoints.Clear();
                foreach (var drawing in drawings)
                {
                    if (!drawing.Active) continue;
                    float age = elapsed - drawing.Index*data.beatDuration;
                    if (age >= data.LaunchTime(drawing.Index))
                    {
                        float launchAt = drawing.Index*data.beatDuration+data.LaunchTime(drawing.Index);
                        Launch(drawing, age-data.LaunchTime(drawing.Index), Mathf.Max(0f,launchAt-frameStart));
                        drawing.Active = false;
                        drawing.Graphic.gameObject.SetActive(false);
                        continue;
                    }
                    Vector2 offset = data.GetFlightOffset(drawing.Field, drawing.Index, elapsed);
                    drawing.Graphic.transform.position = context.Board.PlayArea.TransformPoint(drawing.Start + offset);
                    float edges = Mathf.Min(drawing.Vertices.Length, age/data.strokeInterval);
                    drawing.Graphic.Draw(drawing.Vertices, edges, StrokeWidth);
                    int edge = Mathf.Min(drawing.Vertices.Length-1, Mathf.FloorToInt(edges));
                    drawing.Chalk.rectTransform.anchoredPosition = Vector2.Lerp(drawing.Vertices[edge],
                        drawing.Vertices[(edge+1)%drawing.Vertices.Length], Mathf.Clamp01(edges-edge));
                    drawing.Chalk.rectTransform.localRotation = Quaternion.Euler(0,0,32f);
                    drawing.Chalk.gameObject.SetActive(edges < drawing.Vertices.Length);
                    if (edges >= drawing.Vertices.Length)
                    {
                        Vector2 warning = drawing.Warning + offset;
                        warning.y = Mathf.Clamp(warning.y, drawing.Field.yMin + 26f, drawing.Field.yMax - 26f);
                        warningPoints.Add(warning);
                    }
                }
                context.Board.ShowAttackWarnings(this, warningPoints.ToArray(), elapsed);
                if (elapsed >= data.Duration) Cancel();
            }

            private void BeginDrawing(int index)
            {
                Drawing drawing = drawings.Find(item => !item.Active);
                if (drawing == null)
                {
                    drawing = new Drawing();
                    var go = new GameObject("Chalk shape drawing (runtime)", typeof(RectTransform),
                        typeof(CanvasRenderer), typeof(CombatChalkSketchGraphic));
                    go.layer = context.Board.gameObject.layer;
                    go.transform.SetParent(context.Board.ProjectilePresentationRoot, false);
                    drawing.Graphic = go.GetComponent<CombatChalkSketchGraphic>();
                    drawing.Graphic.material = data.chalkMaterial;
                    drawing.Graphic.raycastTarget = false;
                    drawing.Graphic.maskable = false;
                    drawing.Graphic.rectTransform.sizeDelta = new Vector2(160,160);
                    var tip = new GameObject("Drawing chalk tip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    tip.layer = go.layer;
                    tip.transform.SetParent(go.transform, false);
                    drawing.Chalk = tip.GetComponent<Image>();
                    drawing.Chalk.sprite = data.chalkTip;
                    drawing.Chalk.preserveAspect = true;
                    drawing.Chalk.raycastTarget = false;
                    drawing.Chalk.maskable = false;
                    drawing.Chalk.rectTransform.sizeDelta = new Vector2(40,14);
                    drawings.Add(drawing);
                }
                drawing.Active = true;
                drawing.Index = index;
                drawing.Vertices = Vertices(index, data.chalkPrefab.GetComponent<RectTransform>().rect.width);
                Rect field = context.Board.PlayArea.rect;
                drawing.Field = field;
                data.GetFlight(field, index, out drawing.Start, out drawing.End);
                drawing.Graphic.transform.position = context.Board.PlayArea.TransformPoint(
                    drawing.Start + data.GetFlightOffset(field, index, elapsed));
                drawing.Graphic.gameObject.SetActive(true);
                drawing.Graphic.Draw(drawing.Vertices, 0, StrokeWidth);
                float warningX = index%2 == 0 ? field.xMin+26f : field.xMax-26f;
                float t = (warningX-drawing.Start.x)/(drawing.End.x-drawing.Start.x);
                drawing.Warning = Vector2.LerpUnclamped(drawing.Start, drawing.End, t);
            }

            private void Launch(Drawing drawing, float overdue, float firstFrameDelay)
            {
                // A suspended frame must not release a pile of already expired shots.
                if (overdue >= data.flightDuration) return;
                Vector2 travel = drawing.End-drawing.Start;
                int shapeIndex = drawing.Index;
                Rect field = drawing.Field;
                float launchAt = shapeIndex * data.beatDuration + data.LaunchTime(shapeIndex);
                for (int i=0; i<drawing.Vertices.Length; i++)
                {
                    Vector2 a = drawing.Vertices[i], b = drawing.Vertices[(i+1)%drawing.Vertices.Length];
                    Vector2 start = drawing.Start+(a+b)*.5f;
                    float angle = Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg;
                    // Board.TickBullets consumes this same frame after the move. Hold
                    // its pre-launch portion so a long frame advances the flight once.
                    var bullet = context.Board.SpawnExteriorEnemyBullet(data.chalkPrefab,
                        start + data.GetFlightOffset(field, shapeIndex, launchAt),
                        context.SessionVersion, context.PhaseVersion, firstFrameDelay);
                    if (bullet == null) continue;
                    bullet.ConfigurePathMotion(new ParametricProjectileMotion(data.flightDuration,
                        t => start + travel * t + data.GetFlightOffset(field, shapeIndex,
                            launchAt + t * data.flightDuration), t => angle));
                    bullets.Add((bullet,bullet.PoolLeaseVersion));
                }
            }

            public void Cancel()
            {
                if (cancelled) return;
                cancelled = true;
                if (context.Board != null)
                {
                    context.Board.HideAttackWarning(this);
                    foreach (var bullet in bullets) context.Board.ReturnEnemyBullet(bullet.bullet,bullet.lease);
                }
                bullets.Clear();
                foreach (var drawing in drawings)
                {
                    if (drawing.Graphic == null) continue;
                    drawing.Graphic.gameObject.SetActive(false);
                    if (Application.isPlaying) UnityEngine.Object.Destroy(drawing.Graphic.gameObject);
                    else UnityEngine.Object.DestroyImmediate(drawing.Graphic.gameObject);
                }
                drawings.Clear();
            }
        }
    }
}
