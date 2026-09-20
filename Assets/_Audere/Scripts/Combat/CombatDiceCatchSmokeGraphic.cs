using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Audere.Combat
{
    // DiceCatcher catch effect: six 8x8 particles, each drifting one random source
    // pixel per frame, rotating one degree and shrinking by .025 at 60 fps.
    // The shared graphic batches bursts so catching several dice creates no UI objects.
    public sealed class CombatDiceCatchSmokeGraphic : MaskableGraphic
    {
        [SerializeField] private Texture2D sourceParticle;
        [SerializeField] private Color fallbackColor = new Color32(173, 146, 191, 255);
        [SerializeField, Min(.1f)] private float sourcePixelScale = 4f;

        private const int ParticlesPerBurst = 6;
        private const int MaxBursts = 8;
        private const float SourceFramesPerSecond = 60f;
        private const float ShrinkPerFrame = .025f;
        private const float Lifetime = 1f / (SourceFramesPerSecond * ShrinkPerFrame);
        private readonly List<Burst> bursts = new List<Burst>(MaxBursts);
        private int emissionSerial;

        private struct Burst
        {
            public Vector2 Center;
            public float Age;
            public int Seed;
            public Color Tint;
        }

        public int ActiveBurstCount => bursts.Count;
        public override Texture mainTexture => sourceParticle != null ? sourceParticle : base.mainTexture;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        public void Emit(Vector2 localCenter, Color iconColor)
        {
            if (bursts.Count == MaxBursts) bursts.RemoveAt(0);
            Color tint = iconColor.a > .01f ? iconColor : fallbackColor;
            tint.a = 1f;
            bursts.Add(new Burst { Center = localCenter, Age = 0f, Seed = ++emissionSerial, Tint = tint });
            SetVerticesDirty();
        }

        public void Clear()
        {
            if (bursts.Count == 0) return;
            bursts.Clear();
            SetVerticesDirty();
        }

        private void Update()
        {
            if (bursts.Count == 0) return;
            float step = Time.unscaledDeltaTime;
            for (int i = bursts.Count - 1; i >= 0; i--)
            {
                Burst burst = bursts[i];
                burst.Age += step;
                if (burst.Age >= Lifetime) bursts.RemoveAt(i);
                else bursts[i] = burst;
            }
            SetVerticesDirty();
        }

        protected override void OnDisable()
        {
            Clear();
            base.OnDisable();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            for (int b = 0; b < bursts.Count; b++)
            {
                Burst burst = bursts[b];
                float sourceFrames = burst.Age * SourceFramesPerSecond;
                float scale = Mathf.Max(0f, 1f - ShrinkPerFrame * sourceFrames);
                float size = 8f * sourcePixelScale * scale;
                float angle = sourceFrames * Mathf.Deg2Rad;
                for (int p = 0; p < ParticlesPerBurst; p++)
                {
                    Vector2 velocity = new Vector2(
                        Hash(2 * p, burst.Seed) * 2f - 1f,
                        Hash(2 * p + 1, burst.Seed) * 2f - 1f);
                    Vector2 center = burst.Center + velocity * sourceFrames * sourcePixelScale;
                    AddQuad(mesh, center, size, angle, burst.Tint);
                }
            }
        }

        private static float Hash(int particle, int seed)
        {
            return Mathf.Repeat(Mathf.Sin(particle * 127.1f + seed * 311.7f) * 43758.5453f, 1f);
        }

        private static void AddQuad(VertexHelper mesh, Vector2 center, float size, float angle, Color tint)
        {
            float half = size * .5f;
            float sin = Mathf.Sin(angle);
            float cos = Mathf.Cos(angle);
            Vector2 right = new Vector2(cos, sin) * half;
            Vector2 up = new Vector2(-sin, cos) * half;
            int index = mesh.currentVertCount;
            mesh.AddVert(center - right - up, tint, new Vector2(0f, 0f));
            mesh.AddVert(center - right + up, tint, new Vector2(0f, 1f));
            mesh.AddVert(center + right + up, tint, new Vector2(1f, 1f));
            mesh.AddVert(center + right - up, tint, new Vector2(1f, 0f));
            mesh.AddTriangle(index, index + 1, index + 2);
            mesh.AddTriangle(index, index + 2, index + 3);
        }
    }
}
