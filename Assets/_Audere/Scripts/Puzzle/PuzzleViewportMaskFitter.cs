using UnityEngine;
using UnityEngine.Rendering;

namespace Audere.Puzzle
{
    /// <summary>Extends the outside cover to the camera without moving the authored aperture or its child UI.</summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public sealed class PuzzleViewportMaskFitter : MonoBehaviour
    {
        [Tooltip("Top, Bottom, Left, Right. These authored transforms remain unchanged.")]
        [SerializeField] private SpriteRenderer[] originalEdges;
        [Tooltip("Top, Bottom, Left, Right. Shared prefab children used only for screen coverage.")]
        [SerializeField] private SpriteRenderer[] coverageEdges;
        private Camera ownerCamera;

        private void OnEnable()
        {
            ownerCamera = GetComponentInParent<Camera>();
            RenderPipelineManager.beginCameraRendering += BeforeCameraRendering;
            Camera.onPreCull += BeforeCameraCull;
            FitToCamera();
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeforeCameraRendering;
            Camera.onPreCull -= BeforeCameraCull;
        }

        private void LateUpdate() => FitToCamera();
        private void BeforeCameraRendering(ScriptableRenderContext context, Camera camera) => BeforeCameraCull(camera);
        private void BeforeCameraCull(Camera camera)
        {
            if (camera == ownerCamera) FitToCamera();
        }

        public bool FitToCamera()
        {
            if (ownerCamera == null) ownerCamera = GetComponentInParent<Camera>();
            if (ownerCamera == null || originalEdges == null || coverageEdges == null ||
                originalEdges.Length != 4 || coverageEdges.Length != 4) return false;
            for (int i = 0; i < 4; i++)
                if (originalEdges[i] == null || coverageEdges[i] == null || originalEdges[i].sprite == null) return false;

            float depth = Vector3.Dot(transform.position - ownerCamera.transform.position, ownerCamera.transform.forward);
            if (depth <= 0f) return false;
            Vector3 lower = transform.InverseTransformPoint(ownerCamera.ViewportToWorldPoint(new Vector3(0f, 0f, depth)));
            Vector3 upper = transform.InverseTransformPoint(ownerCamera.ViewportToWorldPoint(new Vector3(1f, 1f, depth)));
            float bleed = Mathf.Abs(upper.y - lower.y) * 2f / Mathf.Max(1, ownerCamera.pixelHeight);
            float xMin = lower.x - bleed, xMax = upper.x + bleed;
            float yMin = lower.y - bleed, yMax = upper.y + bleed;
            float left = Mathf.Clamp(LocalBounds(originalEdges[2]).xMax, xMin, xMax);
            float right = Mathf.Clamp(LocalBounds(originalEdges[3]).xMin, left, xMax);
            float bottom = Mathf.Clamp(LocalBounds(originalEdges[1]).yMax, yMin, yMax);
            float top = Mathf.Clamp(LocalBounds(originalEdges[0]).yMin, bottom, yMax);

            FitEdge(0, Rect.MinMaxRect(xMin, top, xMax, yMax));
            FitEdge(1, Rect.MinMaxRect(xMin, yMin, xMax, bottom));
            FitEdge(2, Rect.MinMaxRect(xMin, bottom, left, top));
            FitEdge(3, Rect.MinMaxRect(right, bottom, xMax, top));
            return true;
        }

        private Rect LocalBounds(SpriteRenderer renderer)
        {
            Bounds bounds = renderer.localBounds;
            Matrix4x4 matrix = transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
            Vector3 a = matrix.MultiplyPoint3x4(new Vector3(bounds.min.x, bounds.min.y, 0f));
            Vector3 b = matrix.MultiplyPoint3x4(new Vector3(bounds.max.x, bounds.max.y, 0f));
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        private void FitEdge(int index, Rect rect)
        {
            SpriteRenderer source = originalEdges[index], cover = coverageEdges[index];
            cover.enabled = source.enabled && source.gameObject.activeInHierarchy && rect.width > 0f && rect.height > 0f;
            if (!cover.enabled) return;
            cover.sprite = source.sprite;
            cover.sharedMaterial = source.sharedMaterial;
            cover.color = source.color;
            cover.sortingLayerID = source.sortingLayerID;
            cover.sortingOrder = source.sortingOrder;
            Bounds sprite = source.sprite.bounds;
            Vector3 scale = new Vector3(rect.width / sprite.size.x, rect.height / sprite.size.y, 1f);
            Vector3 center = new Vector3(rect.center.x, rect.center.y, 0f) - Vector3.Scale(sprite.center, scale);
            // Coverage's parent is an identity transform in this prefab; authored edges never resize.
            cover.transform.localRotation = Quaternion.identity;
            cover.transform.localPosition = center;
            cover.transform.localScale = scale;
        }
    }
}
