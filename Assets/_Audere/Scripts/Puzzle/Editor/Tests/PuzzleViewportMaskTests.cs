#if UNITY_EDITOR
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Audere.Puzzle.Editor.Tests
{
    public sealed class PuzzleViewportMaskTests
    {
        private const string PrefabPath = "Assets/_Audere/Prefabs/Puzzle/Camera/PuzzleViewportMask.prefab";
        private static readonly string[] EdgeNames = { "Mask Top", "Mask Bottom", "Mask Left", "Mask Right" };
        private Scene previewScene;
        private Camera camera;
        private Transform mask;
        private PuzzleViewportMaskFitter fitter;
        private SpriteRenderer[] originals;
        private SpriteRenderer[] coverage;
        private Vector3[] positions;
        private Vector3[] scales;
        private Quaternion[] rotations;
        private Rect originalOpening;
        private Transform authoredChild;
        private Vector3 childLocalPose;
        private Vector3 childWorldPose;

        [SetUp]
        public void SetUp()
        {
            previewScene = EditorSceneManager.NewPreviewScene();
            var cameraObject = new GameObject("Viewport geometry test camera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraObject, previewScene);
            camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 1.25f;
            camera.aspect = 16f / 9f;
            camera.transform.position = new Vector3(3f, -2f, -10f);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab);
            mask = ((GameObject)PrefabUtility.InstantiatePrefab(prefab, camera.transform)).transform;
            mask.localPosition = new Vector3(0f, 0f, 9f);
            mask.localRotation = Quaternion.identity;
            mask.localScale = Vector3.one * .5814f;
            fitter = mask.GetComponent<PuzzleViewportMaskFitter>();
            Assert.IsNotNull(fitter, "The reusable prefab must own its viewport coverage component.");
            originals = EdgeNames.Select(n => mask.Find(n).GetComponent<SpriteRenderer>()).ToArray();
            coverage = mask.Find("Screen Coverage").GetComponentsInChildren<SpriteRenderer>(true);
            Assert.AreEqual(4, coverage.Length);
            positions = originals.Select(r => r.transform.localPosition).ToArray();
            scales = originals.Select(r => r.transform.localScale).ToArray();
            rotations = originals.Select(r => r.transform.localRotation).ToArray();
            originalOpening = Opening();

            // Scene 40 has authored choice UI under Mask Bottom. Its anchor must survive fitting unchanged.
            authoredChild = new GameObject("Authored choice anchor", typeof(RectTransform)).transform;
            authoredChild.SetParent(originals[1].transform, false);
            authoredChild.localPosition = new Vector3(.23f, -.14f, -.1f);
            authoredChild.localScale = new Vector3(.7f, .9f, 1f);
            childLocalPose = authoredChild.localPosition;
            childWorldPose = authoredChild.position;
        }

        [TestCase(4f / 3f)]
        [TestCase(16f / 9f)]
        [TestCase(21f / 9f)]
        [TestCase(32f / 9f)]
        [TestCase(9f / 16f)]
        public void SharedPrefab_CoversViewportAtDifferentAspectsWithoutChangingOpeningOrAuthoredChildren(float aspect)
        {
            camera.aspect = aspect;
            // A larger orthographic view exposes the top/bottom failure as well as the wide-aspect side failure.
            foreach (float zoom in new[] { 1.25f, 3f })
            {
                camera.orthographicSize = zoom;
                Assert.IsTrue(fitter.FitToCamera());
                AssertCoveredOutsideOpening();
                AssertOriginalsUnchanged();
                Assert.Less(Vector3.Distance(childWorldPose, authoredChild.position), .00001f,
                    "Fitting must not move existing child UI through a changed mask-edge transform.");
            }
        }

        [Test]
        public void Refit_AfterCameraMoveResizeAndMaskScaleChangeUsesItsOwnCamera()
        {
            Assert.IsTrue(fitter.FitToCamera());
            var unrelatedObject = new GameObject("Unrelated Main Camera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(unrelatedObject, previewScene);
            unrelatedObject.tag = "MainCamera";
            unrelatedObject.GetComponent<Camera>().orthographicSize = 20f;
            unrelatedObject.GetComponent<Camera>().aspect = .5f;

            camera.transform.position += new Vector3(7f, 4f, -3f);
            camera.aspect = 32f / 9f;
            camera.orthographicSize = 2.1f;
            mask.localPosition = new Vector3(.2f, -.1f, 9f);
            mask.localScale = new Vector3(.43f, .72f, .5814f);
            Assert.IsTrue(fitter.FitToCamera());
            AssertCoveredOutsideOpening();
            AssertOriginalsUnchanged();

            camera.aspect = 4f / 3f;
            camera.orthographicSize = 1.25f;
            Assert.IsTrue(fitter.FitToCamera());
            AssertCoveredOutsideOpening();
            AssertOriginalsUnchanged();
        }

        private void AssertOriginalsUnchanged()
        {
            for (int i = 0; i < originals.Length; i++)
            {
                Assert.AreEqual(positions[i], originals[i].transform.localPosition, EdgeNames[i]);
                Assert.AreEqual(scales[i], originals[i].transform.localScale, EdgeNames[i]);
                Assert.AreEqual(rotations[i], originals[i].transform.localRotation, EdgeNames[i]);
            }
            var opening = Opening();
            Assert.AreEqual(originalOpening.xMin, opening.xMin, .00001f);
            Assert.AreEqual(originalOpening.xMax, opening.xMax, .00001f);
            Assert.AreEqual(originalOpening.yMin, opening.yMin, .00001f);
            Assert.AreEqual(originalOpening.yMax, opening.yMax, .00001f);
            Assert.AreSame(originals[1].transform, authoredChild.parent);
            Assert.AreEqual(childLocalPose, authoredChild.localPosition);
            Assert.AreEqual(new Vector3(.7f, .9f, 1f), authoredChild.localScale);
        }

        private void AssertCoveredOutsideOpening()
        {
            Rect opening = Opening();
            var renderers = originals.Concat(coverage).Where(r => r.enabled && r.gameObject.activeInHierarchy).ToArray();
            var covered = renderers.Select(LocalRect).ToArray();
            foreach (var renderer in coverage.Where(r => r.enabled && r.gameObject.activeInHierarchy))
            {
                Rect rect = LocalRect(renderer);
                float overlapX = Mathf.Min(rect.xMax, opening.xMax) - Mathf.Max(rect.xMin, opening.xMin);
                float overlapY = Mathf.Min(rect.yMax, opening.yMax) - Mathf.Max(rect.yMin, opening.yMin);
                Assert.IsFalse(overlapX > .0001f && overlapY > .0001f,
                    renderer.name + " must not cover the authored central opening.");
            }

            float depth = Vector3.Dot(mask.position - camera.transform.position, camera.transform.forward);
            int exteriorSamples = 0;
            // Include every outer edge and all four corners, not only an interior grid.
            for (int y = 0; y <= 20; y++)
            for (int x = 0; x <= 40; x++)
            {
                Vector3 world = camera.ViewportToWorldPoint(new Vector3(x / 40f, y / 20f, depth));
                Vector2 point = mask.InverseTransformPoint(world);
                if (point.x > opening.xMin + .0001f && point.x < opening.xMax - .0001f &&
                    point.y > opening.yMin + .0001f && point.y < opening.yMax - .0001f) continue;
                exteriorSamples++;
                Assert.IsTrue(covered.Any(r => ContainsWithTolerance(r, point)),
                    "Visible leak outside opening at viewport " + x + "/40, " + y + "/20; local " + point);
            }
            Assert.Greater(exteriorSamples, 0);
        }

        private Rect Opening()
        {
            return Rect.MinMaxRect(LocalRect(originals[2]).xMax, LocalRect(originals[1]).yMax,
                LocalRect(originals[3]).xMin, LocalRect(originals[0]).yMin);
        }

        private Rect LocalRect(SpriteRenderer renderer)
        {
            Bounds bounds = renderer.localBounds;
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            for (int y = 0; y < 2; y++)
            for (int x = 0; x < 2; x++)
            {
                Vector3 corner = new Vector3(x == 0 ? bounds.min.x : bounds.max.x,
                    y == 0 ? bounds.min.y : bounds.max.y, bounds.center.z);
                Vector2 local = mask.InverseTransformPoint(renderer.transform.TransformPoint(corner));
                min = Vector2.Min(min, local);
                max = Vector2.Max(max, local);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static bool ContainsWithTolerance(Rect rect, Vector2 point) =>
            point.x >= rect.xMin - .0001f && point.x <= rect.xMax + .0001f &&
            point.y >= rect.yMin - .0001f && point.y <= rect.yMax + .0001f;

        [TearDown]
        public void TearDown()
        {
            if (previewScene.IsValid()) EditorSceneManager.ClosePreviewScene(previewScene);
        }
    }
}
#endif
