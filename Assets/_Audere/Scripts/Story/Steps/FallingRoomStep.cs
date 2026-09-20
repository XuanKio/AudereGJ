using System.Collections;
using System.Collections.Generic;
using Audere.Story.Presentation;
using Audere.World;
using UnityEngine;

namespace Audere.Story.Steps
{
    // Owns the room illusion while the visible child event stages the actor, dialogue and hand-off.
    public sealed class FallingRoomStep : StoryStep
    {
        [SerializeField] private FallingRoomProfile profile;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private WorldModeController worldMode;
        [SerializeField] private SpriteRenderer[] tiles;
        [SerializeField] private Transform[] furniture;
        [SerializeField] private SpriteRenderer[] masks;
        [SerializeField] private Transform actor;
        [SerializeField] private SpriteRenderer groundedShadow;
        [SerializeField] private StoryEvent sequence;

        private readonly List<Piece> pieces = new List<Piece>();
        private readonly List<Sprite> generatedSprites = new List<Sprite>();
        private SpriteRenderer[] streaks;
        private Vector3[] furniturePositions;
        private Quaternion[] furnitureRotations;
        private SpriteRenderer[] furnitureRenderers;
        private int[] furnitureOrders;
        private bool[] tileEnabled;
        private Color[] maskColors;
        private Color cameraColor;
        private Vector3 actorPosition, shadowPosition, shadowScale;
        private Quaternion actorRotation, shadowRotation;
        private bool shadowEnabled, captured, ownsSequence;
        private WorldGameplayMode sourceMode;
        private GameObject visuals;

        private sealed class Piece
        {
            public SpriteRenderer Renderer;
            public Vector3 Start;
            public Quaternion Rotation;
            public float Delay, Side, Spin;
            public Color Color;
        }

        public FallingRoomProfile Profile => profile;
        public StoryEvent Sequence => sequence;

        protected override IEnumerator Execute()
        {
            if (profile == null || worldCamera == null || worldMode == null || actor == null ||
                groundedShadow == null || sequence == null || sequence.transform.parent != transform ||
                sequence.IsPlaying || sequence.AutoPlayNextEvent || tiles == null || furniture == null || masks == null)
            {
                Debug.LogError("[FallingRoomStep] Bind the room, profile and idle child sequence.", this);
                FailStep();
                yield break;
            }

            bool done = false;
            StoryEventResult result = StoryEventResult.Failed;
            try
            {
                BeginPresentation();
                ownsSequence = true;
                if (!sequence.Play(value => { result = value; done = true; })) done = true;
                float elapsed = 0f;
                while (!done)
                {
                    SamplePresentation(elapsed);
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
            }
            finally
            {
                StopPresentation();
            }
            if (result == StoryEventResult.Completed) CompleteStep();
            else if (result == StoryEventResult.Cancelled) Cancel();
            else FailStep();
        }

        private void BeginPresentation()
        {
            foreach (var tile in tiles) if (tile == null) throw new MissingReferenceException("Falling room tile is missing.");
            foreach (var item in furniture) if (item == null) throw new MissingReferenceException("Falling room furniture is missing.");
            foreach (var mask in masks) if (mask == null) throw new MissingReferenceException("Falling room mask is missing.");
            sourceMode = worldMode.CurrentMode;
            cameraColor = worldCamera.backgroundColor;
            actorPosition = actor.position; actorRotation = actor.rotation;
            shadowPosition = groundedShadow.transform.position;
            shadowRotation = groundedShadow.transform.rotation;
            shadowScale = groundedShadow.transform.localScale;
            shadowEnabled = groundedShadow.enabled;
            tileEnabled = new bool[tiles.Length];
            furniturePositions = new Vector3[furniture.Length];
            furnitureRotations = new Quaternion[furniture.Length];
            furnitureRenderers = new SpriteRenderer[furniture.Length];
            furnitureOrders = new int[furniture.Length];
            maskColors = new Color[masks.Length];
            for (int i = 0; i < tiles.Length; i++) tileEnabled[i] = tiles[i].enabled;
            for (int i = 0; i < furniture.Length; i++)
            {
                furniturePositions[i] = furniture[i].position;
                furnitureRotations[i] = furniture[i].rotation;
                furnitureRenderers[i] = furniture[i].GetComponent<SpriteRenderer>();
                if (furnitureRenderers[i] != null) furnitureOrders[i] = furnitureRenderers[i].sortingOrder;
            }
            for (int i = 0; i < masks.Length; i++) maskColors[i] = masks[i].color;
            captured = true;
            worldCamera.backgroundColor = profile.VoidColor;
            foreach (var mask in masks) mask.color = profile.VoidColor;
            groundedShadow.enabled = false;
            foreach (var renderer in furnitureRenderers) if (renderer != null) renderer.sortingOrder = Mathf.Min(renderer.sortingOrder, 3);
            visuals = new GameObject("Falling room runtime visuals");
            visuals.transform.SetParent(transform, false);
            visuals.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            visuals.transform.localScale = Vector3.one;
            for (int i = 0; i < tiles.Length; i++)
            {
                var tile = tiles[i];
                if (!tileEnabled[i] || tile.sprite == null) continue;
                Sprite source = tile.sprite;
                Rect rect = source.rect;
                for (int y = 0; y < 2; y++) for (int x = 0; x < 2; x++)
                {
                    var part = new Rect(rect.x + x * rect.width / 2, rect.y + y * rect.height / 2, rect.width / 2, rect.height / 2);
                    var sprite = Sprite.Create(source.texture, part, Vector2.one * .5f, source.pixelsPerUnit, 0, SpriteMeshType.FullRect);
                    generatedSprites.Add(sprite);
                    var shard = NewRenderer("Tile fragment", sprite, tile.sortingLayerID, tile.sortingOrder);
                    shard.sharedMaterial = tile.sharedMaterial;
                    Vector3 center = new Vector3(((x + .5f) * rect.width / 2 - source.pivot.x) / source.pixelsPerUnit,
                        ((y + .5f) * rect.height / 2 - source.pivot.y) / source.pixelsPerUnit, 0);
                    shard.transform.position = tile.transform.TransformPoint(center);
                    shard.transform.rotation = tile.transform.rotation;
                    shard.transform.localScale = tile.transform.lossyScale;
                    shard.color = tile.color;
                    pieces.Add(new Piece { Renderer = shard, Start = shard.transform.position, Rotation = shard.transform.rotation,
                        Delay = Vector2.Distance(tile.transform.position, actorPosition) * .65f,
                        Side = (x == 0 ? -1 : 1) * (.045f + i * .003f), Spin = (x == 0 ? -1 : 1) * (25 + y * 17), Color = tile.color });
                }
                tile.enabled = false;
            }
            var pixel = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * .5f, 1);
            generatedSprites.Add(pixel);
            streaks = new SpriteRenderer[profile.StreakCount];
            for (int i = 0; i < streaks.Length; i++) streaks[i] = NewRenderer("Rising pixel streak", pixel, SortingLayer.NameToID("Player"), 0);
            SamplePresentation(0);
        }

        private SpriteRenderer NewRenderer(string label, Sprite sprite, int layer, int order)
        {
            var go = new GameObject(label);
            go.transform.SetParent(visuals.transform, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite; renderer.sortingLayerID = layer; renderer.sortingOrder = order;
            return renderer;
        }

        private void SamplePresentation(float time)
        {
            if (!captured) return;
            visuals.SetActive(worldMode.CurrentMode == WorldGameplayMode.Story);
            float rise = profile.Rise(Mathf.Max(0, time - .35f));
            foreach (var piece in pieces)
            {
                float t = Mathf.Max(0, time - piece.Delay);
                float shake = t < .18f ? Mathf.Sin(t * 90) * .006f : 0;
                piece.Renderer.transform.position = piece.Start + new Vector3(piece.Side * t + shake, rise - .17f * t, 0);
                piece.Renderer.transform.rotation = piece.Rotation * Quaternion.Euler(0, 0, piece.Spin * t);
                Color c = piece.Color; c.a *= 1 - Mathf.SmoothStep(0, 1, t / profile.FractureDuration); piece.Renderer.color = c;
            }
            for (int i = 0; i < furniture.Length; i++)
            {
                float depth = .75f + i % 3 * .22f;
                furniture[i].position = furniturePositions[i] + new Vector3(Mathf.Sin(time * .8f + i) * .015f * Mathf.Clamp01(time), rise * depth, 0);
                furniture[i].rotation = furnitureRotations[i] * Quaternion.Euler(0, 0, Mathf.Sin(time * .55f + i) * Mathf.Min(time * 5, 18));
            }
            float height = worldCamera.orthographicSize * 2;
            float width = height * worldCamera.aspect;
            float fade = Mathf.SmoothStep(0, 1, time / profile.FractureDuration);
            for (int i = 0; i < streaks.Length; i++)
            {
                float x = Mathf.Repeat(i * .618034f, 1);
                float depth = .5f + Mathf.Repeat(i * .317f, 1);
                float y = Mathf.Repeat(i * .381966f + rise * depth / height, 1);
                var r = streaks[i];
                r.transform.position = worldCamera.transform.position + new Vector3((x - .5f) * width, (y - .5f) * height, 5);
                r.transform.localScale = new Vector3(.003f + i % 3 * .0015f, (.025f + i % 5 * .025f) * (1 + fade), 1);
                Color c = profile.StreakColor; c.a *= fade * (.35f + depth * .45f); r.color = c;
            }
        }

        protected override void OnCancelled()
        {
            bool restoreMode = captured;
            StopPresentation();
            if (restoreMode && worldMode != null) worldMode.ApplyModeImmediate(sourceMode);
        }

        private void StopPresentation()
        {
            if (ownsSequence)
            {
                ownsSequence = false;
                if (sequence != null && sequence.IsPlaying) sequence.Cancel();
            }
            if (!captured) return;
            captured = false;
            if (worldCamera != null) worldCamera.backgroundColor = cameraColor;
            for (int i = 0; i < masks.Length; i++) if (masks[i] != null) masks[i].color = maskColors[i];
            for (int i = 0; i < tiles.Length; i++) if (tiles[i] != null) tiles[i].enabled = tileEnabled[i];
            for (int i = 0; i < furniture.Length; i++) if (furniture[i] != null) furniture[i].SetPositionAndRotation(furniturePositions[i], furnitureRotations[i]);
            for (int i = 0; i < furnitureRenderers.Length; i++) if (furnitureRenderers[i] != null) furnitureRenderers[i].sortingOrder = furnitureOrders[i];
            if (actor != null) actor.SetPositionAndRotation(actorPosition, actorRotation);
            if (groundedShadow != null)
            {
                groundedShadow.transform.SetPositionAndRotation(shadowPosition, shadowRotation);
                groundedShadow.transform.localScale = shadowScale;
                groundedShadow.enabled = shadowEnabled;
            }
            if (visuals != null) { visuals.SetActive(false); Release(visuals); }
            foreach (var sprite in generatedSprites) Release(sprite);
            visuals = null; streaks = null; pieces.Clear(); generatedSprites.Clear();
        }

        private static void Release(Object value)
        {
            if (Application.isPlaying) Destroy(value);
            else DestroyImmediate(value);
        }
    }
}
