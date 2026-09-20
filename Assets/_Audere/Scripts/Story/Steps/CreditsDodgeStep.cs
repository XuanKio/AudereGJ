using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Audere.Audio;
using Audere.Dialogue;
using Audere.GameplayInput;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Audere.Story.Steps
{
    /// <summary>A full-screen credit dodge sequence with the combat Heart Visual.</summary>
    public sealed class CreditsDodgeStep : StoryStep
    {
        [SerializeField] private RectTransform creditsCanvas;
        [SerializeField] private TextMeshProUGUI creditsText;
        [SerializeField] private Image heartVisual;
        [SerializeField] private Sprite heartSprite;
        [SerializeField, Min(1f)] private float duration = 30f;
        [SerializeField, Range(1f, 1.5f)] private float backgroundMusicBoost = 1.3f;

        private sealed class LetterProjectile
        {
            public RectTransform Rect;
            public Vector2 Velocity;
            public Vector2 HitHalfSize;
            public float Speed;
            public float HomingStrength;
            public float Age;
            public int SplitStage;
            public float SplitAfter;
            public string Text;
        }

        private readonly List<LetterProjectile> projectiles = new List<LetterProjectile>();
        private readonly List<string> creditLines = new List<string>();
        private RectTransform arena;
        private RectTransform heart;
        private Image heartImage;
        private Color authoredTextColor;
        private GameplayInputGate gate;
        private GameplayInputToken inputToken;
        private float hitCooldown;
        private int waveIndex;
        private bool ownsPresentation;

        public float Duration => duration;
        public RectTransform CreditsCanvas => creditsCanvas;
        public TextMeshProUGUI CreditsText => creditsText;

        protected override IEnumerator Execute()
        {
            if (creditsCanvas == null || creditsText == null ||
                !creditsCanvas.gameObject.activeInHierarchy)
            {
                Debug.LogError("[CreditsDodgeStep] Assign an active credits canvas and text.", this);
                FailStep();
                yield break;
            }

            GameplayUIRoot ui = GameplayUIRoot.Instance;
            gate = ui != null ? ui.InputGate : null;
            // The credits are a self-contained mouse dodge scene. A hidden or missing
            // gameplay UI must not prevent the Heart and hazards from appearing.
            if (gate != null && gate.isActiveAndEnabled)
                inputToken = gate.PushMode(this, GameplayInputMode.CreditsDodge);

            authoredTextColor = creditsText.color;
            ownsPresentation = true;
            PrepareWords();
            BuildArena();
            Canvas.ForceUpdateCanvases();
            AudioService.Instance?.SetMusicBoost(this, backgroundMusicBoost);

            float elapsed = 0f;
            float nextWave = .12f;
            try
            {
                while (elapsed < duration)
                {
                    float delta = Time.unscaledDeltaTime;
                    elapsed += delta;
                    creditsText.color = new Color(authoredTextColor.r, authoredTextColor.g,
                        authoredTextColor.b, authoredTextColor.a * (1f - Mathf.Clamp01(elapsed / .45f)));

                    MoveHeart();
                    hitCooldown = Mathf.Max(0f, hitCooldown - delta);
                    heartImage.color = hitCooldown > 0f && Mathf.FloorToInt(elapsed * 16f) % 2 == 0
                        ? new Color(1f, .34f, .42f, 1f) : Color.white;

                    while (elapsed >= nextWave)
                    {
                        SpawnWave(elapsed);
                        nextWave += elapsed < 10f ? .85f : elapsed < 20f ? .68f : .5f;
                    }
                    MoveProjectiles(delta);
                    yield return null;
                }
            }
            finally
            {
                Cleanup(elapsed < duration);
            }

            CompleteStep();
        }

        protected override void OnCancelled() => Cleanup(true);

        private void OnDestroy() => Cleanup(true);

        private void PrepareWords()
        {
            creditLines.Clear();
            string plain = Regex.Replace(creditsText.text, "<[^>]+>", string.Empty);
            foreach (string raw in Regex.Split(plain, @"\r?\n"))
            {
                string line = raw.Trim();
                if (line.Length >= 5)
                    creditLines.Add(line);
            }
            if (creditLines.Count == 0)
                creditLines.Add("AUDERE");
            // Start with a multi-word line so the first split is visible immediately.
            waveIndex = creditLines.Count > 1 ? 1 : 0;
            hitCooldown = 0f;
        }

        private void BuildArena()
        {
            GameObject field = new GameObject("Credit Dodge Field", typeof(RectTransform));
            arena = (RectTransform)field.transform;
            arena.SetParent(creditsCanvas, false);
            arena.anchorMin = Vector2.zero;
            arena.anchorMax = Vector2.one;
            arena.pivot = new Vector2(.5f, .5f);
            arena.offsetMin = arena.offsetMax = Vector2.zero;
            arena.SetAsLastSibling();

            if (heartVisual != null)
            {
                GameObject clone = Instantiate(heartVisual.gameObject, arena);
                clone.name = "Heart Visual";
                clone.SetActive(true);
                heartImage = clone.GetComponent<Image>();
            }
            if (heartImage == null)
                heartImage = MakeImage("Heart Visual", arena, Color.white);
            if (heartImage.sprite == null)
                heartImage.sprite = heartSprite;
            heartImage.color = Color.white;
            heartImage.raycastTarget = false;
            heartImage.preserveAspect = true;
            heart = heartImage.rectTransform;
            heart.anchorMin = heart.anchorMax = new Vector2(.5f, .5f);
            heart.localScale = Vector3.one;
            heart.sizeDelta = new Vector2(46f, 46f);
            heart.anchoredPosition = Vector2.zero;
        }

        private static Image MakeImage(string name, Transform parent, Color color)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.SetParent(parent, false);
            Image image = gameObject.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private void MoveHeart()
        {
            if (heart == null || (inputToken.IsValid && gate != null &&
                (!gate.IsActive(inputToken) || !gate.Allows(GameplayInputMode.CreditsDodge))))
                return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                arena, Input.mousePosition, null, out Vector2 next))
                return;
            Vector2 half = arena.rect.size * .5f - new Vector2(22f, 22f);
            heart.anchoredPosition = new Vector2(
                Mathf.Clamp(next.x, -half.x, half.x),
                Mathf.Clamp(next.y, -half.y, half.y));
        }

        private void SpawnWave(float elapsed)
        {
            Vector2 half = arena.rect.size * .5f;
            string line = creditLines[waveIndex % creditLines.Count];
            Vector2 start;
            switch (waveIndex % 4)
            {
                case 0: start = new Vector2(-half.x * .72f, half.y * .55f); break;
                case 1: start = new Vector2(half.x * .72f, half.y * .3f); break;
                case 2: start = new Vector2(Mathf.Sin(waveIndex * 2.399f) * half.x * .6f,
                    half.y * .72f); break;
                default: start = new Vector2(-half.x * .6f, -half.y * .45f); break;
            }
            float width = Mathf.Min(arena.rect.width * .72f,
                Mathf.Max(220f, line.Length * 18f));
            Spawn(line, start, Aim(start, elapsed < 10f ? 265f : 335f),
                31f, width, 165f, 0, elapsed < 8f ? .85f : .55f);
            heart.SetAsLastSibling();
            waveIndex++;
        }

        private Vector2 Aim(Vector2 start, float speed)
        {
            Vector2 direction = heart.anchoredPosition - start;
            return (direction.sqrMagnitude > .001f ? direction.normalized : Vector2.down) * speed;
        }

        private void Spawn(string text, Vector2 position, Vector2 velocity,
            float fontSize, float width, float homingStrength,
            int splitStage, float splitAfter)
        {
            GameObject gameObject = new GameObject(splitStage == 0 ? "Credit Line" :
                splitStage == 1 ? "Credit Word" : "Credit Letter", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            RectTransform rect = (RectTransform)gameObject.transform;
            rect.SetParent(arena, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(width, 60f);
            rect.anchoredPosition = position;

            TextMeshProUGUI label = gameObject.GetComponent<TextMeshProUGUI>();
            label.font = creditsText.font;
            label.fontSize = fontSize;
            label.text = text;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(.96f, .88f, 1f, 1f);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            projectiles.Add(new LetterProjectile
            {
                Rect = rect,
                Velocity = velocity,
                HitHalfSize = new Vector2(width * .42f, 19f),
                Speed = velocity.magnitude,
                HomingStrength = homingStrength,
                SplitStage = splitStage,
                SplitAfter = splitAfter,
                Text = text,
            });
        }

        private void Split(LetterProjectile projectile)
        {
            string[] pieces = projectile.SplitStage == 0
                ? Regex.Split(projectile.Text.Trim(), @"\s+")
                : projectile.Text.ToCharArray().Select(character => character.ToString()).ToArray();
            if (pieces.Length == 0)
                return;

            Vector2 center = projectile.Rect.anchoredPosition;
            int count = Mathf.Min(pieces.Length, projectile.SplitStage == 0 ? 12 : 14);
            for (int i = 0; i < count; i++)
            {
                string piece = pieces[i].Trim();
                if (piece.Length == 0)
                    continue;
                float spread = i - (count - 1f) * .5f;
                Vector2 position = center + new Vector2(spread * (projectile.SplitStage == 0 ? 22f : 12f),
                    Mathf.Sin((waveIndex + i) * 2.3f) * 20f);
                Vector2 velocity = Aim(position, projectile.SplitStage == 0 ? 310f : 365f) +
                    new Vector2(spread * 26f, Mathf.Cos((waveIndex + i) * 2.1f) * 48f);
                Spawn(piece, position, velocity,
                    projectile.SplitStage == 0 ? 35f : 41f,
                    projectile.SplitStage == 0 ? Mathf.Clamp(piece.Length * 21f, 65f, 190f) : 48f,
                    projectile.SplitStage == 0 ? 140f : 95f,
                    projectile.SplitStage + 1, projectile.SplitStage == 0 ? .65f : 0f);
            }
            heart.SetAsLastSibling();
        }

        private void MoveProjectiles(float delta)
        {
            Vector2 half = arena.rect.size * .5f;
            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                LetterProjectile projectile = projectiles[i];
                if (projectile.Rect == null)
                {
                    projectiles.RemoveAt(i);
                    continue;
                }

                if (projectile.SplitAfter > 0f && projectile.Age >= projectile.SplitAfter)
                {
                    Split(projectile);
                    Destroy(projectile.Rect.gameObject);
                    projectiles.RemoveAt(i);
                    continue;
                }

                Vector2 toHeart = heart.anchoredPosition - projectile.Rect.anchoredPosition;
                if (toHeart.sqrMagnitude > .001f)
                    projectile.Velocity = Vector2.MoveTowards(projectile.Velocity,
                        toHeart.normalized * projectile.Speed,
                        projectile.HomingStrength * delta);
                projectile.Age += delta;
                projectile.Rect.anchoredPosition += projectile.Velocity * delta;
                Vector2 position = projectile.Rect.anchoredPosition;
                Vector2 distance = heart.anchoredPosition - position;
                if (hitCooldown <= 0f &&
                    Mathf.Abs(distance.x) < projectile.HitHalfSize.x + 12f &&
                    Mathf.Abs(distance.y) < projectile.HitHalfSize.y + 12f)
                {
                    hitCooldown = .7f;
                    AudioService.Instance?.Play(AudioId.Player_Hurt);
                }
                if (projectile.Age > 7f || Mathf.Abs(position.x) > half.x + 400f ||
                    Mathf.Abs(position.y) > half.y + 140f &&
                    Vector2.Dot(position, projectile.Velocity) > 0f)
                {
                    Destroy(projectile.Rect.gameObject);
                    projectiles.RemoveAt(i);
                }
            }
        }

        private void Cleanup(bool restoreCredits)
        {
            if (!ownsPresentation)
                return;

            ownsPresentation = false;
            if (restoreCredits && creditsText != null)
                creditsText.color = authoredTextColor;
            if (gate != null && inputToken.IsValid)
                gate.Release(inputToken);
            inputToken = default;
            gate = null;
            AudioService.Instance?.ReleaseMusicOwner(this);
            if (arena != null)
                Destroy(arena.gameObject);
            arena = null;
            heart = null;
            heartImage = null;
            projectiles.Clear();
        }
    }
}
