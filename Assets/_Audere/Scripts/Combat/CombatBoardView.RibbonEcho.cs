using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Audere.Combat
{
    public sealed partial class CombatBoardView
    {
        [SerializeField] private Material ribbonEchoMaterial;
        private sealed class RibbonEcho { public Image Image; public float Age; public Color Color; public bool Active; }
        private readonly List<RibbonEcho> ribbonEchoes = new List<RibbonEcho>();
        public int ActiveRibbonEchoCount { get { int n = 0; foreach (var e in ribbonEchoes) if (e.Active) n++; return n; } }

        public void EmitRibbonEcho(RectTransform source, float hue)
        {
            Color tint = Color.HSVToRGB(Mathf.Repeat(hue, 1f), .65f, 1f);
            tint.a = .32f;
            EmitRibbonEcho(source, tint);
        }

        public void EmitRibbonEcho(RectTransform source, Color tint)
        {
            if (source == null || exteriorProjectileRoot == null) return;
            var sourceImage = source.GetComponentInChildren<Image>();
            if (sourceImage == null) return;
            RibbonEcho echo = null;
            foreach (var candidate in ribbonEchoes) if (!candidate.Active) { echo = candidate; break; }
            if (echo == null)
            {
                if (ribbonEchoes.Count >= 192) return;
                var go = new GameObject("Ribbon echo (pooled, harmless)", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(exteriorProjectileRoot, false);
                echo = new RibbonEcho { Image = go.GetComponent<Image>() };
                echo.Image.raycastTarget = false;
                echo.Image.material = ribbonEchoMaterial;
                ribbonEchoes.Add(echo);
            }
            echo.Active = true; echo.Age = 0f;
            echo.Color = tint;
            echo.Image.sprite = sourceImage.sprite; echo.Image.preserveAspect = true;
            var rect = echo.Image.rectTransform;
            rect.sizeDelta = sourceImage.rectTransform.rect.size;
            rect.pivot = sourceImage.rectTransform.pivot;
            rect.SetPositionAndRotation(sourceImage.transform.position, sourceImage.transform.rotation);
            Vector3 scale = sourceImage.transform.lossyScale, parent = rect.parent.lossyScale;
            rect.localScale = new Vector3(scale.x / parent.x, scale.y / parent.y, 1f);
            rect.SetAsFirstSibling();
            echo.Image.color = echo.Color;
            echo.Image.gameObject.SetActive(true);
        }

        private void TickRibbonEchoes(float deltaTime)
        {
            foreach (var echo in ribbonEchoes)
            {
                if (!echo.Active) continue;
                echo.Age += Mathf.Max(0f, deltaTime);
                if (echo.Age >= .28f) { echo.Active = false; echo.Image.gameObject.SetActive(false); continue; }
                Color c = echo.Color; c.a *= Mathf.Pow(1f - echo.Age / .28f, 1.5f); echo.Image.color = c;
            }
        }

        private void ClearRibbonEchoes()
        {
            foreach (var echo in ribbonEchoes) { echo.Active = false; if (echo.Image != null) echo.Image.gameObject.SetActive(false); }
        }

        public void ClearMotionEchoes() => ClearRibbonEchoes();
    }

    internal sealed class RibbonProjectileMotion : ICombatProjectileMotion
    {
        private readonly CombatBoardView board;
        private readonly ParametricProjectileMotion motion;
        private float echoTime, age;
        public RibbonProjectileMotion(CombatBoardView board, float duration, System.Func<float, Vector2> position, float spin)
            : this(board, duration, position, t => t * spin) { }

        public RibbonProjectileMotion(CombatBoardView board, float duration, System.Func<float, Vector2> position,
            System.Func<float, float> rotation)
        {
            this.board = board;
            motion = new ParametricProjectileMotion(duration, position, rotation);
        }
        public bool Tick(RectTransform target, float deltaTime)
        {
            bool alive = motion.Tick(target, deltaTime);
            age += deltaTime; echoTime += deltaTime;
            if (deltaTime > 0f && echoTime >= .055f)
            {
                echoTime %= .055f;
                board.EmitRibbonEcho(target, age * .4f);
            }
            return alive;
        }
        public void Cancel() => motion.Cancel();
    }
}
