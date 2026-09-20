using Audere.Puzzle;
using TMPro;
using UnityEngine;

namespace Audere.Story.Presentation
{
    /// <summary>Scene-authored dream scenery only. Never creates tiles or moves the player.</summary>
    public sealed class DreamAtmosphereView : MonoBehaviour
    {
        [SerializeField] private StoryEvent owner;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private GridPlayer player;
        [SerializeField] private Transform cameraStart;
        [SerializeField] private Transform playerStart;
        [SerializeField] private SpriteRenderer[] floatingTiles;
        [SerializeField] private SpriteRenderer[] pathRenderers;
        [SerializeField] private TMP_Text[] murmurs;
        [SerializeField, Min(.1f)] private float textRepeatWidth = 5f;
        [SerializeField, Range(0f, 2f)] private float parallax = .85f;
        [SerializeField, Min(0f)] private float floatHeight = .055f;
        [SerializeField, Min(0f)] private float textDrift = .04f;
        [Tooltip("Ascending pressure threshold for each scene-authored murmur. Empty preserves legacy presentation.")]
        [SerializeField] private float[] murmurThresholds = new float[0];

        [SerializeField, Min(0f)] private float horizontalDrift;
        [SerializeField, Min(0f)] private float floatRotation;
        private Quaternion[] tileRotations;
        private bool[] independentFloat;
        private Vector3[] tilePositions, textPositions;
        private Quaternion[] textRotations;
        private Color[] tileColors, pathColors, textColors;
        private TMP_MeshInfo[][] textMeshes;
        private float elapsed;
        private bool captured;
        public bool IsRunning { get; private set; }
        public float Chaos { get; private set; }
        public float Pressure { get; private set; }
        private float fallDrop, enclosure;

        public void SetPressure(float value) => Pressure = Mathf.Clamp01(value);
        public void SetFall(float drop, float progress)
        {
            fallDrop = drop;
            enclosure = Mathf.Clamp01(progress);
        }

        private void Capture()
        {
            if (captured) return;
            tilePositions = new Vector3[floatingTiles.Length];
            tileColors = new Color[floatingTiles.Length];
            tileRotations = new Quaternion[floatingTiles.Length];
            independentFloat = new bool[floatingTiles.Length];
            pathColors = new Color[pathRenderers.Length];
            textPositions = new Vector3[murmurs.Length];
            textRotations = new Quaternion[murmurs.Length];
            textColors = new Color[murmurs.Length];
            textMeshes = new TMP_MeshInfo[murmurs.Length][];
            for (int i = 0; i < floatingTiles.Length; i++)
            {
                tilePositions[i] = floatingTiles[i].transform.localPosition;
                tileColors[i] = floatingTiles[i].color;
                tileRotations[i] = floatingTiles[i].transform.localRotation;
                independentFloat[i] = true;
                // Child fringes move with their parent, without a second bob.
                for (int j = 0; j < floatingTiles.Length; j++)
                    if (floatingTiles[i].transform.parent == floatingTiles[j].transform) independentFloat[i] = false;
            }
            for (int i = 0; i < pathRenderers.Length; i++) pathColors[i] = pathRenderers[i].color;
            for (int i = 0; i < murmurs.Length; i++)
            {
                var text = murmurs[i];
                textPositions[i] = text.transform.position;
                textRotations[i] = text.transform.rotation;
                textColors[i] = text.color;
                // Hidden TMP objects may not have initialized their vertex arrays yet.
                // Capture authored presentation now; cache glyph geometry only after activation.
            }
            captured = true;
        }

        public void Begin()
        {
            ResetPresentation();
            IsRunning = true;
        }

        public void ResetPresentation()
        {
            // Prepare while the dream roots are still hidden, before the glass reveals them.
            // A plain StopAndRestore on first load had nothing captured and left all 64 texts visible.
            Capture();
            StopAndRestore();
        }

        public void SetChaos(float value)
        {
            Chaos = Mathf.Clamp01(value);
            // Explicit environmental collapse. The player's shadow and body are not in these arrays.
            for (int i = 0; i < pathRenderers.Length; i++)
                if (pathRenderers[i] != null) pathRenderers[i].color = Fade(pathColors[i], 1f - Chaos);
            for (int i = 0; i < floatingTiles.Length; i++)
                if (floatingTiles[i] != null) floatingTiles[i].color = Fade(tileColors[i], 1f - Chaos);
        }

        private void LateUpdate()
        {
            if (!IsRunning) return;
            if (owner == null || !owner.IsPlaying || player == null || worldCamera == null)
            {
                StopAndRestore();
                return;
            }
            elapsed += Time.unscaledDeltaTime;
            float travel = player.transform.position.x - playerStart.position.x;
            // X only: a hop never shakes the camera, and the authored Y/framing remains unchanged.
            Vector3 cameraPose = cameraStart.position + Vector3.right * travel + Vector3.up * fallDrop;
            worldCamera.transform.position = cameraPose;
            for (int i = 0; i < floatingTiles.Length; i++)
            {
                var tile = floatingTiles[i];
                if (tile != null && independentFloat[i])
                {
                    float phase = i * 1.7f;
                    tile.transform.localPosition = tilePositions[i] + new Vector3(
                        (Mathf.Sin(elapsed * .55f + phase) - Mathf.Sin(phase)) * horizontalDrift,
                        (Mathf.Sin(elapsed * .85f + phase) - Mathf.Sin(phase)) * floatHeight, 0f);
                    tile.transform.localRotation = tileRotations[i] * Quaternion.Euler(0f, 0f,
                        (Mathf.Sin(elapsed * .6f + phase) - Mathf.Sin(phase)) * floatRotation);
                }
            }
            for (int i = 0; i < murmurs.Length; i++)
            {
                TMP_Text text = murmurs[i];
                if (text == null) continue;
                Vector3 pose = textPositions[i];
                float offset = pose.x - cameraStart.position.x - travel * parallax - elapsed * textDrift;
                pose.x = cameraPose.x + Mathf.Repeat(offset + textRepeatWidth * .5f, textRepeatWidth) - textRepeatWidth * .5f;
                float intensity = Mathf.Max(Chaos, Pressure);
                pose.y += fallDrop + Mathf.Sin(elapsed * (1f + intensity * 2f) + i) * (.025f + intensity * .12f);
                if (enclosure > 0f)
                {
                    float angle = i * 2.399963f + elapsed * (i % 2 == 0 ? .24f : -.19f);
                    float radius = .55f + (i % 6) * .17f;
                    Vector3 ring = player.transform.position + new Vector3(Mathf.Cos(angle) * radius * 1.65f,
                        Mathf.Sin(angle) * radius, pose.z - player.transform.position.z);
                    pose = Vector3.Lerp(pose, ring, enclosure);
                }
                text.transform.position = pose;
                text.transform.rotation = textRotations[i] * Quaternion.Euler(0f, 0f,
                    Mathf.Sin(elapsed * (1f + intensity * 3f) + i * 2f) * (2f + intensity * 22f));
                float visibility = murmurThresholds.Length == murmurs.Length
                    ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((Pressure - murmurThresholds[i]) / .12f)) : 1f;
                text.color = Fade(textColors[i], visibility * (1f + intensity * 1.6f));
                // TMP rebuilds dirty colors at PreRender. Generate now so it cannot overwrite the warped vertices later.
                if (text.havePropertiesChanged) text.ForceMeshUpdate();
                WarpText(i);
            }
        }

        private void WarpText(int index)
        {
            TMP_Text text = murmurs[index];
            if (!text.isActiveAndEnabled) return;
            if (textMeshes[index] == null)
            {
                text.ForceMeshUpdate();
                var info = text.textInfo.meshInfo;
                if (info == null || info.Length == 0) return;
                for (int m = 0; m < info.Length; m++) if (info[m].vertices == null) return;
                textMeshes[index] = text.textInfo.CopyMeshInfoVertexData();
            }
            var source = textMeshes[index];
            var mesh = text.textInfo.meshInfo;
            float intensity = Mathf.Max(Chaos, Pressure);
            for (int m = 0; m < mesh.Length && m < source.Length; m++)
            {
                var vertices = mesh[m].vertices;
                var original = source[m].vertices;
                int count = Mathf.Min(mesh[m].vertexCount, original.Length);
                for (int v = 0; v < count; v++)
                {
                    Vector3 p = original[v];
                    p.y += Mathf.Sin(p.x * 5f + elapsed * (1.3f + intensity * 4f) + index) * (.005f + intensity * .065f);
                    p.x += Mathf.Sin(p.y * 6f + elapsed * 1.5f + index) * (.003f + intensity * .035f);
                    vertices[v] = p;
                }
            }
            text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
        }

        public void StopAndRestore()
        {
            IsRunning = false;
            elapsed = 0f;
            Chaos = 0f;
            Pressure = 0f;
            fallDrop = enclosure = 0f;
            if (!captured) return;
            for (int i = 0; i < floatingTiles.Length; i++)
                if (floatingTiles[i] != null)
                {
                    floatingTiles[i].transform.localPosition = tilePositions[i];
                    floatingTiles[i].transform.localRotation = tileRotations[i];
                    floatingTiles[i].color = tileColors[i];
                }
            for (int i = 0; i < pathRenderers.Length; i++)
                if (pathRenderers[i] != null) pathRenderers[i].color = pathColors[i];
            for (int i = 0; i < murmurs.Length; i++)
                if (murmurs[i] != null)
                {
                    murmurs[i].transform.SetPositionAndRotation(textPositions[i], textRotations[i]);
                    murmurs[i].color = Fade(textColors[i], murmurThresholds.Length == murmurs.Length && murmurThresholds[i] > 0f ? 0f : 1f);
                    if (murmurs[i].isActiveAndEnabled) murmurs[i].ForceMeshUpdate();
                }
            if (worldCamera != null && cameraStart != null) worldCamera.transform.position = cameraStart.position;
        }

        private static Color Fade(Color color, float multiplier) => new Color(color.r, color.g, color.b, Mathf.Clamp01(color.a * multiplier));
        private void OnDisable() => StopAndRestore();
    }
}
