using System.Collections.Generic;
using UnityEngine;

namespace Audere.Story.Presentation
{
    /// <summary>Breaks the actual tile sprite triangles into small textured pieces; owns only the environment.</summary>
    public sealed class DreamTileFragments : MonoBehaviour
    {
        [SerializeField] private StoryEvent owner;
        [SerializeField] private SpriteRenderer tile;
        [SerializeField] private SpriteRenderer[] tileLayers;
        private readonly List<Transform> pieces = new List<Transform>();
        private readonly List<Mesh> meshes = new List<Mesh>();
        private readonly List<Vector3> origins = new List<Vector3>();
        private bool[] enabledStates;
        private Material material;
        private float elapsed;
        public int PieceCount => pieces.Count;
        public bool IsBroken { get; private set; }
        public SpriteRenderer Tile => tile;

        private void OnEnable() { if (owner != null) owner.Ended += OnEventEnded; }
        private void OnDisable()
        {
            if (owner != null) owner.Ended -= OnEventEnded;
            Restore();
        }
        private void OnEventEnded(StoryEventResult result) => Restore();

        public bool Break()
        {
            Restore();
            if (tile == null || tile.sprite == null || tileLayers == null) return false;
            enabledStates = new bool[tileLayers.Length];
            for (int i = 0; i < tileLayers.Length; i++)
            {
                if (tileLayers[i] == null) continue;
                enabledStates[i] = tileLayers[i].enabled;
                tileLayers[i].enabled = false;
            }
            material = new Material(tile.sharedMaterial);
            material.mainTexture = tile.sprite.texture;
            Vector2[] vertices = tile.sprite.vertices, uv = tile.sprite.uv;
            ushort[] triangles = tile.sprite.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
                Split(vertices[triangles[i]], vertices[triangles[i + 1]], vertices[triangles[i + 2]],
                    uv[triangles[i]], uv[triangles[i + 1]], uv[triangles[i + 2]], 2);
            elapsed = 0f;
            IsBroken = true;
            return true;
        }

        private void Split(Vector2 a, Vector2 b, Vector2 c, Vector2 ua, Vector2 ub, Vector2 uc, int depth)
        {
            if (depth > 0)
            {
                Vector2 ab = (a + b) * .5f, bc = (b + c) * .5f, ca = (c + a) * .5f;
                Vector2 uab = (ua + ub) * .5f, ubc = (ub + uc) * .5f, uca = (uc + ua) * .5f;
                Split(a, ab, ca, ua, uab, uca, depth - 1);
                Split(ab, b, bc, uab, ub, ubc, depth - 1);
                Split(ca, bc, c, uca, ubc, uc, depth - 1);
                Split(ab, bc, ca, uab, ubc, uca, depth - 1);
                return;
            }
            Vector3 va = World(a), vb = World(b), vc = World(c), center = (va + vb + vc) / 3f;
            var go = new GameObject("Dream tile shard " + pieces.Count, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(transform, true);
            go.transform.position = center;
            var mesh = new Mesh { name = "Dream tile fragment" };
            mesh.vertices = new[] { va - center, vb - center, vc - center };
            mesh.uv = new[] { ua, ub, uc };
            mesh.colors = new[] { tile.color, tile.color, tile.color };
            mesh.triangles = new[] { 0, 1, 2, 2, 1, 0 };
            mesh.RecalculateBounds();
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.sortingLayerID = tile.sortingLayerID;
            renderer.sortingOrder = tile.sortingOrder;
            pieces.Add(go.transform); meshes.Add(mesh); origins.Add(center);
        }

        private Vector3 World(Vector2 point) => tile.transform.TransformPoint(new Vector3(
            point.x * (tile.flipX ? -1 : 1), point.y * (tile.flipY ? -1 : 1), 0f));

        private void LateUpdate()
        {
            if (!IsBroken) return;
            elapsed += Time.unscaledDeltaTime;
            for (int i = 0; i < pieces.Count; i++)
            {
                float spread = Mathf.Sin(i * 2.399963f);
                pieces[i].position = origins[i] + new Vector3(spread * elapsed * .38f,
                    (.18f + (i % 5) * .055f) * elapsed - (1.2f + i % 4 * .18f) * elapsed * elapsed, 0f);
                pieces[i].rotation = Quaternion.Euler(0f, 0f, spread * elapsed * 210f);
            }
        }

        public void Restore()
        {
            IsBroken = false;
            for (int i = 0; i < pieces.Count; i++) if (pieces[i] != null) Destroy(pieces[i].gameObject);
            foreach (var mesh in meshes) if (mesh != null) Destroy(mesh);
            if (material != null) Destroy(material);
            pieces.Clear(); meshes.Clear(); origins.Clear();
            if (enabledStates != null)
                for (int i = 0; i < tileLayers.Length; i++)
                    if (tileLayers[i] != null) tileLayers[i].enabled = enabledStates[i];
            enabledStates = null;
        }
    }
}
