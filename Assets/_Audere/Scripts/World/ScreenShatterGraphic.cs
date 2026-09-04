using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Audere.World
{
    // A frozen frame split into actual moving polygons, not UV offsets inside a fixed mask.
    // Coordinates use screen-height units; UVs always refer to the original frozen image.
    [AddComponentMenu("")]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ScreenShatterGraphic : MaskableGraphic
    {
        private sealed class Shard
        {
            public Vector2[] points;
            public Vector2 center;
            public Vector3 velocity, spin;
            public float delay;
            public Vector2[] edgeTimes;
            public bool[] primaryEdges;
        }

        private sealed class CrackNode
        {
            public Vector2 point;
            public readonly List<int> neighbors = new List<int>();
            public float distance = float.PositiveInfinity;
            public int parent = -1;
        }

        private readonly List<Shard> shards = new List<Shard>();
        private readonly List<Shard> drawOrder = new List<Shard>();
        private Texture2D snapshot;
        private FullscreenShatterSettings settings;
        private float aspect, timeline;
        public override Texture mainTexture => snapshot != null ? snapshot : Texture2D.whiteTexture;
        public int PieceCount => shards.Count;
        public float FlightTime => settings == null ? 0f : Mathf.Max(0f, timeline - settings.BreakTime);

        public void Initialize(Texture2D frozenFrame, FullscreenShatterSettings configuration)
        {
            snapshot = frozenFrame; // Controller owns and releases the texture.
            settings = configuration;
            aspect = (float)snapshot.width / snapshot.height;
            material = settings.ShardMaterial;
            raycastTarget = false;
            maskable = false;
            BuildShards();
            SetAllDirty();
        }

        public void SetTime(float time)
        {
            timeline = time;
            SetVerticesDirty();
        }

        private void BuildShards()
        {
            shards.Clear();
            drawOrder.Clear();
            var random = new System.Random(settings.Seed);
            int count = settings.SpokeCount;
            var impact = new Vector2(settings.Impact.x * aspect, settings.Impact.y);
            var rings = new Vector2[4, count];
            float[] radii = { .12f, .36f, .78f, aspect + 2f };
            for (int i = 0; i < count; i++)
            {
                float angle = (i + Range(random, -.19f, .19f)) * Mathf.PI * 2f / count;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                for (int ring = 0; ring < 4; ring++)
                    rings[ring, i] = impact + direction * radii[ring] * Range(random, .82f, 1.18f);
            }
            for (int i = 0; i < count; i++)
            {
                int next = (i + 1) % count;
                AddShard(impact, rings[0, i], rings[0, next], impact, random);
                for (int ring = 0; ring < 3; ring++)
                {
                    AddShard(rings[ring, i], rings[ring + 1, i], rings[ring + 1, next], impact, random);
                    AddShard(rings[ring, i], rings[ring + 1, next], rings[ring, next], impact, random);
                }
            }
            drawOrder.AddRange(shards);
            BuildCrackRoutes(impact);
            // All pieces travel away from the virtual lens. Stable painter order avoids a
            // second scene camera, physics simulation, and changes to world sorting layers.
            drawOrder.Sort((a, b) => b.velocity.z.CompareTo(a.velocity.z));
        }

        private void BuildCrackRoutes(Vector2 impact)
        {
            var nodes = new List<CrackNode>();
            var lookup = new Dictionary<Vector2Int, int>();
            var ids = new Dictionary<Shard, int[]>();
            foreach (Shard shard in shards)
            {
                int[] indices = new int[shard.points.Length];
                for (int i = 0; i < indices.Length; i++)
                {
                    Vector2 p = shard.points[i];
                    var key = new Vector2Int(Mathf.RoundToInt(p.x * 100000), Mathf.RoundToInt(p.y * 100000));
                    if (!lookup.TryGetValue(key, out int id))
                    {
                        id = nodes.Count;
                        lookup.Add(key, id);
                        nodes.Add(new CrackNode { point = p });
                    }
                    indices[i] = id;
                }
                ids.Add(shard, indices);
                for (int i = 0; i < indices.Length; i++)
                {
                    int a = indices[i], b = indices[(i + 1) % indices.Length];
                    if (!nodes[a].neighbors.Contains(b)) nodes[a].neighbors.Add(b);
                    if (!nodes[b].neighbors.Contains(a)) nodes[b].neighbors.Add(a);
                }
            }
            int center = ClosestNode(nodes, impact);
            nodes[center].distance = 0f;
            var visited = new bool[nodes.Count];
            // Shortest paths follow the actual glass seams, so growing cracks never jump
            // across faces or become unrelated to the later fragment boundaries.
            for (int pass = 0; pass < nodes.Count; pass++)
            {
                int current = -1;
                for (int i = 0; i < nodes.Count; i++)
                    if (!visited[i] && (current < 0 || nodes[i].distance < nodes[current].distance)) current = i;
                if (current < 0) break;
                visited[current] = true;
                foreach (int neighbor in nodes[current].neighbors)
                {
                    float cost = Vector2.Distance(nodes[current].point, nodes[neighbor].point);
                    if (OnBorder(nodes[current].point, nodes[neighbor].point)) cost *= 8f;
                    float distance = nodes[current].distance + cost;
                    if (distance < nodes[neighbor].distance)
                    {
                        nodes[neighbor].distance = distance;
                        nodes[neighbor].parent = current;
                    }
                }
            }
            var primary = new Dictionary<Vector2Int, Vector2>();
            Vector2[] entries = { new Vector2(aspect * .45f, 1f), new Vector2(0f, .62f), new Vector2(aspect, .43f) };
            for (int entry = 0; entry < entries.Length; entry++)
            {
                int current = ClosestNode(nodes, entries[entry], true);
                float length = nodes[current].distance;
                float delay = entry * .085f;
                for (int guard = 0; guard < nodes.Count && nodes[current].parent >= 0; guard++)
                {
                    int next = nodes[current].parent;
                    float start = Mathf.Lerp(delay, .48f, 1f - nodes[current].distance / length);
                    float end = Mathf.Lerp(delay, .48f, 1f - nodes[next].distance / length);
                    var key = EdgeKey(current, next);
                    var times = current < next ? new Vector2(start, end) : new Vector2(end, start);
                    if (!primary.ContainsKey(key)) primary.Add(key, times);
                    current = next;
                }
            }
            float farthest = 0f;
            foreach (CrackNode node in nodes) farthest = Mathf.Max(farthest, Vector2.Distance(node.point, impact));
            foreach (Shard shard in shards)
            {
                int[] indices = ids[shard];
                shard.edgeTimes = new Vector2[indices.Length];
                shard.primaryEdges = new bool[indices.Length];
                for (int i = 0; i < indices.Length; i++)
                {
                    int a = indices[i], b = indices[(i + 1) % indices.Length];
                    if (primary.TryGetValue(EdgeKey(a, b), out Vector2 times))
                    {
                        shard.edgeTimes[i] = a < b ? times : new Vector2(times.y, times.x);
                        shard.primaryEdges[i] = true;
                    }
                    else
                    {
                        float da = Vector2.Distance(nodes[a].point, impact), db = Vector2.Distance(nodes[b].point, impact);
                        float start = .52f + .34f * Mathf.Min(da, db) / farthest;
                        float end = Mathf.Min(1f, start + .055f + .16f * Vector2.Distance(nodes[a].point, nodes[b].point) / farthest);
                        shard.edgeTimes[i] = da <= db ? new Vector2(start, end) : new Vector2(end, start);
                    }
                }
            }
        }

        private bool OnBorder(Vector2 a, Vector2 b) =>
            (Mathf.Abs(a.x) < .0001f && Mathf.Abs(b.x) < .0001f) ||
            (Mathf.Abs(a.x - aspect) < .0001f && Mathf.Abs(b.x - aspect) < .0001f) ||
            (Mathf.Abs(a.y) < .0001f && Mathf.Abs(b.y) < .0001f) ||
            (Mathf.Abs(a.y - 1f) < .0001f && Mathf.Abs(b.y - 1f) < .0001f);

        private static Vector2Int EdgeKey(int a, int b) => new Vector2Int(Mathf.Min(a, b), Mathf.Max(a, b));

        private static int ClosestNode(List<CrackNode> nodes, Vector2 point, bool sameBoundary = false)
        {
            int best = -1;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (sameBoundary && !(Mathf.Abs(nodes[i].point.x - point.x) < .0001f || Mathf.Abs(nodes[i].point.y - point.y) < .0001f)) continue;
                if (best < 0 || (nodes[i].point - point).sqrMagnitude < (nodes[best].point - point).sqrMagnitude) best = i;
            }
            return Mathf.Max(0, best);
        }

        private void AddShard(Vector2 a, Vector2 b, Vector2 c, Vector2 impact, System.Random random)
        {
            var polygon = new List<Vector2> { a, b, c };
            polygon = Clip(polygon, 0, 0f, true);
            polygon = Clip(polygon, 0, aspect, false);
            polygon = Clip(polygon, 1, 0f, true);
            polygon = Clip(polygon, 1, 1f, false);
            if (polygon.Count < 3) return;
            float area = 0f;
            Vector2 center = Vector2.zero;
            for (int i = 0; i < polygon.Count; i++)
            {
                Vector2 p = polygon[i], q = polygon[(i + 1) % polygon.Count];
                area += p.x * q.y - q.x * p.y;
                center += p;
            }
            if (Mathf.Abs(area) < .00001f) return;
            center /= polygon.Count;
            Vector2 outward = (center - impact).normalized;
            shards.Add(new Shard
            {
                points = polygon.ToArray(), center = center,
                velocity = new Vector3(outward.x * Range(random, .32f, .95f),
                    .45f + outward.y * .3f + Range(random, 0f, .4f), Range(random, .15f, .7f)) * settings.Impulse,
                spin = new Vector3(Range(random, -145f, 145f), Range(random, -175f, 175f), Range(random, -85f, 85f)),
                delay = Vector2.Distance(center, impact) * .06f,
            });
        }

        private static List<Vector2> Clip(List<Vector2> input, int axis, float boundary, bool keepGreater)
        {
            var output = new List<Vector2>();
            if (input.Count == 0) return output;
            Vector2 previous = input[input.Count - 1];
            bool previousInside = keepGreater ? previous[axis] >= boundary : previous[axis] <= boundary;
            foreach (Vector2 current in input)
            {
                bool inside = keepGreater ? current[axis] >= boundary : current[axis] <= boundary;
                if (inside != previousInside)
                    output.Add(Vector2.LerpUnclamped(previous, current, (boundary - previous[axis]) / (current[axis] - previous[axis])));
                if (inside) output.Add(current);
                previous = current;
                previousInside = inside;
            }
            return output;
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (snapshot == null || settings == null || timeline >= settings.ClearTime) return;
            float crack = settings.Crack.Evaluate(timeline);
            foreach (Shard shard in drawOrder)
            {
                float flight = Mathf.Max(0f, FlightTime - shard.delay);
                Quaternion rotation = Quaternion.Euler(shard.spin * flight * settings.Spin);
                Vector3 displacement = shard.velocity * flight + Vector3.down * (.5f * settings.Gravity * flight * flight);
                float front = (rotation * Vector3.forward).z;
                Color tint = front >= 0f ? Color.Lerp(Color.white, settings.FrontTint, crack) * (.46f + .54f * front) : settings.BackTint;
                tint.a = 1f;
                int start = mesh.currentVertCount;
                foreach (Vector2 point in shard.points)
                    mesh.AddVert(Project(point, 0f, shard, rotation, displacement), tint,
                        new Vector4(point.x / aspect, point.y, 0f, 0f));
                for (int i = 1; i < shard.points.Length - 1; i++) mesh.AddTriangle(start, start + i, start + i + 1);

                for (int i = 0; i < shard.points.Length; i++)
                {
                    Vector2 a = shard.points[i], b = shard.points[(i + 1) % shard.points.Length];
                    Vector3 pa = Project(a, 0f, shard, rotation, displacement);
                    Vector3 pb = Project(b, 0f, shard, rotation, displacement);
                    if (flight > 0f)
                    {
                        Vector3 backA = Project(a, settings.Thickness, shard, rotation, displacement);
                        Vector3 backB = Project(b, settings.Thickness, shard, rotation, displacement);
                        Vector3 edgeDirection = rotation * new Vector3(b.x - a.x, b.y - a.y, 0f).normalized;
                        Color side = settings.EdgeTint * (.5f + .5f * Mathf.Abs(edgeDirection.y));
                        side.a = 1f;
                        AddQuad(mesh, pa, pb, backB, backA, side, side);
                    }
                    if (flight == 0f && OnBorder(a, b)) continue;
                    Vector2 reveal = shard.edgeTimes[i];
                    if (crack <= Mathf.Min(reveal.x, reveal.y)) continue;
                    // Clip the visible line at its advancing tip instead of fading a whole
                    // screen-spanning segment in at once. Profile plateaus make it stutter.
                    if (crack < Mathf.Max(reveal.x, reveal.y))
                    {
                        float tip = Mathf.InverseLerp(reveal.x, reveal.y, crack);
                        if (reveal.x < reveal.y) pb = Vector3.Lerp(pa, pb, tip);
                        else pa = Vector3.Lerp(pa, pb, tip);
                    }
                    Vector3 normal = new Vector3(-(pb - pa).y, (pb - pa).x).normalized * rectTransform.rect.height * (shard.primaryEdges[i] ? .00065f : .00038f);
                    Color rim = settings.CrackTint;
                    rim.a *= shard.primaryEdges[i] ? .72f : .4f;
                    if (flight > 0f) rim.a *= .7f;
                    Color shadow = new Color(.002f, .003f, .008f, .55f);
                    AddQuad(mesh, pa - normal * 2f, pb - normal * 2f, pb + normal * 2f, pa + normal * 2f, shadow, shadow);
                    AddQuad(mesh, pa, pb, pb + normal, pa + normal, rim, rim);
                }
            }
        }

        private Vector3 Project(Vector2 point, float depth, Shard shard, Quaternion rotation, Vector3 displacement)
        {
            Vector3 local = new Vector3(point.x - shard.center.x, point.y - shard.center.y, depth);
            Vector3 p = rotation * local + new Vector3(shard.center.x - aspect * .5f, shard.center.y - .5f) + displacement;
            float perspective = 3f / Mathf.Max(.5f, 3f + p.z);
            Rect rect = rectTransform.rect;
            return new Vector3(rect.center.x + p.x * perspective * rect.width / aspect,
                rect.center.y + p.y * perspective * rect.height, 0f);
        }

        private static void AddQuad(VertexHelper mesh, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color ca, Color cb)
        {
            int start = mesh.currentVertCount;
            var solid = new Vector4(0f, 0f, 1f, 0f);
            mesh.AddVert(a, ca, solid); mesh.AddVert(b, cb, solid);
            mesh.AddVert(c, cb, solid); mesh.AddVert(d, ca, solid);
            mesh.AddTriangle(start, start + 1, start + 2); mesh.AddTriangle(start, start + 2, start + 3);
        }

        private static float Range(System.Random random, float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());
    }
}
