using System;
using UnityEngine;
using UnityEngine.UI;

namespace Audere.UI
{
    /// <summary>
    /// Lightweight drifting leaf particles for the main-menu Canvas.
    /// Uses a tiny source texture and animates with unscaled time.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MainMenuDustGraphic : MaskableGraphic
    {
        [Header("Leaf")]
        [SerializeField] private Texture2D leafTexture;
        [SerializeField, Min(1)] private int particleCount = 22;
        [SerializeField, Min(1f)] private float minSize = 20f;
        [SerializeField, Min(1f)] private float maxSize = 54f;
        [SerializeField] private Color warmTint = new Color(1f, 0.96f, 0.82f, 0.78f);
        [SerializeField] private Color coolTint = new Color(0.9f, 0.97f, 1f, 0.65f);

        [Header("Motion")]
        [SerializeField, Min(0f)] private float minFallSpeed = 8f;
        [SerializeField, Min(0f)] private float maxFallSpeed = 23f;
        [SerializeField, Min(0f)] private float horizontalDrift = 20f;
        [SerializeField, Min(0f)] private float minRotationSpeed = 10f;
        [SerializeField, Min(0f)] private float maxRotationSpeed = 44f;
        [SerializeField] private int randomSeed = 90210;

        [Header("Canvas Order")]
        [SerializeField, Min(0)] private int siblingIndex = 1;

        private const float EdgeFadeDistance = 100f;

        [Serializable]
        private struct Leaf
        {
            public Vector2 position;
            public float size;
            public float fallSpeed;
            public float phase;
            public float swayFrequency;
            public float swayStrength;
            public float rotation;
            public float rotationSpeed;
            public float opacity;
            public float tintMix;
        }

        [NonSerialized] private Leaf[] leaves;
        [NonSerialized] private Rect cachedRect;
        [NonSerialized] private bool previewBuilt;

        public override Texture mainTexture =>
            leafTexture != null ? leafTexture : Texture2D.whiteTexture;

        protected override void OnEnable()
        {
            base.OnEnable();
            raycastTarget = false;

            if (transform.parent != null)
                transform.SetSiblingIndex(Mathf.Clamp(siblingIndex, 0, transform.parent.childCount - 1));

            RebuildLeaves();
            SetMaterialDirty();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();

            if (!isActiveAndEnabled)
                return;

            Rect current = rectTransform.rect;
            if (!Approximately(current, cachedRect))
                RebuildLeaves();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            particleCount = Mathf.Max(1, particleCount);
            minSize = Mathf.Max(1f, minSize);
            maxSize = Mathf.Max(minSize, maxSize);
            maxFallSpeed = Mathf.Max(minFallSpeed, maxFallSpeed);
            maxRotationSpeed = Mathf.Max(minRotationSpeed, maxRotationSpeed);

            if (isActiveAndEnabled)
            {
                RebuildLeaves();
                SetMaterialDirty();
            }
        }
#endif

        private void Update()
        {
            EnsureLeaves();

            if (!Application.isPlaying)
            {
                if (!previewBuilt)
                {
                    previewBuilt = true;
                    SetVerticesDirty();
                }

                return;
            }

            previewBuilt = false;
            float deltaTime = Time.unscaledDeltaTime;
            float time = Time.unscaledTime;
            Rect area = rectTransform.rect;

            for (int i = 0; i < leaves.Length; i++)
            {
                Leaf leaf = leaves[i];
                float sway = Mathf.Sin(time * leaf.swayFrequency + leaf.phase);
                leaf.position.x += sway * horizontalDrift * leaf.swayStrength * deltaTime;
                leaf.position.y -= leaf.fallSpeed * deltaTime;
                leaf.rotation += leaf.rotationSpeed * deltaTime;

                if (leaf.position.y + leaf.size < area.yMin)
                    RespawnAtTop(ref leaf, area, i);

                if (leaf.position.x < area.xMin - leaf.size)
                    leaf.position.x = area.xMax + leaf.size;
                else if (leaf.position.x > area.xMax + leaf.size)
                    leaf.position.x = area.xMin - leaf.size;

                leaves[i] = leaf;
            }

            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            EnsureLeaves();

            if (leafTexture == null || leaves == null || leaves.Length == 0)
                return;

            float time = Application.isPlaying ? Time.unscaledTime : 1.75f;
            Rect area = rectTransform.rect;

            for (int i = 0; i < leaves.Length; i++)
                AddLeaf(vertexHelper, leaves[i], area, time);
        }

        private void AddLeaf(VertexHelper vertexHelper, Leaf leaf, Rect area, float time)
        {
            float bottomFade = Mathf.Clamp01((leaf.position.y - area.yMin) / EdgeFadeDistance);
            float topFade = Mathf.Clamp01((area.yMax - leaf.position.y) / EdgeFadeDistance);
            float edgeFade = Mathf.SmoothStep(0f, 1f, Mathf.Min(bottomFade, topFade));

            float flip = Mathf.Abs(Mathf.Cos(time * leaf.swayFrequency * 0.72f + leaf.phase));
            float widthScale = Mathf.Lerp(0.28f, 1f, flip);
            float shimmer = Mathf.Lerp(0.62f, 1f, flip);
            float aspect = leafTexture.height > 0
                ? (float)leafTexture.width / leafTexture.height
                : 0.64f;

            float halfHeight = leaf.size * 0.5f;
            float halfWidth = halfHeight * aspect * widthScale;
            float radians = leaf.rotation * Mathf.Deg2Rad;
            Vector2 right = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
            Vector2 up = new Vector2(-right.y, right.x);

            Color leafColor = Color.Lerp(warmTint, coolTint, leaf.tintMix);
            leafColor.a = Mathf.Clamp01(leafColor.a * leaf.opacity * edgeFade * shimmer);

            int start = vertexHelper.currentVertCount;
            vertexHelper.AddVert(leaf.position - right * halfWidth - up * halfHeight, leafColor, new Vector2(0f, 0f));
            vertexHelper.AddVert(leaf.position - right * halfWidth + up * halfHeight, leafColor, new Vector2(0f, 1f));
            vertexHelper.AddVert(leaf.position + right * halfWidth + up * halfHeight, leafColor, new Vector2(1f, 1f));
            vertexHelper.AddVert(leaf.position + right * halfWidth - up * halfHeight, leafColor, new Vector2(1f, 0f));
            vertexHelper.AddTriangle(start, start + 1, start + 2);
            vertexHelper.AddTriangle(start, start + 2, start + 3);
        }

        private void EnsureLeaves()
        {
            Rect current = rectTransform.rect;
            if (leaves == null || leaves.Length != particleCount || !Approximately(current, cachedRect))
                RebuildLeaves();
        }

        private void RebuildLeaves()
        {
            cachedRect = rectTransform.rect;
            leaves = new Leaf[particleCount];
            System.Random random = new System.Random(randomSeed);

            for (int i = 0; i < leaves.Length; i++)
            {
                float sizeBias = Next01(random);
                float direction = Next01(random) < 0.5f ? -1f : 1f;
                leaves[i] = new Leaf
                {
                    position = new Vector2(
                        Mathf.Lerp(cachedRect.xMin, cachedRect.xMax, Next01(random)),
                        Mathf.Lerp(cachedRect.yMin, cachedRect.yMax, Next01(random))),
                    size = Mathf.Lerp(minSize, maxSize, sizeBias * sizeBias),
                    fallSpeed = Mathf.Lerp(minFallSpeed, maxFallSpeed, Next01(random)),
                    phase = Next01(random) * Mathf.PI * 2f,
                    swayFrequency = Mathf.Lerp(0.32f, 0.78f, Next01(random)),
                    swayStrength = Mathf.Lerp(0.45f, 1f, Next01(random)),
                    rotation = Next01(random) * 360f,
                    rotationSpeed = direction * Mathf.Lerp(minRotationSpeed, maxRotationSpeed, Next01(random)),
                    opacity = Mathf.Lerp(0.38f, 0.88f, Next01(random)),
                    tintMix = Next01(random)
                };
            }

            previewBuilt = false;
            SetVerticesDirty();
        }

        private void RespawnAtTop(ref Leaf leaf, Rect area, int index)
        {
            System.Random random = new System.Random(randomSeed + index * 97 + Time.frameCount);
            leaf.position = new Vector2(
                Mathf.Lerp(area.xMin, area.xMax, Next01(random)),
                area.yMax + leaf.size);
            leaf.phase = Next01(random) * Mathf.PI * 2f;
            leaf.rotation = Next01(random) * 360f;
            leaf.opacity = Mathf.Lerp(0.38f, 0.88f, Next01(random));
        }

        private static float Next01(System.Random random)
        {
            return (float)random.NextDouble();
        }

        private static bool Approximately(Rect a, Rect b)
        {
            return Mathf.Approximately(a.x, b.x)
                && Mathf.Approximately(a.y, b.y)
                && Mathf.Approximately(a.width, b.width)
                && Mathf.Approximately(a.height, b.height);
        }
    }
}
