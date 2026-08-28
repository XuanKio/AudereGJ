using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Audere.Combat
{
    // Runtime-owned ambient layer; it never participates in raycast or combat input.
    [DisallowMultipleComponent]
    public sealed class CombatAnxietyTextFieldView : MonoBehaviour
    {
        private sealed class LabelState
        {
            public RectTransform Rect;
            public TMP_Text Text;
            public float BaseAlpha;
            public float BaseScale;
            public float BaseRotation;
            public float RotationWobble;
            public float DriftAmplitude;
            public float Phase;
            public float SpiralAngle;
            public float SpiralRadius;
            public float AngularSpeed;
            public float InwardSpeed;
        }

        [SerializeField] private RectTransform content;
        [SerializeField] private TMP_FontAsset font;
        [Header("Density")]
        [SerializeField, Range(128, 384)] private int labelCount = 384;
        [SerializeField, Range(12, 30)] private int simulationFramesPerSecond = 12;
        [Header("Spiral")]
        [SerializeField, Min(12f)] private float innerSpiralRadius = 38f;
        [SerializeField, Min(.25f)] private float minimumSpiralDuration = 36f;
        [SerializeField, Min(.25f)] private float maximumSpiralDuration = 52f;
        [SerializeField, Min(.1f)] private float fadeDuration = .65f;
        [SerializeField] private Color textColor = new Color(.86f, .78f, .88f, .28f);

        private readonly List<LabelState> labels = new List<LabelState>();
        private readonly List<string> wordTokens = new List<string>();
        private float fadeElapsed;
        private float simulationAccumulator;
        private System.Random tokenRandom;
        private Transform spiralTarget;
        private bool visible;

        public bool IsVisible => visible;

        public void SetSpiralTarget(Transform target)
        {
            spiralTarget = target;
        }

        public void Show(IReadOnlyList<string> lines, int sessionVersion)
        {
            if (content == null || lines == null || lines.Count == 0)
                return;
            EnsureLabels();
            tokenRandom = new System.Random(sessionVersion * 397 ^ 0x51A9);
            wordTokens.Clear();
            wordTokens.AddRange(CollectWords(lines));
            Rect bounds = content.rect;
            Vector2 center = ResolveSpiralCenter();
            float maximumRadius = ResolveMaximumSpiralRadius(center, bounds);
            for (int i = 0; i < labels.Count; i++)
                InitialiseLabel(labels[i], i, center, maximumRadius, bounds, true);
            fadeElapsed = 0f;
            simulationAccumulator = 0f;
            visible = true;
            content.gameObject.SetActive(true);
        }

        public void Tick(float deltaTime)
        {
            if (!visible || content == null)
                return;
            fadeElapsed += Mathf.Max(0f, deltaTime);
            float alpha = Mathf.Clamp01(fadeElapsed / Mathf.Max(.1f, fadeDuration));
            simulationAccumulator += Mathf.Max(0f, deltaTime);
            float simulationInterval = 1f / Mathf.Max(15, simulationFramesPerSecond);
            if (simulationAccumulator < simulationInterval)
                return;
            // Ambient text does not need a 60 Hz simulation; this keeps the layer dense
            // without spending a full UI update on every combat frame.
            float simulationDeltaTime = Mathf.Min(simulationAccumulator, .1f);
            simulationAccumulator = 0f;
            Rect bounds = content.rect;
            Vector2 center = ResolveSpiralCenter();
            float maximumRadius = ResolveMaximumSpiralRadius(center, bounds);
            for (int i = 0; i < labels.Count; i++)
            {
                LabelState state = labels[i];
                state.SpiralAngle += state.AngularSpeed * simulationDeltaTime;
                state.SpiralRadius -= state.InwardSpeed * simulationDeltaTime;
                if (state.SpiralRadius <= innerSpiralRadius)
                {
                    InitialiseLabel(state, i, center, maximumRadius, bounds, false);
                    continue;
                }

                state.Phase += simulationDeltaTime * (1f + Mathf.Abs(state.AngularSpeed) * .25f);
                Vector2 radial = new Vector2(Mathf.Cos(state.SpiralAngle), Mathf.Sin(state.SpiralAngle));
                Vector2 tangent = new Vector2(-radial.y, radial.x);
                float drift = Mathf.Sin(state.Phase) * state.DriftAmplitude;
                float pulse = Mathf.Sin(state.Phase * .63f + i * .19f) * state.DriftAmplitude * .3f;
                state.Rect.anchoredPosition = center + radial * (state.SpiralRadius + pulse) + tangent * drift;
                float radiusT = Mathf.Clamp01(state.SpiralRadius / Mathf.Max(innerSpiralRadius + 1f, maximumRadius));
                float bend = Mathf.Sin(state.Phase + i * .37f);
                state.Rect.localRotation = Quaternion.Euler(
                    0f, 0f, state.BaseRotation + bend * state.RotationWobble);
                float scale = state.BaseScale * Mathf.Lerp(1.08f, .78f, 1f - radiusT) + bend * .035f;
                state.Rect.localScale = Vector3.one * scale;
                Color color = state.Text.color;
                // Keep the center legible as labels spiral inward. Previously the center
                // dropped to 18% opacity and made the supposedly dense field look hollow.
                color.a = state.BaseAlpha * alpha * Mathf.Lerp(.68f, 1f, radiusT);
                state.Text.color = color;
            }
        }

        public void ForceHide()
        {
            visible = false;
            fadeElapsed = 0f;
            for (int i = 0; i < labels.Count; i++)
                if (labels[i]?.Text != null) labels[i].Text.gameObject.SetActive(false);
            if (content != null && content.gameObject.activeSelf)
                content.gameObject.SetActive(false);
        }

        private void EnsureLabels()
        {
            while (labels.Count < labelCount)
            {
                GameObject labelObject = new GameObject(
                    $"Anxiety Text {labels.Count + 1:00}",
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                RectTransform rect = labelObject.GetComponent<RectTransform>();
                rect.SetParent(content, false);
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
                rect.sizeDelta = new Vector2(720f, 94f);
                TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
                ConfigureText(label);
                labels.Add(new LabelState { Rect = rect, Text = label });
            }
        }

        private void InitialiseLabel(
            LabelState state,
            int labelIndex,
            Vector2 center,
            float maximumRadius,
            Rect bounds,
            bool initialSpawn)
        {
            state.Text.text = ChooseWord(tokenRandom);
            float fontSize = Mathf.Lerp(22f, 42f, (float)tokenRandom.NextDouble());
            if (tokenRandom.NextDouble() < .12d)
                fontSize *= Mathf.Lerp(1.25f, 1.9f, (float)tokenRandom.NextDouble());
            state.Text.fontSize = fontSize;
            float alphaMultiplier = Mathf.Lerp(.72f, 1.42f, (float)tokenRandom.NextDouble());
            if (tokenRandom.NextDouble() < .09d)
                alphaMultiplier *= Mathf.Lerp(1.45f, 2f, (float)tokenRandom.NextDouble());
            state.BaseAlpha = Mathf.Clamp(textColor.a * alphaMultiplier, .1f, .62f);
            state.BaseScale = Mathf.Lerp(.86f, 1.18f, (float)tokenRandom.NextDouble());
            double orientation = tokenRandom.NextDouble();
            state.BaseRotation = orientation < .7d ? 0f : orientation < .85d ? 90f : -90f;
            state.RotationWobble = 0f;
            state.DriftAmplitude = Mathf.Lerp(2f, 12f, (float)tokenRandom.NextDouble());
            state.Text.color = new Color(textColor.r, textColor.g, textColor.b, 0f);
            Vector2 position = initialSpawn
                ? DensePointInBounds(labelIndex, labels.Count, bounds)
                : RandomPointOnBounds(bounds);
            Vector2 offset = position - center;
            state.SpiralAngle = Mathf.Atan2(offset.y, offset.x);
            state.SpiralRadius = Mathf.Max(innerSpiralRadius + 1f, offset.magnitude);
            if (offset.sqrMagnitude < (innerSpiralRadius + 1f) * (innerSpiralRadius + 1f))
                position = center + new Vector2(Mathf.Cos(state.SpiralAngle), Mathf.Sin(state.SpiralAngle)) * state.SpiralRadius;
            float duration = Mathf.Lerp(minimumSpiralDuration, maximumSpiralDuration, (float)tokenRandom.NextDouble());
            state.InwardSpeed = maximumRadius / Mathf.Max(.25f, duration);
            float direction = tokenRandom.NextDouble() < .5d ? -1f : 1f;
            state.AngularSpeed = direction * Mathf.Lerp(.035f, .11f, (float)tokenRandom.NextDouble());
            state.Phase = Mathf.Lerp(0f, Mathf.PI * 2f, (float)tokenRandom.NextDouble());
            state.Rect.sizeDelta = new Vector2(300f, 84f);
            state.Rect.anchoredPosition = position;
            state.Rect.localRotation = Quaternion.Euler(0f, 0f, state.BaseRotation);
            state.Rect.localScale = Vector3.one * state.BaseScale;
            state.Text.gameObject.SetActive(true);
        }

        private Vector2 ResolveSpiralCenter()
        {
            if (spiralTarget == null || content == null)
                return content != null ? content.rect.center : Vector2.zero;
            Vector3 local = content.InverseTransformPoint(spiralTarget.position);
            return new Vector2(local.x, local.y);
        }

        private static float ResolveMaximumSpiralRadius(Vector2 center, Rect bounds)
        {
            float topLeft = Vector2.Distance(center, new Vector2(bounds.xMin, bounds.yMax));
            float topRight = Vector2.Distance(center, new Vector2(bounds.xMax, bounds.yMax));
            float bottomLeft = Vector2.Distance(center, new Vector2(bounds.xMin, bounds.yMin));
            float bottomRight = Vector2.Distance(center, new Vector2(bounds.xMax, bounds.yMin));
            return Mathf.Max(topLeft, topRight, bottomLeft, bottomRight) + 180f;
        }

        private Vector2 DensePointInBounds(int index, int count, Rect bounds)
        {
            float aspect = bounds.width / Mathf.Max(1f, bounds.height);
            int columns = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(count * aspect)));
            int rows = Mathf.Max(1, Mathf.CeilToInt(count / (float)columns));
            int column = index % columns;
            int row = index / columns;
            float cellWidth = bounds.width / columns;
            float cellHeight = bounds.height / rows;
            float jitterX = Mathf.Lerp(-.43f, .43f, (float)tokenRandom.NextDouble()) * cellWidth;
            float jitterY = Mathf.Lerp(-.43f, .43f, (float)tokenRandom.NextDouble()) * cellHeight;
            return new Vector2(
                bounds.xMin + (column + .5f) * cellWidth + jitterX,
                bounds.yMin + (row + .5f) * cellHeight + jitterY);
        }

        private Vector2 RandomPointOnBounds(Rect bounds)
        {
            float t = (float)tokenRandom.NextDouble();
            switch (tokenRandom.Next(4))
            {
                case 0: return new Vector2(Mathf.Lerp(bounds.xMin, bounds.xMax, t), bounds.yMax);
                case 1: return new Vector2(Mathf.Lerp(bounds.xMin, bounds.xMax, t), bounds.yMin);
                case 2: return new Vector2(bounds.xMin, Mathf.Lerp(bounds.yMin, bounds.yMax, t));
                default: return new Vector2(bounds.xMax, Mathf.Lerp(bounds.yMin, bounds.yMax, t));
            }
        }

        private string ChooseWord(System.Random random)
        {
            return wordTokens.Count > 0 ? wordTokens[random.Next(wordTokens.Count)] : "…";
        }

        private static List<string> CollectWords(IReadOnlyList<string> lines)
        {
            var words = new List<string>();
            for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
            {
                string line = lines[lineIndex];
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] pieces = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                for (int pieceIndex = 0; pieceIndex < pieces.Length; pieceIndex++)
                {
                    string word = TrimNonWordEdges(pieces[pieceIndex]);
                    if (!string.IsNullOrEmpty(word)) words.Add(word);
                }
            }
            return words;
        }

        private static string TrimNonWordEdges(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            int start = 0;
            int end = value.Length - 1;
            while (start <= end && !char.IsLetterOrDigit(value[start])) start++;
            while (end >= start && !char.IsLetterOrDigit(value[end])) end--;
            return start <= end ? value.Substring(start, end - start + 1) : string.Empty;
        }

        private void ConfigureText(TextMeshProUGUI label)
        {
            label.font = font;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
        }

        private void OnDisable()
        {
            ForceHide();
        }
    }
}
